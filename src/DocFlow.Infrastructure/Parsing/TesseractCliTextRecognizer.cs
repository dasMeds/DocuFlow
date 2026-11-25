using System.ComponentModel;
using System.Diagnostics;
using DocFlow.Core.Models;

namespace DocFlow.Infrastructure.Parsing;

/// <summary>
/// Runs the Tesseract command-line OCR engine with the configured language data directory.
/// </summary>
public sealed class TesseractCliTextRecognizer : IImageTextRecognizer
{
    private readonly string _tessdataDirectory;
    private readonly string _executablePath;

    /// <summary>
    /// Creates a recognizer that looks for English data under the supplied tessdata directory.
    /// </summary>
    public TesseractCliTextRecognizer(string tessdataDirectory, string executablePath = "tesseract")
    {
        _tessdataDirectory = Path.GetFullPath(tessdataDirectory);
        _executablePath = executablePath;
    }

    /// <inheritdoc />
    public async Task<DocumentTextExtraction> RecognizeTextAsync(string imagePath, CancellationToken ct = default)
    {
        var languageFile = Path.Combine(_tessdataDirectory, "eng.traineddata");
        if (!File.Exists(languageFile))
        {
            return new DocumentTextExtraction(
                string.Empty,
                $"OCR is not configured: place eng.traineddata in '{_tessdataDirectory}' to enable image text extraction.");
        }

        var startInfo = new ProcessStartInfo
        {
            FileName = _executablePath,
            RedirectStandardError = true,
            RedirectStandardOutput = true,
            UseShellExecute = false,
            CreateNoWindow = true
        };
        startInfo.ArgumentList.Add(imagePath);
        startInfo.ArgumentList.Add("stdout");
        startInfo.ArgumentList.Add("-l");
        startInfo.ArgumentList.Add("eng");
        startInfo.ArgumentList.Add("--tessdata-dir");
        startInfo.ArgumentList.Add(_tessdataDirectory);

        using var process = new Process { StartInfo = startInfo };
        try
        {
            if (!process.Start())
            {
                return new DocumentTextExtraction(string.Empty, "Tesseract OCR could not be started.");
            }
        }
        catch (Win32Exception)
        {
            return new DocumentTextExtraction(
                string.Empty,
                $"Tesseract OCR executable '{_executablePath}' was not found. Install Tesseract or configure its executable path.");
        }

        var outputTask = process.StandardOutput.ReadToEndAsync(ct);
        var errorTask = process.StandardError.ReadToEndAsync(ct);
        try
        {
            await process.WaitForExitAsync(ct);
        }
        catch (OperationCanceledException)
        {
            process.Kill(entireProcessTree: true);
            throw;
        }

        var output = await outputTask;
        var error = await errorTask;
        return process.ExitCode == 0
            ? new DocumentTextExtraction(output)
            : new DocumentTextExtraction(string.Empty, $"Tesseract OCR failed: {error.Trim()}");
    }
}
