using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Wex.Cards.Domain.Entities;

namespace Wex.Cards.Infrastructure.Persistence.Configurations;

internal sealed class TransactionConfiguration : IEntityTypeConfiguration<Transaction>
{
    public void Configure(EntityTypeBuilder<Transaction> builder)
    {
        builder.ToTable("Transactions");
        builder.HasKey(t => t.Id);
        builder.Property(t => t.Id).ValueGeneratedNever();

        builder.Property(t => t.Description).IsRequired().HasMaxLength(200);
        builder.Property(t => t.TransactionDate).IsRequired();

        builder.ComplexProperty(t => t.Amount, m =>
        {
            m.Property(x => x.Amount)
                .HasColumnName("AmountValue")
                .HasColumnType("numeric(19,4)")
                .IsRequired();
            m.Property(x => x.Currency)
                .HasColumnName("AmountCurrency")
                .HasMaxLength(3)
                .IsRequired();
        });

        builder.HasOne<Card>().WithMany().HasForeignKey(t => t.CardId).IsRequired();
    }
}
