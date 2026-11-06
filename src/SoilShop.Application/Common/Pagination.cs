
namespace SoilShop.Application;

public static class Pagination
{
public static (int Page, int Size) Validate(int page, int size)
    {
        if (page < 1 || page > 1000000 || size < 1 || size > 100) throw new BusinessException(400, "Page must be positive and pageSize between 1 and 100.");
        return (page, size);
    }
}
