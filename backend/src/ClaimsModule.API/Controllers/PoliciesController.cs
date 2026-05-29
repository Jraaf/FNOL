using ClaimsModule.Application.Policies.Queries.GetPolicyCoverage;
using ClaimsModule.Application.Policies.Queries.SearchPolicies;
using MediatR;
using Microsoft.AspNetCore.Mvc;

namespace ClaimsModule.API.Controllers;

[ApiController]
[Route("api/policies")]
public class PoliciesController : ControllerBase
{
    private readonly IMediator _mediator;
    public PoliciesController(IMediator mediator) => _mediator = mediator;

    [HttpGet("search")]
    public async Task<IActionResult> Search([FromQuery] string q = "", CancellationToken ct = default)
        => Ok(await _mediator.Send(new SearchPoliciesQuery(q), ct));

    [HttpGet("{id:guid}/coverage")]
    public async Task<IActionResult> GetCoverage([FromRoute] Guid id, CancellationToken ct)
        => Ok(await _mediator.Send(new GetPolicyCoverageQuery(id), ct));
}
