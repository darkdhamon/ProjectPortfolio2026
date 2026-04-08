using Microsoft.Data.SqlClient;

namespace ProjectPortfolio2026.Server.Data;

public static class ConnectionStringPathResolver
{
    public static string ResolveDataPaths(string connectionString, string contentRootPath)
    {
        var dataDirectory = Path.Combine(contentRootPath, "App_Data");
        Directory.CreateDirectory(dataDirectory);

        var builder = new SqlConnectionStringBuilder(connectionString);
        if (string.IsNullOrWhiteSpace(builder.AttachDBFilename))
        {
            return connectionString.Replace(
                "|DataDirectory|",
                dataDirectory,
                StringComparison.OrdinalIgnoreCase);
        }

        var attachDbFilename = builder.AttachDBFilename.Replace(
            "|DataDirectory|",
            dataDirectory,
            StringComparison.OrdinalIgnoreCase);
        attachDbFilename = attachDbFilename
            .Replace(Path.AltDirectorySeparatorChar, Path.DirectorySeparatorChar)
            .Replace('\\', Path.DirectorySeparatorChar);

        if (!Path.IsPathRooted(attachDbFilename))
        {
            attachDbFilename = Path.GetFullPath(Path.Combine(contentRootPath, attachDbFilename));
        }

        builder.AttachDBFilename = attachDbFilename;
        return builder.ConnectionString;
    }
}
