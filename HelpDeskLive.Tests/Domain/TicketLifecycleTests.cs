using FluentAssertions;
using HelpDeskLive.Domain.Entities;
using HelpDeskLive.Domain.Enums;
using Xunit;

namespace HelpDeskLive.Tests.Domain;

public class TicketLifecycleTests
{
    [Fact]
    public void Ticket_Status_Transitions_MustFollowExactOrder()
    {
        // 1. Initial creation is Open
        var ticket = new Ticket("Printer issue", "Cannot print document", TicketPriority.High, 1, 10);
        ticket.Status.Should().Be(TicketStatus.Open);
        ticket.ResolvedAt.Should().BeNull();
        ticket.ClosedAt.Should().BeNull();

        // 2. Open -> InProgress
        ticket.AssignToAgent(20);
        ticket.ChangeStatus(TicketStatus.InProgress);
        ticket.Status.Should().Be(TicketStatus.InProgress);

        // 3. InProgress -> Resolved
        ticket.ChangeStatus(TicketStatus.Resolved);
        ticket.Status.Should().Be(TicketStatus.Resolved);
        ticket.ResolvedAt.Should().NotBeNull();
        ticket.ClosedAt.Should().BeNull();

        // 4. Resolved -> Closed
        ticket.ChangeStatus(TicketStatus.Closed);
        ticket.Status.Should().Be(TicketStatus.Closed);
        ticket.ClosedAt.Should().NotBeNull();
        ticket.ResolvedAt.Should().NotBeNull();
    }

    [Fact]
    public void Ticket_InvalidStatusTransitions_ShouldThrowInvalidOperationException()
    {
        var ticket = new Ticket("Network issue", "Slow connection", TicketPriority.Medium, 1, 10);

        // Cannot skip directly from Open to Closed
        Action skipToClosed = () => ticket.ChangeStatus(TicketStatus.Closed);
        skipToClosed.Should().Throw<InvalidOperationException>()
            .WithMessage("*Cannot change ticket status from Open to Closed*");

        // Cannot skip directly from Open to Resolved
        Action skipToResolved = () => ticket.ChangeStatus(TicketStatus.Resolved);
        skipToResolved.Should().Throw<InvalidOperationException>()
            .WithMessage("*Cannot change ticket status from Open to Resolved*");

        // Move to InProgress
        ticket.AssignToAgent(20);
        ticket.ChangeStatus(TicketStatus.InProgress);

        // Cannot go back from InProgress to Open
        Action backToOpen = () => ticket.ChangeStatus(TicketStatus.Open);
        backToOpen.Should().Throw<InvalidOperationException>();

        // Cannot skip from InProgress to Closed
        Action skipInProgressToClosed = () => ticket.ChangeStatus(TicketStatus.Closed);
        skipInProgressToClosed.Should().Throw<InvalidOperationException>();

        // Move to Resolved
        ticket.ChangeStatus(TicketStatus.Resolved);

        // Cannot go back from Resolved to InProgress
        Action backToInProgress = () => ticket.ChangeStatus(TicketStatus.InProgress);
        backToInProgress.Should().Throw<InvalidOperationException>();

        // Move to Closed
        ticket.ChangeStatus(TicketStatus.Closed);

        // Cannot reopen Closed ticket
        Action reopenClosed = () => ticket.ChangeStatus(TicketStatus.Open);
        reopenClosed.Should().Throw<InvalidOperationException>();
    }

    [Fact]
    public void Ticket_ResolvedAt_And_ClosedAt_AreRecordedIndependently()
    {
        var ticket = new Ticket("Software install", "Requesting IDE install", TicketPriority.Low, 1, 10);
        ticket.AssignToAgent(20);
        ticket.ChangeStatus(TicketStatus.InProgress);

        var beforeResolve = DateTime.UtcNow.AddSeconds(-1);
        ticket.ChangeStatus(TicketStatus.Resolved);
        var afterResolve = DateTime.UtcNow.AddSeconds(1);

        ticket.ResolvedAt.Should().NotBeNull();
        ticket.ResolvedAt.Value.Should().BeOnOrAfter(beforeResolve).And.BeOnOrBefore(afterResolve);
        ticket.ClosedAt.Should().BeNull();

        var beforeClose = DateTime.UtcNow.AddSeconds(-1);
        ticket.ChangeStatus(TicketStatus.Closed);
        var afterClose = DateTime.UtcNow.AddSeconds(1);

        ticket.ClosedAt.Should().NotBeNull();
        ticket.ClosedAt.Value.Should().BeOnOrAfter(beforeClose).And.BeOnOrBefore(afterClose);
        ticket.ResolvedAt.Value.Should().BeOnOrBefore(ticket.ClosedAt.Value);
    }

    [Fact]
    public void Ticket_EscalateToQueue_RecordsReason_And_TransitionsToEscalated()
    {
        var ticket = new Ticket("Server crash", "Production down", TicketPriority.Critical, 1, 10);
        ticket.AssignToAgent(20);
        ticket.ChangeStatus(TicketStatus.InProgress);

        const string reason = "Requires Tier 2 Infrastructure specialist to review server logs.";
        var escalation = ticket.EscalateToQueue(reason);

        ticket.Status.Should().Be(TicketStatus.Escalated);
        ticket.AssignedAgentId.Should().BeNull();
        ticket.Escalations.Should().ContainSingle();

        escalation.TicketId.Should().Be(ticket.Id);
        escalation.PreviousAgentId.Should().Be(20);
        escalation.Reason.Should().Be(reason);
        escalation.NewAgentId.Should().BeNull();

        // New agent picks it up
        ticket.AssignToAgent(25);
        escalation.NewAgentId.Should().Be(25);
        ticket.AssignedAgentId.Should().Be(25);

        // Resume to InProgress from Escalated
        ticket.ChangeStatus(TicketStatus.InProgress);
        ticket.Status.Should().Be(TicketStatus.InProgress);
    }
}
