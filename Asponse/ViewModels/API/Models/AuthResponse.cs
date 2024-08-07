using J = Newtonsoft.Json.JsonPropertyAttribute;

namespace Asponse.ViewModels.API.Models;

public class AuthResponse
{
    [J("access_token")] public string AccessToken { get; set; }
}