using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Wex.Cards.Domain.Entities;

namespace Wex.Cards.Infrastructure.Persistence.Configurations;

internal sealed class CardConfiguration : IEntityTypeConfiguration<Card>
{
    public void Configure(EntityTypeBuilder<Card> builder)
    {
        builder.ToTable("Cards");
        builder.HasKey(c => c.Id);
        builder.Property(c => c.Id).ValueGeneratedNever();

        builder.ComplexProperty(c => c.CreditLimit, m =>
        {
            m.Property(x => x.Amount)
                .HasColumnName("CreditLimitAmount")
                .HasColumnType("numeric(19,4)")
                .IsRequired();
            m.Property(x => x.Currency)
                .HasColumnName("CreditLimitCurrency")
                .HasMaxLength(3)
                .IsRequired();
        });
    }
}
