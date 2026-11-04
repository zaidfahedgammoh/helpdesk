using FluentAssertions;
using HelpDeskLive.Application.Abstractions;
using HelpDeskLive.Application.Common.Exceptions;
using HelpDeskLive.Application.Tickets.AssignTicket;
using HelpDeskLive.Application.Tickets.CreateTicket;
using HelpDeskLive.Application.Chat;
using HelpDeskLive.Domain.Entities;
using HelpDeskLive.Domain.Enums;
using Moq;
using Xunit;

namespace HelpDeskLive.Tests.Domain;

public class BusinessRuleLimitTests
{
    [Fact]
    public async Task Agent_CannotHold_MoreThan5_InProgressTickets()
    {
        var ticketRepoMock = new Mock<ITicketRepository>();
        var userRepoMock = new Mock<IUserRepository>();

        const int agentId = 5;
        const int departmentId = 1;
        var agent = new User("Agent Smith", "agent@helpdesk.com", "hash", UserRole.Agent, PreferredLanguage.English, departmentId: departmentId);
        userRepoMock.Setup(r => r.GetByIdAsync(agentId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(agent);

        var ticket = new Ticket("Issue #1", "Desc", TicketPriority.High, departmentId, 100);
        ticketRepoMock.Setup(r => r.GetByIdAsync(1, It.IsAny<CancellationToken>()))
            .ReturnsAsync(ticket);

        // When agent already has 5 active tickets
        ticketRepoMock.Setup(r => r.CountActiveTicketsForAgentAsync(agentId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(5);

        var handler = new AssignTicketCommandHandler(ticketRepoMock.Object, userRepoMock.Object);
        var command = new AssignTicketCommand(1, agentId);

        Func<Task> act = async () => await handler.Handle(command, CancellationToken.None);

        await act.Should().ThrowAsync<ConflictException>()
            .WithMessage("*maximum limit of 5 in-progress tickets*");
    }

    [Fact]
    public async Task Customer_CannotHave_MoreThan3_OpenTickets()
    {
        var ticketRepoMock = new Mock<ITicketRepository>();
        var issueTypeRepoMock = new Mock<IIssueTypeRepository>();
        var currentUserMock = new Mock<ICurrentUser>();

        const int customerId = 10;
        currentUserMock.Setup(u => u.UserId).Returns(customerId);
        currentUserMock.Setup(u => u.Role).Returns("Customer");

        ticketRepoMock.Setup(r => r.CustomerExistsAsync(customerId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);

        // When customer already has 3 open tickets
        ticketRepoMock.Setup(r => r.CountOpenTicketsForCustomerAsync(customerId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(3);

        var handler = new CreateTicketCommandHandler(ticketRepoMock.Object, issueTypeRepoMock.Object, currentUserMock.Object);
        var command = new CreateTicketCommand(null, "Another Issue", "Description details");

        Func<Task> act = async () => await handler.Handle(command, CancellationToken.None);

        await act.Should().ThrowAsync<InvalidOperationException>()
            .WithMessage("*cannot have more than three open tickets*");
    }

    [Fact]
    public async Task ClosedTicket_CannotReceive_ChatMessages()
    {
        var ticketRepoMock = new Mock<ITicketRepository>();
        var chatRepoMock = new Mock<IChatRepository>();
        var userRepoMock = new Mock<IUserRepository>();

        const int ticketId = 42;
        const int customerId = 10;

        var ticket = new Ticket("Broken Monitor", "No display", TicketPriority.Medium, 1, customerId);
        ticket.AssignToAgent(20);
        ticket.ChangeStatus(TicketStatus.InProgress);
        ticket.ChangeStatus(TicketStatus.Resolved);
        ticket.ChangeStatus(TicketStatus.Closed);

        var customer = new User("Jane Customer", "jane@test.com", "hash", UserRole.Customer, PreferredLanguage.English);

        ticketRepoMock.Setup(r => r.GetByIdAsync(ticketId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(ticket);
        userRepoMock.Setup(r => r.GetByIdAsync(customerId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(customer);

        var handler = new SendChatMessageCommandHandler(ticketRepoMock.Object, chatRepoMock.Object, userRepoMock.Object);
        var command = new SendChatMessageCommand(ticketId, customerId, "Is anyone still here?");

        Func<Task> act = async () => await handler.Handle(command, CancellationToken.None);

        await act.Should().ThrowAsync<InvalidOperationException>()
            .WithMessage("*Messages cannot be sent on a closed ticket*");
    }
}
