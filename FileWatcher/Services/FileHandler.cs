using System.Text;

using FileWatcher.Services.Processors;

namespace FileWatcher.Services;

public class FileHandler
{
    private readonly ILogger<FileHandler> _logger;
    private readonly IProcessor<string> _processor;
    private readonly DirectoryInfo _outputPath;

    public FileHandler(ILogger<FileHandler> logger, Settings settings, IProcessor<string> processor)
    {
        _logger = logger;
        _processor = processor;
        _outputPath = new DirectoryInfo(settings.OutputPath);
    }

    /// <summary>
    /// This method is the business logic in the application and handle moving the newly processed file.
    /// </summary>
    /// <param name="file">A newly detected file.</param>
    /// <param name="replaceDuplicates">Whether to overwrite the file if it already exists.</param>
    public async Task HandleAsync(FileInfo file, bool replaceDuplicates)
    {
        await _processor.ExecuteAsync(await File.ReadAllTextAsync(file.FullName));
        MoveFinishedFile(file, _outputPath, replaceDuplicates);
    }

    private void MoveFinishedFile(FileInfo file, DirectoryInfo destinationPath, bool replaceDuplicates)
    {
        // Overwrite
        if (replaceDuplicates)
        {
            file.MoveTo(Path.Combine(destinationPath.FullName, file.Name), replaceDuplicates);
            return;
        }

        // Don't overwrite: Add a suffix to duplicate file's filename
        var destinationFileName = file.Name;

        while (true)
        {
            var destinationFullPath = Path.Combine(destinationPath.FullName, destinationFileName);

            if (!File.Exists(destinationFullPath))
            {
                file.MoveTo(destinationFullPath, replaceDuplicates);
                break;
            }

            destinationFileName = AppendSuffix(new FileInfo(destinationFullPath), $"-{Guid.NewGuid()}");
        }
    }

    private string AppendSuffix(FileInfo file, string suffix)
    {
        var sb = new StringBuilder(file.Name.Replace(file.Extension, ""));

        sb.Append(suffix);
        sb.Append(file.Extension);

        var result = sb.ToString();

        return result;
    }
}