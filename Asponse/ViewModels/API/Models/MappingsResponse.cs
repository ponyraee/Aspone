using J = Newtonsoft.Json.JsonPropertyAttribute;

namespace Asponse.ViewModels.API.Models;

public class MappingsResponse
{
    [J("url")] public string Url { get; set; }
    [J("fileName")] public string FileName { get; set; }
}