using ClaimsModule.Application.Claims.Dtos;
using MediatR;

namespace ClaimsModule.Application.Reserves.Queries.ListReserves;

public record ListReservesQuery(Guid ClaimId) : IRequest<IReadOnlyCollection<ReserveComponentDto>>;
