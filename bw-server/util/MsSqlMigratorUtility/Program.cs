﻿using Bit.Migrator;
using CommandDotNet;
using Microsoft.Extensions.Logging;

internal class Program
{
    private static IDictionary<string, string> Parameters { get; set; }

    private static int Main(string[] args)
    {
        return new AppRunner<Program>().Run(args);
    }

    [DefaultCommand]
    public void Execute(
        [Operand(Description = "Database connection string")]
        string databaseConnectionString,
        [Option('v', "verbose", Description = "Enable verbose output of migrator logs")]
        bool verbose = false,
        [Option('r', "repeatable", Description = "Mark scripts as repeatable")]
        bool repeatable = false,
        [Option('f', "folder", Description = "Folder name of database scripts")]
        string folderName = MigratorConstants.DefaultMigrationsFolderName) => MigrateDatabase(databaseConnectionString, verbose, repeatable, folderName);

    private static void WriteUsageToConsole()
    {
        Console.WriteLine("Usage: MsSqlMigratorUtility <database-connection-string>");
        Console.WriteLine("Usage: MsSqlMigratorUtility <database-connection-string> -v|--verbose (for verbose output of migrator logs)");
        Console.WriteLine("Usage: MsSqlMigratorUtility <database-connection-string> -r|--repeatable (for marking scripts as repeatable) -f|--folder <folder-name-in-migrator-project> (for specifying folder name of scripts)");
        Console.WriteLine("Usage: MsSqlMigratorUtility <database-connection-string> -v|--verbose (for verbose output of migrator logs) -r|--repeatable (for marking scripts as repeatable) -f|--folder <folder-name-in-migrator-project> (for specifying folder name of scripts)");
    }

    private static bool MigrateDatabase(string databaseConnectionString, bool verbose = false, bool repeatable = false, string folderName = "")
    {
        var logger = CreateLogger(verbose);

        logger.LogInformation($"Migrating database with repeatable: {repeatable} and folderName: {folderName}.");

        var migrator = new DbMigrator(databaseConnectionString, logger);
        bool success = false;
        if (!string.IsNullOrWhiteSpace(folderName))
        {
            success = migrator.MigrateMsSqlDatabaseWithRetries(verbose, repeatable, folderName);
        }
        else
        {
            success = migrator.MigrateMsSqlDatabaseWithRetries(verbose, repeatable);
        }

        return success;
    }

    private static ILogger<DbMigrator> CreateLogger(bool verbose)
    {
        var loggerFactory = LoggerFactory.Create(builder =>
        {
            builder
                .AddFilter("Microsoft", LogLevel.Warning)
                .AddFilter("System", LogLevel.Warning)
                .AddConsole();

            if (verbose)
            {
                builder.AddFilter("DbMigrator.DbMigrator", LogLevel.Debug);
            }
            else
            {
                builder.AddFilter("DbMigrator.DbMigrator", LogLevel.Information);
            }
        });
        var logger = loggerFactory.CreateLogger<DbMigrator>();
        return logger;
    }
}
