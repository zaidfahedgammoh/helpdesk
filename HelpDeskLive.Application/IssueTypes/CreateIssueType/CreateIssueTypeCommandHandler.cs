using HelpDeskLive.Application.Abstractions;
using HelpDeskLive.Domain.Entities;
using MediatR;

namespace HelpDeskLive.Application.IssueTypes.CreateIssueType;

public sealed class CreateIssueTypeCommandHandler : IRequestHandler<CreateIssueTypeCommand, int>
{
    private readonly IIssueTypeRepository _issueTypeRepository;
    private readonly ITicketRepository _ticketRepository;

    public CreateIssueTypeCommandHandler(IIssueTypeRepository issueTypeRepository, ITicketRepository ticketRepository)
    {
        _issueTypeRepository = issueTypeRepository;
        _ticketRepository = ticketRepository;
    }

    public async Task<int> Handle(CreateIssueTypeCommand request, CancellationToken cancellationToken)
    {
        var departmentExists = await _ticketRepository.DepartmentExistsAsync(request.DepartmentId, cancellationToken);
        if (!departmentExists)
            throw new ArgumentException("Department does not exist.");

        var issueType = new IssueType(request.Name, request.Description, request.DepartmentId, request.DefaultPriority);
        await _issueTypeRepository.AddAsync(issueType, cancellationToken);
        await _issueTypeRepository.SaveChangesAsync(cancellationToken);
        return issueType.Id;
    }
}