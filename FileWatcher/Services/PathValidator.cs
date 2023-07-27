namespace FileWatcher.Services;

public class PathValidator
{
    private readonly ILogger<PathValidator> _logger;

    public PathValidator(ILogger<PathValidator> logger)
    {
        _logger = logger;
    }

    public void Validate(string path)
    {
        var directory = new DirectoryInfo(path);

        try
        {

            if (!directory.Exists)
            {
                _logger.LogInformation("Created {path} since it did not exist", directory.FullName);
                directory.Create();
            }

            var testFile = new FileInfo(Path.Combine(directory.FullName, "test_file"));
            testFile.Create().Close();
            testFile.Delete();
        }
        catch (Exception e)
        {
            _logger.LogError("Failed to validate path for {path} due to {error}", directory.FullName, e);
        }
    }
}