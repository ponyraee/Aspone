using Asponse.ViewModels;
using CUE4Parse.Compression;
using Serilog;
using Serilog.Sinks.SystemConsole.Themes;

namespace Asponse.Services;

public static class ApplicationServices
{
    private static readonly string OutputDirectory = Path.Combine(Environment.CurrentDirectory, "Output");
    public static readonly string DataDirectory = Path.Combine(OutputDirectory, ".data");
    public static readonly string ManifestCacheDirectory = Path.Combine(DataDirectory, "Manifest Cache");
    public static readonly string ChunkCacheDirectory = Path.Combine(DataDirectory, "Chunk Cache");

    public static readonly ApiEndpointViewModel ApiVM = new ApiEndpointViewModel();
    public static readonly CUE4ParseViewModel CUE4ParseVM = new CUE4ParseViewModel();
    
    public static async Task Initialize()
    {
#if !DEBUG
        Log.Logger = new LoggerConfiguration()
            .WriteTo.Console(theme: SystemConsoleTheme.Literate)
            .CreateLogger();
#else
        Log.Logger = new LoggerConfiguration()
            .WriteTo.Console(theme: SystemConsoleTheme.Literate)
            .MinimumLevel.Debug()
            .CreateLogger();
#endif

        foreach (var directory in new[] { OutputDirectory, DataDirectory, ManifestCacheDirectory, ChunkCacheDirectory })
        {
            if (!Directory.Exists(directory))
                Directory.CreateDirectory(directory);
        }
        
        await InitOodle().ConfigureAwait(false);
        await InitZlib().ConfigureAwait(false);
    }

    private static async Task InitOodle()
    {
        var oodlePath = Path.Combine(DataDirectory, OodleHelper.OODLE_DLL_NAME);
        if (!File.Exists(oodlePath)) await OodleHelper.DownloadOodleDllAsync(oodlePath).ConfigureAwait(false);
        OodleHelper.Initialize(oodlePath);
    }

    private static async Task InitZlib()
    {
        var zlibPath = Path.Combine(DataDirectory, ZlibHelper.DLL_NAME);
        if (!File.Exists(zlibPath)) await ZlibHelper.DownloadDllAsync(zlibPath).ConfigureAwait(false);
        ZlibHelper.Initialize(zlibPath);
    }
}