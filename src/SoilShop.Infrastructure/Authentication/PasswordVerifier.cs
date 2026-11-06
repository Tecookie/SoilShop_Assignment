using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;
using Microsoft.Extensions.Configuration;
using SoilShop.Application;
using SoilShop.Domain;

namespace SoilShop.Infrastructure;

public sealed class PasswordVerifier : IPasswordVerifier
{
    private readonly PasswordHasher<User> hasher = new();
    public bool Verify(User user, string password) => hasher.VerifyHashedPassword(user, user.PasswordHash, password) != PasswordVerificationResult.Failed;
}
