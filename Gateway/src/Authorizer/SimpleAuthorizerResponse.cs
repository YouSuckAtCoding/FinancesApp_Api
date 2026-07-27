using System.Text.Json.Serialization;

namespace Gateway.Authorizer;

/// <summary>
/// The "simple" response shape an HTTP API REQUEST authorizer returns when
/// EnableSimpleResponses = true. The gateway only looks at <see cref="IsAuthorized"/> to
/// allow/deny; <see cref="Context"/> is passed through and is readable in route/integration
/// mappings as $context.authorizer.&lt;key&gt;.
/// </summary>
public sealed class SimpleAuthorizerResponse
{
    [JsonPropertyName("isAuthorized")]
    public bool IsAuthorized { get; init; }

    [JsonPropertyName("context")]
    public Dictionary<string, string> Context { get; init; } = new();

    public static SimpleAuthorizerResponse Deny() => new() { IsAuthorized = false };

    public static SimpleAuthorizerResponse Allow(string authHeader) => new()
    {
        IsAuthorized = true,
        Context = { ["authHeader"] = authHeader }
    };
}
