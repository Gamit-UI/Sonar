using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace Sonar.Databases.Contexts;

internal sealed class DetectionContext(ILogger<DetectionContext> logger, IHostApplicationLifetime hostApplicationLifetime)
    : ContextBase(logger, hostApplicationLifetime, DbPath, "detections.db")
{
    private readonly IHostApplicationLifetime hostApplicationLifetime = hostApplicationLifetime;

    public override void CreateTables()
    {
        try
        {
            SetPragmas();
            CreateTable();
        }
        catch (Exception ex)
        {
            logger.LogCritical(ex, "Could not create database tables");
            hostApplicationLifetime.StopApplication();
        }
    }

    private void SetPragmas()
    {
        try
        {
            const string sql = """
                               PRAGMA journal_mode = 'wal';
                               PRAGMA synchronous = normal;
                               PRAGMA auto_vacuum = 0;
                               """;

            using var connection = CreateSingleConnection();
            connection.DbConnection.Open();
            using var command = connection.DbConnection.CreateCommand();
            command.CommandText = sql;
            command.ExecuteNonQuery();
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "An error has occurred");
            throw;
        }
    }

    private void CreateTable()
    {
        try
        {
            const string sql = """
                               CREATE TABLE IF NOT EXISTS "Detections" (
                                   "Id" INTEGER NOT NULL CONSTRAINT "PK_Detections" PRIMARY KEY,
                                   "Date" INTEGER NOT NULL,
                                   "ComputerId" INTEGER NOT NULL,
                                   "LevelId" INTEGER NOT NULL,
                                   "RuleId" INTEGER NOT NULL,
                                   "Details" TEXT NOT NULL
                               );

                               CREATE TABLE IF NOT EXISTS "Computers" (
                                   "Id" INTEGER NOT NULL CONSTRAINT "PK_Computers" PRIMARY KEY,
                                   "Value" TEXT NOT NULL UNIQUE
                               );

                               CREATE TABLE IF NOT EXISTS "Levels" (
                                   "Id" INTEGER NOT NULL CONSTRAINT "PK_Levels" PRIMARY KEY,
                                   "Value" TEXT NOT NULL UNIQUE
                               );

                               CREATE TABLE IF NOT EXISTS "Rules" (
                                   "Id" INTEGER NOT NULL CONSTRAINT "PK_Rules" PRIMARY KEY,
                                   "Value" TEXT NOT NULL UNIQUE,
                                   "Title" TEXT NOT NULL,
                                   "Link" TEXT NOT NULL
                               );

                               CREATE INDEX IF NOT EXISTS idx_detections_computer_level_rule_date ON Detections(ComputerId, LevelId, RuleId, Date);
                               """;

            using var connection = CreateSingleConnection();
            connection.DbConnection.Open();
            using var command = connection.DbConnection.CreateCommand();
            command.CommandText = sql;
            command.ExecuteNonQuery();
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "An error has occurred");
            throw;
        }
    }
}