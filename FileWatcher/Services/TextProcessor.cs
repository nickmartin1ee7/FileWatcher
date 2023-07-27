using System.Text;

namespace FileWatcher.Services;

public class TextProcessor : IProcessor<string>
{
    private readonly ILogger<TextProcessor> _logger;

    public TextProcessor(ILogger<TextProcessor> logger)
    {
        _logger = logger;
    }

    public Task ExecuteAsync(string item)
    {
        var data = string.IsNullOrEmpty(item)
        ? "Nothing!"
        : Convert.ToBase64String(Encoding.UTF8.GetBytes(item));

        _logger.LogInformation("Processed data: {data}",
            data);

        return Task.CompletedTask;
    }
}