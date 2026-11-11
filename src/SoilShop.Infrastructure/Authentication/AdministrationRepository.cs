using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using SoilShop.Application;
using SoilShop.Domain;
namespace SoilShop.Infrastructure;

public sealed class AdministrationRepository(ShopDbContext db) : IAdministrationRepository
{
    public async Task<PageResult<AccountDto>> AccountsAsync(string? search,int page,int size,CancellationToken ct)
    {
        var q=db.Users.AsNoTracking();
        if(!string.IsNullOrWhiteSpace(search)) q=q.Where(x=>x.Username.ToLower().Contains(search.ToLower()) || x.DisplayName.ToLower().Contains(search.ToLower()));
        return new(await q.OrderBy(x=>x.Username).ThenBy(x=>x.Id).Skip((page-1)*size).Take(size).Select(x=>new AccountDto(x.Id,x.Username,x.DisplayName,x.Role,x.IsActive,x.Version)).ToListAsync(ct),await q.CountAsync(ct),page,size);
    }
    public async Task<AccountDto> SaveAccountAsync(int? id,AccountInput input,CancellationToken ct)
    {
        await using var tx=await db.Database.BeginTransactionAsync(ct);
        await db.Database.ExecuteSqlRawAsync("SELECT pg_advisory_xact_lock(23203)",ct);
        var user=id is null ? new User() : await db.Users.SingleOrDefaultAsync(x=>x.Id==id,ct) ?? throw new BusinessException(404,"Account not found.");
        if(id is not null && user.Version!=input.ExpectedVersion) throw new BusinessException(409,"Account changed. Reload.");
        if(id is not null && user.IsActive && user.Role=="Admin" && (!input.IsActive || input.Role!="Admin") && !await db.Users.AnyAsync(x=>x.Id!=id && x.IsActive && x.Role=="Admin",ct)) throw new BusinessException(409,"Keep at least one active Admin.");
        user.Username=input.Username; user.DisplayName=input.DisplayName; user.Role=input.Role; user.IsActive=input.IsActive;
        if(input.Password is not null) user.PasswordHash=new PasswordHasher<User>().HashPassword(user,input.Password);
        if(id is null) db.Users.Add(user); else user.Version++;
        await db.SaveChangesAsync(ct); await tx.CommitAsync(ct);
        return new(user.Id,user.Username,user.DisplayName,user.Role,user.IsActive,user.Version);
    }
    public async Task<PageResult<SupplierAdminDto>> SuppliersAsync(string? search,bool? active,int page,int size,CancellationToken ct)
    {
        var q=db.Suppliers.AsNoTracking();
        if(!string.IsNullOrWhiteSpace(search)) q=q.Where(x=>x.Code.ToLower().Contains(search.ToLower()) || x.Name.ToLower().Contains(search.ToLower()));
        if(active.HasValue) q=q.Where(x=>x.IsActive==active.Value);
        return new(await q.OrderBy(x=>x.Name).ThenBy(x=>x.Id).Skip((page-1)*size).Take(size).Select(x=>new SupplierAdminDto(x.Id,x.Code,x.Name,x.ContactNote,x.IsActive,x.Version)).ToListAsync(ct),await q.CountAsync(ct),page,size);
    }
    public async Task<SupplierAdminDto> UpdateSupplierAsync(int id,SupplierUpdateInput input,CancellationToken ct)
    {
        var supplier=await db.Suppliers.SingleOrDefaultAsync(x=>x.Id==id,ct) ?? throw new BusinessException(404,"Supplier not found.");
        if(supplier.Version!=input.ExpectedVersion) throw new BusinessException(409,"Supplier changed. Reload.");
        supplier.Code=input.Code; supplier.Name=input.Name; supplier.ContactNote=input.ContactNote; supplier.IsActive=input.IsActive; supplier.Version++;
        await db.SaveChangesAsync(ct); return new(supplier.Id,supplier.Code,supplier.Name,supplier.ContactNote,supplier.IsActive,supplier.Version);
    }
}
