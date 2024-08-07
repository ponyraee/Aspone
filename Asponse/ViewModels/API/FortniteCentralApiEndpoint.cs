using Asponse.Framework;
using Asponse.ViewModels.API.Models;
using RestSharp;
using Serilog;

namespace Asponse.ViewModels.API;

public class FortniteCentralApiEndpoint : AbstractApiProvider
{
    private const string MAPPINGS_URL = "https://fortnitecentral.genxgames.gg/api/v1/mappings";

    public FortniteCentralApiEndpoint(RestClient client)  : base(client) { }

    public async Task<MappingsResponse[]> GetMappingsAsync()
    {
        var request = new FRestRequest(MAPPINGS_URL);
        var response = await _client.ExecuteAsync<MappingsResponse[]>(request);
        Log.Information("[{Method}] [{Status}({StatusCode})] '{Resource}'", request.Method, response.StatusDescription, (int) response.StatusCode, request.Resource);
        return (response is { IsSuccessful: true, Data: not null } ? response.Data : [new MappingsResponse()]) ?? throw new NullReferenceException();
    }
}