using HelpDeskLive.Application.Abstractions;
using HelpDeskLive.Domain.Entities;
using HelpDeskLive.Domain.Enums;
using MediatR;

namespace HelpDeskLive.Application.Tickets.CreateTicket;

public sealed class CreateTicketCommandHandler
    : IRequestHandler<CreateTicketCommand, int>
{
    private readonly ITicketRepository _ticketRepository;
    private readonly IIssueTypeRepository _issueTypeRepository;
    private readonly ICurrentUser _currentUser;

    public CreateTicketCommandHandler(
        ITicketRepository ticketRepository,
        IIssueTypeRepository issueTypeRepository,
        ICurrentUser currentUser)
    {
        _ticketRepository = ticketRepository;
        _issueTypeRepository = issueTypeRepository;
        _currentUser = currentUser;
    }

    public async Task<int> Handle(
        CreateTicketCommand request,
        CancellationToken cancellationToken)
    {
        if (_currentUser.UserId is not int customerId ||
            _currentUser.Role != UserRole.Customer.ToString())
        {
            throw new UnauthorizedAccessException(
                "Only authenticated customers can create tickets.");
        }

        var issueType = await _issueTypeRepository.GetByIdAsync(
            request.IssueTypeId,
            cancellationToken);

        if (issueType is null || !issueType.IsActive)
            throw new InvalidOperationException("The selected issue type is not available.");

        if (string.IsNullOrWhiteSpace(request.Description))
            throw new ArgumentException("Ticket description is required.");

        if (request.Description.Length > 5000)
            throw new ArgumentException("Ticket description cannot exceed 5000 characters.");

        var customerExists = await _ticketRepository.CustomerExistsAsync(
            customerId,
            cancellationToken);

        if (!customerExists)
            throw new UnauthorizedAccessException("The authenticated customer does not exist.");

        var openTicketCount = await _ticketRepository.CountOpenTicketsForCustomerAsync(
            customerId,
            cancellationToken);

        if (openTicketCount >= 3)
            throw new InvalidOperationException("A customer cannot have more than three open tickets.");

        var ticket = new Ticket(
            issueType.Name,
            request.Description,
            issueType.DefaultPriority,
            issueType.DepartmentId,
            customerId);

        await _ticketRepository.AddAsync(ticket, cancellationToken);
        await _ticketRepository.SaveChangesAsync(cancellationToken);

        return ticket.Id;
    }
}