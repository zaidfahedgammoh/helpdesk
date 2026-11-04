using AutoMapper;
using HelpDeskLive.Application.Abstractions;
using HelpDeskLive.Application.Common.Exceptions;
using HelpDeskLive.Domain.Enums;
using MediatR;

namespace HelpDeskLive.Application.Tickets.GetTicketById;

public sealed record GetTicketByIdQuery(int TicketId) : IRequest<TicketDto>;

public sealed class GetTicketByIdQueryHandler : IRequestHandler<GetTicketByIdQuery, TicketDto>
{
    private readonly ITicketRepository _ticketRepository;
    private readonly IUserRepository _userRepository;
    private readonly ICurrentUser _currentUser;
    private readonly IMapper _mapper;

    public GetTicketByIdQueryHandler(
        ITicketRepository ticketRepository,
        IUserRepository userRepository,
        ICurrentUser currentUser,
        IMapper mapper)
    {
        _ticketRepository = ticketRepository;
        _userRepository = userRepository;
        _currentUser = currentUser;
        _mapper = mapper;
    }

    public async Task<TicketDto> Handle(GetTicketByIdQuery request, CancellationToken ct)
    {
        if (_currentUser.UserId is not int userId)
            throw new UnauthorizedException();

        var ticket = await _ticketRepository.GetByIdAsync(request.TicketId, ct);
        if (ticket is null)
            throw new NotFoundException("Ticket", request.TicketId);

        if (_currentUser.Role == UserRole.Customer.ToString())
        {
            if (ticket.CustomerId != userId)
                throw new ForbiddenException("You cannot view tickets that do not belong to you.");
        }
        else if (_currentUser.Role == UserRole.Agent.ToString())
        {
            var agent = await _userRepository.GetByIdAsync(userId, ct);
            if (agent is null || agent.DepartmentId != ticket.DepartmentId)
                throw new ForbiddenException("You cannot view tickets outside your department.");
        }

        return _mapper.Map<TicketDto>(ticket);
    }
}
