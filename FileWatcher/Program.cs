using System.Collections.Concurrent;

using FileWatcher;

IHost host = Host.CreateDefaultBuilder(args)
    .ConfigureServices(services =>
    {
        services.AddSingleton<Settings>(sp =>
            sp.GetRequiredService<IConfiguration>()
                .GetSection(nameof(Settings))
                .Get<Settings>()
                ?? throw new InvalidOperationException("Settings is missing from configuration"));

        services.AddSingleton<ProcessQueue>();

        services.AddSingleton<FileSystemWatcher>(sp =>
        {
            var settings = sp.GetRequiredService<Settings>();
            var logger = sp.GetRequiredService<ILogger<FileSystemWatcher>>();
            var processQueue = sp.GetRequiredService<ProcessQueue>();

            ValidateInputPath();
            ValidateOutputPath();

            var fw = new FileSystemWatcher(settings.InputPath, settings.FileFilter);

            ConfigureFileWatcher();

            return fw;

            void ValidateInputPath()
            {
                var input = new DirectoryInfo(settings.InputPath);

                if (!input.Exists)
                {
                    logger.LogInformation("Created {path} since it did not exist", input.FullName);
                    input.Create();
                }
                else
                {
                    logger.LogInformation("Using Input {path}", input.FullName);
                }

                try
                {
                    var testFile = new FileInfo(Path.Combine(input.FullName, "testInput"));
                    testFile.Create().Close();
                    testFile.Delete();
                }
                catch (Exception e)
                {
                    logger.LogCritical("Failed to write to {path} with test file due to {error}", input.FullName, e);
                    throw;
                }
            }

            void ValidateOutputPath()
            {
                var output = new DirectoryInfo(settings.OutputPath);

                if (!output.Exists)
                {
                    logger.LogInformation("Created {path} since it did not exist", output.FullName);
                    Directory.CreateDirectory(settings.OutputPath);
                    output.Create();
                }
                else
                {
                    logger.LogInformation("Using Output {path}", output.FullName);
                }

                try
                {
                    var testFile = new FileInfo(Path.Combine(output.FullName, "testOutput"));
                    testFile.Create().Close();
                    testFile.Delete();
                }
                catch (Exception e)
                {
                    logger.LogCritical("Failed to write to {path} with test file due to {error}", output.FullName, e);
                    throw;
                }
            }

            void ConfigureFileWatcher()
            {
                fw.NotifyFilter = NotifyFilters.FileName;
                fw.Created += (o, e) =>
                {
                    logger.LogDebug("Observed new File Created: {fileName}", e.Name);
                    processQueue.Enqueue(new FileInfo(e.FullPath));
                };
            }
        });

        services.AddHostedService<Foreman>();
    })
    .Build();

await host.RunAsync();

public class ProcessQueue : ConcurrentQueue<FileInfo>
{
}