using HelpDeskLive.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace HelpDeskLive.Infrastructure.Data.Configurations;

public class TicketEscalationConfiguration : IEntityTypeConfiguration<TicketEscalation>
{
    public void Configure(EntityTypeBuilder<TicketEscalation> builder)
    {
        builder.ToTable("TicketEscalations");

        builder.HasKey(e => e.Id);

        builder.Property(e => e.TicketId)
            .IsRequired();

        builder.Property(e => e.PreviousAgentId)
            .IsRequired();

        builder.Property(e => e.NewAgentId)
            .IsRequired(false);

        builder.Property(e => e.Reason)
            .IsRequired()
            .HasMaxLength(1000);

        builder.Property(e => e.EscalatedAt)
            .IsRequired();

        builder.Property(e => e.ReassignedAt)
            .IsRequired(false);

        builder.HasIndex(e => e.TicketId);
        builder.HasIndex(e => e.PreviousAgentId);
        builder.HasIndex(e => e.NewAgentId);

        builder.HasOne<Ticket>()
            .WithMany(t => t.Escalations)
            .HasForeignKey(e => e.TicketId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne<User>()
            .WithMany()
            .HasForeignKey(e => e.PreviousAgentId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne<User>()
            .WithMany()
            .HasForeignKey(e => e.NewAgentId)
            .OnDelete(DeleteBehavior.Restrict)
            .IsRequired(false);
    }
}
