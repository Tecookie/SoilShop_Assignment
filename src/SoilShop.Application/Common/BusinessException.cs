
namespace SoilShop.Application;

public class BusinessException(int status, string message) : Exception(message) { public int Status { get; } = status; }
