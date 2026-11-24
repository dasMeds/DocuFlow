# DocFlow — Async Document Processing Pipeline

A clean, modular **.NET 8** solution demonstrating an **Async Document Processing Pipeline** with a shared core engine, **ASP.NET Core Web API + SignalR**, **C# Desktop GUI (WPF / MVVM)**, and background queue workers (`BackgroundService` + `System.Threading.Channels`).

---

## Architecture Overview

```text
DocFlow-Async-Pipeline/
├── src/
│   ├── DocFlow.Core/              # Domain models, status enums, interfaces, validation rules
│   ├── DocFlow.Infrastructure/    # Bounded Channel<T> queue, BackgroundService workers, parsers, repository
│   ├── DocFlow.Api/               # ASP.NET Core Web API, SignalR real-time hub, browser dashboard
│   └── DocFlow.Desktop/           # C# Desktop GUI (WPF MVVM) for local batch intake, review & monitoring
└── tests/
    └── DocFlow.Tests/             # Unit tests for extraction, validation rules, and state transitions
```

### How the Pipeline Works

1. **Instant Intake (`Queued`):** Documents (`.pdf`, `.csv`, `.json`, `.txt`) are dropped via the Desktop GUI or uploaded via `POST /api/documents`. Files are persisted immediately and a job reference (`JobId`) is enqueued without blocking the UI or HTTP request.
2. **Background Worker Execution (`Processing`):** Background worker tasks pull jobs from the bounded queue and execute distinct stages:
   - `Extracting`: Parses text, structured fields, or line items via pluggable `IDocumentProcessor` implementations.
   - `Validating`: Checks required fields and `decimal` financial totals.
3. **Human-in-the-Loop & Dead-Letter Handling:**
   - `Completed`: Passed validation and persisted cleanly.
   - `NeedsReview`: Extraction succeeded, but a business rule flagged a discrepancy (e.g., line items do not match total) for manual inspection and approval.
   - `Failed`: Exhausted automatic retries with exponential backoff; isolated with error diagnostics for one-click retry.

---

## Prerequisites

- [.NET 8 SDK](https://dotnet.microsoft.com/download/dotnet/8.0)
- Windows 10/11 (required only if building/running `DocFlow.Desktop`; `DocFlow.Api` and `DocFlow.Core` run cross-platform on Windows, macOS, and Linux)

---

## Quick Start

### 1. Run the Web API & Browser Workbench
```bash
dotnet run --project src/DocFlow.Api
```
Open `https://localhost:5001` (or the printed localhost URL) to access the REST API, SignalR live updates, and web dashboard.

### 2. Run the C# Desktop GUI (Windows)
```bash
dotnet run --project src/DocFlow.Desktop
```

### 3. Run Unit Tests
```bash
dotnet test
```

---

## Key API Endpoints

| Method | Endpoint | Description |
| :--- | :--- | :--- |
| `POST` | `/api/documents` | Upload one or more documents (`202 Accepted` + `JobId`) |
| `GET` | `/api/jobs` | List all pipeline jobs and current stage/status |
| `POST` | `/api/jobs/{id}/approve` | Approve or edit a job flagged as `NeedsReview` |
| `POST` | `/api/jobs/{id}/retry` | Re-enqueue a `Failed` job |
| `WS` | `/hubs/pipeline` | SignalR hub pushing live job status transitions |
