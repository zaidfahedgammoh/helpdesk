
using HelpDeskLive.Application.Abstractions;
using HelpDeskLive.Domain.Entities;
using MediatR;

namespace HelpDeskLive.Application.Tickets.CreateTicket;

public sealed class CreateTicketCommandHandler
    : IRequestHandler<CreateTicketCommand, int>
{
    private readonly ITicketRepository _ticketRepository;

    public CreateTicketCommandHandler(ITicketRepository ticketRepository)
    {
        _ticketRepository = ticketRepository;
    }

    public async Task<int> Handle(
        CreateTicketCommand request,
        CancellationToken cancellationToken)
    {
        var departmentExists =
            await _ticketRepository.DepartmentExistsAsync(
                request.DepartmentId,
                cancellationToken);

        if (!departmentExists)
        {
            throw new InvalidOperationException(
                "The specified department does not exist.");
        }

        var ticket = new Ticket(
            request.Title,
            request.Description,
            request.Priority,
            request.DepartmentId,
            request.CustomerId);

        await _ticketRepository.AddAsync(ticket, cancellationToken);
        await _ticketRepository.SaveChangesAsync(cancellationToken);

        return ticket.Id;
    }
}
