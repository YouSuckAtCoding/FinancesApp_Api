using Serilog.Events;
using Serilog.Formatting;
using Serilog.Formatting.Compact;

namespace FinancesApp_Api.StartUp;

public class CommaDelimitedJsonFormatter : ITextFormatter
{
    private readonly CompactJsonFormatter _inner = new();

    public void Format(LogEvent logEvent, TextWriter output)
    {
        _inner.Format(logEvent, output);
        output.Write(",");
    }
}
