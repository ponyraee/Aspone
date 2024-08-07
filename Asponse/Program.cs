using static Asponse.Services.ApplicationServices;

namespace Asponse;

public static class Program
{
    public static async Task Main(string[] args)
    {
        await Initialize().ConfigureAwait(false);
        await CUE4ParseVM.Initialize();
        await CUE4ParseVM.ExportFiles();
    }
}