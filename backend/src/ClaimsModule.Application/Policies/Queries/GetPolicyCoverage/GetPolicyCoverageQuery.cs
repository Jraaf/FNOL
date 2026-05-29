using ClaimsModule.Application.Common.Exceptions;
using ClaimsModule.Application.Common.Interfaces;
using MediatR;

namespace ClaimsModule.Application.Policies.Queries.GetPolicyCoverage;

public record GetPolicyCoverageQuery(Guid PolicyId) : IRequest<IReadOnlyCollection<PolicyCoverage>>;

public class GetPolicyCoverageHandler : IRequestHandler<GetPolicyCoverageQuery, IReadOnlyCollection<PolicyCoverage>>
{
    private readonly IPolicyService _service;
    public GetPolicyCoverageHandler(IPolicyService service) => _service = service;

    public async Task<IReadOnlyCollection<PolicyCoverage>> Handle(GetPolicyCoverageQuery request, CancellationToken ct)
    {
        _ = await _service.GetByIdAsync(request.PolicyId, ct)
            ?? throw new NotFoundException("Policy", request.PolicyId);
        return await _service.GetCoveragesAsync(request.PolicyId, ct);
    }
}
