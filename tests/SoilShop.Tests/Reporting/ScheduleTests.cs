using SoilShop.Application;
using Xunit;
namespace SoilShop.Tests;

public sealed class ScheduleTests
{
    [Fact]
    public void Jobs_follow_Vietnam_midnight_and_cutoff_times()
    {
        var midnight=new DateTime(2026,11,10,17,0,0,DateTimeKind.Utc);
        Assert.Null(VietnamSchedule.SummaryDate(midnight.AddMinutes(4)));
        Assert.Equal(new DateOnly(2026,11,10),VietnamSchedule.SummaryDate(midnight.AddMinutes(5)));
        Assert.Null(VietnamSchedule.BackupDate(midnight.AddMinutes(14)));
        Assert.Equal(new DateOnly(2026,11,11),VietnamSchedule.BackupDate(midnight.AddMinutes(15)));
        Assert.Equal(new DateOnly(2026,11,10),VietnamSchedule.SummaryDate(midnight.AddHours(20)));
    }
}
