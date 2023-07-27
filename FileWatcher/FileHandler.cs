using System.Text;

namespace FileWatcher;

public class FileHandler
{
    private const string SUFFIX = " (Copy)";

    private readonly DirectoryInfo _output;

    public FileHandler(Settings settings)
    {
        _output = new DirectoryInfo(settings.OutputPath);
    }

    /// <summary>
    /// This method is the business logic in the application that handles processing a new file.
    /// </summary>
    /// <param name="file">A newly detected file.</param>
    /// <param name="replaceDuplicates">Whether to overwrite the file if it already exists.</param>
    public void Handle(FileInfo file, bool replaceDuplicates)
    {
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