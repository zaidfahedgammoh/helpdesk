
using HelpDeskLive.Application.Abstractions;
using HelpDeskLive.Domain.Entities;
using MediatR;

namespace HelpDeskLive.Application.Tickets.CreateTicket;

public sealed class CreateTicketCommandHandler
    : IRequestHandler<CreateTicketCommand, int>
{
    private readonly ITicketRepository _ticketRepository;
    private readonly ICurrentUser _currentUser;

    public CreateTicketCommandHandler(
        ITicketRepository ticketRepository,
        ICurrentUser currentUser)
    {
        _ticketRepository = ticketRepository;
        _currentUser = currentUser;
    }

    public async Task<int> Handle(
        CreateTicketCommand request,
        CancellationToken cancellationToken)
    {
        var customerId = _currentUser.UserId;

        if (customerId is null)
        {
            throw new UnauthorizedAccessException(
                "An authenticated user is required to create a ticket.");
        }

        var departmentExists =
            await _ticketRepository.DepartmentExistsAsync(
                request.DepartmentId,
                cancellationToken);

        if (!departmentExists)
        {
            throw new InvalidOperationException(
                "The specified department does not exist.");
        }

        var customerExists =
            await _ticketRepository.CustomerExistsAsync(
                customerId.Value,
                cancellationToken);

        if (!customerExists)
        {
            throw new UnauthorizedAccessException(
                "Only authenticated customers can create tickets.");
        }

        var openTicketCount =
            await _ticketRepository.CountOpenTicketsForCustomerAsync(
                customerId.Value,
                cancellationToken);

        if (openTicketCount >= 3)
        {
            throw new InvalidOperationException(
                "A customer cannot have more than three open tickets.");
        }

        var ticket = new Ticket(
            request.Title,
            request.Description,
            request.Priority,
            request.DepartmentId,
            customerId.Value);

        await _ticketRepository.AddAsync(
            ticket,
            cancellationToken);

        await _ticketRepository.SaveChangesAsync(
            cancellationToken);

        return ticket.Id;
    }
}
