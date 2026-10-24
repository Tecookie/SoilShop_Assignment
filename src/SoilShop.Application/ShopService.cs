using AutoMapper;
using SoilShop.Domain;

namespace SoilShop.Application;

public sealed class ShopProfile : Profile
{
    public ShopProfile()
    {
        CreateMap<Product, ProductDto>();
        CreateMap<Supplier, SupplierDto>();
        CreateMap<Sku, SkuDto>();
        CreateMap<RestockSuggestion, SuggestionDto>();
    }
}

public sealed class ShopService(IShopRepository repository, IMapper mapper, ICalculationClient calculation)
{
    public static (int Page, int Size) Paging(int page, int size)
    {
        if (page < 1 || page > 1000000 || size < 1 || size > 100) throw new BusinessException(400, "Page must be positive and pageSize between 1 and 100.");
        return (page, size);
    }
    private static string Code(string code)
    {
        if (string.IsNullOrWhiteSpace(code)) throw new BusinessException(400, "Code is required.");
        return code.Trim().ToUpperInvariant();
    }
    private static string Name(string name)
    {
        if (string.IsNullOrWhiteSpace(name)) throw new BusinessException(400, "Name is required.");
        return name.Trim();
    }
    private static string? Optional(string? value) => string.IsNullOrWhiteSpace(value) ? null : value.Trim();
    public async Task<PageResult<ProductDto>> ProductsAsync(string? search, bool? active, string sort, int page, int size, CancellationToken ct, string? productType = null)
    {
        Paging(page, size);
        if (sort is not ("name" or "code" or "newest")) throw new BusinessException(400, "Sort must be name, code or newest.");
        ValidateProductType(productType);
        var result = await repository.ProductsAsync(search, active, sort, page, size, ct, productType);
        return new(mapper.Map<List<ProductDto>>(result.Items), result.Total, page, size);
    }
    public async Task<ProductDto> SaveProductAsync(int? id, ProductInput input, CancellationToken ct)
    {
        ValidateProductType(input.ProductType);
        var product = id is null ? new Product() : await repository.ProductAsync(id.Value, ct) ?? throw new BusinessException(404, "Product not found.");
        if (id is not null && input.ExpectedVersion != product.Version) throw new BusinessException(409, "Product has changed. Reload before saving.");
        product.Code = Code(input.Code); product.Name = Name(input.Name); product.Brand = Optional(input.Brand);
        product.ProductType = input.ProductType;
        product.Ingredients = Optional(input.Ingredients); product.SourceReference = Optional(input.SourceReference);
        product.ReviewedAt = null; product.ReviewedBy = null; product.IsActive = input.IsActive;
        if (id is not null) product.Version++;
        return mapper.Map<ProductDto>(await repository.SaveProductAsync(product, id is null, ct));
    }
    public async Task<ProductDto> ProductAsync(int id, CancellationToken ct) => mapper.Map<ProductDto>(await repository.ProductAsync(id, ct) ?? throw new BusinessException(404, "Product not found."));
    public async Task DeleteProductAsync(int id, CancellationToken ct) => await repository.DeleteProductAsync(await repository.ProductAsync(id, ct) ?? throw new BusinessException(404, "Product not found."), ct);
    public async Task<IReadOnlyList<SupplierDto>> SuppliersAsync(CancellationToken ct) => mapper.Map<List<SupplierDto>>(await repository.SuppliersAsync(ct));
    public async Task<SupplierDto> AddSupplierAsync(SupplierInput input, CancellationToken ct) => mapper.Map<SupplierDto>(await repository.AddSupplierAsync(new Supplier { Code = Code(input.Code), Name = Name(input.Name), ContactNote = Optional(input.ContactNote) }, ct));
    private static void ValidateProductType(string? value)
    {
        if (value is not null && !ProductTypes.IsValid(value)) throw new BusinessException(400, "Unknown product type. Choose a supported catalogue group.");
    }
    public async Task<PageResult<SkuDto>> SkusAsync(string? search, bool lowOnly, int? productId, int page, int size, CancellationToken ct, string? productType = null)
    {
        Paging(page, size); ValidateProductType(productType); var result = await repository.SkusAsync(search, lowOnly, productId, page, size, ct, productType);
        return new(mapper.Map<List<SkuDto>>(result.Items), result.Total, page, size);
    }
    public async Task<SkuDto> SkuAsync(int id, CancellationToken ct) => mapper.Map<SkuDto>(await repository.SkuAsync(id, ct) ?? throw new BusinessException(404, "SKU not found."));
    public async Task<SkuDto> SaveSkuAsync(int? id, SkuInput input, CancellationToken ct)
    {
        if (decimal.Truncate(input.SalePriceVnd) != input.SalePriceVnd || decimal.Round(input.PackageAmount, 2) != input.PackageAmount || input.TargetStock <= input.LowStockThreshold) throw new BusinessException(400, "Use whole VND, a package amount with at most two decimals, and target above threshold.");
        if (await repository.ProductAsync(input.ProductId, ct) is null) throw new BusinessException(400, "Product not found.");
        if (input.PreferredSupplierId is int supplier && !await repository.SupplierActiveAsync(supplier, ct)) throw new BusinessException(400, "Active supplier required.");
        var sku = id is null ? new Sku() : await repository.SkuAsync(id.Value, ct) ?? throw new BusinessException(404, "SKU not found.");
        if (id is not null && input.ExpectedVersion != sku.Version) throw new BusinessException(409, "SKU has changed. Reload before saving.");
        // Identity and packaging are immutable once created, including when history references them.
        if (id is not null && (sku.ProductId != input.ProductId || sku.PackageAmount != input.PackageAmount || sku.PackageUnit != input.PackageUnit || sku.Code != Code(input.Code))) throw new BusinessException(400, "Create another SKU to change identity or packaging.");
        sku.ProductId = input.ProductId; sku.Code = Code(input.Code); sku.PackageAmount = input.PackageAmount;
        sku.PackageUnit = input.PackageUnit; sku.SalePriceVnd = input.SalePriceVnd; sku.LowStockThreshold = input.LowStockThreshold;
        sku.TargetStock = input.TargetStock; sku.PreferredSupplierId = input.PreferredSupplierId; sku.IsActive = input.IsActive;
        if (id is not null) sku.Version++;
        return mapper.Map<SkuDto>(await repository.SaveSkuAsync(sku, id is null, ct));
    }
    public async Task<TradeDto> CreateTradeAsync(bool receipt, TradeInput input, int actorId, CancellationToken ct)
    {
        if (input.Lines.Count is < 1 or > 100 || input.Lines.Select(x => x.SkuId).Distinct().Count() != input.Lines.Count) throw new BusinessException(400, "Use 1–100 distinct SKU lines.");
        if (receipt && (input.SupplierId is not int supplier || !await repository.SupplierActiveAsync(supplier, ct))) throw new BusinessException(400, "An active supplier is required.");
        if (!receipt && (input.SupplierId is not null || input.Lines.Any(x => x.UnitCostVnd is not null))) throw new BusinessException(400, "Sales use current catalogue prices; omit supplier and costs.");
        foreach (var line in input.Lines)
        {
            if (line.Quantity is < 1 or > 1000000 || line.SkuId < 1) throw new BusinessException(400, "Invalid SKU or bag quantity.");
            if (receipt && line.UnitCostVnd is decimal cost && (cost <= 0 || cost > 1000000000000m || decimal.Truncate(cost) != cost)) throw new BusinessException(400, "Cost must be positive whole VND.");
        }
        return await repository.CreateTradeAsync(receipt, input, actorId, ct);
    }
    public async Task<TradeDto> TradeAsync(bool receipt, int id, CancellationToken ct) => await repository.TradeAsync(receipt, id, ct) ?? throw new BusinessException(404, "Document not found.");
    public Task<PageResult<TradeDto>> TradesAsync(bool receipt, int page, int size, CancellationToken ct) { Paging(page, size); return repository.TradesAsync(receipt, page, size, ct); }
    public async Task<TradeDto> ConfirmAsync(bool receipt, int id, ConfirmInput input, int actorId, CancellationToken ct)
    {
        if (input.ActionKey == Guid.Empty || input.ExpectedVersion < 1) throw new BusinessException(400, "Action key and expected version are required.");
        var existing = await TradeAsync(receipt, id, ct);
        // Repository still checks the key under the document lock for confirmed retry requests.
        decimal total = 0;
        if (existing.Status == "Draft") total = await calculation.TotalAsync(await repository.QuoteAsync(receipt, id, ct), ct);
        return await repository.ConfirmAsync(receipt, id, input, total, actorId, ct);
    }
    public Task<PageResult<MovementDto>> MovementsAsync(int? skuId, int page, int size, CancellationToken ct) { Paging(page, size); return repository.MovementsAsync(skuId, page, size, ct); }
    public async Task<IReadOnlyList<SuggestionDto>> SuggestionsAsync(CancellationToken ct) => mapper.Map<List<SuggestionDto>>(await repository.SuggestionsAsync(ct));
}

public sealed class AuthService(IShopRepository repository, IPasswordVerifier passwords, ITokenIssuer tokens)
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
