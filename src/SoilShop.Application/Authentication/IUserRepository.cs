using SoilShop.Domain;

namespace SoilShop.Application;

public interface IUserRepository
{
    Task<User?> FindUserAsync(string username, CancellationToken ct);
    Task<User?> UserAsync(int id, CancellationToken ct);
}
