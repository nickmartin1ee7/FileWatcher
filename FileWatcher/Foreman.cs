using Timer = System.Timers.Timer;

namespace FileWatcher;

public class Foreman : BackgroundService
{
    private readonly ILogger<Foreman> _logger;
    private readonly Settings _settings;
    private readonly ProcessQueue _processQueue;
    private readonly FileSystemWatcher _fileSystemWatcher;
    private readonly Task[] _workers;
    private readonly DirectoryInfo _input;
    private readonly DirectoryInfo _output;
    private readonly PathValidator _pathValidator;
    private readonly object _foremanJobLock = new();

    private static int ThreadId => Environment.CurrentManagedThreadId;

    public Foreman(
        ILogger<Foreman> logger,
        Settings settings,
        ProcessQueue processQueue,
        FileSystemWatcher fileSystemWatcher,
        PathValidator pathValidator)
    {
        _logger = logger;
        _settings = settings;
        _processQueue = processQueue;
        _fileSystemWatcher = fileSystemWatcher;
        _pathValidator = pathValidator;

        _input = new DirectoryInfo(_settings.InputPath);
        _output = new DirectoryInfo(_settings.OutputPath);
        _workers = new Task[_settings.Workers];
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("Foreman on Thread Id {threadId} started at: {time}", ThreadId, DateTimeOffset.Now);

        var foremanTimer = new Timer(TimeSpan.FromSeconds(_settings.ForemanJobInSeconds).TotalMilliseconds);
        foremanTimer.Elapsed += (_, _) =>
            ForemanJob();

        try
        {
            StartWorkers(stoppingToken);
            foremanTimer.Start();
            _fileSystemWatcher.EnableRaisingEvents = true;

            await Task.Delay(-1, stoppingToken);
        }
        catch (TaskCanceledException)
        {
        }

        foremanTimer.Stop();
        _logger.LogInformation("Foreman on Thread Id {threadId} stopped at: {time}", ThreadId, DateTimeOffset.Now);
    }

    private void ForemanJob()
    {
        lock (_foremanJobLock)
        {
            try
            {
                _pathValidator.Validate(_settings.InputPath);
                _pathValidator.Validate(_settings.OutputPath);

                int enqueued = 0;
                var files = _input.EnumerateFiles(_settings.FileFilter).ToArray();

                Parallel.ForEach(files, (file) =>
                {

                    if (_processQueue.FirstOrDefault(enqueuedFile => enqueuedFile.FullName == file.FullName) is not null)
                        return;

                    _processQueue.Enqueue(file);
                    enqueued++;
                });

                if (files.Any())
                    _logger.LogInformation(
                        "Foreman on Thread Id {threadId} found {initialFileCount} files and enqueued {enqueuedFileCount} of them at: {time}",
                        ThreadId, files.Length, enqueued, DateTimeOffset.Now);
            }
            catch (Exception e)
            {
                _logger.LogError("Foreman on Thread Id {threadId} failed to perform job due to {error} at: {time}",
                    ThreadId, e, DateTimeOffset.Now);
            }
        }
    }

    private void StartWorkers(CancellationToken stoppingToken)
    {
        _logger.LogInformation("Foreman on Thread Id {threadId} enlisting {workerCount} workers at: {time}", ThreadId, _workers.Length, DateTimeOffset.Now);

        for (int i = 0; i < _workers.Length; i++)
        {
            _workers[i] = CreateWorker(i + 1, stoppingToken);
        }
    }

    private Task CreateWorker(int workerId, CancellationToken stoppingToken) =>
        Task.Run(async () => await WorkerJobAsync(workerId, stoppingToken));

    private async Task WorkerJobAsync(int workerId, CancellationToken stoppingToken)
    {
        _logger.LogInformation("Worker {workerId} on Thread Id {threadId} started at: {time}", workerId, ThreadId, DateTimeOffset.Now);

        while (!stoppingToken.IsCancellationRequested)
        {
            if (!_processQueue.TryDequeue(out var file))
            {
                await Task.Delay(1);
                continue;
            }

            _logger.LogDebug("Worker {workerId} on Thread Id {threadId} is processing file {fileName} at: {time}", workerId, ThreadId, file.Name, DateTimeOffset.Now);

            try
            {
                if (!File.Exists(file.FullName))
                {
                    _logger.LogDebug("Worker {workerId} on Thread Id {threadId} found that the file {fileName} no longer exists at: {time}", workerId, ThreadId, file.Name, DateTimeOffset.Now);
                    continue;
                }

                // Processing here
                file.MoveTo(Path.Combine(_output.FullName, file.Name), true);

                _logger.LogDebug("Worker {workerId} on Thread Id {threadId} is finished processing file {fileName} at: {time}", workerId, ThreadId, file.Name, DateTimeOffset.Now);
            }
            catch (Exception e)
            {
                _logger.LogWarning("Worker {workerId} on Thread Id {threadId} failed due to {error} and is re-enqueueing file {fileName} at: {time}", workerId, ThreadId, e.Message, file.Name, DateTimeOffset.Now);
                _processQueue.Enqueue(file);
            }
        }

        _logger.LogInformation("Worker {workerId} on Thread Id {threadId} ended at: {time}", workerId, ThreadId, DateTimeOffset.Now);
    }
}