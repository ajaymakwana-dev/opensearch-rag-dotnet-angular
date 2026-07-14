using System.Text;
using Microsoft.AspNetCore.Mvc;
using RagOpenSearch.Application.DTOs;
using RagOpenSearch.Application.Interfaces;

namespace RagOpenSearch.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public sealed class DocumentsController : ControllerBase
{
    private readonly IDocumentIngestService _ingestService;

    public DocumentsController(IDocumentIngestService ingestService)
    {
        _ingestService = ingestService;
    }

    [HttpGet]
    public async Task<ActionResult<IReadOnlyList<DocumentListItemDto>>> List(CancellationToken cancellationToken)
    {
        var docs = await _ingestService.ListAsync(cancellationToken);
        return Ok(docs);
    }

    /// <summary>Upload a .txt / .md / .csv file (UTF-8) for RAG indexing.</summary>
    [HttpPost("upload")]
    [RequestSizeLimit(10 * 1024 * 1024)]
    public async Task<ActionResult<IngestDocumentResultDto>> Upload(IFormFile file, CancellationToken cancellationToken)
    {
        if (file is null || file.Length == 0)
        {
            return BadRequest("A non-empty file is required.");
        }

        await using var stream = file.OpenReadStream();
        using var reader = new StreamReader(stream, Encoding.UTF8, detectEncodingFromByteOrderMarks: true);
        var content = await reader.ReadToEndAsync(cancellationToken);

        var result = await _ingestService.IngestTextAsync(
            file.FileName,
            content,
            file.ContentType,
            cancellationToken);

        return Ok(result);
    }

    /// <summary>Ingest raw text without multipart upload.</summary>
    [HttpPost("ingest-text")]
    public async Task<ActionResult<IngestDocumentResultDto>> IngestText(
        [FromBody] IngestTextRequest request,
        CancellationToken cancellationToken)
    {
        if (request is null || string.IsNullOrWhiteSpace(request.Content))
        {
            return BadRequest("Content is required.");
        }

        var result = await _ingestService.IngestTextAsync(
            request.FileName ?? "pasted.txt",
            request.Content,
            "text/plain",
            cancellationToken);

        return Ok(result);
    }

    [HttpDelete("{documentId}")]
    public async Task<IActionResult> Delete(string documentId, CancellationToken cancellationToken)
    {
        var deleted = await _ingestService.DeleteAsync(documentId, cancellationToken);
        return deleted ? NoContent() : NotFound();
    }

    public sealed class IngestTextRequest
    {
        public string? FileName { get; set; }
        public string Content { get; set; } = string.Empty;
    }
}
