
using HelpDeskLive.Domain.Enums;

namespace HelpDeskLive.Domain.Entities;

public class IssueType
{
    public int Id { get; private set; }

    public string Name { get; private set; } = string.Empty;

    public string Description { get; private set; } = string.Empty;

    public int DepartmentId { get; private set; }

    public TicketPriority DefaultPriority { get; private set; }

    public bool IsActive { get; private set; }

    private IssueType()
    {
    }

    public IssueType(
        string name,
        string description,
        int departmentId,
        TicketPriority defaultPriority)
    {
        if (string.IsNullOrWhiteSpace(name))
            throw new ArgumentException("Issue type name is required.");

        if (string.IsNullOrWhiteSpace(description))
            throw new ArgumentException("Issue type description is required.");

        if (departmentId <= 0)
            throw new ArgumentException("Invalid department ID.");

        if (!Enum.IsDefined(defaultPriority))
            throw new ArgumentException("Invalid priority.");

        Name = name.Trim();
        Description = description.Trim();
        DepartmentId = departmentId;
        DefaultPriority = defaultPriority;
        IsActive = true;
    }

    public void Update(
        string name,
        string description,
        TicketPriority defaultPriority)
    {
        if (string.IsNullOrWhiteSpace(name))
            throw new ArgumentException("Issue type name is required.");

        if (string.IsNullOrWhiteSpace(description))
            throw new ArgumentException("Issue type description is required.");

        if (!Enum.IsDefined(defaultPriority))
            throw new ArgumentException("Invalid priority.");

        Name = name.Trim();
        Description = description.Trim();
        DefaultPriority = defaultPriority;
    }

    public void Deactivate()
    {
        IsActive = false;
    }

    public void Activate()
    {
        IsActive = true;
    }
}
