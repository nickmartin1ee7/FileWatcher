using System.Collections.Concurrent;

namespace FileWatcher;

public class FileProcessQueue : ConcurrentQueue<FileInfo>
{
}