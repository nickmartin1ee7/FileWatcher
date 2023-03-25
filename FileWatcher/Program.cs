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

        services.AddSingleton<PathValidator>();

        services.AddSingleton<FileSystemWatcher>(sp =>
        {
            var settings = sp.GetRequiredService<Settings>();
            var logger = sp.GetRequiredService<ILogger<FileSystemWatcher>>();
            var processQueue = sp.GetRequiredService<ProcessQueue>();
            var pathValidator = sp.GetRequiredService<PathValidator>();

            pathValidator.Validate(settings.InputPath);
            var fw = new FileSystemWatcher(settings.InputPath, settings.FileFilter);

            ConfigureFileWatcher();

            return fw;

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