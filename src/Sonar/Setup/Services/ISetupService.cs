using Sonar.Setup.Services.Options;

namespace Sonar.Setup.Services;

internal interface ISetupService
{
    ValueTask<SetupOptions> InitializeAsync(CancellationToken cancellationToken);
    ValueTask<Stream> GetRulesConfigFilesAsync(SetupOptions setupOptions, IProgress<double> progress, CancellationToken cancellationToken);
    ValueTask<Stream> GetEncodedRulesAsync(SetupOptions setupOptions, IProgress<double> progress, CancellationToken cancellationToken);
}