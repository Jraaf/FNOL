using ClaimsModule.Application.Claims.Dtos;
using ClaimsModule.Domain.Enums;
using MediatR;

namespace ClaimsModule.Application.Documents.Commands.UploadDocument;

public record UploadDocumentCommand(
    Guid ClaimId,
    string FileName,
    string ContentType,
    DocumentType DocumentType,
    Stream Content) : IRequest<ClaimDocumentDto>;
