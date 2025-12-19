using System.Collections.Concurrent;
using System.Diagnostics;
using System.Runtime.CompilerServices;
using Microsoft.Data.Sqlite;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace Sonar.Databases.Contexts;

internal abstract class ContextBase
{
    private sealed class Locker
    {
        public readonly object Lock = new();
        public readonly SemaphoreSlim SemaphoreSlim = new(1, 1);
        public readonly ConcurrentDictionary<int, SingleThreadedConnection> ConnectionsSync = new();
        public readonly ConcurrentDictionary<int, SingleThreadedConnection> ConnectionsAsync = new();
    }

    private sealed class MonitoredSqliteConnection(ILogger logger, string connectionString, string member) : SqliteConnection(connectionString)
    {
        private readonly Stopwatch watch = Stopwatch.StartNew();

        protected override void Dispose(bool disposing)
        {
            base.Dispose(disposing);
            if (watch.Elapsed >= ConnectionTimeWarning)
            {
                logger.LogWarning("[{Name}] Connection has been opened longer than '{Time}s'", member, ConnectionTimeWarning.TotalSeconds);
            }
        }
    }

    private static readonly Lock InitializationLocker = new();
    private static readonly ConcurrentDictionary<string, Locker> Lockers = new();
    private static readonly TimeSpan ConnectionTimeWarning = TimeSpan.FromSeconds(5);
    public static readonly string DbPath = Path.Join(AppContext.BaseDirectory, "Database");

    public string ConnectionString { get; }
    public ILogger Logger { get; }

    protected ContextBase(ILogger logger, IHostApplicationLifetime hostApplicationLifetime, string path, string fileName)
    {
        Logger = logger;
        try
        {
            lock (InitializationLocker)
            {
                if (!Directory.Exists(path))
                {
                    Directory.CreateDirectory(path);
                }

                var dbPath = Path.Join(path, fileName);
                ConnectionString = new SqliteConnectionStringBuilder
                {
                    DataSource = dbPath
                }.ToString();
            }
        }
        catch (Exception ex)
        {
            ConnectionString = string.Empty;
            logger.LogCritical(ex, "Could not setup database context");
            hostApplicationLifetime.StopApplication();
        }
    }

    public abstract void CreateTables();

    public SingleThreadedConnection CreateSingleConnection([CallerMemberName] string member = "")
    {
        var locker = Lockers.GetOrAdd(ConnectionString, valueFactory: _ => new Locker());
        Monitor.Enter(locker.Lock);
        const int key = 1;
        var watch = Stopwatch.StartNew();
        var connection = locker.ConnectionsSync.GetOrAdd(key, valueFactory: _ => new SingleThreadedConnection(new SqliteConnection(ConnectionString), onDispose: dbConnection =>
        {
            if (locker.ConnectionsSync.TryGetValue(key, out var connection))
            {
                connection.Decrement();
                if (connection.Count == 0)
                {
                    locker.ConnectionsSync.TryRemove(key, out var _);
                    dbConnection.Dispose();
                }
            }
            else
            {
                dbConnection.Dispose();
            }

            Monitor.Exit(locker.Lock);
            if (watch.Elapsed >= ConnectionTimeWarning)
            {
                Logger.LogWarning("[{Name}] Connection has been opened longer than '{Time}s'", member, ConnectionTimeWarning.TotalSeconds);
            }
        }));

        connection.Increment();
        return connection;
    }
    
    public SqliteConnection CreateConnection([CallerMemberName] string member = "") => new MonitoredSqliteConnection(Logger, ConnectionString, member);
    
    /// <summary>
    /// Use this if context is Async because we can't resume to the Thread that locked the Lock object
    /// </summary>
    /// <param name="cancellationToken"></param>
    /// <param name="member"></param>
    /// <returns></returns>
    public async Task<SingleThreadedConnection> CreateConnectionAsync(CancellationToken cancellationToken, [CallerMemberName] string member = "")
    {
        var locker = Lockers.GetOrAdd(ConnectionString, valueFactory: _ => new Locker());
        await locker.SemaphoreSlim.WaitAsync(cancellationToken);
        const int key = 1;
        var watch = Stopwatch.StartNew();
        var connection = locker.ConnectionsAsync.GetOrAdd(key, valueFactory: _ => new SingleThreadedConnection(new SqliteConnection(ConnectionString), onDispose: dbConnection =>
        {
            if (locker.ConnectionsAsync.TryGetValue(key, out var connection))
            {
                connection.Decrement();
                if (connection.Count == 0)
                {
                    locker.ConnectionsAsync.TryRemove(key, out var _);
                    dbConnection.Dispose();
                }
            }
            else
            {
                dbConnection.Dispose();
            }

            locker.SemaphoreSlim.Release();
            if (watch.Elapsed >= ConnectionTimeWarning)
            {
                Logger.LogWarning("[{Name}] Connection has been opened longer than '{Time}s'", member, ConnectionTimeWarning.TotalSeconds);
            }
        }));

        connection.Increment();
        return connection;
    }
}