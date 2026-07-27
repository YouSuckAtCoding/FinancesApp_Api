using System.Net.Http.Json;
using Amazon.Lambda.APIGatewayEvents;
using Amazon.Lambda.Core;

// Use System.Text.Json for (de)serializing the Lambda event + response.
[assembly: LambdaSerializer(typeof(Amazon.Lambda.Serialization.SystemTextJson.DefaultLambdaJsonSerializer))]

namespace Gateway.Authorizer;

/// <summary>
/// HTTP API (payload format 2.0) REQUEST authorizer for the phantom-token pattern.
///
/// Per request the gateway hands us the cookies. We pull the opaque session ref out of the
/// X-Access-Token cookie, swap it for a real JWT at Identity.Api /exchange, and hand that JWT
/// back to the gateway in the authorizer context. The gateway's parameter mapping then injects
/// it as the Authorization header before proxying to the backend — so the backend keeps
/// validating JWTs exactly as it does today, and the browser never sees a token.
/// </summary>
public class Function
{
    private const string SessionCookieName = "X-Access-Token";

    // Reuse the connection pool across warm invocations — never new up HttpClient per call.
    private static readonly HttpClient Http = new();

    private static readonly string IdentityApiBaseUrl =
        Environment.GetEnvironmentVariable("IDENTITY_API_BASE_URL")
        ?? throw new InvalidOperationException("IDENTITY_API_BASE_URL not configured.");

    private static readonly string InternalSecret =
        Environment.GetEnvironmentVariable("INTERNAL_SECRET")
        ?? throw new InvalidOperationException("INTERNAL_SECRET not configured.");

    public async Task<SimpleAuthorizerResponse> FunctionHandler(APIGatewayHttpApiV2ProxyRequest request,
                                                                ILambdaContext context)
    {
        var sessionRef = ExtractSessionRef(request.Cookies);
        if (string.IsNullOrEmpty(sessionRef))
            return SimpleAuthorizerResponse.Deny();

        try
        {
            var jwt = await ExchangeReferenceForJwt(sessionRef, context);
            if (string.IsNullOrEmpty(jwt))
                return SimpleAuthorizerResponse.Deny();

            // The context value is what parameter mapping reads as $context.authorizer.authHeader.
            return SimpleAuthorizerResponse.Allow(authHeader: $"Bearer {jwt}");
        }
        catch (Exception ex)
        {
            // Any failure (Identity.Api down, bad ref, timeout) = deny. Never fail open.
            context.Logger.LogError($"Exchange failed: {ex.Message}");
            return SimpleAuthorizerResponse.Deny();
        }
    }

    /// <summary>Calls Identity.Api /exchange with the internal secret; returns the raw JWT body.</summary>
    private static async Task<string?> ExchangeReferenceForJwt(string sessionRef, ILambdaContext context)
    {
        using var httpRequest = new HttpRequestMessage(
            HttpMethod.Post,
            $"{IdentityApiBaseUrl.TrimEnd('/')}/exchange")
        {
            Content = JsonContent.Create(new { AccessToken = sessionRef })
        };
        httpRequest.Headers.Add("X-Internal-Secret", InternalSecret);

        using var response = await Http.SendAsync(httpRequest);

        if (!response.IsSuccessStatusCode)
        {
            context.Logger.LogWarning($"/exchange returned {(int)response.StatusCode}");
            return null;
        }

        return await response.Content.ReadAsStringAsync();
    }

    /// <summary>HTTP API 2.0 delivers cookies as a "name=value" string array.</summary>
    private static string? ExtractSessionRef(IList<string>? cookies)
    {
        if (cookies is null)
            return null;

        foreach (var cookie in cookies)
        {
            var separator = cookie.IndexOf('=');
            if (separator <= 0)
                continue;

            var name = cookie.AsSpan(0, separator).Trim();
            if (name.SequenceEqual(SessionCookieName))
                return cookie[(separator + 1)..].Trim();
        }

        return null;
    }
}
