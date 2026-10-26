
namespace HelpDeskLive.Domain.Entities;

public class Department
{
    public int Id { get; private set; }

    public string Name { get; private set; } = string.Empty;

    private Department()
    {
    }

    public Department(string name)
    {
        if (string.IsNullOrWhiteSpace(name))
        {
            throw new ArgumentException(
                "Department name cannot be empty.",
                nameof(name));
        }

        Name = name.Trim();
    }
}
