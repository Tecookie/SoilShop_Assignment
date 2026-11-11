using System.ComponentModel.DataAnnotations;
namespace SoilShop.Application;

public sealed record AccountDto(int Id,string Username,string DisplayName,string Role,bool IsActive,long Version);
public sealed record AccountInput([Required,MaxLength(100),RegularExpression("^[A-Za-z0-9._-]+$")] string Username,
    [Required,MaxLength(150)] string DisplayName,[Required,RegularExpression("^(Admin|Employee)$")] string Role,
    bool IsActive=true,[MinLength(12),MaxLength(200)] string? Password=null,long? ExpectedVersion=null);
public sealed record SupplierUpdateInput([Required,MaxLength(40)] string Code,[Required,MaxLength(200)] string Name,
    [MaxLength(2000)] string? ContactNote,bool IsActive,[Range(1,long.MaxValue)] long ExpectedVersion);
public sealed record SupplierAdminDto(int Id,string Code,string Name,string? ContactNote,bool IsActive,long Version);
public interface IAdministrationRepository
{
    Task<PageResult<AccountDto>> AccountsAsync(string? search,int page,int size,CancellationToken ct);
    Task<AccountDto> SaveAccountAsync(int? id,AccountInput input,CancellationToken ct);
    Task<PageResult<SupplierAdminDto>> SuppliersAsync(string? search,bool? active,int page,int size,CancellationToken ct);
    Task<SupplierAdminDto> UpdateSupplierAsync(int id,SupplierUpdateInput input,CancellationToken ct);
}
public sealed class AdministrationService(IAdministrationRepository repository)
{
    public Task<PageResult<AccountDto>> AccountsAsync(string? search,int page,int size,CancellationToken ct) { Pagination.Validate(page,size); return repository.AccountsAsync(search,page,size,ct); }
    public Task<AccountDto> SaveAccountAsync(int? id,AccountInput input,CancellationToken ct)
    {
        if(string.IsNullOrWhiteSpace(input.DisplayName) || (id is null && input.Password is null)) throw new BusinessException(400,"Display name and an initial password of at least 12 characters are required.");
        return repository.SaveAccountAsync(id,input with { Username=input.Username.Trim(),DisplayName=input.DisplayName.Trim() },ct);
    }
    public Task<PageResult<SupplierAdminDto>> SuppliersAsync(string? search,bool? active,int page,int size,CancellationToken ct) { Pagination.Validate(page,size); return repository.SuppliersAsync(search,active,page,size,ct); }
    public Task<SupplierAdminDto> UpdateSupplierAsync(int id,SupplierUpdateInput input,CancellationToken ct)
    {
        if(string.IsNullOrWhiteSpace(input.Code) || string.IsNullOrWhiteSpace(input.Name)) throw new BusinessException(400,"Supplier code and name are required.");
        return repository.UpdateSupplierAsync(id,input with { Code=input.Code.Trim().ToUpperInvariant(),Name=input.Name.Trim(),ContactNote=input.ContactNote?.Trim() },ct);
    }
}
