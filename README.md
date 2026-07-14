# OpenSearch RAG Sample (.NET + Angular)

Portfolio-ready **Retrieval-Augmented Generation (RAG)** sample using:

- **ASP.NET Core Web API** (Clean Architecture + **Repository pattern**, **no MediatR**)
- **Angular 19** admin/Q&A UI
- **OpenSearch** k-NN vector search
- Optional **OpenAI** embeddings/chat, or built-in **Mock** mode for local demos

Inspired by enterprise RAG work (document ingest → embeddings → semantic retrieval → grounded answers).

## Solution layout

```
opensearch-rag-dotnet-angular/
├── docker-compose.yml          # OpenSearch (+ Dashboards)
├── sample-docs/                # Demo corpus to upload
├── backend/
│   └── src/
│       ├── RagOpenSearch.Api
│       ├── RagOpenSearch.Application   # Services + DTOs (controllers call these directly)
│       ├── RagOpenSearch.Domain        # Entities + IDocumentRepository / IVectorSearchRepository
│       └── RagOpenSearch.Infrastructure# OpenSearch repos + LLM/embedding adapters
└── frontend/                   # Angular SPA
```

```
Angular → Controllers → Application services → Repository interfaces → OpenSearch / LLM
```

| Layer | Responsibility |
|--------|----------------|
| `DocumentsController` / `RagController` | HTTP only |
| `DocumentIngestService` / `RagQueryService` | Use cases |
| `IDocumentRepository` / `IVectorSearchRepository` | Persistence + vector search contracts |
| `OpenSearch*Repository` | OpenSearch indexing & k-NN queries |
| `IEmbeddingService` / `ILlmService` | Embeddings + answer generation |

## Prerequisites

- .NET 8 SDK (or newer SDK that can target `net8.0`)
- Node.js 20+
- Docker Desktop

## 1. Start OpenSearch

```bash
docker compose up -d
```

- OpenSearch: http://localhost:9200  
- Dashboards: http://localhost:5601  

Security plugin is disabled for local demos.

## 2. Run the API

```bash
cd backend
dotnet run --project src/RagOpenSearch.Api --launch-profile http
```

Swagger: http://localhost:5244/swagger

Default LLM mode is **Mock** (no API key). To use OpenAI, edit `appsettings.json`:

```json
"Llm": {
  "Provider": "OpenAI",
  "ApiKey": "sk-...",
  "EmbeddingDimensions": 1536
}
```

When switching embedding dimensions, delete the `rag-chunks` index (or wipe the Docker volume) so the knn mapping is recreated.

## 3. Run Angular

```bash
cd frontend
npm start
```

UI: http://localhost:4200

## Try it

1. Upload `sample-docs/employee-handbook.txt` and `sample-docs/product-faq.txt`
2. Ask: *How many PTO days do full-time employees receive?*
3. Inspect cited chunks under **Sources**

## API surface

| Method | Route | Description |
|--------|--------|-------------|
| `GET` | `/api/documents` | List indexed documents |
| `POST` | `/api/documents/upload` | Multipart file ingest |
| `POST` | `/api/documents/ingest-text` | Raw JSON text ingest |
| `DELETE` | `/api/documents/{id}` | Delete document + chunks |
| `POST` | `/api/rag/search` | Semantic search only |
| `POST` | `/api/rag/ask` | Retrieve + generate answer |
| `GET` | `/health` | Health check |

## Configuration

`backend/src/RagOpenSearch.Api/appsettings.json`

- `OpenSearch:Uri` — default `http://localhost:9200`
- `Llm:Provider` — `Mock` or `OpenAI`
- `Llm:EmbeddingDimensions` — must match OpenSearch `knn_vector` dimension (default `384` for Mock)

## Why this sample

Shows a practical GitHub portfolio stack matching common enterprise RAG delivery:

- Clean separation of API / application / domain / infrastructure
- Explicit repositories instead of MediatR handlers
- Real OpenSearch k-NN indexing path
- Angular UI with upload, Q&A, and citation display

## License

MIT — use freely in portfolios and demos.
