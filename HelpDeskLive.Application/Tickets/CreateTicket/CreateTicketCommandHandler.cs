
using HelpDeskLive.Application.Abstractions;
using HelpDeskLive.Domain.Entities;
using HelpDeskLive.Domain.Enums;
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
        if (_currentUser.UserId is not int customerId)
        {
            throw new UnauthorizedAccessException(
                "An authenticated user is required to create a ticket.");
        }

        if (_currentUser.Role != UserRole.Customer.ToString())
        {
            throw new UnauthorizedAccessException(
                "Only customers can create tickets.");
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
                customerId,
                cancellationToken);

        if (!customerExists)
        {
            throw new UnauthorizedAccessException(
                "The authenticated customer does not exist.");
        }

        var openTicketCount =
            await _ticketRepository.CountOpenTicketsForCustomerAsync(
                customerId,
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
            customerId);

        await _ticketRepository.AddAsync(
            ticket,
            cancellationToken);

        await _ticketRepository.SaveChangesAsync(
            cancellationToken);

        return ticket.Id;
    }
}
