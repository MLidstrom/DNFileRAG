namespace DNFileRAG.Core.Configuration;

public class FileWatcherOptions
{
    public const string SectionName = "FileWatcher";

    public string WatchPath { get; set; } = "/app/data/documents";
    public bool IncludeSubdirectories { get; set; } = true;
    public string[] SupportedExtensions { get; set; } = [".pdf", ".docx", ".txt", ".md", ".html", ".png", ".jpg", ".jpeg", ".webp"];
    public int DebounceMilliseconds { get; set; } = 500;
}
