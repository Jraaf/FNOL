using ClaimsModule.Application.Claims.Commands.CreateClaim;
using ClaimsModule.Application.Claims.Commands.TransitionClaimStatus;
using ClaimsModule.Application.Claims.Queries.GetClaimAudit;
using ClaimsModule.Application.Claims.Queries.GetClaimDetail;
using ClaimsModule.Application.Claims.Queries.ListClaims;
using ClaimsModule.Domain.Enums;
using MediatR;
using Microsoft.AspNetCore.Mvc;

namespace ClaimsModule.API.Controllers;

[ApiController]
[Route("api/claims")]
public class ClaimsController : ControllerBase
{
    private readonly IMediator _mediator;
    public ClaimsController(IMediator mediator) => _mediator = mediator;

    [HttpPost]
    [ProducesResponseType(typeof(CreateClaimResult), StatusCodes.Status201Created)]
    public async Task<IActionResult> Create([FromBody] CreateClaimCommand command, CancellationToken ct)
    {
        var result = await _mediator.Send(command, ct);
        return CreatedAtAction(nameof(GetById), new { id = result.ClaimId }, result);
    }

    [HttpGet]
    public Task<IActionResult> List(
        [FromQuery] ClaimStatus? status,
        [FromQuery] DateTimeOffset? lossDateFrom,
        [FromQuery] DateTimeOffset? lossDateTo,
        [FromQuery] string? assignedHandlerUserId,
        [FromQuery] string? causeOfLossCode,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 25,
        CancellationToken ct = default)
        => Send(new ListClaimsQuery(status, lossDateFrom, lossDateTo, assignedHandlerUserId, causeOfLossCode, page, pageSize), ct);

    [HttpGet("{id:guid}")]
    public Task<IActionResult> GetById([FromRoute] Guid id, CancellationToken ct)
        => Send(new GetClaimDetailQuery(id), ct);

    public record TransitionRequest(ClaimStatus ToStatus, string? Reason);
    [HttpPut("{id:guid}/status")]
    public Task<IActionResult> TransitionStatus([FromRoute] Guid id, [FromBody] TransitionRequest body, CancellationToken ct)
        => Send(new TransitionClaimStatusCommand(id, body.ToStatus, body.Reason), ct);

    [HttpGet("{id:guid}/audit")]
    public Task<IActionResult> GetAudit([FromRoute] Guid id, CancellationToken ct)
        => Send(new GetClaimAuditQuery(id), ct);

    private async Task<IActionResult> Send<TResponse>(IRequest<TResponse> request, CancellationToken ct)
        => Ok(await _mediator.Send(request, ct));
}
