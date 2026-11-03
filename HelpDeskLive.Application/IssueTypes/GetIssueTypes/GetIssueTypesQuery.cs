
using HelpDeskLive.Application.IssueTypes.DTOs;
using MediatR;

namespace HelpDeskLive.Application.IssueTypes.GetIssueTypes;

public sealed record GetIssueTypesQuery
    : IRequest<IReadOnlyList<IssueTypeDto>>;
