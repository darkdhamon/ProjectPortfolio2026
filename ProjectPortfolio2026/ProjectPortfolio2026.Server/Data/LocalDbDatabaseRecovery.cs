using Microsoft.Data.SqlClient;

namespace ProjectPortfolio2026.Server.Data;

public static class LocalDbDatabaseRecovery
{
    private const int DatabaseAlreadyExistsErrorNumber = 1801;

    public static bool CanRecover(string connectionString, int sqlErrorNumber, string sqlErrorMessage)
    {
        if (sqlErrorNumber != DatabaseAlreadyExistsErrorNumber &&
            !sqlErrorMessage.Contains("already exists", StringComparison.OrdinalIgnoreCase))
        {
            return false;
        }

        if (!TryGetRecoveryTarget(connectionString, out var recoveryTarget))
        {
            return false;
        }

        return !File.Exists(recoveryTarget.AttachDbFilePath);
    }

    public static async Task<bool> TryRecoverAsync(string connectionString, CancellationToken cancellationToken = default)
    {
        if (!TryGetRecoveryTarget(connectionString, out var recoveryTarget))
        {
            return false;
        }

        if (File.Exists(recoveryTarget.AttachDbFilePath))
        {
            return false;
        }

        EnsureAttachDirectoryExists(recoveryTarget.AttachDbFilePath);

        var masterConnectionStringBuilder = new SqlConnectionStringBuilder(connectionString)
        {
            InitialCatalog = "master",
            AttachDBFilename = string.Empty
        };

        await using var connection = new SqlConnection(masterConnectionStringBuilder.ConnectionString);
        await connection.OpenAsync(cancellationToken);

        var escapedDatabaseIdentifier = EscapeIdentifier(recoveryTarget.DatabaseName);
        var escapedDatabaseLiteral = EscapeLiteral(recoveryTarget.DatabaseName);

        var commandText = $"""
            IF DB_ID(N'{escapedDatabaseLiteral}') IS NOT NULL
            BEGIN
                DROP DATABASE [{escapedDatabaseIdentifier}];
            END
            """;

        await using var command = new SqlCommand(commandText, connection);
        await command.ExecuteNonQueryAsync(cancellationToken);
        return true;
    }

    public static bool TryGetRecoveryTarget(string connectionString, out LocalDbRecoveryTarget recoveryTarget)
    {
        var builder = new SqlConnectionStringBuilder(connectionString);
        if (!IsLocalDbConnection(builder))
        {
            recoveryTarget = default;
            return false;
        }

        var databaseName = builder.InitialCatalog;
        var attachDbFilePath = builder.AttachDBFilename;

        if (string.IsNullOrWhiteSpace(databaseName) || string.IsNullOrWhiteSpace(attachDbFilePath))
        {
            recoveryTarget = default;
            return false;
        }

        recoveryTarget = new LocalDbRecoveryTarget(databaseName, attachDbFilePath);
        return true;
    }

    private static bool IsLocalDbConnection(SqlConnectionStringBuilder builder)
    {
        return builder.DataSource.Contains("(localdb)", StringComparison.OrdinalIgnoreCase);
    }

    public static void EnsureAttachDirectoryExists(string attachDbFilePath)
    {
        var attachDirectory = Path.GetDirectoryName(attachDbFilePath);
        if (!string.IsNullOrWhiteSpace(attachDirectory))
        {
            Directory.CreateDirectory(attachDirectory);
        }
    }

    private static string EscapeIdentifier(string value)
    {
        return value.Replace("]", "]]", StringComparison.Ordinal);
    }

    private static string EscapeLiteral(string value)
    {
        return value.Replace("'", "''", StringComparison.Ordinal);
    }
}

public readonly record struct LocalDbRecoveryTarget(string DatabaseName, string AttachDbFilePath);
