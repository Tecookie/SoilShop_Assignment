using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;
using Microsoft.Extensions.Configuration;
using SoilShop.Application;
using SoilShop.Domain;

namespace SoilShop.Infrastructure;

public static class DatabaseSetup
{
    public static async Task InitializeAsync(ShopDbContext db, IConfiguration config, CancellationToken ct = default)
    {
        await db.Database.MigrateAsync(ct);
        if (await db.Users.AnyAsync(ct)) return;
        var username = config["Bootstrap:Username"];
        var password = config["Bootstrap:Password"];
        if (string.IsNullOrWhiteSpace(username) || username.Length > 100 || string.IsNullOrWhiteSpace(password) || password.Length < 12)
            throw new InvalidOperationException("Set Bootstrap username and a password of at least 12 characters for the first start.");
        var user = new User { Username = username.Trim().ToLowerInvariant(), DisplayName = "Shop administrator", Role = "Admin" };
        user.PasswordHash = new PasswordHasher<User>().HashPassword(user, password);
        db.Users.Add(user); await db.SaveChangesAsync(ct);
    }
}
