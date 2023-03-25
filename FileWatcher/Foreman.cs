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

    private int _threadId => Environment.CurrentManagedThreadId;

    public Foreman(
        ILogger<Foreman> logger,
        Settings settings,
        ProcessQueue processQueue,
        FileSystemWatcher fileSystemWatcher)
    {
        _logger = logger;
        _settings = settings;
        _processQueue = processQueue;
        _fileSystemWatcher = fileSystemWatcher;

        _input = new DirectoryInfo(_settings.InputPath);
        _output = new DirectoryInfo(_settings.OutputPath);
        _workers = new Task[_settings.Workers];
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("Foreman on Thread Id {threadId} started at: {time}", _threadId, DateTimeOffset.Now);

        try
        {
            InitialScanInput();
            StartWorkers(stoppingToken);

            _fileSystemWatcher.EnableRaisingEvents = true;

            await Task.Delay(-1, stoppingToken);

        }
        catch (TaskCanceledException)
        {
        }

        _logger.LogInformation("Foreman on Thread Id {threadId} stopped at: {time}", _threadId, DateTimeOffset.Now);
    }

    private void InitialScanInput()
    {
        try
        {
            var initialFiles = _input.EnumerateFiles(_settings.FileFilter).ToArray();

            foreach (var file in initialFiles)
            {
                _processQueue.Enqueue(file);
            }

            if (initialFiles.Any())
                _logger.LogInformation("Foreman on Thread Id {threadId} found {initialFileCount} initial files at: {time}", _threadId, initialFiles.Length, DateTimeOffset.Now);
        }
        catch (Exception e)
        {
            _logger.LogError("Foreman on Thread Id {threadId} failed to perform initial scan due to {error} at: {time}", _threadId, e, DateTimeOffset.Now);
        }
    }

    private void StartWorkers(CancellationToken stoppingToken)
    {
        _logger.LogInformation("Foreman on Thread Id {threadId} enlisting {workerCount} workers at: {time}", _threadId, _workers.Length, DateTimeOffset.Now);

        for (int i = 0; i < _workers.Length; i++)
        {
            _workers[i] = CreateWorker(i + 1, stoppingToken);
        }
    }

    private Task CreateWorker(int workerId, CancellationToken stoppingToken) =>
        Task.Run(() => WorkerJob(workerId, stoppingToken));

    private void WorkerJob(int workerId, CancellationToken stoppingToken)
    {
        _logger.LogInformation("Worker {workerId} on Thread Id {threadId} started at: {time}", workerId, _threadId, DateTimeOffset.Now);

        while (!stoppingToken.IsCancellationRequested)
        {
            if (!_processQueue.TryDequeue(out var file))
                continue;

            _logger.LogInformation("Worker {workerId} on Thread Id {threadId} is processing file {fileName} at: {time}", workerId, _threadId, file.Name, DateTimeOffset.Now);

            try
            {
                if (!File.Exists(file.FullName))
                {
                    _logger.LogDebug("Worker {workerId} on Thread Id {threadId} found that the file {fileName} no longer exists at: {time}", workerId, _threadId, file.Name, DateTimeOffset.Now);
                    continue;
                }

                // Processing here
                file.MoveTo(Path.Combine(_output.FullName, file.Name), true);

                _logger.LogDebug("Worker {workerId} on Thread Id {threadId} is finished processing file {fileName} at: {time}", workerId, _threadId, file.Name, DateTimeOffset.Now);
            }
            catch (Exception e)
            {
                _logger.LogError("Worker {workerId} on Thread Id {threadId} failed due to {error} and is re-enqueueing file {fileName} at: {time}", workerId, _threadId, e, file.Name, DateTimeOffset.Now);
                _processQueue.Enqueue(file);
            }
        }

        _logger.LogInformation("Worker {workerId} on Thread Id {threadId} ended at: {time}", workerId, _threadId, DateTimeOffset.Now);
    }
}
