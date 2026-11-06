
namespace SoilShop.Application;

public sealed record PageResult<T>(IReadOnlyList<T> Items, int Total, int Page, int PageSize);
