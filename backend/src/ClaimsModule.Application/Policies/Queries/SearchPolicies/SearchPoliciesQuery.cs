using ClaimsModule.Application.Common.Interfaces;
using MediatR;

namespace ClaimsModule.Application.Policies.Queries.SearchPolicies;

public record SearchPoliciesQuery(string Query) : IRequest<IReadOnlyCollection<PolicySummary>>;

public class SearchPoliciesHandler : IRequestHandler<SearchPoliciesQuery, IReadOnlyCollection<PolicySummary>>
{
    private readonly IPolicyService _service;
    public SearchPoliciesHandler(IPolicyService service) => _service = service;

    public Task<IReadOnlyCollection<PolicySummary>> Handle(SearchPoliciesQuery request, CancellationToken ct)
        => _service.SearchAsync(request.Query ?? string.Empty, ct);
}
