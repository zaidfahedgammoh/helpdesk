
using HelpDeskLive.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace HelpDeskLive.Infrastructure.Data.Configurations;

public class RefreshTokenConfiguration
    : IEntityTypeConfiguration<RefreshToken>
{
    public void Configure(EntityTypeBuilder<RefreshToken> builder)
    {
        builder.ToTable("RefreshTokens");

        builder.HasKey(token => token.Id);

        builder.Property(token => token.Token)
            .IsRequired()
            .HasMaxLength(500);

        builder.Property(token => token.ExpiresAt)
            .IsRequired();

        builder.Property(token => token.RevokedAt)
            .IsRequired(false);

        builder.HasIndex(token => token.UserId);

        builder.HasOne<User>()
            .WithMany()
            .HasForeignKey(token => token.UserId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
