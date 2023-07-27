namespace FileWatcher
{
    public class Settings
    {
        /// <summary>
        /// This is the file path being monitored for new files.
        /// </summary>
        public string InputPath { get; set; } = null!;
        /// <summary>
        /// This is the file path where newly discovered files are moved.
        /// </summary>
        public string OutputPath { get; set; } = null!;
        /// <summary>
        /// This is the file extenstion pattern that is checked before moving a newly discovered file.
        /// </summary>
        public string FileFilter { get; set; } = null!;
        /// <summary>
        /// This is the amount of worker threads that are concurrently processing files in the watched directory.
        /// </summary>
        public int Workers { get; set; } = 1;
        /// <summary>
        /// This is the scanning interval that the directory is checked for new files.
        /// </summary>
        public double ForemanJobInMilliSeconds { get; set; }
        /// <summary>
        /// This is the amount of time a file is permitted to stay until it is re-enqueued for processing by the workers.
        /// </summary>
        public double LateFileInMilliSeconds { get; set; }

    }
}
