using ClaimsModule.Application.Documents.Commands.UploadDocument;
using ClaimsModule.Application.Documents.Queries.ListDocuments;
using ClaimsModule.Domain.Enums;
using MediatR;
using Microsoft.AspNetCore.Mvc;

namespace ClaimsModule.API.Controllers;

[ApiController]
[Route("api/claims/{claimId:guid}/documents")]
public class DocumentsController : ControllerBase
{
    private readonly IMediator _mediator;
    public DocumentsController(IMediator mediator) => _mediator = mediator;

    // Wrap the form parts in a single DTO so Swashbuckle generates a valid
    // multipart/form-data schema. Mixing [FromForm] IFormFile with sibling
    // [FromForm] scalar parameters trips Swagger generation.
    public class UploadDocumentForm
    {
        public IFormFile File { get; set; } = default!;
        public DocumentType DocumentType { get; set; } = DocumentType.Other;
    }

    [HttpPost]
    [Consumes("multipart/form-data")]
    [RequestSizeLimit(50 * 1024 * 1024)]
    public async Task<IActionResult> Upload(
        [FromRoute] Guid claimId,
        [FromForm] UploadDocumentForm body,
        CancellationToken ct = default)
    {
        if (body.File == null || body.File.Length == 0)
            return BadRequest(new { error = "file is required" });
        await using var stream = body.File.OpenReadStream();
        var result = await _mediator.Send(new UploadDocumentCommand(
            claimId, body.File.FileName, body.File.ContentType ?? "application/octet-stream",
            body.DocumentType, stream), ct);
        return Ok(result);
    }

    [HttpGet]
    public async Task<IActionResult> List([FromRoute] Guid claimId, CancellationToken ct)
        => Ok(await _mediator.Send(new ListDocumentsQuery(claimId), ct));
}
