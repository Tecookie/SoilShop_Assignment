using Microsoft.Extensions.Configuration;
using SoilShop.Worker;
using Xunit;
namespace SoilShop.Tests;

public sealed class WorkerSettingsTests
{
    [Fact]
    public void Root_settings_have_safe_defaults_and_are_validated()
    {
        var defaults=WorkerSettings.Load(new ConfigurationBuilder().AddInMemoryCollection().Build());
        Assert.Equal(3,defaults.MaxRetries);Assert.Equal(25,defaults.BatchSize);
        var config=new ConfigurationBuilder().AddInMemoryCollection(new Dictionary<string,string?> { ["BatchSize"]="7",["MaxRetries"]="0",["ReclaimSeconds"]="2" }).Build();
        var custom=WorkerSettings.Load(config);Assert.Equal(7,custom.BatchSize);Assert.Equal(0,custom.MaxRetries);Assert.Equal(2,custom.ReclaimSeconds);
        config["BatchSize"]="101";Assert.Throws<InvalidOperationException>(()=>WorkerSettings.Load(config));
        config["BatchSize"]="bad";Assert.Throws<InvalidOperationException>(()=>WorkerSettings.Load(config));
    }
}
