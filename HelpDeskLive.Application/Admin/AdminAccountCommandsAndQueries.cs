using FluentValidation;
using HelpDeskLive.Application.Abstractions;
using HelpDeskLive.Domain.Entities;
using HelpDeskLive.Domain.Enums;
using MediatR;

namespace HelpDeskLive.Application.Admin;

public sealed record DepartmentDto(int Id, string Name);

public sealed record GetDepartmentsQuery : IRequest<IReadOnlyList<DepartmentDto>>;

public sealed class GetDepartmentsQueryHandler : IRequestHandler<GetDepartmentsQuery, IReadOnlyList<DepartmentDto>>
{
    private readonly IDepartmentRepository _departmentRepository;

    public GetDepartmentsQueryHandler(IDepartmentRepository departmentRepository)
    {
        _departmentRepository = departmentRepository;
    }

    public async Task<IReadOnlyList<DepartmentDto>> Handle(GetDepartmentsQuery request, CancellationToken ct)
    {
        var list = await _departmentRepository.GetAllAsync(ct);
        return list.Select(d => new DepartmentDto(d.Id, d.Name)).ToList();
    }
}

public sealed record AccountDto(int Id, string Name, string Email, string Role, int? DepartmentId);

public sealed record GetAccountsByRoleQuery(UserRole Role) : IRequest<IReadOnlyList<AccountDto>>;

public sealed class GetAccountsByRoleQueryHandler : IRequestHandler<GetAccountsByRoleQuery, IReadOnlyList<AccountDto>>
{
    private readonly IUserRepository _userRepository;

    public GetAccountsByRoleQueryHandler(IUserRepository userRepository)
    {
        _userRepository = userRepository;
    }

    public async Task<IReadOnlyList<AccountDto>> Handle(GetAccountsByRoleQuery request, CancellationToken ct)
    {
        var users = await _userRepository.GetByRoleAsync(request.Role, ct);
        return users.Select(u => new AccountDto(u.Id, u.Name, u.Email, u.Role.ToString(), u.DepartmentId)).ToList();
    }
}

public sealed record CreateAccountCommand(
    string Name,
    string Email,
    string PasswordHash,
    UserRole Role,
    int? DepartmentId
) : IRequest<AccountDto>;

public sealed class CreateAccountCommandValidator : AbstractValidator<CreateAccountCommand>
{
    public CreateAccountCommandValidator()
    {
        RuleFor(x => x.Name).NotEmpty().MaximumLength(100);
        RuleFor(x => x.Email).NotEmpty().EmailAddress().MaximumLength(200);
        RuleFor(x => x.PasswordHash).NotEmpty();
        RuleFor(x => x.Role).IsInEnum();
    }
}

public sealed class CreateAccountCommandHandler : IRequestHandler<CreateAccountCommand, AccountDto>
{
    private readonly IUserRepository _userRepository;
    private readonly IDepartmentRepository _departmentRepository;

    public CreateAccountCommandHandler(IUserRepository userRepository, IDepartmentRepository departmentRepository)
    {
        _userRepository = userRepository;
        _departmentRepository = departmentRepository;
    }

    public async Task<AccountDto> Handle(CreateAccountCommand request, CancellationToken ct)
    {
        var email = request.Email.Trim().ToLowerInvariant();
        if (await _userRepository.EmailExistsAsync(email, ct))
            throw new InvalidOperationException("Email already exists.");

        if (request.Role == UserRole.Agent)
        {
            if (request.DepartmentId is not int deptId || !await _departmentRepository.ExistsAsync(deptId, ct))
                throw new ArgumentException("Select a valid department for the agent.");
        }
        else if (request.Role == UserRole.Customer && request.DepartmentId is not null)
        {
            throw new ArgumentException("Customer accounts cannot have an assigned department.");
        }

        var account = new User(request.Name, email, request.PasswordHash, request.Role, PreferredLanguage.English, request.DepartmentId);
        await _userRepository.AddAsync(account, ct);
        await _userRepository.SaveChangesAsync(ct);

        return new AccountDto(account.Id, account.Name, account.Email, account.Role.ToString(), account.DepartmentId);
    }
}

public sealed record ChangeAgentDepartmentCommand(int AgentId, int DepartmentId) : IRequest;

public sealed class ChangeAgentDepartmentCommandHandler : IRequestHandler<ChangeAgentDepartmentCommand>
{
    private readonly IUserRepository _userRepository;
    private readonly IDepartmentRepository _departmentRepository;

    public ChangeAgentDepartmentCommandHandler(IUserRepository userRepository, IDepartmentRepository departmentRepository)
    {
        _userRepository = userRepository;
        _departmentRepository = departmentRepository;
    }

    public async Task Handle(ChangeAgentDepartmentCommand request, CancellationToken ct)
    {
        var agent = await _userRepository.GetByIdAsync(request.AgentId, ct);
        if (agent is null || agent.Role != UserRole.Agent)
            throw new KeyNotFoundException($"Agent #{request.AgentId} was not found.");

        if (!await _departmentRepository.ExistsAsync(request.DepartmentId, ct))
            throw new ArgumentException($"Department #{request.DepartmentId} does not exist.");

        agent.ChangeDepartment(request.DepartmentId);
        await _userRepository.SaveChangesAsync(ct);
    }
}
