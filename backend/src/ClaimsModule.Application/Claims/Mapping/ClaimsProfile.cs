using AutoMapper;
using ClaimsModule.Application.Claims.Dtos;
using ClaimsModule.Domain.Entities;
using ClaimsModule.Domain.Enums;

namespace ClaimsModule.Application.Claims.Mapping;

public class ClaimsProfile : Profile
{
    public ClaimsProfile()
    {
        CreateMap<ClaimParty, ClaimPartyDto>();
        CreateMap<ClaimRiskObject, ClaimRiskObjectDto>();
        CreateMap<ReserveHistory, ReserveHistoryDto>();
        CreateMap<ClaimReserveComponent, ReserveComponentDto>()
            .ForCtorParam(nameof(ReserveComponentDto.History),
                o => o.MapFrom(s => s.History.OrderBy(h => h.ChangeSequence)));
        CreateMap<ClaimDocument, ClaimDocumentDto>();
        CreateMap<ClaimAuditLog, ClaimAuditEntryDto>();

        CreateMap<Claim, ClaimSummaryDto>()
            .ForCtorParam(nameof(ClaimSummaryDto.ReserveTotal),
                o => o.MapFrom(c => c.Reserves
                    .Where(r => r.ApprovalStatus == ReserveApprovalStatus.AutoApproved
                                || r.ApprovalStatus == ReserveApprovalStatus.Approved)
                    .Sum(r => r.CurrentAmount)));

        CreateMap<Claim, ClaimDetailDto>()
            .ForCtorParam(nameof(ClaimDetailDto.LossLocation),
                o => o.MapFrom(c => c.LossEvent != null ? c.LossEvent.LossLocation : string.Empty))
            .ForCtorParam(nameof(ClaimDetailDto.LossDescription),
                o => o.MapFrom(c => c.LossEvent != null ? c.LossEvent.LossDescription : string.Empty));
    }
}
