namespace FileWatcher;

public class FileHandler
{
    private readonly DirectoryInfo _output;

    public FileHandler(Settings settings)
    {
        _output = new DirectoryInfo(settings.OutputPath);
    }

    /// <summary>
    /// This method is the business logic in the application that handles processing a new file.
    /// </summary>
    /// <param name="file">A newly detected file.</param>
    public void Handle(FileInfo file)
    {
        file.MoveTo(Path.Combine(_output.FullName, file.Name), true);
    }
}