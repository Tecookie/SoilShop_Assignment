using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using SoilShop.Application;
using SoilShop.Domain;
using SoilShop.Infrastructure;
using Xunit;
namespace SoilShop.Tests;

public sealed partial class TradingTests
{
    private async Task<SkuDto> Current(HttpClient client,int id)=>(await client.GetFromJsonAsync<SkuDto>($"/api/skus/{id}"))!;
    [Fact]
    public async Task Count_approval_is_atomic_once_and_zero_delta_has_no_movement()
    {
        using var c=await Login();var sku=await Sku(c);await Receive(c,sku.Id,8);sku=await Current(c,sku.Id);
        var count=await Post<StockCount>(c,"/api/stock-counts",new CountInput(sku.Id,8,sku.StockVersion,6,"Physical count"));
        Assert.Equal(8,(await Current(c,sku.Id)).StockQty);
        var review=new CountReviewInput(count.Version,true,"Checked discrepancy");
        var replies=await Task.WhenAll(c.PostAsJsonAsync($"/api/stock-counts/{count.Id}/review",review),c.PostAsJsonAsync($"/api/stock-counts/{count.Id}/review",review));
        Assert.Single(replies,x=>x.IsSuccessStatusCode);Assert.Single(replies,x=>x.StatusCode==HttpStatusCode.Conflict);
        sku=await Current(c,sku.Id);Assert.Equal(6,sku.StockQty);
        using var scope=factory.Services.CreateScope();var db=scope.ServiceProvider.GetRequiredService<ShopDbContext>();
        Assert.Equal(1,await db.StockAdjustments.CountAsync(x=>x.StockCountId==count.Id));
        var movement=await db.StockMovements.SingleAsync(x=>x.SkuId==sku.Id&&x.Kind=="Adjustment");Assert.Equal(-2,movement.DeltaQty);Assert.Equal(6,movement.BalanceAfter);
        var same=await Post<StockCount>(c,"/api/stock-counts",new CountInput(sku.Id,6,sku.StockVersion,6,"Recount unchanged"));
        await Post<StockCount>(c,$"/api/stock-counts/{same.Id}/review",new CountReviewInput(same.Version,true,"No discrepancy"));
        Assert.Equal(sku.StockVersion,(await Current(c,sku.Id)).StockVersion);Assert.False(await db.StockAdjustments.AnyAsync(x=>x.StockCountId==same.Id));
    }
    [Fact]
    public async Task Stale_count_submission_and_approval_cannot_overwrite_stock()
    {
        using var c=await Login();var sku=await Sku(c);await Receive(c,sku.Id,5);sku=await Current(c,sku.Id);
        var count=await Post<StockCount>(c,"/api/stock-counts",new CountInput(sku.Id,5,sku.StockVersion,4,"Count before next receipt"));
        await Receive(c,sku.Id,2);
        Assert.Equal(HttpStatusCode.Conflict,(await c.PostAsJsonAsync("/api/stock-counts",new CountInput(sku.Id,5,sku.StockVersion,4,"Old snapshot"))).StatusCode);
        var result=await Post<StockCount>(c,$"/api/stock-counts/{count.Id}/review",new CountReviewInput(count.Version,true,"Check current stock"));
        Assert.Equal("RecountRequired",result.Status);Assert.Equal(7,(await Current(c,sku.Id)).StockQty);
        using var scope=factory.Services.CreateScope();Assert.False(await scope.ServiceProvider.GetRequiredService<ShopDbContext>().StockAdjustments.AnyAsync(x=>x.StockCountId==count.Id));
    }
    [Fact]
    public async Task Direct_adjustment_replays_once_rejects_payload_reuse_and_negative_stock()
    {
        using var c=await Login();var sku=await Sku(c);var input=new AdjustmentInput(Guid.NewGuid(),sku.Id,3,sku.StockVersion,"Opening stock");
        var results=await Task.WhenAll(c.PostAsJsonAsync("/api/stock-adjustments",input),c.PostAsJsonAsync("/api/stock-adjustments",input));
        Assert.All(results,x=>Assert.True(x.IsSuccessStatusCode));Assert.Equal(3,(await Current(c,sku.Id)).StockQty);
        Assert.Equal(HttpStatusCode.Conflict,(await c.PostAsJsonAsync("/api/stock-adjustments",input with {DeltaQty=4})).StatusCode);
        sku=await Current(c,sku.Id);
        Assert.Equal(HttpStatusCode.Conflict,(await c.PostAsJsonAsync("/api/stock-adjustments",new AdjustmentInput(Guid.NewGuid(),sku.Id,-4,sku.StockVersion,"Damage"))).StatusCode);
        Assert.Equal(3,(await Current(c,sku.Id)).StockQty);
    }
    [Fact]
    public async Task Suggestion_conversion_is_atomic_idempotent_and_does_not_add_stock()
    {
        using var c=await Login();var sku=await Sku(c);var supplier=await Post<SupplierDto>(c,"/api/suppliers",new {code=Guid.NewGuid().ToString("N"),name="Conversion supplier"});
        using var scope=factory.Services.CreateScope();var db=scope.ServiceProvider.GetRequiredService<ShopDbContext>();
        var suggestion=new RestockSuggestion {SkuId=sku.Id,ObservedQty=0,SuggestedQty=30};db.RestockSuggestions.Add(suggestion);await db.SaveChangesAsync();
        var input=new ConvertSuggestionInput(suggestion.Version,supplier.Id);
        var a=await Post<TradeDto>(c,$"/api/restock-suggestions/{suggestion.Id}/convert",input);
        var b=await Post<TradeDto>(c,$"/api/restock-suggestions/{suggestion.Id}/convert",input);
        Assert.Equal(a.Id,b.Id);Assert.Equal("Draft",a.Status);Assert.Equal(30,a.Lines.Single().Quantity);Assert.Null(a.Lines.Single().UnitPriceVnd);Assert.Equal(0,(await Current(c,sku.Id)).StockQty);
        Assert.Equal(HttpStatusCode.NoContent,(await c.DeleteAsync($"/api/purchase-receipts/{a.Id}?expectedVersion={a.Version}")).StatusCode);
        await db.Entry(suggestion).ReloadAsync();Assert.Equal("Open",suggestion.Status);Assert.Null(suggestion.ReceiptId);
    }
    [Fact]
    public async Task Suggestion_inbox_search_filters_by_sku_and_pages()
    {
        using var c=await Login();var sku=await Sku(c);
        using var scope=factory.Services.CreateScope();var db=scope.ServiceProvider.GetRequiredService<ShopDbContext>();
        db.RestockSuggestions.Add(new RestockSuggestion {SkuId=sku.Id,ObservedQty=0,SuggestedQty=30});await db.SaveChangesAsync();
        var result=(await c.GetFromJsonAsync<PageResult<SuggestionDto>>($"/api/restock-suggestions/search?search={sku.Code}&page=1&pageSize=1"))!;
        Assert.Equal(sku.Id,Assert.Single(result.Items).SkuId);Assert.Equal(1,result.Total);
        Assert.Equal(HttpStatusCode.BadRequest,(await c.GetAsync("/api/restock-suggestions/search?page=0")).StatusCode);
    }
    [Fact]
    public async Task Reports_exclude_cancelled_sales_on_refresh_and_replace_snapshots()
    {
        using var c=await Login();var sku=await Sku(c);await Receive(c,sku.Id,10);
        var sale=await Draft(c,"sales",[new {skuId=sku.Id,quantity=2}]);
        sale=await Post<TradeDto>(c,$"/api/sales/{sale.Id}/confirm",new ConfirmInput(Guid.NewGuid(),sale.Version));
        // Isolate this report date from other tests; Vietnam midnight starts at UTC 17:00.
        var date=new DateOnly(2001,2,3);using var scope=factory.Services.CreateScope();var db=scope.ServiceProvider.GetRequiredService<ShopDbContext>();
        await db.Sales.Where(x=>x.Id==sale.Id).ExecuteUpdateAsync(s=>s.SetProperty(x=>x.ConfirmedAt,new DateTime(2001,2,2,17,0,0,DateTimeKind.Utc)));
        await Post<DailySummary>(c,$"/api/summaries/{date:yyyy-MM-dd}/refresh",new {});
        Assert.True(await new SummaryProcessor(db).ProcessNextAsync(CancellationToken.None));
        var result=(await c.GetFromJsonAsync<SummaryResult>($"/api/summaries/{date:yyyy-MM-dd}"))!;
        Assert.Equal(100000,result.Summary.RevenueVnd);Assert.Equal(2,result.Summary.SoldBags);Assert.Equal(1,result.Summary.SaleCount);Assert.Equal(8,result.Stock.Single(x=>x.SkuId==sku.Id).StockQty);
        await Post<TradeDto>(c,$"/api/sales/{sale.Id}/cancel",new CancelTradeInput(Guid.NewGuid(),sale.Version,"Report cancellation check"));
        await Post<DailySummary>(c,$"/api/summaries/{date:yyyy-MM-dd}/refresh",new {});db.ChangeTracker.Clear();
        await new SummaryProcessor(db).ProcessNextAsync(CancellationToken.None);
        result=(await c.GetFromJsonAsync<SummaryResult>($"/api/summaries/{date:yyyy-MM-dd}"))!;
        Assert.Equal(0,result.Summary.RevenueVnd);Assert.Equal(0,result.Summary.SoldBags);Assert.Equal(0,result.Summary.SaleCount);Assert.Equal(10,result.Stock.Single(x=>x.SkuId==sku.Id).StockQty);
        Assert.Equal(result.Stock.Count,result.Stock.Select(x=>x.SkuId).Distinct().Count());
    }
    [Fact]
    public async Task Dashboard_groups_sales_and_receipts_by_vietnam_date_and_excludes_cancellations()
    {
        using var c=await Login();var sku=await Sku(c);var supplier=await Post<SupplierDto>(c,"/api/suppliers",new {code=Guid.NewGuid().ToString("N"),name="Dashboard supplier"});
        var receipt=await Draft(c,"purchase-receipts",[new {skuId=sku.Id,quantity=5,unitCostVnd=30000}],supplier.Id);
        receipt=await Post<TradeDto>(c,$"/api/purchase-receipts/{receipt.Id}/confirm",new ConfirmInput(Guid.NewGuid(),receipt.Version));
        var sale=await Draft(c,"sales",[new {skuId=sku.Id,quantity=2}]);sale=await Post<TradeDto>(c,$"/api/sales/{sale.Id}/confirm",new ConfirmInput(Guid.NewGuid(),sale.Version));
        using var scope=factory.Services.CreateScope();var db=scope.ServiceProvider.GetRequiredService<ShopDbContext>();
        var utc=new DateTime(2002,1,6,17,0,0,DateTimeKind.Utc); // Monday 00:00 in Vietnam.
        await db.PurchaseReceipts.Where(x=>x.Id==receipt.Id).ExecuteUpdateAsync(s=>s.SetProperty(x=>x.ConfirmedAt,utc));
        await db.Sales.Where(x=>x.Id==sale.Id).ExecuteUpdateAsync(s=>s.SetProperty(x=>x.ConfirmedAt,utc));
        var day=(await c.GetFromJsonAsync<DashboardResult>("/api/dashboard?from=2002-01-07&to=2002-01-07&groupBy=day"))!;
        Assert.Equal(150000,day.Totals.ReceiptVnd);Assert.Equal(5,day.Totals.ReceivedBags);Assert.Equal(100000,day.Totals.SaleVnd);Assert.Equal(2,day.Totals.SoldBags);
        var week=(await c.GetFromJsonAsync<DashboardResult>("/api/dashboard?from=2002-01-05&to=2002-01-13&groupBy=week"))!;
        Assert.Equal(2,week.Periods.Count);Assert.Equal(new DateOnly(2002,1,6),week.Periods[0].To);Assert.Equal(100000,week.Periods[1].SaleVnd);
        await Post<TradeDto>(c,$"/api/sales/{sale.Id}/cancel",new CancelTradeInput(Guid.NewGuid(),sale.Version,"Dashboard cancellation"));
        day=(await c.GetFromJsonAsync<DashboardResult>("/api/dashboard?from=2002-01-07&to=2002-01-07"))!;
        Assert.Equal(0,day.Totals.SaleVnd);Assert.Equal(150000,day.Totals.ReceiptVnd);
        Assert.Equal(HttpStatusCode.BadRequest,(await c.GetAsync("/api/dashboard?from=2000-01-01&to=2002-01-07")).StatusCode);
        Assert.Equal(HttpStatusCode.BadRequest,(await c.GetAsync("/api/dashboard?from=2002-01-07&to=2002-01-07&groupBy=month")).StatusCode);
        using var anonymous=factory.CreateClient();Assert.Equal(HttpStatusCode.Unauthorized,(await anonymous.GetAsync("/api/dashboard?from=2002-01-07&to=2002-01-07")).StatusCode);
    }
    [Fact]
    public async Task Employee_can_submit_counts_but_cannot_administer_or_adjust_or_backup()
    {
        using var admin=await Login();var account=await Post<AccountDto>(admin,"/api/accounts",new AccountInput("emp-"+Guid.NewGuid().ToString("N"),"Workflow employee","Employee",Password:"Employee-test-password-738!"));
        using var employee=factory.CreateClient();var login=await Post<LoginResult>(employee,"/api/auth/login",new {username=account.Username,password="Employee-test-password-738!"});employee.DefaultRequestHeaders.Authorization=new AuthenticationHeaderValue("Bearer",login.Token);
        var sku=await Sku(admin);var count=await Post<StockCount>(employee,"/api/stock-counts",new CountInput(sku.Id,0,0,0,"Employee count"));
        Assert.Equal(HttpStatusCode.Forbidden,(await employee.PostAsJsonAsync($"/api/stock-counts/{count.Id}/review",new CountReviewInput(1,true,"Employee attempt"))).StatusCode);
        Assert.Equal(HttpStatusCode.Forbidden,(await employee.PostAsJsonAsync("/api/stock-adjustments",new AdjustmentInput(Guid.NewGuid(),sku.Id,1,0,"Employee attempt"))).StatusCode);
        Assert.Equal(HttpStatusCode.Forbidden,(await employee.GetAsync("/api/accounts")).StatusCode);Assert.Equal(HttpStatusCode.Forbidden,(await employee.GetAsync("/api/backups")).StatusCode);
        Assert.Equal(HttpStatusCode.Forbidden,(await employee.PostAsJsonAsync("/api/backups",new {requestKey=Guid.NewGuid()})).StatusCode);
        Assert.Equal(HttpStatusCode.OK,(await employee.GetAsync("/api/summaries")).StatusCode);
    }
    [Fact]
    public async Task Account_changes_invalidate_tokens_and_never_expose_password_hashes()
    {
        using var admin=await Login();var account=await Post<AccountDto>(admin,"/api/accounts",new AccountInput("user-"+Guid.NewGuid().ToString("N"),"Test user","Employee",Password:"Employee-test-password-738!"));
        using var employee=factory.CreateClient();var login=await Post<LoginResult>(employee,"/api/auth/login",new {username=account.Username,password="Employee-test-password-738!"});employee.DefaultRequestHeaders.Authorization=new AuthenticationHeaderValue("Bearer",login.Token);
        var input=new AccountInput(account.Username,account.DisplayName,"Employee",false,ExpectedVersion:account.Version);
        Assert.True((await admin.PutAsJsonAsync($"/api/accounts/{account.Id}",input)).IsSuccessStatusCode);Assert.Equal(HttpStatusCode.Unauthorized,(await employee.GetAsync("/api/skus")).StatusCode);
        var json=await admin.GetStringAsync("/api/accounts");Assert.DoesNotContain("password",json,StringComparison.OrdinalIgnoreCase);
        Assert.Equal(HttpStatusCode.Conflict,(await admin.PutAsJsonAsync($"/api/accounts/{account.Id}",input)).StatusCode);
    }
    [Fact]
    public async Task Last_active_admin_is_preserved()
    {
        using var c=await Login();var accounts=(await c.GetFromJsonAsync<PageResult<AccountDto>>("/api/accounts?search=test-admin"))!;var admin=Assert.Single(accounts.Items,x=>x.Username=="test-admin");
        Assert.Equal(HttpStatusCode.Conflict,(await c.PutAsJsonAsync($"/api/accounts/{admin.Id}",new AccountInput(admin.Username,admin.DisplayName,"Employee",ExpectedVersion:admin.Version))).StatusCode);
    }
    [Fact]
    public async Task Supplier_directory_search_and_updates_use_versions()
    {
        using var c=await Login();var code="DIR"+Guid.NewGuid().ToString("N");var supplier=await Post<SupplierDto>(c,"/api/suppliers",new {code,name="Directory supplier"});
        var data=(await c.GetFromJsonAsync<PageResult<SupplierAdminDto>>($"/api/supplier-directory?search={code}"))!;var item=Assert.Single(data.Items);
        var update=new SupplierUpdateInput(item.Code,"Updated supplier","Contact note",false,item.Version);
        Assert.True((await c.PutAsJsonAsync($"/api/supplier-directory/{supplier.Id}",update)).IsSuccessStatusCode);
        Assert.Equal(HttpStatusCode.Conflict,(await c.PutAsJsonAsync($"/api/supplier-directory/{supplier.Id}",update)).StatusCode);
        Assert.Empty((await c.GetFromJsonAsync<PageResult<SupplierAdminDto>>($"/api/supplier-directory?search={code}&active=true"))!.Items);
    }
    [Fact]
    public async Task Deactivated_supplier_cannot_be_used_for_new_or_confirmed_receipts_and_history_remains()
    {
        using var c=await Login();var sku=await Sku(c);var code="S-"+Guid.NewGuid().ToString("N");
        var supplier=await Post<SupplierDto>(c,"/api/suppliers",new {code,name="Supplier retained in history"});
        var confirmed=await Draft(c,"purchase-receipts",[new {skuId=sku.Id,quantity=2,unitCostVnd=100}],supplier.Id);
        confirmed=await Post<TradeDto>(c,$"/api/purchase-receipts/{confirmed.Id}/confirm",new ConfirmInput(Guid.NewGuid(),confirmed.Version));
        var draft=await Draft(c,"purchase-receipts",[new {skuId=sku.Id,quantity=1,unitCostVnd=100}],supplier.Id);
        var dir=(await c.GetFromJsonAsync<PageResult<SupplierAdminDto>>($"/api/supplier-directory?search={code}"))!;var item=Assert.Single(dir.Items);
        Assert.True((await c.PutAsJsonAsync($"/api/supplier-directory/{supplier.Id}",new SupplierUpdateInput(item.Code,item.Name,item.ContactNote,false,item.Version))).IsSuccessStatusCode);
        Assert.Equal(HttpStatusCode.BadRequest,(await c.PostAsJsonAsync("/api/purchase-receipts",new {supplierId=supplier.Id,lines=new[]{new {skuId=sku.Id,quantity=1,unitCostVnd=100}}})).StatusCode);
        Assert.Equal(HttpStatusCode.Conflict,(await c.PostAsJsonAsync($"/api/purchase-receipts/{draft.Id}/confirm",new ConfirmInput(Guid.NewGuid(),draft.Version))).StatusCode);
        var preserved=(await c.GetFromJsonAsync<TradeDto>($"/api/purchase-receipts/{draft.Id}"))!;Assert.Equal("Draft",preserved.Status);Assert.Equal(supplier.Id,preserved.SupplierId);
        var history=(await c.GetFromJsonAsync<TradeDto>($"/api/purchase-receipts/{confirmed.Id}"))!;Assert.Equal("Confirmed",history.Status);Assert.Equal(supplier.Id,history.SupplierId);Assert.Equal(200,history.TotalVnd);
    }
    [Fact]
    public async Task Document_search_and_status_filters_return_only_matching_records()
    {
        using var c=await Login();var sku=await Sku(c);var sale=await Draft(c,"sales",[new {skuId=sku.Id,quantity=1}]);
        var found=(await c.GetFromJsonAsync<PageResult<TradeDto>>($"/api/sales?search={sale.Code}&status=Draft&createdBy={sale.CreatedBy}"))!;Assert.Equal(sale.Id,Assert.Single(found.Items).Id);
        Assert.Empty((await c.GetFromJsonAsync<PageResult<TradeDto>>($"/api/sales?search={sale.Code}&status=Confirmed"))!.Items);
        Assert.Equal(HttpStatusCode.BadRequest,(await c.GetAsync("/api/sales?status=Wrong")).StatusCode);
        Assert.Equal(HttpStatusCode.BadRequest,(await c.GetAsync("/api/sales?from=2026-11-11&to=2026-11-10")).StatusCode);
    }
    [Fact]
    public async Task Manual_backup_request_is_durable_and_same_key_replays()
    {
        using var c=await Login();var key=Guid.NewGuid();var a=await Post<BackupRun>(c,"/api/backups",new {requestKey=key});var b=await Post<BackupRun>(c,"/api/backups",new {requestKey=key});Assert.Equal(a.Id,b.Id);Assert.Equal("Queued",a.Status);
        using var scope=factory.Services.CreateScope();var db=scope.ServiceProvider.GetRequiredService<ShopDbContext>();Assert.Equal(1,await db.BackupRuns.CountAsync(x=>x.Id==key));
    }
}
