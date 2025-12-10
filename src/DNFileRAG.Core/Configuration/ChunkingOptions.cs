namespace DNFileRAG.Core.Configuration;

public class ChunkingOptions
{
    public const string SectionName = "Chunking";

    public int ChunkSize { get; set; } = 1500;
    public int ChunkOverlap { get; set; } = 200;
}
