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
        CreateMap<Product, SoilMetadataDto>().ForMember(x => x.ProductId, o => o.MapFrom(x => x.Id)).ForMember(x => x.PlantGroups, o => o.Ignore());
    }
}
