using Microsoft.Extensions.Logging;
using Sonar.Setup.Services.Options;

namespace Sonar.Setup.Services.NonInteractive;

internal sealed class NonInteractiveSetupService(ILogger<NonInteractiveSetupService> logger, HttpClient httpClient) : SetupService(httpClient)
{
    public override ValueTask<SetupOptions> InitializeAsync(CancellationToken cancellationToken)
    {
        if (!IsAdministrator())
        {
            logger.LogWarning("Sonar is not running as Administrator. Security channel may not be consumed.");
        }
        
        return ValueTask.FromResult(new SetupOptions(Interactive: false, Online: false, (in _, in _) => true));
    }
}