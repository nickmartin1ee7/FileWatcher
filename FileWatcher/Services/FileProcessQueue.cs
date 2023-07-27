using System.Collections.Concurrent;

namespace FileWatcher.Services;

public class FileProcessQueue : ConcurrentQueue<FileInfo>
{
}