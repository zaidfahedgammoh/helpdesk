
using HelpDeskLive.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace HelpDeskLive.Infrastructure.Data.Configurations;

public class ChatMessageConfiguration
    : IEntityTypeConfiguration<ChatMessage>
{
    public void Configure(EntityTypeBuilder<ChatMessage> builder)
    {
        builder.ToTable("ChatMessages");

        builder.HasKey(message => message.Id);

        builder.Property(message => message.Content)
            .IsRequired()
            .HasMaxLength(5000);

        builder.Property(message => message.SentAt)
            .IsRequired();

        builder.Property(message => message.IsRead)
            .IsRequired();

        builder.HasIndex(message => new
        {
            message.TicketId,
            message.SentAt
        });

        builder.HasIndex(message => message.SenderId);

        builder.HasOne<Ticket>()
            .WithMany()
            .HasForeignKey(message => message.TicketId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne<User>()
            .WithMany()
            .HasForeignKey(message => message.SenderId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
