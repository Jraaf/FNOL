using ClaimsModule.Application.Reserves.Commands.AdjustReserve;
using ClaimsModule.Application.Reserves.Commands.ApproveReserve;
using ClaimsModule.Application.Reserves.Commands.OpenReserve;
using ClaimsModule.Application.Reserves.Commands.RejectReserve;
using ClaimsModule.Application.Reserves.Queries.ListReserves;
using ClaimsModule.Domain.Enums;
using MediatR;
using Microsoft.AspNetCore.Mvc;

namespace ClaimsModule.API.Controllers;

[ApiController]
[Route("api/claims/{claimId:guid}/reserves")]
public class ReservesController : ControllerBase
{
    private readonly IMediator _mediator;
    public ReservesController(IMediator mediator) => _mediator = mediator;

    public record OpenReserveRequest(ReserveComponentType ComponentType, decimal Amount, string? Notes);
    [HttpPost]
    public async Task<IActionResult> Open([FromRoute] Guid claimId, [FromBody] OpenReserveRequest body, CancellationToken ct)
        => Ok(await _mediator.Send(new OpenReserveCommand(claimId, body.ComponentType, body.Amount, body.Notes), ct));

    public record AdjustReserveRequest(decimal NewAmount, string ChangeReason);
    [HttpPut("{reserveId:guid}")]
    public async Task<IActionResult> Adjust([FromRoute] Guid claimId, [FromRoute] Guid reserveId,
        [FromBody] AdjustReserveRequest body, CancellationToken ct)
        => Ok(await _mediator.Send(new AdjustReserveCommand(claimId, reserveId, body.NewAmount, body.ChangeReason), ct));

    [HttpGet]
    public async Task<IActionResult> List([FromRoute] Guid claimId, CancellationToken ct)
        => Ok(await _mediator.Send(new ListReservesQuery(claimId), ct));

    public record ApproveReserveRequest(string? Comment);
    [HttpPost("{reserveId:guid}/approve")]
    public async Task<IActionResult> Approve([FromRoute] Guid claimId, [FromRoute] Guid reserveId,
        [FromBody] ApproveReserveRequest? body, CancellationToken ct)
        => Ok(await _mediator.Send(new ApproveReserveCommand(claimId, reserveId, body?.Comment), ct));

    public record RejectReserveRequest(string RejectionReason);
    [HttpPost("{reserveId:guid}/reject")]
    public async Task<IActionResult> Reject([FromRoute] Guid claimId, [FromRoute] Guid reserveId,
        [FromBody] RejectReserveRequest body, CancellationToken ct)
        => Ok(await _mediator.Send(new RejectReserveCommand(claimId, reserveId, body.RejectionReason), ct));
}
