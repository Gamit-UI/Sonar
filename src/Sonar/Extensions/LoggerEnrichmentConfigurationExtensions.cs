using Serilog;
using Serilog.Configuration;
using Sonar.Logging;

namespace Sonar.Extensions;

internal static class LoggerEnrichmentConfigurationExtensions
{
    extension(LoggerEnrichmentConfiguration enrichmentConfiguration)
    {
        public LoggerConfiguration WithAssemblyVersion()
        {
            var version = typeof(LoggerEnrichmentConfigurationExtensions).Assembly.GetVersion();
            return enrichmentConfiguration.WithProperty(nameof(Version), version);
        }

        public LoggerConfiguration WithSourceContext()
        {
            return enrichmentConfiguration.With<SourceContextEnricher>();
        }
    }
}