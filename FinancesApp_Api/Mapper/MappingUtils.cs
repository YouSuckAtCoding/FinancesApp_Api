using System.Globalization;

namespace FinancesApp_Api.Mapper;

public class MappingUtils
{
    public static DateTimeOffset? ParseToDateTimeOffset(string input)
    {
        if (string.IsNullOrWhiteSpace(input))
            return null;

        if (DateTimeOffset.TryParse(input,
                CultureInfo.InvariantCulture,
                DateTimeStyles.RoundtripKind,
                out DateTimeOffset result))
            return result;

        return null;
    }
}
