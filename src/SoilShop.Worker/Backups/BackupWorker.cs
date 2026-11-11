using System.Diagnostics;
using Microsoft.EntityFrameworkCore;
using Npgsql;
using SoilShop.Domain;
using SoilShop.Infrastructure;
using SoilShop.Application;
namespace SoilShop.Worker;

public sealed class BackupWorker(IServiceScopeFactory scopes,IConfiguration config,WorkerSettings settings,ILogger<BackupWorker> logger) : BackgroundService
{
    protected override async Task ExecuteAsync(CancellationToken ct)
    {
        while(!ct.IsCancellationRequested)
        {
            try { await TickAsync(ct); }
            catch(OperationCanceledException) when(ct.IsCancellationRequested) { break; }
            catch(Exception ex) { logger.LogWarning("Backup worker will retry: {ErrorType}",ex.GetType().Name); }
            try { await Task.Delay(TimeSpan.FromSeconds(settings.JobPollSeconds),ct); } catch(OperationCanceledException) { break; }
        }
    }
    public async Task TickAsync(CancellationToken ct)
    {
        await using var scope=scopes.CreateAsyncScope(); var db=scope.ServiceProvider.GetRequiredService<ShopDbContext>();
        await db.Database.OpenConnectionAsync(ct);
        // Session lock spans the external process and retention; another Worker never prunes an active dump.
        var connection=(NpgsqlConnection)db.Database.GetDbConnection();
        await using var acquire=new NpgsqlCommand("SELECT pg_try_advisory_lock(23204)",connection);
        if(!(bool)(await acquire.ExecuteScalarAsync(ct))!) return;
        try
        {
            if(VietnamSchedule.BackupDate(DateTime.UtcNow) is DateOnly date)
            {
                var id=Guid.NewGuid();
                await db.Database.ExecuteSqlInterpolatedAsync($"INSERT INTO backup_runs (id,kind,scheduled_date) VALUES ({id},'Daily',{date}) ON CONFLICT (scheduled_date) WHERE scheduled_date IS NOT NULL DO NOTHING",ct);
            }
            // A previous owner died. Its unacknowledged process is retried using the same run identity.
            await db.BackupRuns.Where(x=>x.Status=="Processing").ExecuteUpdateAsync(s=>s.SetProperty(x=>x.Status,"Queued"),ct);
            var run=await db.BackupRuns.Where(x=>x.Status=="Queued").OrderBy(x=>x.CreatedAt).FirstOrDefaultAsync(ct);
            if(run is null) return;
            var directory=Path.GetFullPath(config["Backups:Directory"] ?? "/backups");
            var filename=$"soilshop-{run.Id:N}.dump"; var path=Path.Combine(directory,filename);var temporary=path+".partial";
            run.Status="Processing"; run.Attempts++; run.LastError=null; await db.SaveChangesAsync(ct);
            try
            {
                Directory.CreateDirectory(directory);
                await RunTool("pg_dump",["--format=custom","--no-owner","--no-acl","--file",temporary],ct);
                await RunTool("pg_restore",["--list",temporary],ct);
                File.Move(temporary,path,overwrite:true);
                run.Status="Completed"; run.Filename=filename; run.Retained=true; run.CompletedAt=DateTime.UtcNow;
                await db.SaveChangesAsync(ct);
                var expired=await db.BackupRuns.Where(x=>x.Status=="Completed" && x.Retained).OrderByDescending(x=>x.CompletedAt).ThenByDescending(x=>x.Id).Skip(7).ToListAsync(ct);
                foreach(var old in expired)
                {
                    if(old.Filename is null || Path.GetFileName(old.Filename)!=old.Filename) throw new InvalidOperationException("Invalid stored backup filename.");
                    File.Delete(Path.Combine(directory,old.Filename)); old.Retained=false;
                }
                await db.SaveChangesAsync(ct);
            }
            catch(Exception ex) when(ex is not OperationCanceledException)
            {
                if(run.Status!="Completed")
                {
                    try { File.Delete(temporary); } catch(IOException) { } catch(UnauthorizedAccessException) { }
                    run.Status="Failed"; run.Retained=false; run.CompletedAt=DateTime.UtcNow;
                    run.LastError="Backup failed. Previous successful archives were preserved. Check Worker logs and queue a new request.";
                    await db.SaveChangesAsync(ct);
                }
                logger.LogWarning("Backup/retention failed for {BackupId}: {ErrorType}",run.Id,ex.GetType().Name);
            }
        }
        finally
        {
            await using var release=new NpgsqlCommand("SELECT pg_advisory_unlock(23204)",connection);
            await release.ExecuteScalarAsync(CancellationToken.None);
        }
    }
    private async Task RunTool(string tool,IReadOnlyList<string> args,CancellationToken ct)
    {
        var settings=new NpgsqlConnectionStringBuilder(config.GetConnectionString("Shop"));
        var start=new ProcessStartInfo(tool) { UseShellExecute=false,RedirectStandardError=true,RedirectStandardOutput=true };
        foreach(var arg in args) start.ArgumentList.Add(arg);
        start.Environment["PGHOST"]=settings.Host; start.Environment["PGPORT"]=settings.Port.ToString(System.Globalization.CultureInfo.InvariantCulture);
        start.Environment["PGUSER"]=settings.Username; start.Environment["PGDATABASE"]=settings.Database; start.Environment["PGPASSWORD"]=settings.Password;
        using var process=Process.Start(start) ?? throw new InvalidOperationException("Could not start backup tool.");
        var error=process.StandardError.ReadToEndAsync(ct); var output=process.StandardOutput.ReadToEndAsync(ct);
        using var timeout=CancellationTokenSource.CreateLinkedTokenSource(ct); timeout.CancelAfter(TimeSpan.FromMinutes(10));
        try { await process.WaitForExitAsync(timeout.Token); await Task.WhenAll(error,output); }
        catch(OperationCanceledException) when(!ct.IsCancellationRequested)
        {
            if(!process.HasExited) process.Kill(entireProcessTree:true);
            throw new TimeoutException($"{tool} exceeded the backup time limit.");
        }
        catch { if(!process.HasExited) process.Kill(entireProcessTree:true); throw; }
        if(process.ExitCode!=0) throw new InvalidOperationException($"{tool} failed (exit {process.ExitCode}).");
    }
}
