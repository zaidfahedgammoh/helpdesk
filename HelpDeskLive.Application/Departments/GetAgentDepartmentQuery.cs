using HelpDeskLive.Application.Abstractions;
using HelpDeskLive.Domain.Enums;
using MediatR;

namespace HelpDeskLive.Application.Departments;

public sealed record GetAgentDepartmentQuery(int AgentId) : IRequest<int?>;

public sealed class GetAgentDepartmentQueryHandler : IRequestHandler<GetAgentDepartmentQuery, int?>
{
    private readonly IUserRepository _userRepository;

    public GetAgentDepartmentQueryHandler(IUserRepository userRepository)
    {
        _userRepository = userRepository;
    }

    public async Task<int?> Handle(GetAgentDepartmentQuery request, CancellationToken ct)
    {
        var user = await _userRepository.GetByIdAsync(request.AgentId, ct);
        if (user is null || user.Role != UserRole.Agent) return null;
        return user.DepartmentId;
    }
}
