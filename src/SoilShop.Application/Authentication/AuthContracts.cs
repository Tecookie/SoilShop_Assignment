using System.ComponentModel.DataAnnotations;
using SoilShop.Domain;

namespace SoilShop.Application;

public sealed record LoginInput([Required, MaxLength(100)] string Username, [Required, MaxLength(200)] string Password);

public sealed record UserDto(int Id, string Username, string DisplayName, string Role);

public sealed record LoginResult(string Token, DateTime ExpiresAt, UserDto User);

public interface IPasswordVerifier { bool Verify(User user, string password); }

public interface ITokenIssuer { LoginResult Issue(User user); }
