using FluentAssertions;
using HelpDeskLive.Application.Chat;
using HelpDeskLive.Application.Tickets.Agent;
using HelpDeskLive.Application.Tickets.AssignTicket;
using HelpDeskLive.Application.Tickets.CreateTicket;
using Xunit;

namespace HelpDeskLive.Tests.Application;

public class ValidationTests
{
    [Fact]
    public void EscalateTicketCommandValidator_RequiresNonEmptyReason_And_Max1000Chars()
    {
        var validator = new EscalateTicketCommandValidator();

        // Valid command
        var valid = new EscalateTicketCommand(1, 2, "Technical blocker with cloud server");
        validator.Validate(valid).IsValid.Should().BeTrue();

        // Empty reason fails
        var emptyReason = new EscalateTicketCommand(1, 2, "");
        var emptyResult = validator.Validate(emptyReason);
        emptyResult.IsValid.Should().BeFalse();
        emptyResult.Errors.Should().Contain(e => e.PropertyName == "Reason");

        // Reason > 1000 characters fails
        var longReason = new EscalateTicketCommand(1, 2, new string('A', 1001));
        var longResult = validator.Validate(longReason);
        longResult.IsValid.Should().BeFalse();
        longResult.Errors.Should().Contain(e => e.PropertyName == "Reason");

        // Invalid TicketId or AgentId fails
        var invalidIds = new EscalateTicketCommand(0, -1, "Valid reason");
        var idsResult = validator.Validate(invalidIds);
        idsResult.IsValid.Should().BeFalse();
    }

    [Fact]
    public void SendChatMessageCommandValidator_RequiresContent_And_Max5000Chars()
    {
        var validator = new SendChatMessageCommandValidator();

        // Valid
        var valid = new SendChatMessageCommand(10, 5, "Hello, can you help me?");
        validator.Validate(valid).IsValid.Should().BeTrue();

        // Empty content fails
        var empty = new SendChatMessageCommand(10, 5, "   ");
        validator.Validate(empty).IsValid.Should().BeFalse();

        // Over 5000 chars fails
        var tooLong = new SendChatMessageCommand(10, 5, new string('C', 5001));
        validator.Validate(tooLong).IsValid.Should().BeFalse();
    }

    [Fact]
    public void CreateTicketCommandValidator_ValidatesRules()
    {
        var validator = new CreateTicketCommandValidator();

        var valid = new CreateTicketCommand(1, null, "Detailed explanation of the issue");
        validator.Validate(valid).IsValid.Should().BeTrue();

        var emptyDesc = new CreateTicketCommand(1, null, "");
        validator.Validate(emptyDesc).IsValid.Should().BeFalse();
    }

    [Fact]
    public void AssignTicketCommandValidator_ValidatesIds()
    {
        var validator = new AssignTicketCommandValidator();

        var valid = new AssignTicketCommand(10, 5);
        validator.Validate(valid).IsValid.Should().BeTrue();

        var invalidTicket = new AssignTicketCommand(0, 5);
        validator.Validate(invalidTicket).IsValid.Should().BeFalse();

        var invalidAgent = new AssignTicketCommand(10, 0);
        validator.Validate(invalidAgent).IsValid.Should().BeFalse();
    }
}
