namespace DNFileRAG.Core.Configuration;

public class QdrantOptions
{
    public const string SectionName = "Qdrant";

    public string Host { get; set; } = "localhost";
    public int Port { get; set; } = 6333;
    public string CollectionName { get; set; } = "documents";
    public int VectorSize { get; set; } = 1536;
}
