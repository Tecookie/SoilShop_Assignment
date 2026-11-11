using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SoilShop.Application;
namespace SoilShop.Api;

[ApiController,Authorize(Roles="Admin"),Route("api/accounts")]
public sealed class AccountsController(AdministrationService service) : ControllerBase
{
    [HttpGet] public Task<PageResult<AccountDto>> List(CancellationToken ct,string? search=null,int page=1,int pageSize=20)=>service.AccountsAsync(search,page,pageSize,ct);
    [HttpPost] public async Task<ActionResult<AccountDto>> Create(AccountInput input,CancellationToken ct) { var result=await service.SaveAccountAsync(null,input,ct); return Created($"/api/accounts/{result.Id}",result); }
    [HttpPut("{id:int}")] public Task<AccountDto> Update(int id,AccountInput input,CancellationToken ct)=>service.SaveAccountAsync(id,input,ct);
}
[ApiController,Authorize,Route("api/supplier-directory")]
public sealed class SupplierDirectoryController(AdministrationService service) : ControllerBase
{
    [HttpGet] public Task<PageResult<SupplierAdminDto>> List(CancellationToken ct,string? search=null,bool? active=null,int page=1,int pageSize=20)=>service.SuppliersAsync(search,active,page,pageSize,ct);
    [HttpPut("{id:int}"),Authorize(Roles="Admin")] public Task<SupplierAdminDto> Update(int id,SupplierUpdateInput input,CancellationToken ct)=>service.UpdateSupplierAsync(id,input,ct);
}
