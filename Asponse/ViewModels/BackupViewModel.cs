using Serilog;
using static Asponse.Services.ApplicationServices;

namespace Asponse.ViewModels;

public class BackupViewModel
{
    public async Task DownloadBackup()
    {
        // Downloads the latest backup
        var backups = await ApiVM.FModelApi.GetBackupsAsync();
        var backupPath = Path.Combine(DataDirectory, backups![4].FileName);
        Log.Information("Downloading {name}", backups[4].FileName);
        await ApiVM.DownloadFileAsync(backups[4].DownloadUrl, Path.Combine(DataDirectory, backups[4].FileName));
        Log.Information("Downloaded {name} at {path}", backups[4].FileName, backupPath);
    }

    public string GetBackup()
    {
        var backupPath = new DirectoryInfo(DataDirectory).GetFiles("*.fbkp");
        return backupPath.OrderByDescending(f => f.LastWriteTimeUtc).First().FullName;
    }
}