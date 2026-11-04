using HelpDeskLive.Domain.Entities;
using HelpDeskLive.Domain.Enums;
using Xunit;
namespace HelpDeskLive.Domain.Tests;
public sealed class TicketLifecycleTests
{
    [Fact]
    public void TicketStartsOpenAndFollowsNormalLifecycle()
    {
        var ticket = new Ticket("Cannot sign in", "Login page fails", TicketPriority.High, 2, 10);
        Assert.Equal(TicketStatus.Open, ticket.Status);
        ticket.AssignToAgent(20);
        ticket.ChangeStatus(TicketStatus.InProgress);
        ticket.ChangeStatus(TicketStatus.Resolved);
        ticket.ChangeStatus(TicketStatus.Closed);
        Assert.Equal(TicketStatus.Closed, ticket.Status);
        Assert.NotNull(ticket.ClosedAt);
    }

    [Fact]
    public void TicketCannotSkipLifecycleStates()
    {
        var ticket = new Ticket("Cannot sign in", "Login page fails", TicketPriority.High, 2, 10);
        Assert.Throws<InvalidOperationException>(() => ticket.ChangeStatus(TicketStatus.Resolved));
        Assert.Throws<InvalidOperationException>(() => ticket.ChangeStatus(TicketStatus.Closed));
    }

    [Fact]
    public void EscalationReleasesAgentAndReturnsTicketToEscalationQueue()
    {
        var ticket = new Ticket("Cannot sign in", "Login page fails", TicketPriority.Critical, 2, 10);
        ticket.AssignToAgent(20);
        ticket.ChangeStatus(TicketStatus.InProgress);
        ticket.EscalateToQueue();
        Assert.Equal(TicketStatus.Escalated, ticket.Status);
        Assert.Null(ticket.AssignedAgentId);
        ticket.AssignToAgent(21);
        ticket.ChangeStatus(TicketStatus.InProgress);
        Assert.Equal(TicketStatus.InProgress, ticket.Status);
        Assert.Equal(21, ticket.AssignedAgentId);
    }

    [Fact]
    public void ClosedTicketCannotBeReopened()
    {
        var ticket = new Ticket("Cannot sign in", "Login page fails", TicketPriority.Medium, 2, 10);
        ticket.AssignToAgent(20);
        ticket.ChangeStatus(TicketStatus.InProgress);
        ticket.ChangeStatus(TicketStatus.Closed);
        Assert.Throws<InvalidOperationException>(() => ticket.ChangeStatus(TicketStatus.InProgress));
    }

    [Fact]
    public void EscalationRequiresReason()
    {
        Assert.Throws<ArgumentException>(() => new TicketEscalation(1, 2, " "));
    }
}