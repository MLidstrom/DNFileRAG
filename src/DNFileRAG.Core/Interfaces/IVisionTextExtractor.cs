namespace DNFileRAG.Core.Interfaces;

/// <summary>
/// Extracts text and a short description from an image for indexing.
/// </summary>
public interface IVisionTextExtractor
{
    Task<VisionTextResult> ExtractAsync(
        byte[] imageBytes,
        string? fileName = null,
        CancellationToken cancellationToken = default);
}

public sealed class VisionTextResult
{
    public required string ExtractedText { get; init; }
    public required string Description { get; init; }
}


