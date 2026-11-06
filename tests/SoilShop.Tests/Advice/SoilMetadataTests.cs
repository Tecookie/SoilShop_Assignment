using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using SoilShop.Application;
using Xunit;

namespace SoilShop.Tests;

public sealed class SoilMetadataTests(ShopFactory factory) : IClassFixture<ShopFactory>
{
    [Fact]
    public async Task Metadata_validates_ranges_and_review_requires_source_and_is_invalidated_by_edit()
    {
        using var c = factory.CreateClient();
        var login = await c.PostAsJsonAsync("/api/auth/login", new { username = "test-admin", password = "Isolated-test-password-738!" });
        var session = (await login.Content.ReadFromJsonAsync<LoginResult>())!;
        c.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", session.Token);
        var create = await c.PostAsJsonAsync("/api/products", new { code = Guid.NewGuid().ToString("N"), name = "Soil metadata test" });
        var product = (await create.Content.ReadFromJsonAsync<ProductDto>())!;
        var path = $"/api/products/{product.Id}/metadata";
        var initial = await c.GetFromJsonAsync<SoilMetadataDto>(path);
        Assert.Null(initial!.Aeration); Assert.Null(initial.EcMin); Assert.Null(initial.SampleAvailable);
        Assert.Equal(HttpStatusCode.BadRequest, (await c.PostAsJsonAsync(path + "/review", new { expectedVersion = initial.Version })).StatusCode);
        Assert.Equal(HttpStatusCode.BadRequest, (await c.PutAsJsonAsync(path, new SoilMetadataInput(initial.Version, PhMin: 7, PhMax: 5))).StatusCode);
        Assert.Equal(HttpStatusCode.BadRequest, (await c.PutAsJsonAsync(path, new SoilMetadataInput(initial.Version, EcMin: 0.1m, EcMax: 0.4m))).StatusCode);
        var input = new SoilMetadataInput(initial.Version, Drainage: "High", Aeration: "High", WaterRetention: "Medium", PhMin: 5.5m, PhMax: 6.5m,
            EcMin: 0.1m, EcMax: 0.4m, EcMethod: "SME", UseContext: "Pot", SampleAvailable: false,
            StructureNotes: "Supplied structure", NutritionNotes: "Supplied nutrient label", OrganicMatterNotes: "Supplied organic notes",
            BiologyNotes: "Unknown organisms", CleanlinessNotes: "Supplier claim only", Ingredients: "Supplied ingredients",
            SourceReference: "Isolated synthetic test label; no agronomic claim", PlantGroups: ["Foliage", "Bonsai"]);
        var savedResponse = await c.PutAsJsonAsync(path, input); savedResponse.EnsureSuccessStatusCode();
        var saved = (await savedResponse.Content.ReadFromJsonAsync<SoilMetadataDto>())!;
        Assert.False(saved.SampleAvailable); Assert.Equal(2, saved.PlantGroups.Count);
        var review = await c.PostAsJsonAsync(path + "/review", new { expectedVersion = saved.Version }); review.EnsureSuccessStatusCode();
        var reviewed = (await review.Content.ReadFromJsonAsync<SoilMetadataDto>())!;
        Assert.NotNull(reviewed.ReviewedAt); Assert.Equal(session.User.Id, reviewed.ReviewedBy);
        Assert.Equal(HttpStatusCode.Conflict, (await c.PutAsJsonAsync(path, input)).StatusCode);
        var changedResponse = await c.PutAsJsonAsync(path, input with { ExpectedVersion = reviewed.Version, Aeration = "Medium" }); changedResponse.EnsureSuccessStatusCode();
        var changed = (await changedResponse.Content.ReadFromJsonAsync<SoilMetadataDto>())!;
        Assert.Null(changed.ReviewedAt); Assert.Null(changed.ReviewedBy);
    }
}
