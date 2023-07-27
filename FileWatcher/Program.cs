using FileWatcher;
using FileWatcher.Services;

IHost host = Host.CreateDefaultBuilder(args)
    .ConfigureServices(services =>
    {
        services.AddSingleton<Settings>(sp =>
            sp.GetRequiredService<IConfiguration>()
                .GetSection(nameof(Settings))
                .Get<Settings>()
                ?? throw new InvalidOperationException("Settings is missing from configuration"));

        services.AddSingleton<IProcessor<string>, TextProcessor>();
        services.AddSingleton<FileProcessQueue>();
        services.AddSingleton<FileHandler>();
        services.AddSingleton<PathValidator>();

        services.AddSingleton<FileSystemWatcher>(sp =>
        {
            var settings = sp.GetRequiredService<Settings>();
            var logger = sp.GetRequiredService<ILogger<FileSystemWatcher>>();
            var processQueue = sp.GetRequiredService<FileProcessQueue>();
            var pathValidator = sp.GetRequiredService<PathValidator>();

            pathValidator.Validate(settings.InputPath);
            var fw = new FileSystemWatcher(settings.InputPath, settings.FileFilter);

            ConfigureFileWatcher();

            return fw;

            void ConfigureFileWatcher()
            {
                fw.NotifyFilter = NotifyFilters.FileName | NotifyFilters.CreationTime;
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