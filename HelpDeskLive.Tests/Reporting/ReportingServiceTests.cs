using FluentAssertions;
using HelpDeskLive.Domain.Entities;
using HelpDeskLive.Domain.Enums;
using HelpDeskLive.Infrastructure.Data;
using HelpDeskLive.Infrastructure.Services;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using Xunit;

namespace HelpDeskLive.Tests.Reporting;

public class ReportingServiceTests
{
    private static HelpDeskDbContext CreateInMemoryDbContext()
    {
        var options = new DbContextOptionsBuilder<HelpDeskDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;
        return new HelpDeskDbContext(options);
    }

    [Fact]
    public async Task GetDepartmentTicketStatsViaLinqAsync_CalculatesCorrectAggregations()
    {
        await using var db = CreateInMemoryDbContext();

        var dept1 = new Department("IT Support");
        var dept2 = new Department("Billing");
        db.Departments.AddRange(dept1, dept2);
        await db.SaveChangesAsync();

        var customer = new User("Customer Bob", "bob@test.com", "hash", UserRole.Customer, PreferredLanguage.English);
        var agent = new User("Agent Alice", "alice@test.com", "hash", UserRole.Agent, PreferredLanguage.English, departmentId: dept1.Id);
        db.Users.AddRange(customer, agent);
        await db.SaveChangesAsync();

        // Ticket 1: Open in IT
        var t1 = new Ticket("Broken Laptop", "Hardware", TicketPriority.High, dept1.Id, customer.Id);

        // Ticket 2: InProgress in IT
        var t2 = new Ticket("Network Down", "LAN Cable", TicketPriority.Critical, dept1.Id, customer.Id);
        t2.AssignToAgent(agent.Id);
        t2.ChangeStatus(TicketStatus.InProgress);

        // Ticket 3: Resolved in IT
        var t3 = new Ticket("Password Reset", "Forgot", TicketPriority.Low, dept1.Id, customer.Id);
        t3.AssignToAgent(agent.Id);
        t3.ChangeStatus(TicketStatus.InProgress);
        t3.ChangeStatus(TicketStatus.Resolved);

        // Ticket 4: Closed in Billing
        var t4 = new Ticket("Invoice Query", "Refund", TicketPriority.Medium, dept2.Id, customer.Id);
        t4.AssignToAgent(agent.Id);
        t4.ChangeStatus(TicketStatus.InProgress);
        t4.ChangeStatus(TicketStatus.Resolved);
        t4.ChangeStatus(TicketStatus.Closed);

        db.Tickets.AddRange(t1, t2, t3, t4);
        await db.SaveChangesAsync();

        var configMock = new Mock<IConfiguration>();
        var sectionMock = new Mock<IConfigurationSection>();
        sectionMock.Setup(s => s["DefaultConnection"]).Returns("Server=test;Database=test;");
        configMock.Setup(c => c.GetSection("ConnectionStrings")).Returns(sectionMock.Object);
        var service = new AdminReportingService(db, configMock.Object, NullLogger<AdminReportingService>.Instance);

        var report = await service.GetDepartmentTicketStatsViaLinqAsync();

        report.Should().HaveCount(2);

        var itStats = report.First(r => r.DepartmentName == "IT Support");
        itStats.TotalTickets.Should().Be(3);
        itStats.OpenTickets.Should().Be(1);
        itStats.InProgressTickets.Should().Be(1);
        itStats.ResolvedTickets.Should().Be(1);
        itStats.ClosedTickets.Should().Be(0);

        var billingStats = report.First(r => r.DepartmentName == "Billing");
        billingStats.TotalTickets.Should().Be(1);
        billingStats.ClosedTickets.Should().Be(1);
    }
}
