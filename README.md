# DNFileRAG

A **.NET 9** real-time, file-driven **RAG (Retrieval-Augmented Generation)** engine that watches a folder, ingests documents, and serves fast answers over an HTTP API (with **Qdrant** vector search).

[![License: MIT](https://img.shields.io/badge/License-MIT-yellow.svg)](https://opensource.org/licenses/MIT)
[![.NET](https://img.shields.io/badge/.NET-9.0-blue.svg)](https://dotnet.microsoft.com/)
[![CI](https://github.com/MLidstrom/DNFileRAG/actions/workflows/ci.yml/badge.svg?branch=master)](https://github.com/MLidstrom/DNFileRAG/actions/workflows/ci.yml)
[![NuGet](https://img.shields.io/nuget/v/DNFileRAG.Infrastructure.svg)](https://www.nuget.org/packages/DNFileRAG.Infrastructure)

<a href="https://youtu.be/6O5fafYHkAc">
  <img src="https://img.youtube.com/vi/6O5fafYHkAc/maxresdefault.jpg" alt="Watch the demo" width="600">
</a>

*Click to watch the demo video*

## What you get

- **Real-time ingestion**: watches a folder and keeps your index up to date
- **Formats**: `.pdf`, `.docx`, `.txt`, `.md`, `.html`, `.png`, `.jpg`, `.jpeg`, `.webp`
- **Providers**: OpenAI / Azure OpenAI / Anthropic / Ollama (local)
- **Vector store**: Qdrant
- **API**: `/api/query`, `/api/documents`, `/api/health`
- **Example UI**: mock company landing page + popup help chat (`examples/HelpChat`)

## Choose your path

- **Tutorial 1: Local dev (recommended)**: Ollama + Qdrant + `dotnet run` (fastest to try)
- **Tutorial 2: HelpChat demo UI**: run a static page that calls your local API
- **Tutorial 3: Docker deploy**: `docker-compose up -d` (self-contained stack)
- **Tutorial 4: Testing**: fast vs integration tests
- **Tutorial 5: Production tips**: hardening checklist

---

## Tutorial 1 — Local dev (Ollama + Qdrant)

This uses the defaults in `src/DNFileRAG/appsettings.Development.json`:
- API on `http://localhost:8181`
- `ApiSecurity:RequireApiKey = false` (no key needed)
- Embeddings + LLM via **Ollama**
- Qdrant vector size **1024** (matches `mxbai-embed-large`)

### Step 1) Start Qdrant

```bash
docker run -p 6333:6333 -p 6334:6334 qdrant/qdrant
```

### Step 2) Install Ollama + pull models

```bash
ollama pull mxbai-embed-large
ollama pull llama3.2:3b
ollama pull llava
```

> Images: Vision extraction is enabled by default in Development via `Vision:Enabled=true` and uses the Ollama model `llava` (configurable).

### Step 3) Run DNFileRAG

```bash
dotnet run --project ./src/DNFileRAG
```

### Step 4) Add documents

Put files into:
- `src/DNFileRAG/data/documents/`

If you want to watch a different folder, change `FileWatcher:WatchPath` in:
- `src/DNFileRAG/appsettings.Development.json`

### Step 5) Verify indexing

```bash
curl http://localhost:8181/api/documents
```

### Step 6) Query

- **cURL**

```bash
curl -X POST http://localhost:8181/api/query \
  -H "Content-Type: application/json" \
  -d "{\"query\":\"What are our support hours?\"}"
```

- **PowerShell**

```bash
Invoke-RestMethod http://localhost:8181/api/query -Method Post -ContentType "application/json" -Body (@{ query = "What are our support hours?" } | ConvertTo-Json)
```

---

## Tutorial 2 — HelpChat demo UI (landing page + popup chat)

HelpChat is a static mock company page under `examples/HelpChat/` that opens a popup chat and calls your local DNFileRAG API.

### Step 1) Start DNFileRAG

Follow Tutorial 1 so the API is running at `http://localhost:8181`.

### Step 2) Serve the static files

```bash
cd examples/HelpChat
python -m http.server 3000
```

### Step 3) Open it

Open `http://localhost:3000` and click **Help**.

> You can open `examples/HelpChat/index.html` directly, but some browsers restrict `file://` pages from calling `http://localhost`.

---

## Tutorial 3 — Docker deploy (self-contained stack)

This uses `docker-compose.yml` to run:
- Qdrant
- Ollama
- DNFileRAG API on `http://localhost:8080`

### Step 1) Start the stack

```bash
docker-compose up -d
```

### Step 2) Add documents

Files in `./documents` are mounted into the container at `/app/data/documents`:

```bash
mkdir -p documents
cp /path/to/your/files/* documents/
```

### Step 3) Query

```bash
curl -X POST http://localhost:8080/api/query \
  -H "Content-Type: application/json" \
  -d '{"query":"What are our support hours?"}'
```

### Stop / reset

```bash
docker-compose down
docker-compose down -v   # also removes Qdrant + Ollama volumes
```

---

## Tutorial 4 — Testing

### Fast tests (unit tests + fast checks)

- **Windows (PowerShell)**

```bash
dotnet test .\DNFileRAG.sln -c Release --filter "Category!=Integration"
```

- **macOS/Linux (bash/zsh)**

```bash
dotnet test ./DNFileRAG.sln -c Release --filter "Category!=Integration"
```

### Full suite (includes integration tests)

Integration tests may start Docker containers (Testcontainers) and will run slower.

- **Windows (PowerShell)**

```bash
dotnet test .\DNFileRAG.sln -c Release
```

- **macOS/Linux (bash/zsh)**

```bash
dotnet test ./DNFileRAG.sln -c Release
```

### Note on FluentAssertions licensing

Tests use **FluentAssertions**. If you plan commercial use, you may need a commercial license (see the warning emitted during test runs).

---

## Tutorial 5 — Production tips (checklist)

### Security

- **Enable API keys**: set `ApiSecurity:RequireApiKey = true` and configure `ApiSecurity:ApiKeys`.
- **Run behind HTTPS**: terminate TLS at a reverse proxy (or configure Kestrel HTTPS). Ensure forwarded headers are configured if applicable.
- **CORS**: lock down origins (avoid `AllowAnyOrigin()` for production).

### Reliability

- **Persist Qdrant**: store Qdrant data on durable storage (volumes/backups).
- **Health checks**: use `/api/health` and `/api/health/detailed` for monitoring.
- **Resource sizing**: embeddings + parsing can be CPU/RAM heavy; size accordingly.

### Performance & quality

- **Vector size must match your embedding model** (e.g., `mxbai-embed-large` → 1024).
- **Tune chunking**: `Chunking:ChunkSize` and `Chunking:ChunkOverlap`.
- **Tune retrieval**: `Rag:DefaultTopK` and `Rag:MinRelevanceScore`.

### Ops

- **Logging**: keep Production log levels at Info/Warn (Debug is noisy).
- **Documents path**: ensure your `FileWatcher:WatchPath` points to the mounted directory in your environment.

---

## API reference

| Method | Endpoint | Description |
|--------|----------|-------------|
| `POST` | `/api/query` | Query the RAG engine |
| `GET` | `/api/documents` | List indexed documents |
| `POST` | `/api/documents/reindex` | Trigger full reindex |
| `DELETE` | `/api/documents?filePath=...` | Remove a document from the index |
| `GET` | `/api/health` | Basic health check |
| `GET` | `/api/health/detailed` | Detailed component health status |

## Using as a NuGet package

```bash
dotnet add package DNFileRAG.Infrastructure
```

Then register services:

```csharp
using DNFileRAG;

var builder = WebApplication.CreateBuilder(args);
builder.Services.AddDNFileRAGServices(builder.Configuration);
```

## Project structure

```
DNFileRAG/
├── src/
│   ├── DNFileRAG/                 # Web API host
│   ├── DNFileRAG.Core/            # Domain models & interfaces
│   └── DNFileRAG.Infrastructure/  # External service implementations
├── tests/
│   └── DNFileRAG.Tests/           # Test suite
└── examples/
    └── HelpChat/                  # Mock landing page + popup support chat
```

## Contributing

Contributions are welcome! Please see `CONTRIBUTING.md` for guidelines.

## License

MIT License — see `LICENSE`.

## Acknowledgments

- [Qdrant](https://qdrant.tech/) - Vector database
- [Serilog](https://serilog.net/) - Structured logging
- [PdfPig](https://github.com/UglyToad/PdfPig) - PDF parsing
- [DocumentFormat.OpenXml](https://github.com/OfficeDev/Open-XML-SDK) - DOCX parsing


