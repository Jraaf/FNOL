using ClaimsModule.Application.Reference.Queries.GetCauseCodes;
using ClaimsModule.Application.Reference.Queries.GetClaimStatuses;
using MediatR;
using Microsoft.AspNetCore.Mvc;

namespace ClaimsModule.API.Controllers;

[ApiController]
[Route("api/reference")]
public class ReferenceController : ControllerBase
{
    private readonly IMediator _mediator;
    public ReferenceController(IMediator mediator) => _mediator = mediator;

    [HttpGet("cause-of-loss-codes")]
    public async Task<IActionResult> GetCauseCodes([FromQuery] string? perilCategory, CancellationToken ct)
        => Ok(await _mediator.Send(new GetCauseCodesQuery(perilCategory), ct));

    [HttpGet("claim-statuses")]
    public async Task<IActionResult> GetStatuses(CancellationToken ct)
        => Ok(await _mediator.Send(new GetClaimStatusesQuery(), ct));
}
