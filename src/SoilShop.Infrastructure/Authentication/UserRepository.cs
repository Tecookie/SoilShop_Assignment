using Microsoft.EntityFrameworkCore;
using SoilShop.Application;
using SoilShop.Domain;
namespace SoilShop.Infrastructure;

public sealed class UserRepository(ShopDbContext db) : IUserRepository
{
    public Task<User?> FindUserAsync(string username, CancellationToken ct) => db.Users.AsNoTracking().SingleOrDefaultAsync(x => x.Username == username, ct);

    public Task<User?> UserAsync(int id, CancellationToken ct) => db.Users.AsNoTracking().SingleOrDefaultAsync(x => x.Id == id, ct);
}
