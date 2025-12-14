namespace DNFileRAG.Core.Configuration;

public class VisionOptions
{
    public const string SectionName = "Vision";

    /// <summary>
    /// Enables image understanding during ingestion (e.g., OCR-ish extraction + captioning).
    /// </summary>
    public bool Enabled { get; set; } = false;

    /// <summary>
    /// Provider name. Currently supported: Ollama.
    /// </summary>
    public string Provider { get; set; } = "Ollama";

    public OllamaVisionOptions Ollama { get; set; } = new();
}

public class OllamaVisionOptions
{
    public string BaseUrl { get; set; } = "http://localhost:11434";

    /// <summary>
    /// Vision-capable model. Example: llava.
    /// </summary>
    public string Model { get; set; } = "llava";
}


