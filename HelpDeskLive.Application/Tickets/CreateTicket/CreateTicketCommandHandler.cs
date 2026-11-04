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

    public async Task<int> Handle(CreateTicketCommand request, CancellationToken cancellationToken)
    {
        if (_currentUser.UserId is not int customerId ||
            _currentUser.Role != UserRole.Customer.ToString())
            throw new UnauthorizedAccessException("Only authenticated customers can create tickets.");

        if (string.IsNullOrWhiteSpace(request.Description) || request.Description.Length > 5000)
            throw new ArgumentException("Enter a description of up to 5000 characters.");

        if (!await _ticketRepository.CustomerExistsAsync(customerId, cancellationToken))
            throw new UnauthorizedAccessException("The authenticated customer does not exist.");

        var openCount = await _ticketRepository.CountOpenTicketsForCustomerAsync(customerId, cancellationToken);
        if (openCount >= 3)
            throw new InvalidOperationException("A customer cannot have more than three open tickets.");

        Ticket ticket;
        if (request.IssueTypeId is int issueTypeId && issueTypeId > 0 && string.IsNullOrWhiteSpace(request.CustomTitle))
        {
            var issueType = await _issueTypeRepository.GetByIdAsync(issueTypeId, cancellationToken);
            if (issueType is null || !issueType.IsActive)
                throw new InvalidOperationException("The selected issue type is not available.");

            ticket = new Ticket(issueType.Name, request.Description, issueType.DefaultPriority, issueType.DepartmentId, customerId);
        }
        else if (request.IssueTypeId is null && !string.IsNullOrWhiteSpace(request.CustomTitle))
        {
            ticket = Ticket.CreateCustomRequest(request.CustomTitle, request.Description, customerId);
        }
        else
        {
            throw new ArgumentException("Choose a predefined issue or enter a custom title for Other.");
        }

        await _ticketRepository.AddAsync(ticket, cancellationToken);
        await _ticketRepository.SaveChangesAsync(cancellationToken);
        return ticket.Id;
    }
}
