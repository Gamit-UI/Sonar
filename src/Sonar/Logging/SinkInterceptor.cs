using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Serilog.Core;
using Serilog.Events;
using Spectre.Console;

namespace Sonar.Logging;

internal sealed class SinkInterceptor(IServiceScope scope) : ILogEventSink, IDisposable
{
    public void Emit(LogEvent logEvent)
    {
        if (!Environment.UserInteractive) return;
        var lifetime = scope.ServiceProvider.GetRequiredService<IHostApplicationLifetime>();
        if (!lifetime.ApplicationStopping.IsCancellationRequested) return;
        if (logEvent.Level is LogEventLevel.Error or LogEventLevel.Fatal)
        {
            AnsiConsole.MarkupLine($"[white on darkred_1]{logEvent.RenderMessage()}[/]");
            var path = new TextPath(Path.Join(AppContext.BaseDirectory, "Logs", "Sonar.log"))
                .SeparatorColor(Color.Blue)
                .LeafColor(Color.Red);
            var panel = new Panel(path);
            panel.Header("Log File").HeavyBorder();
            AnsiConsole.Write(panel);
        }
    }

    public void Dispose()
    {
        scope.Dispose();
    }
}