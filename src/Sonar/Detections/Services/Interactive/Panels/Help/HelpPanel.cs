using System.Reactive;
using Spectre.Console;
using Spectre.Console.Rendering;

namespace Sonar.Detections.Services.Interactive.Panels.Help;

internal sealed class HelpPanel : InteractivePanel
{
    private readonly Table table = new Table()
        .Expand()
        .RoundedBorder()
        .ShowRowSeparators()
        .AddColumn(new TableColumn(new Markup("[bold]Key[/]")).Centered())
        .AddColumn(new TableColumn(new Markup("[bold]Description[/]")).LeftAligned())
        .AddRow("D", "Display detections")
        .AddRow("S", "Display statistics")
        .AddRow("E", "Display exports")
        .AddRow("H", "Display help")
        .AddRow("Ctrl+C", "Exit");
    
    protected override ValueTask<IRenderable> BuildLayoutAsync(CancellationToken cancellationToken)
    {
        AnsiConsole.Write(new Rule("[blue]Help[/]").LeftJustified());
        return ValueTask.FromResult<IRenderable>(table);
    }

    public override void OnTick(Unit value)
    {
        table.UpdateCell(0, 0, "D");
        table.UpdateCell(0, 1, "Display detections");
        
        table.UpdateCell(1, 0, "S");
        table.UpdateCell(1, 1, "Display statistics");
        
        table.UpdateCell(2, 0, "E");
        table.UpdateCell(2, 1, "Display exports");
        
        table.UpdateCell(3, 0, "H");
        table.UpdateCell(3, 1, "Display help");
        
        table.UpdateCell(4, 0, "Ctrl+C");
        table.UpdateCell(4, 1, "Exit");
    }
}