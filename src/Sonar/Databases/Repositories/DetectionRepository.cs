using System.Runtime.CompilerServices;
using System.Threading.Tasks.Dataflow;
using Microsoft.Data.Sqlite;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Sonar.Databases.Contexts;
using Sonar.Detections;
using Sonar.Extensions;
using Sonar.Helpers;
using Sonar.Rules;
using Sonar.Rules.Extensions;

namespace Sonar.Databases.Repositories;

internal sealed class DetectionRepository : IDetectionRepository
{
    private readonly ILogger<DetectionRepository> logger;
    private readonly DetectionContext detectionContext;
    private readonly DataFlowHelper.PeriodicBlock<RuleMatch> detectionBlock;
    private readonly IDisposable subscription;

    private const string Insertion = @"INSERT OR IGNORE INTO Rules (Value, Title, Link) VALUES (@RuleGuid, @RuleTitle, @RuleLink);
INSERT OR IGNORE INTO Levels (Value) VALUES (@LevelName);
INSERT OR IGNORE INTO Computers (Value) VALUES (@ComputerName);
INSERT INTO Detections (Id, Date, ComputerId, LevelId, RuleId, Details) VALUES (@Id, @Date, (SELECT Id FROM Computers WHERE Value = @ComputerName), (SELECT Id FROM Levels WHERE Value = @LevelName), (SELECT Id FROM Rules WHERE Value = @RuleGuid), @DetailsValue);";

    private long id;

    public DetectionRepository(ILogger<DetectionRepository> logger, IHostApplicationLifetime applicationLifetime, DetectionContext detectionContext)
    {
        this.logger = logger;
        this.detectionContext = detectionContext;
        detectionBlock = CreateDetectionBlock(applicationLifetime.ApplicationStopping, out var detectionLink);
        subscription = detectionLink;
    }

    public async ValueTask InitializeAsync(CancellationToken cancellationToken)
    {
        detectionContext.CreateTables();
        using var connection = await detectionContext.CreateConnectionAsync(cancellationToken);
        await connection.DbConnection.OpenAsync(cancellationToken);
        await LoadPrimaryKeyAsync(connection.DbConnection, cancellationToken);
    }

    public async ValueTask InsertAsync(RuleMatch match, CancellationToken cancellationToken)
    {
        if (!await detectionBlock.SendAsync(match, cancellationToken))
        {
            logger.Throttle(nameof(DetectionRepository), itself => itself.LogError("Could not post detection"), expiration: TimeSpan.FromMinutes(1));
        }
    }

    public async ValueTask<IDictionary<DateTime, IDictionary<DetectionSeverity, long>>> GetSeveritiesByDateAsync(TimeSpan since, CancellationToken cancellationToken)
    {
        var values = new Dictionary<DateTime, IDictionary<DetectionSeverity, long>>();
        var today = DateTime.Today;
        try
        {
            await using var connection = detectionContext.CreateConnection();
            await connection.OpenAsync(cancellationToken);
            await using var command = connection.CreateCommand();
            var startDate = today.Subtract(since);
            var endDate = today;
            for (var date = startDate; date <= endDate; date = date.AddDays(1))
            {
                var detectionCountBySeverity = new Dictionary<DetectionSeverity, long>();
                foreach (var severity in Enum.GetValues<DetectionSeverity>())
                {
                    command.CommandText = $@"SELECT COUNT(*)
FROM Detections AS D INDEXED BY idx_detections_computer_level_rule_date
INNER JOIN Computers AS C ON C.Id = D.ComputerId
INNER JOIN Levels AS L ON L.Id = D.LevelId
INNER JOIN Rules AS R ON R.Id = D.RuleId
WHERE D.ComputerId IN (SELECT Id FROM Computers WHERE 1 = 1)
AND D.LevelId IN (SELECT Id FROM Levels WHERE Value = '{Enum.GetName(severity)}')
AND D.RuleId IN (SELECT Id FROM Rules WHERE 1 = 1)
AND D.Date >= {date.Ticks} AND D.Date < {date.AddDays(1).Ticks};";

                    using var cts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
                    cts.CancelAfter(TimeSpan.FromSeconds(10));
                    await using var reader = await command.ExecuteReaderAsync(cts.Token);
                    if (await reader.ReadAsync(cts.Token))
                    {
                        detectionCountBySeverity[severity] = reader.GetInt64(0);
                    }
                    else
                    {
                        detectionCountBySeverity[severity] = 0L;
                    }
                }

                values[date] = detectionCountBySeverity;
            }
        }
        catch (OperationCanceledException)
        {
            logger.LogWarning("Cancellation has occurred");
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "An error has occurred");
        }

        return values;
    }

    public async ValueTask<IDictionary<string, IDictionary<DetectionSeverity, long>>> GetTopComputersAsync(int limit, CancellationToken cancellationToken)
    {
        var values = new Dictionary<string, IDictionary<DetectionSeverity, long>>();
        try
        {
            await using var connection = detectionContext.CreateConnection();
            await connection.OpenAsync(cancellationToken);
            await using var command = connection.CreateCommand();

            command.CommandText = $@"SELECT C.Value, L.Value, COUNT(D.Id) AS Count
FROM Detections AS D INDEXED BY idx_detections_computer_level_rule_date
INNER JOIN Computers AS C ON D.ComputerId = C.Id
INNER JOIN Levels AS L ON D.LevelId = L.Id
WHERE D.ComputerId IN (SELECT Id FROM Computers WHERE 1 = 1 LIMIT {limit}) AND D.LevelId IN (SELECT Id FROM Levels WHERE 1 = 1) AND D.RuleId IN (SELECT Id FROM Rules WHERE 1 = 1)
GROUP BY C.Value, L.Value
ORDER BY D.Date DESC;";

            using var cts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
            cts.CancelAfter(TimeSpan.FromSeconds(10));
            await using var reader = await command.ExecuteReaderAsync(cts.Token);
            while (await reader.ReadAsync(cts.Token))
            {
                var computer = reader.GetString(0);
                var level = reader.GetString(1);
                var count = reader.GetInt64(2);
                if (values.TryGetValue(computer, out var severities))
                {
                    severities.TryAdd(level.FromLevel(), count);
                }
                else
                {
                    values.TryAdd(computer, new Dictionary<DetectionSeverity, long>
                    {
                        { level.FromLevel(), count }
                    });
                }
            }
        }
        catch (OperationCanceledException)
        {
            logger.LogWarning("Cancellation has occurred");
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "An error has occurred");
        }

        return values;
    }

    public async ValueTask<IDictionary<string, Tuple<DetectionSeverity, long>>> GetRulesAsync(CancellationToken cancellationToken)
    {
        var values = new Dictionary<string, Tuple<DetectionSeverity, long>>();
        try
        {
            await using var connection = detectionContext.CreateConnection();
            await connection.OpenAsync(cancellationToken);
            await using var command = connection.CreateCommand();
            command.CommandText = @"WITH CTE AS (
SELECT D.RuleId AS RuleId, D.LevelId AS LevelId, COUNT(D.Id) AS Count
FROM Detections AS D INDEXED BY idx_detections_computer_level_rule_date
WHERE D.ComputerId IN (SELECT Id FROM Computers WHERE 1 = 1) AND D.LevelId IN (SELECT Id FROM Levels WHERE 1 = 1) AND D.RuleId IN (SELECT Id FROM Rules WHERE 1 = 1)
GROUP BY D.ComputerId, D.LevelId, D.RuleId
)

SELECT R.Title, L.Value, CTE.Count
FROM Rules AS R
INNER JOIN CTE ON CTE.RuleId = R.Id
INNER JOIN Levels AS L ON L.Id = CTE.LevelId;";

            using var cts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
            cts.CancelAfter(TimeSpan.FromSeconds(10));
            await using var reader = await command.ExecuteReaderAsync(cts.Token);
            while (await reader.ReadAsync(cts.Token))
            {
                var rule = reader.GetString(0);
                var level = reader.GetString(1).FromLevel();
                var count = reader.GetInt64(2);
                values.Add(rule, new Tuple<DetectionSeverity, long>(level, count));
            }
        }
        catch (OperationCanceledException)
        {
            logger.LogWarning("Cancellation has occurred");
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "An error has occurred");
        }

        return values;
    }

    public async IAsyncEnumerable<DetectionExport> EnumerateDetectionsByRuleTitleAsync(string rule, [EnumeratorCancellation] CancellationToken cancellationToken)
    {
        await using var connection = detectionContext.CreateConnection();
        await connection.OpenAsync(cancellationToken);
        await using var command = connection.CreateCommand();
        command.Parameters.AddWithValue("Title", rule);
        command.CommandText = @"SELECT D.Date AS Date, C.Value AS Computer, L.Value AS Level, R.Link AS Link, D.Details AS Details
FROM Detections AS D INDEXED BY idx_detections_computer_level_rule_date
INNER JOIN Computers AS C ON D.ComputerId = C.Id
INNER JOIN Levels AS L ON D.LevelId = L.Id
INNER JOIN Rules AS R ON D.RuleId = R.Id
WHERE D.ComputerId IN (SELECT Id FROM Computers WHERE 1 = 1) AND D.LevelId IN (SELECT Id FROM Levels WHERE 1 = 1) AND D.RuleId IN (SELECT Id FROM Rules WHERE Title = @Title)
ORDER BY Date DESC;";

        using var cts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
        cts.CancelAfter(TimeSpan.FromSeconds(10));
        await using var reader = await command.ExecuteReaderAsync(cts.Token);
        while (await reader.ReadAsync(cts.Token))
        {
            var date = reader.GetInt64(0);
            var computer = reader.GetString(1);
            var level = reader.GetString(2);
            var link = reader.GetString(3);
            var details = reader.GetString(4);
            yield return new DetectionExport(new DateTimeOffset(date, TimeSpan.Zero).ToLocalTime().ToString("G"), computer, level, rule, link, FormatDetails(details));
        }
    }
    
    public async IAsyncEnumerable<DetectionExport> EnumerateDetectionsAsync(IEnumerable<DetectionSeverity> severities, [EnumeratorCancellation] CancellationToken cancellationToken)
    {
        await using var connection = detectionContext.CreateConnection();
        await connection.OpenAsync(cancellationToken);
        await using var command = connection.CreateCommand();
        command.Parameters.AddRange(severities.Select((severity, index) => new SqliteParameter("@s" + index, Enum.GetName(severity))));
        command.CommandText = $@"SELECT D.Date AS Date, C.Value AS Computer, L.Value AS Level, R.Title AS Title, R.Link AS Link, D.Details AS Details
FROM Detections AS D INDEXED BY idx_detections_computer_level_rule_date
INNER JOIN Computers AS C ON D.ComputerId = C.Id
INNER JOIN Levels AS L ON D.LevelId = L.Id
INNER JOIN Rules AS R ON D.RuleId = R.Id
WHERE D.ComputerId IN (SELECT Id FROM Computers WHERE 1 = 1) AND D.LevelId IN (SELECT Id FROM Levels WHERE Value IN ({string.Join(",", severities.Select((_, index) => "@s" + index))})) AND D.RuleId IN (SELECT Id FROM Rules WHERE 1 = 1)
ORDER BY Date DESC;";

        using var cts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
        cts.CancelAfter(TimeSpan.FromSeconds(10));
        await using var reader = await command.ExecuteReaderAsync(cts.Token);
        while (await reader.ReadAsync(cts.Token))
        {
            var date = reader.GetInt64(0);
            var computer = reader.GetString(1);
            var rule = reader.GetString(2);
            var level = reader.GetString(3);
            var link = reader.GetString(4);
            var details = reader.GetString(5);
            yield return new DetectionExport(new DateTimeOffset(date, TimeSpan.Zero).ToLocalTime().ToString("G"), computer, level, rule, link, FormatDetails(details));
        }
    }

    public async IAsyncEnumerable<DetectionExport> EnumerateDetectionsByKeywordAsync(string keyword, [EnumeratorCancellation] CancellationToken cancellationToken)
    {
        await using var connection = detectionContext.CreateConnection();
        await connection.OpenAsync(cancellationToken);
        await using var command = connection.CreateCommand();
        command.Parameters.AddWithValue("Keyword", $"%{keyword}%");
        command.CommandText = @"SELECT D.Date AS Date, C.Value AS Computer, L.Value AS Level, R.Title AS Title, R.Link AS Link, D.Details AS Details
FROM Detections AS D INDEXED BY idx_detections_computer_level_rule_date
INNER JOIN Computers AS C ON D.ComputerId = C.Id
INNER JOIN Levels AS L ON D.LevelId = L.Id
INNER JOIN Rules AS R ON D.RuleId = R.Id
WHERE D.ComputerId IN (SELECT Id FROM Computers WHERE 1 = 1) AND D.LevelId IN (SELECT Id FROM Levels WHERE 1 = 1) AND D.RuleId IN (SELECT Id FROM Rules WHERE 1 = 1) AND Details LIKE @Keyword
ORDER BY Date DESC;";

        using var cts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
        cts.CancelAfter(TimeSpan.FromSeconds(10));
        await using var reader = await command.ExecuteReaderAsync(cts.Token);
        while (await reader.ReadAsync(cts.Token))
        {
            var date = reader.GetInt64(0);
            var computer = reader.GetString(1);
            var rule = reader.GetString(2);
            var level = reader.GetString(3);
            var link = reader.GetString(4);
            var details = reader.GetString(5);
            yield return new DetectionExport(new DateTimeOffset(date, TimeSpan.Zero).ToLocalTime().ToString("G"), computer, level, rule, link, FormatDetails(details));
        }
    }
    
    private static IDictionary<string, string> FormatDetails(string details)
    {
        return details.Split(Constants.Separator, StringSplitOptions.RemoveEmptyEntries).Select(value => Format(value.Trim().Split(": ", StringSplitOptions.RemoveEmptyEntries))).Where(kvp => !string.IsNullOrWhiteSpace(kvp.Key) && !string.IsNullOrWhiteSpace(kvp.Value)).ToDictionary();
        KeyValuePair<string, string> Format(string[] pair) => pair.Length == 2 ? new KeyValuePair<string, string>(pair[0], pair[1]) : new KeyValuePair<string, string>();
    }

    private async ValueTask InsertAsync(IList<RuleMatch> detections, CancellationToken cancellationToken)
    {
        try
        {
            if (detections.Count == 0) return;
            using var connection = await detectionContext.CreateConnectionAsync(cancellationToken);
            await connection.DbConnection.OpenAsync(cancellationToken);
            await using var transaction = connection.DbConnection.BeginTransaction();

            await using var command = connection.DbConnection.CreateCommand();
            command.CommandText = Insertion;

            var idParameter = command.Parameters.Add("Id", SqliteType.Integer);
            var date = command.Parameters.Add("Date", SqliteType.Integer);
            var ruleGuidParameter = command.Parameters.Add("RuleGuid", SqliteType.Text);
            var ruleTitleParameter = command.Parameters.Add("RuleTitle", SqliteType.Text);
            var ruleLinkParameter = command.Parameters.Add("RuleLink", SqliteType.Text);
            var levelNameParameter = command.Parameters.Add("LevelName", SqliteType.Text);
            var computerNameParameter = command.Parameters.Add("ComputerName", SqliteType.Text);
            var detailsValueParameter = command.Parameters.Add("DetailsValue", SqliteType.Text);
            foreach (var detection in detections)
            {
                if (string.IsNullOrWhiteSpace(detection.WinEvent.Computer)) continue;
                if (string.IsNullOrWhiteSpace(detection.DetectionDetails.Details)) continue;
                if (string.IsNullOrWhiteSpace(detection.DetectionDetails.RuleMetadata.Link)) continue;

                idParameter.Value = Interlocked.Increment(ref id);
                date.Value = detection.Date.Ticks;
                ruleGuidParameter.Value = detection.DetectionDetails.RuleMetadata.Id;
                ruleTitleParameter.Value = detection.DetectionDetails.RuleMetadata.Title;
                ruleLinkParameter.Value = detection.DetectionDetails.RuleMetadata.Link;
                levelNameParameter.Value = Enum.GetName(detection.DetectionDetails.RuleMetadata.Level.FromLevel());
                computerNameParameter.Value = detection.WinEvent.Computer;
                detailsValueParameter.Value = detection.DetectionDetails.Details;
                await command.ExecuteNonQueryAsync(cancellationToken);
            }

            await transaction.CommitAsync(cancellationToken);
        }
        catch (OperationCanceledException)
        {
            logger.LogWarning("Cancellation has occurred");
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "An error has occurred");
        }
    }

    private async ValueTask LoadPrimaryKeyAsync(SqliteConnection connection, CancellationToken cancellationToken)
    {
        try
        {
            await using var command = connection.CreateCommand();
            command.CommandText = "SELECT MAX(Id) FROM Detections;";
            await using var reader = await command.ExecuteReaderAsync(cancellationToken);
            if (await reader.ReadAsync(cancellationToken))
            {
                id = reader.IsDBNull(0) ? 0L : reader.GetInt64(0);
            }
            else
            {
                id = 0L;
            }
        }
        catch (OperationCanceledException)
        {
            logger.LogWarning("Cancellation has occurred");
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "An error has occurred");
        }
    }

    private DataFlowHelper.PeriodicBlock<RuleMatch> CreateDetectionBlock(CancellationToken cancellationToken, out IDisposable disposableLink)
    {
        var executionDataflow = new ExecutionDataflowBlockOptions
        {
            MaxDegreeOfParallelism = 1,
            SingleProducerConstrained = false,
            BoundedCapacity = 24, // enough to cover if a single write transaction takes up to 1 minute
            CancellationToken = cancellationToken
        };

        var periodicBlock = DataFlowHelper.CreatePeriodicBlock<RuleMatch>(TimeSpan.FromSeconds(1), count: 1000);
        var options = new DataflowLinkOptions { PropagateCompletion = true };
        var propagationBlock = new ActionBlock<IList<RuleMatch>>(async items => { await InsertAsync(items, cancellationToken); }, executionDataflow);
        disposableLink = periodicBlock.LinkTo(propagationBlock, options);
        return periodicBlock;
    }

    public async ValueTask DisposeAsync()
    {
        if (subscription is IAsyncDisposable subscriptionAsyncDisposable)
            await subscriptionAsyncDisposable.DisposeAsync();
        else
            subscription.Dispose();

        await detectionBlock.DisposeAsync();
    }
}