namespace SoilShop.Application;

public static class VietnamSchedule
{
    public static DateTime Local(DateTime utc)=>TimeZoneInfo.ConvertTimeFromUtc(utc,TimeZoneInfo.FindSystemTimeZoneById("Asia/Ho_Chi_Minh"));
    public static DateOnly? SummaryDate(DateTime utc) { var local=Local(utc);return local.TimeOfDay>=TimeSpan.FromMinutes(5)?DateOnly.FromDateTime(local).AddDays(-1):null; }
    public static DateOnly? BackupDate(DateTime utc) { var local=Local(utc);return local.TimeOfDay>=TimeSpan.FromMinutes(15)?DateOnly.FromDateTime(local):null; }
}
