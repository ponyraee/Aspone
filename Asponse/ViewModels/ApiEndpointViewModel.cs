using Asponse.ViewModels.API;
using RestSharp;
using RestSharp.Serializers.NewtonsoftJson;

namespace Asponse.ViewModels;

public class ApiEndpointViewModel
{
    public EpicGamesApiEndpoint EpicGamesApi;

    private readonly RestClient _client = new(new RestClientOptions
    {
        UserAgent = "Asponse",
        Timeout = TimeSpan.FromSeconds(5)
    }, configureSerialization: s => s.UseSerializer<JsonNetSerializer>());
    
    public ApiEndpointViewModel()
    {
        EpicGamesApi = new EpicGamesApiEndpoint(_client);
    }
}