namespace DNFileRAG.Core.Configuration;

public class RagOptions
{
    public const string SectionName = "Rag";

    public int DefaultTopK { get; set; } = 5;
    public float DefaultTemperature { get; set; } = 0.2f;
    public int DefaultMaxTokens { get; set; } = 512;

    /// <summary>
    /// Minimum relevance score (0.0 to 1.0) for retrieved documents.
    /// Documents with lower scores will be filtered out.
    /// Set to 0 to disable threshold filtering.
    /// </summary>
    public float MinRelevanceScore { get; set; } = 0.5f;

    public string SystemPrompt { get; set; } = "You are a helpful assistant that answers questions ONLY based on the provided context from indexed documents. If the context does not contain information relevant to answer the question, you MUST respond with: 'I don't have information about that in the indexed documents.' Do NOT make up information or use external knowledge. Always cite your sources using [Source N] format.";
}
