namespace DNFileRAG.Core.Interfaces;

/// <summary>
/// Interface for LLM generation.
/// </summary>
public interface ILlmProvider
{
    /// <summary>
    /// Gets the model identifier.
    /// </summary>
    string ModelId { get; }

    /// <summary>
    /// Generates a response using the LLM.
    /// </summary>
    /// <param name="systemPrompt">The system prompt.</param>
    /// <param name="userPrompt">The user prompt.</param>
    /// <param name="options">Generation options.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The generated text response.</returns>
    Task<string> GenerateAsync(
        string systemPrompt,
        string userPrompt,
        LlmGenerationOptions? options = null,
        CancellationToken cancellationToken = default);
}

/// <summary>
/// Options for LLM generation.
/// </summary>
public class LlmGenerationOptions
{
    /// <summary>
    /// Sampling temperature (0-2).
    /// </summary>
    public float Temperature { get; init; } = 0.2f;

    /// <summary>
    /// Maximum tokens to generate.
    /// </summary>
    public int MaxTokens { get; init; } = 512;
}
