using Microsoft.Identity.Client;

namespace FinancesApp_Api.Endpoints;

public static class UserEndpoints
{
    public const string Base = "/api/user";
    
    public const string GetAll = "/api/user";

    public const string Get = $"{Base}/get/{{userId}}";
    
    public const string GetByEmail = $"{Base}/get/{{userEmail}}";
    
    public const string GetUserRegisteredAfter = $"{Base}/get/{{date}}";
    
    public const string Create = $"{Base}/create";

    public const string Update = $"{Base}/update/{{userId}}";    

}
