namespace FileWatcher;

public interface IProcessor<in T>
{
    /// <summary>
    /// This is the asynchronous action to perform on each <paramref name="item"/>.
    /// </summary>
    /// <param name="item">The item to process.</param>
    /// <returns></returns>
    Task ExecuteAsync(T item);
}