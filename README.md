# DNFileRAG

A .NET 9-powered, real-time file-driven RAG (Retrieval-Augmented Generation) engine that auto-ingests documents and serves fast, contextual query responses via API.

[![License: MIT](https://img.shields.io/badge/License-MIT-yellow.svg)](https://opensource.org/licenses/MIT)
[![.NET](https://img.shields.io/badge/.NET-9.0-blue.svg)](https://dotnet.microsoft.com/)

## Features

- **Real-time Document Ingestion** - Automatically watches directories and indexes new/modified documents
- **Multiple Document Formats** - Supports PDF, DOCX, TXT, MD, and HTML files
- **Flexible Embedding Providers** - OpenAI, Azure OpenAI, or Ollama (local)
- **Multiple LLM Providers** - OpenAI, Azure OpenAI, Anthropic, or Ollama (local)
- **Vector Search** - Powered by Qdrant for fast semantic search
- **REST API** - Simple API for queries and document management
- **Clean Architecture** - Modular, testable, and extensible design

## Quick Start

### Prerequisites

- [.NET 9 SDK](https://dotnet.microsoft.com/download/dotnet/9.0)
- [Qdrant](https://qdrant.tech/) vector database (Docker recommended)
- One of: OpenAI API key, Azure OpenAI, or [Ollama](https://ollama.ai/) for local inference

### 1. Start Qdrant

```bash
docker run -p 6333:6333 -p 6334:6334 qdrant/qdrant
```

### 2. Clone and Build

```bash
git clone https://github.com/yourusername/DNFileRAG.git
cd DNFileRAG
dotnet build
```

### 3. Configure

Edit `src/DNFileRAG/appsettings.json`:

```json
{
  "Qdrant": {
    "Host": "localhost",
    "Port": 6333,
    "CollectionName": "DNFileRAG",
    "VectorSize": 1536
  },
  "Embedding": {
    "Provider": "Ollama",
    "Ollama": {
      "BaseUrl": "http://localhost:11434",
      "Model": "nomic-embed-text"
    }
  },
  "Llm": {
    "Provider": "Ollama",
    "Ollama": {
      "BaseUrl": "http://localhost:11434",
      "Model": "llama3.2"
    }
  }
}
```

### 4. Run

```bash
cd src/DNFileRAG
dotnet run
```

The API will be available at `http://localhost:8181`

## API Endpoints

| Method | Endpoint | Description |
|--------|----------|-------------|
| `GET` | `/api/documents` | List all indexed documents |
| `POST` | `/api/documents/upload` | Upload a document for indexing |
| `DELETE` | `/api/documents/{fileId}` | Remove a document from the index |
| `POST` | `/api/query` | Query the RAG engine |
| `POST` | `/api/reindex` | Trigger full reindex |
| `GET` | `/health` | Health check |

### Query Example

```bash
curl -X POST http://localhost:8181/api/query \
  -H "Content-Type: application/json" \
  -d '{"query": "What is the main topic of the documents?"}'
```

## Project Structure

```
DNFileRAG/
├── src/
│   ├── DNFileRAG/                 # Web API host
│   ├── DNFileRAG.Core/            # Domain models & interfaces
│   └── DNFileRAG.Infrastructure/  # External service implementations
│       ├── Embeddings/            # Embedding providers
│       ├── Llm/                   # LLM providers
│       ├── Parsers/               # Document parsers
│       ├── Services/              # Core services
│       └── VectorStore/           # Qdrant integration
└── tests/
    └── DNFileRAG.Tests/           # Unit tests
```

## Configuration

### Embedding Providers

| Provider | Config Key | Requirements |
|----------|------------|--------------|
| OpenAI | `OpenAI` | API Key |
| Azure OpenAI | `AzureOpenAI` | Endpoint, API Key, Deployment |
| Ollama | `Ollama` | Local Ollama instance |

### LLM Providers

| Provider | Config Key | Requirements |
|----------|------------|--------------|
| OpenAI | `OpenAI` | API Key |
| Azure OpenAI | `AzureOpenAI` | Endpoint, API Key, Deployment |
| Anthropic | `Anthropic` | API Key |
| Ollama | `Ollama` | Local Ollama instance |

### Local Development with Ollama

For fully local development without API keys:

1. Install [Ollama](https://ollama.ai/)
2. Pull required models:
   ```bash
   ollama pull nomic-embed-text
   ollama pull llama3.2
   ```
3. Set providers to `Ollama` in configuration

## Docker

```bash
docker-compose up -d
```

## Contributing

Contributions are welcome! Please see [CONTRIBUTING.md](CONTRIBUTING.md) for guidelines.

## License

This project is licensed under the MIT License - see the [LICENSE](LICENSE) file for details.

## Acknowledgments

- [Qdrant](https://qdrant.tech/) - Vector database
- [Serilog](https://serilog.net/) - Structured logging
- [PdfPig](https://github.com/UglyToad/PdfPig) - PDF parsing
- [DocumentFormat.OpenXml](https://github.com/OfficeDev/Open-XML-SDK) - DOCX parsing
