using SoilShop.Domain;

namespace SoilShop.Application;

public sealed class AuthService(IUserRepository repository, IPasswordVerifier passwords, ITokenIssuer tokens)
{
    public async Task<UserDto> MeAsync(int id, CancellationToken ct)
    {
        var user = await repository.UserAsync(id, ct) ?? throw new BusinessException(401, "Account is unavailable.");
        return new(user.Id, user.Username, user.DisplayName, user.Role);
    }
    public async Task<LoginResult> LoginAsync(LoginInput input, CancellationToken ct)
    {
        var user = await repository.FindUserAsync(input.Username.Trim().ToLowerInvariant(), ct);
        if (user is null || !user.IsActive || !passwords.Verify(user, input.Password)) throw new BusinessException(401, "Invalid username or password.");
        return tokens.Issue(user);
    }
}
