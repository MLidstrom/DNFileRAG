# Changelog

All notable changes to this project will be documented in this file.

The format is based on [Keep a Changelog](https://keepachangelog.com/en/1.1.0/),
and this project adheres to [Semantic Versioning](https://semver.org/spec/v2.0.0.html).

## [1.2.0] - 2025-12-14

### Added

- **Vision/Image Support**: New image parsing capability for PNG, JPG, JPEG, and WEBP files
  - `IVisionTextExtractor` interface for image text extraction
  - `OllamaVisionTextExtractor` implementation using Ollama's llava model
  - `ImageParser` for processing image files during ingestion
  - Extracts visible text (OCR-like) and generates image descriptions
  - Indexed content is searchable via standard RAG queries
- **VisionOptions** configuration section:
  - `Vision:Enabled` - Enable/disable vision processing (default: false in production, true in development)
  - `Vision:Provider` - Vision provider selection (currently: Ollama)
  - `Vision:Ollama:Model` - Vision model name (default: llava)
  - `Vision:Ollama:BaseUrl` - Ollama API endpoint
- Comprehensive test coverage for vision feature:
  - `ImageParserTests` - Image parser unit tests
  - `OllamaVisionTextExtractorTests` - Vision extractor unit tests
  - `VisionOptions` configuration tests

### Changed

- `FileWatcherOptions.SupportedExtensions` now includes `.png`, `.jpg`, `.jpeg`, `.webp`
- Updated README.md with image format support and llava model installation instructions

## [1.1.0] - 2025-12-12

### Added

- HelpChat example UI (`examples/HelpChat/`) - Mock company landing page with popup support chat
- NuGet package publishing workflow
- Comprehensive integration tests using Testcontainers

### Changed

- Improved Ollama stability with streaming disabled
- Added relevance score filtering (`Rag:MinRelevanceScore`) to prevent off-topic responses

## [1.0.0] - 2025-12-10

### Added

- Initial release of DNFileRAG
- Real-time document ingestion with FileSystemWatcher
- Support for PDF, DOCX, TXT, MD, HTML formats
- Multiple embedding providers: OpenAI, Azure OpenAI, Ollama
- Multiple LLM providers: OpenAI, Azure OpenAI, Anthropic, Ollama
- Qdrant vector store integration
- RESTful API: `/api/query`, `/api/documents`, `/api/health`
- Docker Compose setup with Qdrant and Ollama
- API key authentication with role-based access
- Health check endpoints (basic and detailed)
