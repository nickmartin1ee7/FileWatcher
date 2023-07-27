using System.Text;

namespace FileWatcher.Services;

public class FileHandler
{
    private const string SUFFIX = " (Copy)";

    private readonly IProcessor<string> _processor;

    private readonly DirectoryInfo _output;

    public FileHandler(Settings settings, IProcessor<string> processor)
    {
        _processor = processor;
        _output = new DirectoryInfo(settings.OutputPath);
    }

    /// <summary>
    /// This method is the business logic in the application and handle moving the newly processed file.
    /// </summary>
    /// <param name="file">A newly detected file.</param>
    /// <param name="replaceDuplicates">Whether to overwrite the file if it already exists.</param>
    public async Task HandleAsync(FileInfo file, bool replaceDuplicates)
    {
        // Execute business logic
        await _processor.ExecuteAsync(await File.ReadAllTextAsync(file.FullName));

        // Overwrite
        if (replaceDuplicates)
        {
            file.MoveTo(Path.Combine(_output.FullName, file.Name), replaceDuplicates);
            return;
        }

        // Don't overwrite: Add a suffix to duplicate file's filename
        var destinationFileName = file.Name;

        while (true)
        {
            var destinationFullPath = Path.Combine(_output.FullName, destinationFileName);

            if (!File.Exists(destinationFullPath))
            {
                file.MoveTo(destinationFullPath, replaceDuplicates);
                break;
            }

            destinationFileName = AppendSuffix(new FileInfo(destinationFullPath), SUFFIX);
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