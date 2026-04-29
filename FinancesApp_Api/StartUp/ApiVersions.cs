using Asp.Versioning;

namespace FinancesApp_Api.StartUp;

public static class ApiVersions
{
    public const string V1 = "1.0";

    public const string RoutePrefix = "v{version:apiVersion}";

    public static readonly ApiVersion Current = new(1, 0);
}
