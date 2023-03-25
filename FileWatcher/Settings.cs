namespace FileWatcher
{
    public class Settings
    {
        public string InputPath { get; set; } = null!;
        public string OutputPath { get; set; } = null!;
        public string FileFilter { get; set; } = null!;
        public int Workers { get; set; } = 1;
    }
}
