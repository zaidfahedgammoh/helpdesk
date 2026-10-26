
using HelpDeskLive.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace HelpDeskLive.Infrastructure.Data.Configurations;

public class TicketConfiguration : IEntityTypeConfiguration<Ticket>
{
    public void Configure(EntityTypeBuilder<Ticket> builder)
    {
        builder.ToTable("Tickets");

        builder.HasKey(ticket => ticket.Id);

        builder.Property(ticket => ticket.Title)
            .IsRequired()
            .HasMaxLength(200);

        builder.Property(ticket => ticket.Description)
            .IsRequired()
            .HasMaxLength(5000);

        builder.Property(ticket => ticket.Status)
            .IsRequired();

        builder.Property(ticket => ticket.Priority)
            .IsRequired();

        builder.Property(ticket => ticket.CreatedAt)
            .IsRequired();

        builder.Property(ticket => ticket.ClosedAt)
            .IsRequired(false);

        builder.HasIndex(ticket => ticket.CustomerId);

        builder.HasIndex(ticket => ticket.AssignedAgentId);

        builder.HasIndex(ticket => new
        {
            ticket.Status,
            ticket.CreatedAt
        });

        builder.HasOne<Department>()
            .WithMany()
            .HasForeignKey(ticket => ticket.DepartmentId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne<User>()
            .WithMany()
            .HasForeignKey(ticket => ticket.CustomerId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne<User>()
            .WithMany()
            .HasForeignKey(ticket => ticket.AssignedAgentId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
