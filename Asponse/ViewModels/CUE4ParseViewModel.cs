using System.Diagnostics;
using CUE4Parse_Conversion.Textures;
using CUE4Parse.Compression;
using CUE4Parse.FileProvider;
using CUE4Parse.MappingsProvider;
using CUE4Parse.UE4.Assets.Exports.Sound;
using CUE4Parse.UE4.Assets.Exports.Texture;
using CUE4Parse.UE4.Readers;
using CUE4Parse.UE4.Versions;
using CUE4Parse.UE4.VirtualFileSystem;
using CUE4Parse.Utils;
using EpicManifestParser;
using GenericReader;
using K4os.Compression.LZ4.Streams;
using Serilog;
using static Asponse.Services.ApplicationServices;

namespace Asponse.ViewModels;

public class CUE4ParseViewModel
{
    public StreamedFileProvider Provider = new StreamedFileProvider("FortniteGame", true, new VersionContainer(EGame.GAME_UE5_5));
    public List<VfsEntry> NewFiles = [];
    
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
        await LoadMappings().ConfigureAwait(false);
        await LoadNewFiles().ConfigureAwait(false);
    }
    
    private async Task LoadMappings()
    {
        var mappings = await ApiVM.FortniteCentralApi.GetMappingsAsync().ConfigureAwait(false);
        string mappingsPath;

        if (mappings?.Length <= 0)
        {
            Log.Warning("Response from FortniteCentral was invalid. Trying to find saved mappings");

            var savedMappings = new DirectoryInfo(DataDirectory).GetFiles("*.usmap");
            if (savedMappings.Length <= 0)
            {
                Log.Error("Failed to find saved mappings");
                return;
            }

            mappingsPath = savedMappings.OrderBy(f => f.LastWriteTimeUtc).First().FullName;
        }
        else
        {
            Log.Information("Downloading {name}", mappings[0].FileName);
            mappingsPath = Path.Combine(DataDirectory, mappings[0].FileName);
            await ApiVM.DownloadFileAsync(mappings[0].Url, mappingsPath);
            Log.Information("Downloaded {name} at {path}", mappings[0].FileName, mappingsPath);
        }

        Provider.MappingsContainer = new FileUsmapTypeMappingsProvider(mappingsPath);
        Log.Information("Mappings pulled from {path}", mappingsPath);
    }
    
    private async Task LoadNewFiles()
    {
        await BackupVM.DownloadBackup();
        var backupPath = BackupVM.GetBackup();

        var stopwatch = Stopwatch.StartNew();

        await using var fileStream = new FileStream(backupPath, FileMode.Open);
        await using var memoryStream = new MemoryStream();
        using var reader = new GenericStreamReader(fileStream);

        if (reader.Read<uint>() == 0x184D2204u)
        {
            reader.Position -= 4;
            await using var compressionMethod = LZ4Stream.Decode(fileStream);
            await compressionMethod.CopyToAsync(memoryStream).ConfigureAwait(false);
        }
        else await fileStream.CopyToAsync(memoryStream).ConfigureAwait(false);

        memoryStream.Position = 0;
        await using var archive = new FStreamArchive(fileStream.Name, memoryStream);

        var paths = new Dictionary<string, int>();
        while (archive.Position < archive.Length)
        {
            archive.Position += 29;
            paths[archive.ReadString().ToLower()[1..]] = 0;
            archive.Position += 4;
        }

        foreach (var (key, value) in Provider.Files)
        {
            if (value is not VfsEntry entry || paths.ContainsKey(key) || entry.Path.EndsWith(".uexp") ||
                entry.Path.EndsWith(".ubulk") || entry.Path.EndsWith(".uptnl")) continue;

            NewFiles.Add(entry);
        }
        
        stopwatch.Stop();
        Log.Information("Loaded {files} new files", NewFiles.Count);
    }

    public async Task ExportFiles()
    {
        foreach (var newFile in NewFiles)
        {
            if (newFile.Extension != "uasset" || newFile.PathWithoutExtension.SubstringBefore("/") == "Engine")
                continue;
            
            var fileUObject = await Provider.TryLoadObjectAsync(newFile.PathWithoutExtension + "." + newFile.NameWithoutExtension).ConfigureAwait(false);
            
            switch (fileUObject)
            {
                case UTexture texture:
                {
                    var decodedIcon = texture.Decode();
                    var encodedIcon = decodedIcon?.Encode(ETextureFormat.Png, 100);
                    var exportPath = Path.Combine(ExportDirectory, $"{texture.Name}.png");

                    await File.WriteAllBytesAsync(exportPath, encodedIcon?.ToArray()).ConfigureAwait(false);
                    
                    Log.Information("Export {1} at: {2}", texture.Name, exportPath);
                    continue;
                }
                case USoundWave wave:
                {
                    Log.Information("This is a usound: {1} {2}", fileUObject.Name, fileUObject.Class);
                    continue;
                }
                default:
                    continue;
            }
        }
    }
}