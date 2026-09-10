using Loren.Infrastructure.CanonicalState;
using Loren.Infrastructure.Recovery;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using System.Text.Json;

return await MaintenanceCli.RunAsync(args);

internal static class MaintenanceCli
{
    public static async Task<int> RunAsync(string[] args)
    {
        try
        {
            if (args.Length == 0 || args.Contains("--help", StringComparer.Ordinal))
            {
                Console.Error.WriteLine("Usage: loren-maintenance export --source <loren.db> --output <archive.json>");
                Console.Error.WriteLine("       loren-maintenance restore --archive <archive.json> --target-directory <new-directory>");
                return args.Length == 0 ? 2 : 0;
            }
            return args[0] switch
            {
                "export" => await ExportAsync(args),
                "restore" => await RestoreAsync(args),
                _ => throw new ArgumentException($"Unknown operation '{args[0]}'."),
            };
        }
        catch (JsonException)
        {
            Console.Error.WriteLine("Recovery failed: archive JSON is malformed.");
            return 1;
        }
        catch (SqliteException)
        {
            Console.Error.WriteLine("Recovery failed: database schema is unavailable or incompatible.");
            return 1;
        }
        catch (Exception exception) when (exception is ArgumentException or InvalidDataException or InvalidOperationException or IOException)
        {
            Console.Error.WriteLine($"Recovery failed: {exception.Message}");
            return 1;
        }
    }

    private static async Task<int> ExportAsync(string[] args)
    {
        string sourcePath = Required(args, "--source");
        string outputPath = Required(args, "--output");
        if (!File.Exists(sourcePath)) throw new FileNotFoundException("Source database does not exist.", sourcePath);
        if (File.Exists(outputPath)) throw new IOException("Output archive already exists.");
        await using CanonicalStateDbContext source = Context(sourcePath);
        if ((await source.Database.GetPendingMigrationsAsync()).Any()) throw new InvalidOperationException("Source database has unapplied migrations; migrate it before exporting.");
        string directory = Path.GetDirectoryName(Path.GetFullPath(outputPath))!;
        Directory.CreateDirectory(directory);
        string temporaryPath = Path.Combine(directory, $".{Path.GetFileName(outputPath)}.{Guid.NewGuid():N}.tmp");
        try
        {
            await using (FileStream stream = new(temporaryPath, FileMode.CreateNew, FileAccess.Write, FileShare.None))
                await LogicalStateRecovery.ExportAsync(source, stream);
            File.Move(temporaryPath, outputPath);
        }
        finally
        {
            if (File.Exists(temporaryPath)) File.Delete(temporaryPath);
        }
        return 0;
    }

    private static async Task<int> RestoreAsync(string[] args)
    {
        string archivePath = Required(args, "--archive");
        string targetDirectory = Path.GetFullPath(Required(args, "--target-directory"));
        if (!File.Exists(archivePath)) throw new FileNotFoundException("Archive does not exist.", archivePath);
        if (Directory.Exists(targetDirectory)) throw new IOException("Target directory must be new and absent.");
        string parent = Path.GetDirectoryName(targetDirectory)!;
        Directory.CreateDirectory(parent);
        string temporaryDirectory = Path.Combine(parent, $".{Path.GetFileName(targetDirectory)}.{Guid.NewGuid():N}.tmp");
        Directory.CreateDirectory(temporaryDirectory);
        try
        {
            string databasePath = Path.Combine(temporaryDirectory, "loren.db");
            await using (CanonicalStateDbContext target = Context(databasePath))
            {
                await CanonicalStateDatabase.MigrateAsync(target);
                await using FileStream archive = new(archivePath, FileMode.Open, FileAccess.Read, FileShare.Read);
                await LogicalStateRecovery.RestoreAsync(target, archive);
            }
            Directory.Move(temporaryDirectory, targetDirectory);
        }
        finally
        {
            if (Directory.Exists(temporaryDirectory)) Directory.Delete(temporaryDirectory, recursive: true);
        }
        return 0;
    }

    private static string Required(string[] args, string option)
    {
        int index = Array.IndexOf(args, option);
        if (index < 0 || index + 1 >= args.Length || string.IsNullOrWhiteSpace(args[index + 1])) throw new ArgumentException($"Missing {option}.");
        return args[index + 1];
    }

    private static CanonicalStateDbContext Context(string path) => new(new DbContextOptionsBuilder<CanonicalStateDbContext>().UseSqlite(new SqliteConnectionStringBuilder { DataSource = path, Pooling = false }.ToString()).Options);
}
