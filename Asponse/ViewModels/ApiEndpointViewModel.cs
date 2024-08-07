using Asponse.Framework;
using Asponse.ViewModels.API;
using RestSharp;
using RestSharp.Serializers.NewtonsoftJson;
using Serilog;

namespace Asponse.ViewModels;

public class ApiEndpointViewModel
{
    public EpicGamesApiEndpoint EpicGamesApi;
    public FortniteCentralApiEndpoint FortniteCentralApi;
    public FModelApiEndpoint FModelApi;

    private readonly RestClient _client = new(new RestClientOptions
    {
        UserAgent = "Asponse",
        Timeout = TimeSpan.FromSeconds(5)
    }, configureSerialization: s => s.UseSerializer<JsonNetSerializer>());
    
    public ApiEndpointViewModel()
    {
        EpicGamesApi = new EpicGamesApiEndpoint(_client);
        FortniteCentralApi = new FortniteCentralApiEndpoint(_client);
        FModelApi = new FModelApiEndpoint(_client);
    }
    
    public async Task DownloadFileAsync(string url, string installationLocation)
    {
        if (File.Exists(installationLocation))
            return;
        
        var request = new FRestRequest(url);
        var data = await _client.DownloadDataAsync(request).ConfigureAwait(false);
        if (data?.Length <= 0)
        {
            Log.Error("An error occured while downloading the file");
            return;
        }

        await File.WriteAllBytesAsync(installationLocation, data!).ConfigureAwait(false);
    }
}