using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;
using Microsoft.Extensions.Configuration;
using SoilShop.Application;
using SoilShop.Domain;

namespace SoilShop.Infrastructure;

public sealed class ShopDbContextFactory : IDesignTimeDbContextFactory<ShopDbContext>
{
    public ShopDbContext CreateDbContext(string[] args) => new(new DbContextOptionsBuilder<ShopDbContext>()
        .UseNpgsql(Environment.GetEnvironmentVariable("ConnectionStrings__Shop") ?? "Host=localhost;Database=soilshop;Username=soilshop").Options);
}
