namespace SoilShop.Worker;

// Root configuration keys have no additional prefix; all bounds are validated at startup.
public sealed record WorkerSettings(int BatchSize,int PollSeconds,int ReclaimSeconds,int ReconcileSeconds,int MaxRetries,int JobPollSeconds)
{
    public static WorkerSettings Load(IConfiguration config)
    {
        int Read(string key,int fallback,int min,int max)
        {
            var raw=config[key];
            if(raw is null) return fallback;
            if(!int.TryParse(raw,out var value) || value<min || value>max) throw new InvalidOperationException($"{key} must be between {min} and {max}.");
            return value;
        }
        return new(Read("BatchSize",25,1,100),Read("PollSeconds",2,1,60),Read("ReclaimSeconds",60,1,3600),
            Read("ReconcileSeconds",60,5,3600),Read("MaxRetries",3,0,20),Read("JobPollSeconds",10,1,300));
    }
}
