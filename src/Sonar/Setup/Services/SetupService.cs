using System.Security.Principal;
using Sonar.Extensions;
using Sonar.Helpers;
using Sonar.Setup.Services.Options;

namespace Sonar.Setup.Services;

internal abstract class SetupService(HttpClient httpClient) : ISetupService
{
    public abstract ValueTask<SetupOptions> InitializeAsync(CancellationToken cancellationToken);
    
    protected static bool IsAdministrator()
    {
        using var identity = WindowsIdentity.GetCurrent();
        var principal = new WindowsPrincipal(identity);
        return principal.IsInRole(WindowsBuiltInRole.Administrator);
    }

    public async ValueTask<Stream> GetRulesConfigFilesAsync(SetupOptions setupOptions, IProgress<double> progress, CancellationToken cancellationToken)
    {
        if (!setupOptions.Online) return new ReadProgressStream(typeof(SetupService).Assembly.ReadFromEmbeddedResource("rules_config_files.txt"), progress, xorEncoded: false);
        var destination = new MemoryStream();
        await httpClient.DownloadAsync("https://raw.githubusercontent.com/Yamato-Security/hayabusa-encoded-rules/refs/heads/main/rules_config_files.txt", destination, xorEncoded: false, progress, cancellationToken);
        return destination;
    }

    public async ValueTask<Stream> GetEncodedRulesAsync(SetupOptions setupOptions, IProgress<double> progress, CancellationToken cancellationToken)
    {
        if (!setupOptions.Online) return new ReadProgressStream(typeof(SetupService).Assembly.ReadFromEmbeddedResource("encoded_rules.yml"), progress, xorEncoded: true);
        var destination = new MemoryStream();
        await httpClient.DownloadAsync("https://github.com/Yamato-Security/hayabusa-encoded-rules/raw/refs/heads/main/encoded_rules.yml", destination, xorEncoded: true, progress, cancellationToken);
        return destination;
    }
}