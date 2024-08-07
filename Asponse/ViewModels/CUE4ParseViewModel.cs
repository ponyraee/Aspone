using CUE4Parse.Compression;
using CUE4Parse.FileProvider;
using CUE4Parse.UE4.Readers;
using CUE4Parse.UE4.Versions;
using EpicManifestParser;
using Serilog;
using static Asponse.Services.ApplicationServices;

namespace Asponse.ViewModels;

public class CUE4ParseViewModel
{
    public StreamedFileProvider Provider = new StreamedFileProvider("FortniteGame", true, new VersionContainer(EGame.GAME_UE5_5));
    
    public async Task Initialize()
    {
        var manifestInfo = await ApiVM.EpicGamesApi.GetManifestAsync().ConfigureAwait(false);
        
        var manifestOptions = new ManifestParseOptions
        {
            ManifestCacheDirectory = ManifestCacheDirectory,
            ChunkCacheDirectory = ChunkCacheDirectory,
            Zlibng = ZlibHelper.Instance,
            ChunkBaseUrl = "http://epicgames-download1.akamaized.net/Builds/Fortnite/CloudDir/",
        };

        var (manifest, _) = await manifestInfo.DownloadAndParseAsync(manifestOptions).ConfigureAwait(false);

        foreach (var fileManifest in manifest.FileManifestList)
        {
            if (fileManifest.FileName != "FortniteGame/Content/Paks/global.utoc" && 
                fileManifest.FileName != "FortniteGame/Content/Paks/pakchunk10-WindowsClient.utoc")
                continue;
            
            Provider.RegisterVfs(fileManifest.FileName, [fileManifest.GetStream()],
                it => new FStreamArchive(it, manifest.FileManifestList.First(x => x.FileName.Equals(it)).GetStream(),
                    Provider.Versions));
            
            Log.Information("Downloaded {fileName}", fileManifest.FileName);
        }

        await Provider.MountAsync().ConfigureAwait(false);
    }
}