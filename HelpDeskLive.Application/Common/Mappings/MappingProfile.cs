using AutoMapper;
using HelpDeskLive.Application.Tickets;
using HelpDeskLive.Domain.Entities;

namespace HelpDeskLive.Application.Common.Mappings;

public class MappingProfile : Profile
{
    public MappingProfile()
    {
        CreateMap<Ticket, TicketDto>()
            .ForMember(dest => dest.AgentName, opt => opt.MapFrom(src => src.AssignedAgentId.HasValue ? $"Agent #{src.AssignedAgentId}" : null));
    }
}
