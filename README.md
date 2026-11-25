# DocFlow — Async Document Processing Pipeline

DocFlow is a .NET 11 document-processing solution with a shared domain model, pluggable format parsers, a bounded background queue, an ASP.NET Core/SignalR workbench, and a Windows WPF client. The API and desktop app use the same processing services; the in-memory repository can be replaced without changing the worker.

## Architecture

```text
DocFlow-Async-Pipeline/
├── DocFlow.slnx
├── README.md
├── src/
│   ├── DocFlow.Core/
│   │   ├── Enums/                 # Job lifecycle and pipeline stages
│   │   ├── Interfaces/            # Queue, parser, factory, repository, notifier contracts
│   │   ├── Models/                # Job metadata and extracted invoice data
│   │   └── Validation/            # Decimal-safe invoice rules
│   ├── DocFlow.Infrastructure/
│   │   ├── DependencyInjection/   # Shared service registration
│   │   ├── Parsing/               # Text, PDF, Office, HTML, and image/OCR processors
│   │   ├── Persistence/           # Thread-safe in-memory repository
│   │   ├── Queue/                 # Bounded Channel<Guid> with backpressure
│   │   └── Workers/               # Extraction, validation, notification, and retries
│   ├── DocFlow.Api/
│   │   ├── Hubs/                  # SignalR notifications
│   │   └── wwwroot/
│   │       ├── css/               # Design tokens and responsive workbench styles
│   │       ├── js/                # API, filtering, review actions, and live updates
│   │       └── index.html         # Semantic workbench markup
│   └── DocFlow.Desktop/
│       ├── Services/              # WPF UI-thread notifications
│       ├── Styles/                # Shared WPF colors, brushes, and control styles
│       └── ViewModels/             # Desktop presentation state
└── tests/
    └── DocFlow.Tests/              # Validation, parser routing, and field extraction tests
```

Documents flow from the API or WPF intake into the bounded queue. `DocumentProcessorFactory` selects a parser by extension, `InvoiceFieldExtractor` normalizes fields from the returned text, and `DocumentProcessingWorker` validates and persists the result. Extraction issues become review diagnostics; transient parser failures use exponential retry backoff.

## Supported document formats

| Extension | Parser | Extraction strategy |
|---|---|---|
| `.pdf` | UglyToad.PdfPig | Text from every PDF page |
| `.docx` | DocumentFormat.OpenXml | Word paragraphs and table text |
| `.pptx` | DocumentFormat.OpenXml | Text shapes in presentation slide order |
| `.html`, `.htm` | HtmlAgilityPack | Removes script/style nodes and extracts decoded text |
| `.png`, `.jpg`, `.jpeg`, `.webp` | SixLabors.ImageSharp + Tesseract CLI | Normalizes images to PNG, then runs English OCR |
| `.txt`, `.csv`, `.json`, `.md` | .NET async stream reader | Reads source text asynchronously |

All parsers return text to the same field extractor. It recognizes common labels for invoice number, vendor, issue date, subtotal, tax, and total; unrecognized fields remain empty instead of being fabricated. A 500-character raw-text preview and any non-fatal parser diagnostic are kept on the job.

## Prerequisites

- .NET 11 SDK.
- Windows 10/11 to build or run the WPF desktop client. The API and tests run cross-platform.
- Tesseract is optional for image OCR. Install the Tesseract executable and place `eng.traineddata` in `src/DocFlow.Api/tessdata/` for the API, or in the desktop application's `tessdata/` output folder. The API executable path can be set with `Tesseract__ExecutablePath`; the desktop executable path can be set with `TESSERACT_EXECUTABLE`. Without the language data or executable, image jobs receive a readable diagnostic and are routed to review.
- **ImageSharp licensing:** review the [Six Labors license terms](https://sixlabors.com/pricing/license/) for your use case. Closed-source, for-profit software from a business above the stated revenue threshold requires a commercial license. Configure the license as instructed by Six Labors before distributing a production build when applicable.

## Run locally

Run commands from the repository root.

### ASP.NET Core workbench

```powershell
dotnet run --project .\src\DocFlow.Api\DocFlow.Api.csproj
```

Open the localhost URL printed by ASP.NET Core. The dashboard can upload supported documents, show job status, review extracted fields, retry failed jobs, and export CSV.

### WPF desktop client (Windows)

```powershell
dotnet run --project .\src\DocFlow.Desktop\DocFlow.Desktop.csproj
```

Select or drag files into the workbench. Unsupported extensions are shown as failed jobs with a diagnostic.

### Tests

```powershell
dotnet test .\tests\DocFlow.Tests\DocFlow.Tests.csproj
```

## HTTP and SignalR reference

| Method | Route | Description |
|---|---|---|
| `POST` | `/api/documents` | Upload one or more multipart files; returns `202 Accepted` with created jobs |
| `GET` | `/api/jobs` | List jobs, newest first |
| `GET` | `/api/jobs/{id}` | Get one job by ID |
| `POST` | `/api/jobs/{id}/approve` | Submit corrected invoice fields for a `NeedsReview` job |
| `POST` | `/api/jobs/{id}/retry` | Requeue a `Failed` job |
| `GET` | `/api/jobs/export` | Download job and invoice data as CSV |
| SignalR | `/hubs/pipeline` | Receives `JobUpdated` events for queue, processing, and review changes |

Approval validates required invoice number/vendor and decimal totals before marking the job complete. Retry resets the attempt count and is only accepted for failed jobs.

## Extend or replace services

### Add a document processor

1. Implement `IDocumentProcessor` in `DocFlow.Infrastructure/Parsing/`.
2. Provide `SupportedExtensions`, `CanHandle`, and `ExtractTextAsync`; return the extracted text and an optional diagnostic in `DocumentTextExtraction`.
3. Register it in `DocFlowServiceCollectionExtensions.AddDocFlowProcessing`.
4. Add factory routing and parser tests in `DocFlow.Tests`.

The shared `InvoiceFieldExtractor` runs after every format processor, so a custom parser only needs to return readable text.

### Replace the in-memory repository

`IJobRepository` is the persistence boundary used by the API, desktop app, and worker. To move to EF Core with SQLite, SQL Server, or PostgreSQL:

1. Add the provider package and a `DbContext` with `DocumentJob`/invoice entity configuration.
2. Implement `IJobRepository` using asynchronous EF Core operations and migrations.
3. Register the provider and repository implementation in `AddDocFlowProcessing`, replacing `InMemoryJobRepository`.
4. Keep queue and repository durability consistent. For production recovery, persist queued/outstanding state or use a durable message broker; the current `Channel<Guid>` is intentionally process-local.

The in-memory implementation returns copies of records to avoid callers mutating shared repository state outside repository operations.
