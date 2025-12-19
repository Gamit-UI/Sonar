using System.Reflection;
using System.Runtime.InteropServices;
using Microsoft.Extensions.Hosting;
using Sonar.Databases.Repositories;
using Sonar.Events.Processors;
using Sonar.Extensions;
using Sonar.Rules.Aggregations;
using Sonar.Rules.Aggregations.Interfaces;
using Sonar.Rules.Aggregations.Repositories;
using Sonar.Rules.Helpers;
using Sonar.Rules.Stores;
using Sonar.Setup.Services;
using Sonar.Setup.Services.Options;

namespace Sonar.Setup.Hosting;

internal abstract class SetupHostedService(ISetupService setupService, IDetectionRepository detectionRepository, IRuleStore ruleStore, IAggregationRepository aggregationRepository, IPropertyStore propertyStore, IEventProcessor eventProcessor) : IHostedService
{
    private const string SqliteNativeLibraryName = "e_sqlite3";
    private const string UioHookNativeLibraryName = "uiohook";
    private readonly string nativeLibraryFolder = Path.Join(Path.GetTempPath(), "Sonar");

    protected ValueTask InitializeDatabaseAsync(CancellationToken cancellationToken)
    {
        return detectionRepository.InitializeAsync(cancellationToken);
    }
    
    protected ValueTask<SetupOptions> InitializeSetupAsync(CancellationToken cancellationToken)
    {
        return setupService.InitializeAsync(cancellationToken);
    }

    protected void InitializeAggregator()
    {
        Aggregator.Instance = new Aggregator(aggregationRepository, propertyStore, maxEventsPerRule: 1024);
    }

    protected void InitializeEventProcessor()
    {
        eventProcessor.Initialize();
    }

    protected async ValueTask ExtractDependenciesAsync(SetupOptions setupOptions, CancellationToken cancellationToken)
    {
        async ValueTask SQLiteAsync()
        {
            await using var source = typeof(SetupHostedService).Assembly.ReadFromEmbeddedResource($"{SqliteNativeLibraryName}.dll");
            await using var fileStream = File.OpenWrite(Path.Join(Directory.CreateDirectory(nativeLibraryFolder).FullName, $"{SqliteNativeLibraryName}.dll"));
            await source.CopyToAsync(fileStream, cancellationToken);
        }
        
        async ValueTask UioHookAsync()
        {
            await using var source = typeof(SetupHostedService).Assembly.ReadFromEmbeddedResource($"{UioHookNativeLibraryName}.dll");
            await using var fileStream = File.OpenWrite(Path.Join(Directory.CreateDirectory(nativeLibraryFolder).FullName, $"{UioHookNativeLibraryName}.dll"));
            await source.CopyToAsync(fileStream, cancellationToken);
        }

        NativeLibrary.SetDllImportResolver(typeof(SQLitePCL.SQLite3Provider_e_sqlite3).Assembly, SqliteResolver);
        NativeLibrary.SetDllImportResolver(typeof(SharpHook.Native.UioHook).Assembly, UioHookResolver);

        await SQLiteAsync();
        if (setupOptions.Interactive)
        {
            await UioHookAsync();
        }
    }
    
    private IntPtr SqliteResolver(string libraryName, Assembly assembly, DllImportSearchPath? searchPath)
    {
        if (libraryName == SqliteNativeLibraryName && NativeLibrary.TryLoad(Path.Join(nativeLibraryFolder, $"{SqliteNativeLibraryName}.dll"), out var handle))
        {
            return handle;
        }
        
        return IntPtr.Zero;
    }
    
    private IntPtr UioHookResolver(string libraryName, Assembly assembly, DllImportSearchPath? searchPath)
    {
        if (libraryName == UioHookNativeLibraryName && NativeLibrary.TryLoad(Path.Join(nativeLibraryFolder, $"{UioHookNativeLibraryName}.dll"), out var handle))
        {
            return handle;
        }
        
        return IntPtr.Zero;
    }

    protected ValueTask LoadRuleConfigurationAsync(SetupOptions setupOptions, IProgress<double> progress, CancellationToken cancellationToken)
    {
        return ConfigHelper.InitializeAsync(setupService, setupOptions, progress, cancellationToken);
    }

    protected ValueTask<int> LoadRulesAsync(SetupOptions setupOptions, IProgress<double> progress, Action onBeforeCount, Action<int> onCount, CancellationToken cancellationToken)
    {
        return ruleStore.InitializeAsync(setupOptions, progress, onBeforeCount, onCount, cancellationToken);
    }

    public abstract Task StartAsync(CancellationToken cancellationToken);

    public Task StopAsync(CancellationToken cancellationToken)
    {
        return Task.CompletedTask;
    }
}