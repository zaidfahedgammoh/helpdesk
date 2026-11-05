using MediatR;

namespace HelpDeskLive.Application.Departments;

public sealed record GetAgentDepartmentQuery(int AgentId) : IRequest<int?>;
