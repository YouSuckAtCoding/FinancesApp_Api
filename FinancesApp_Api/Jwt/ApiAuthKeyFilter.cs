using Microsoft.AspNetCore.Authentication.OAuth;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;

namespace FinancesApp_Api.Jwt;

public class ApiAuthKeyFilter : IAuthorizationFilter
{
    private readonly IConfiguration _configuration;
    private const string ApiKeyHeaderName = "x-api-key";
    public ApiAuthKeyFilter(IConfiguration configuration)
    {
        _configuration = configuration;
    }

    public void OnAuthorization(AuthorizationFilterContext context)
    {
        if (!context.HttpContext.Request.Headers.TryGetValue(ApiKeyHeaderName, 
                                                             out var extractedApiKey))
        {
            context.Result = new UnauthorizedObjectResult("API key is missing");
            return;
        }

        var apikey = _configuration["ApiKey"]!;
        if (apikey != extractedApiKey)
        {
            context.Result = new UnauthorizedObjectResult("API key is missing");

        }


    }
}

