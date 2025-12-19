using Microsoft.Data.Sqlite;

namespace Sonar.Databases;

internal sealed class SingleThreadedConnection(SqliteConnection dbConnection, Action<SqliteConnection> onDispose) : IDisposable
{
    public void Dispose()
    {
        onDispose(DbConnection);
    }

    public SqliteConnection DbConnection { get; } = dbConnection;

    private int count;

    public void Increment()
    {
        Interlocked.Increment(ref count);
    }

    public void Decrement()
    {
        Interlocked.Decrement(ref count);
    }

    public int Count => count;
}