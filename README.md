# DNFileRAG

A .NET 9-powered, real-time file-driven RAG (Retrieval-Augmented Generation) engine that auto-ingests documents and serves fast, contextual query responses via API.

[![License: MIT](https://img.shields.io/badge/License-MIT-yellow.svg)](https://opensource.org/licenses/MIT)
[![.NET](https://img.shields.io/badge/.NET-9.0-blue.svg)](https://dotnet.microsoft.com/)
[![CI](https://github.com/MLidstrom/DNFileRAG/actions/workflows/ci.yml/badge.svg?branch=master)](https://github.com/MLidstrom/DNFileRAG/actions/workflows/ci.yml)
[![NuGet](https://img.shields.io/nuget/v/DNFileRAG.Infrastructure.svg)](https://www.nuget.org/packages/DNFileRAG.Infrastructure)

## Installation

### NuGet Package

```bash
dotnet add package DNFileRAG.Infrastructure
```

### From Source

```bash
git clone https://github.com/MLidstrom/DNFileRAG.git
cd DNFileRAG
dotnet build
```

## Testing

### Fast (unit tests only)

Skip Docker/Testcontainers integration tests:

- **Windows (PowerShell):**

```bash
dotnet test .\DNFileRAG.sln -c Release --filter "Category!=Integration"
```

- **macOS/Linux (bash/zsh):**

```bash
dotnet test ./DNFileRAG.sln -c Release --filter "Category!=Integration"
```

### Full (includes integration tests)

Runs everything (some tests start Docker containers via Testcontainers, so this is slower):

- **Windows (PowerShell):**

```bash
dotnet test .\DNFileRAG.sln -c Release
```

- **macOS/Linux (bash/zsh):**

```bash
dotnet test ./DNFileRAG.sln -c Release
```

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
- One of: OpenAI API key, Azure OpenAI, or [Ollama](https://ollama.ai/) for local inference (recommended for local dev)

### Local dev (recommended): Ollama + Qdrant

This path uses `appsettings.Development.json` defaults:
- **No API key required** (`ApiSecurity:RequireApiKey = false`)
- **Ollama embeddings/LLM**
- **Qdrant vector size 1024**

#### 1) Start Qdrant

```bash
docker run -p 6333:6333 -p 6334:6334 qdrant/qdrant
```

#### 2) Install Ollama + pull models

```bash
ollama pull mxbai-embed-large
ollama pull llama3.2:3b
```

#### 3) Clone and run DNFileRAG

```bash
git clone https://github.com/MLidstrom/DNFileRAG.git
cd DNFileRAG
dotnet run --project ./src/DNFileRAG
```

The API will be available at `http://localhost:8181` (Development).

#### 4) Add documents

Put files into:
- **`src/DNFileRAG/data/documents/`**

Supported extensions: `.pdf`, `.docx`, `.txt`, `.md`, `.html`

> Want a different folder? Change `FileWatcher:WatchPath` in `src/DNFileRAG/appsettings.Development.json`.

#### 5) Query the API

- **cURL:**

```bash
curl -X POST http://localhost:8181/api/query \
  -H "Content-Type: application/json" \
  -d "{\"query\":\"What are our support hours?\"}"
```

- **PowerShell:**

```bash
Invoke-RestMethod http://localhost:8181/api/query -Method Post -ContentType "application/json" -Body (@{ query = "What are our support hours?" } | ConvertTo-Json)
```

### HelpChat demo UI (mock company landing page + popup chat)

HelpChat is a static page under `examples/HelpChat/` that shows a mock company landing page with a popup support chat.

1) Start DNFileRAG (see above)
2) Serve the folder:

```bash
cd examples/HelpChat
python -m http.server 3000
```

3) Open `http://localhost:3000`

> You can also open `examples/HelpChat/index.html` directly, but some browsers restrict `file://` requests.

## API endpoints

| Method | Endpoint | Description |
|--------|----------|-------------|
| `POST` | `/api/query` | Query the RAG engine |
| `GET` | `/api/documents` | List all indexed documents |
| `POST` | `/api/documents/reindex` | Trigger full reindex |
| `DELETE` | `/api/documents?filePath=...` | Remove a document from the index |
| `GET` | `/api/health` | Basic health check |
| `GET` | `/api/health/detailed` | Detailed component health status |

### Authentication

API key authentication via `X-API-Key` header. Configure in `appsettings.json`:

```json
{
  "ApiSecurity": {
    "RequireApiKey": true,
    "ApiKeys": ["your-api-key-here"]
  }
}
```

Set `RequireApiKey: false` for development (no key required).

> Note: Development defaults already set `RequireApiKey: false` in `src/DNFileRAG/appsettings.Development.json`.

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
├── tests/
│   └── DNFileRAG.Tests/           # Unit tests
└── examples/
    └── HelpChat/                  # Static HTML/TypeScript chat client
```

## Using as a NuGet Package

Add DNFileRAG to your own .NET application to build custom RAG solutions.

### 1. Install the Package

```bash
dotnet add package DNFileRAG.Infrastructure
```

### 2. Add Configuration

Add to your `appsettings.json`:

```json
{
  "Embedding": {
    "Provider": "Ollama",
    "Ollama": {
      "BaseUrl": "http://localhost:11434",
      "Model": "mxbai-embed-large"
    }
  },
  "Llm": {
    "Provider": "Ollama",
    "Ollama": {
      "BaseUrl": "http://localhost:11434",
      "Model": "llama3.2"
    }
  },
  "Qdrant": {
    "Host": "localhost",
    "Port": 6333,
    "CollectionName": "documents",
    "VectorSize": 1024
  },
  "Rag": {
    "DefaultTopK": 5,
    "DefaultTemperature": 0.2,
    "DefaultMaxTokens": 512,
    "MinRelevanceScore": 0.6,
    "SystemPrompt": "You are a helpful assistant for our company. Answer questions based only on the provided context. Be concise and professional."
  }
}
```

### 3. Register Services

In your `Program.cs`:

```csharp
using DNFileRAG;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddDNFileRAGServices(builder.Configuration);
```

### 4. Use in Your Code

```csharp
using DNFileRAG.Core.Interfaces;
using DNFileRAG.Core.Models;

public class MyService
{
    private readonly IRagEngine _ragEngine;

    public MyService(IRagEngine ragEngine)
    {
        _ragEngine = ragEngine;
    }

    public async Task<RagResponse> AskAsync(string question)
    {
        var query = new RagQuery
        {
            Query = question,
            TopK = 5,
            Temperature = 0.2f,
            MaxTokens = 512
        };

        return await _ragEngine.QueryAsync(query);
    }
}
```

The `IRagEngine` handles embedding generation, vector search, context building, and LLM prompting automatically using your configured `SystemPrompt`.

### Available Providers

| Provider | Embeddings | LLM | Local |
|----------|:----------:|:---:|:-----:|
| OpenAI | ✅ | ✅ | ❌ |
| Azure OpenAI | ✅ | ✅ | ❌ |
| Anthropic | ❌ | ✅ | ❌ |
| Ollama | ✅ | ✅ | ✅ |

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
   ollama pull mxbai-embed-large  # Recommended: 1024 dims, stable
   ollama pull llama3.2
   ```
3. Set providers to `Ollama` in configuration
4. Configure vector size to match embedding model:

| Embedding Model | Vector Size | Notes |
|-----------------|-------------|-------|
| `mxbai-embed-large` | 1024 | Recommended for stability |
| `nomic-embed-text` | 768 | May crash on some PDFs (Ollama Windows bug) |
| `all-minilm` | 384 | Smaller, faster |

### Query Guardrails

DNFileRAG includes relevance score filtering to prevent off-topic queries:

```json
{
  "Rag": {
    "MinRelevanceScore": 0.6
  }
}
```

Queries with no documents above the threshold return a "no relevant information" response without calling the LLM, saving cost and preventing hallucination

## Docker (self-contained local stack)

Run DNFileRAG with a single command - no installation required except Docker:

```bash
# Clone and start (first run downloads large AI models)
git clone https://github.com/MLidstrom/DNFileRAG.git
cd DNFileRAG
docker-compose up -d

# Put your documents here (mapped into the container)
mkdir -p documents
cp /path/to/your/files/* documents/

# Query via API
curl -X POST http://localhost:8080/api/query \
  -H "Content-Type: application/json" \
  -d '{"query": "What are our support hours?"}'
```

**What's included:**
- Qdrant (vector database)
- Ollama (local LLM - no API keys needed)
- DNFileRAG API
- Auto-downloads AI models on first start

**Endpoints:**
- API: http://localhost:8080
- Health: http://localhost:8080/api/health

**Stop/Remove:**
```bash
docker-compose down        # Stop services
docker-compose down -v     # Stop and remove data
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
