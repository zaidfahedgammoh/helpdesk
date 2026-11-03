using HelpDeskLive.Application.Abstractions;
using HelpDeskLive.Application.IssueTypes.DTOs;
using MediatR;

namespace HelpDeskLive.Application.IssueTypes.GetIssueTypes;

public sealed class GetIssueTypesQueryHandler
    : IRequestHandler<GetIssueTypesQuery, IReadOnlyList<IssueTypeDto>>
{
    private readonly IIssueTypeRepository _issueTypeRepository;

    public GetIssueTypesQueryHandler(IIssueTypeRepository issueTypeRepository)
    {
        _issueTypeRepository = issueTypeRepository;
    }

    public async Task<IReadOnlyList<IssueTypeDto>> Handle(
        GetIssueTypesQuery request,
        CancellationToken cancellationToken)
    {
        var issueTypes = await _issueTypeRepository.GetAllActiveAsync(
            cancellationToken);

        return issueTypes
            .Select(issueType => new IssueTypeDto(
                issueType.Id,
                issueType.Name,
                issueType.Description,
                issueType.DepartmentId,
                issueType.DefaultPriority))
            .ToList();
    }
}