using HelpDeskLive.API.Hubs;
using HelpDeskLive.Application.Abstractions;
using HelpDeskLive.Application.Tickets.Agent;
using HelpDeskLive.Domain.Enums;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.SignalR;

namespace HelpDeskLive.API.Controllers;

[ApiController]
[Route("api/agent/tickets")]
[Authorize(Roles = "Agent")]
public sealed class AgentTicketsController : ControllerBase
{
    private readonly ISender _sender;
    private readonly ICurrentUser _currentUser;
    private readonly IHubContext<TicketChatHub> _chatHub;

    public AgentTicketsController(
        ISender sender,
        ICurrentUser currentUser,
        IHubContext<TicketChatHub> chatHub)
    {
        _sender = sender;
        _currentUser = currentUser;
        _chatHub = chatHub;
    }

    [HttpGet]
    public async Task<IActionResult> Mine(CancellationToken ct)
    {
        if (_currentUser.UserId is not int agentId) return Unauthorized();
        var tickets = await _sender.Send(new GetAgentTicketsQuery(agentId), ct);
        return Ok(tickets);
    }

    [HttpPost("{ticketId:int}/start")]
    public async Task<IActionResult> Start(int ticketId, CancellationToken ct)
    {
        if (_currentUser.UserId is not int agentId) return Unauthorized();

        try
        {
            var result = await _sender.Send(new StartTicketCommand(ticketId, agentId), ct);

            await _chatHub.Clients.Group(DepartmentGroup(result.DepartmentId)).SendAsync("TicketAssigned", new
            {
                ticketId,
                agentId,
                status = TicketStatus.InProgress.ToString()
            }, ct);

            return Ok(new { priority = (int?)result.Priority, isLiveChat = result.IsLiveChat });
        }
        catch (InvalidOperationException ex)
        {
            return Conflict(new ProblemDetails { Status = StatusCodes.Status409Conflict, Title = "Conflict", Detail = ex.Message });
        }
        catch (KeyNotFoundException ex)
        {
            return NotFound(new ProblemDetails { Status = StatusCodes.Status404NotFound, Title = "Not Found", Detail = ex.Message });
        }
    }

    [HttpPost("{ticketId:int}/resolve")]
    public async Task<IActionResult> Resolve(int ticketId, CancellationToken ct)
    {
        if (_currentUser.UserId is not int agentId) return Unauthorized();

        try
        {
            await _sender.Send(new ResolveTicketCommand(ticketId, agentId), ct);

            await _chatHub.Clients.Group(TicketGroup(ticketId)).SendAsync(
                "TicketStatusChanged",
                new { ticketId, status = TicketStatus.Resolved.ToString() },
                ct);

            return NoContent();
        }
        catch (InvalidOperationException ex)
        {
            return Conflict(new ProblemDetails { Status = StatusCodes.Status409Conflict, Title = "Conflict", Detail = ex.Message });
        }
        catch (KeyNotFoundException ex)
        {
            return NotFound(new ProblemDetails { Status = StatusCodes.Status404NotFound, Title = "Not Found", Detail = ex.Message });
        }
    }

    [HttpPost("{ticketId:int}/escalate")]
    public async Task<IActionResult> Escalate(
        int ticketId,
        [FromBody] EscalateTicketRequest? request,
        CancellationToken ct)
    {
        if (_currentUser.UserId is not int agentId) return Unauthorized();

        try
        {
            var result = await _sender.Send(new EscalateTicketCommand(ticketId, agentId, request?.Reason), ct);

            await _chatHub.Clients.Group(TicketGroup(ticketId)).SendAsync("TicketEscalated", new
            {
                ticketId,
                status = TicketStatus.Escalated.ToString(),
                message = "Your ticket has been escalated. Another agent will assist you."
            }, ct);

            if (result.DepartmentId is int departmentId)
            {
                await _chatHub.Clients.Group(DepartmentGroup(departmentId)).SendAsync(
                    "EscalatedTicketAvailable",
                    new { ticketId },
                    ct);
            }

            return NoContent();
        }
        catch (InvalidOperationException ex)
        {
            return Conflict(new ProblemDetails { Status = StatusCodes.Status409Conflict, Title = "Conflict", Detail = ex.Message });
        }
        catch (KeyNotFoundException ex)
        {
            return NotFound(new ProblemDetails { Status = StatusCodes.Status404NotFound, Title = "Not Found", Detail = ex.Message });
        }
    }

    [HttpPost("{ticketId:int}/close")]
    public async Task<IActionResult> Close(int ticketId, CancellationToken ct)
    {
        if (_currentUser.UserId is not int agentId) return Unauthorized();

        try
        {
            await _sender.Send(new CloseAgentTicketCommand(ticketId, agentId), ct);

            await _chatHub.Clients.Group(TicketGroup(ticketId)).SendAsync(
                "TicketStatusChanged",
                new { ticketId, status = TicketStatus.Closed.ToString() },
                ct);

            return NoContent();
        }
        catch (InvalidOperationException ex)
        {
            return Conflict(new ProblemDetails { Status = StatusCodes.Status409Conflict, Title = "Conflict", Detail = ex.Message });
        }
        catch (KeyNotFoundException ex)
        {
            return NotFound(new ProblemDetails { Status = StatusCodes.Status404NotFound, Title = "Not Found", Detail = ex.Message });
        }
    }

    private static string TicketGroup(int ticketId) => $"ticket-{ticketId}";
    private static string DepartmentGroup(int departmentId) => $"department-{departmentId}";

    public sealed record EscalateTicketRequest(string? Reason);
}
