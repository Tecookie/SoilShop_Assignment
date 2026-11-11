using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using SoilShop.Domain;
using SoilShop.Infrastructure;
using SoilShop.Application;
using Xunit;

namespace SoilShop.Tests;

public sealed class ShopFactory : WebApplicationFactory<Program>
{
    private readonly SemaphoreSlim loginLock=new(1,1);
    private LoginResult? cachedLogin;
    public async Task<HttpClient> AdminClientAsync()
    {
        await loginLock.WaitAsync();
        try
        {
            if(cachedLogin is null || cachedLogin.ExpiresAt<=DateTime.UtcNow.AddMinutes(1))
            {
                using var loginClient=CreateClient();
                var response=await loginClient.PostAsJsonAsync("/api/auth/login",new {username="test-admin",password="Isolated-test-password-738!"});
                response.EnsureSuccessStatusCode();cachedLogin=(await response.Content.ReadFromJsonAsync<LoginResult>())!;
            }
            var client=CreateClient();client.DefaultRequestHeaders.Authorization=new AuthenticationHeaderValue("Bearer",cachedLogin.Token);return client;
        }
        finally { loginLock.Release(); }
    }
    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        var connection = Environment.GetEnvironmentVariable("SHOP_TEST_CONNECTION")
            ?? throw new InvalidOperationException("Run scripts/test.ps1 to supply an isolated PostgreSQL database.");
        builder.UseEnvironment("Testing");
        builder.ConfigureAppConfiguration((_, config) => config.AddInMemoryCollection(new Dictionary<string, string?>
        {
            ["ConnectionStrings:Shop"] = connection,
            ["Jwt:Key"] = "ISOLATED_TEST_SIGNING_KEY_01234567890123456789",
            ["Bootstrap:Username"] = "test-admin",
            ["Bootstrap:Password"] = "Isolated-test-password-738!",
            ["Messaging:Enabled"] = "false"
        }));
        builder.ConfigureServices(services =>
        {
            services.RemoveAll<ICalculationClient>();
            services.AddSingleton<ICalculationClient, TestCalculation>();
            services.RemoveAll<IProductAdviceClient>();
            services.AddSingleton<IProductAdviceClient, TestAdvice>();
        });
    }
    private sealed class TestCalculation : ICalculationClient
    {
        public Task<decimal> TotalAsync(IReadOnlyList<CalculationLine> lines, CancellationToken ct) =>
            Task.FromResult(lines.Sum(x => x.Quantity * x.Price));
    }
    private sealed class TestAdvice : IProductAdviceClient
    {
        public Task<ProductAdviceRanking> RankAsync(ProductAdviceInput input, IReadOnlyList<ProductAdviceCandidate> candidates, CancellationToken ct) =>
            Task.FromResult(AdviceProtoMapper.Response(SoilShop.CalculationHost.AdviceRules.Rank(AdviceProtoMapper.Request(input, candidates))));
    }
}
