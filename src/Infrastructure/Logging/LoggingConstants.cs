namespace Infrastructure.Logging;

/// <summary>
/// Well-known keys used when enriching Serilog's LogContext (configured in API/Program.cs)
/// so every log line for a request can be correlated together.
/// </summary>
public static class LoggingConstants
{
    public const string CorrelationIdHeader = "X-Correlation-Id";

    public const string CorrelationIdProperty = "CorrelationId";
}
