using Serilog.Core;
using Serilog.Events;

namespace TranscriptAnalyzer.POC.Infrastructure.Logging.Enrichers
{
    public sealed class RedactEnricher(string[] fields) : ILogEventEnricher
    {
        private readonly HashSet<string> _fields = [.. fields.Select(f => f.ToLowerInvariant())];
        public void Enrich(LogEvent logEvent, ILogEventPropertyFactory factory)
        {
            foreach (var prop in logEvent.Properties.ToList())
            {
                if (_fields.Contains(prop.Key.ToLowerInvariant()))
                {
                    logEvent.AddOrUpdateProperty(factory.CreateProperty(prop.Key, "***REDACTED***"));
                }
            }
        }
    }
}
