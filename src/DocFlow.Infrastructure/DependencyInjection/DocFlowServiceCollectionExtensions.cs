using DocFlow.Core.Interfaces;
using DocFlow.Infrastructure.Parsing;
using DocFlow.Infrastructure.Persistence;
using DocFlow.Infrastructure.Queue;
using DocFlow.Infrastructure.Workers;
using Microsoft.Extensions.DependencyInjection;

namespace DocFlow.Infrastructure.DependencyInjection;

/// <summary>
/// Registers the document-processing pipeline and its pluggable format processors.
/// </summary>
public static class DocFlowServiceCollectionExtensions
{
    /// <summary>
    /// Adds queue, persistence, processors, extraction, OCR, and background-worker services.
    /// </summary>
    public static IServiceCollection AddDocFlowProcessing(
        this IServiceCollection services,
        string tessdataDirectory,
        string tesseractExecutable = "tesseract")
    {
        services.AddSingleton<IDocumentQueue, ChannelDocumentQueue>();
        services.AddSingleton<IJobRepository, InMemoryJobRepository>();
        services.AddSingleton<IInvoiceFieldExtractor, InvoiceFieldExtractor>();
        services.AddSingleton<IDocumentProcessor, PlainTextDocumentProcessor>();
        services.AddSingleton<IDocumentProcessor, PdfDocumentProcessor>();
        services.AddSingleton<IDocumentProcessor, OfficeDocumentProcessor>();
        services.AddSingleton<IDocumentProcessor, HtmlDocumentProcessor>();
        services.AddSingleton<IImageTextRecognizer>(_ =>
            new TesseractCliTextRecognizer(tessdataDirectory, tesseractExecutable));
        services.AddSingleton<IDocumentProcessor, ImageOcrDocumentProcessor>();
        services.AddSingleton<IDocumentProcessorFactory, DocumentProcessorFactory>();
        services.AddHostedService<DocumentProcessingWorker>();
        return services;
    }
}
