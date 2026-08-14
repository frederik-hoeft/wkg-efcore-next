using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Wkg.EntityFrameworkCore.Configuration;

namespace Wkg.EntityFrameworkCore.Tests.Model.Discovery.Entities;

// Entity with foreign key relationship
public sealed class Magazine : BaseProduct, IDiscoverableModelConfiguration<Magazine>
{
    public int CategoryId { get; set; }
    public required Category Category { get; set; }
    public int IssueNumber { get; set; }

    public static void Configure(EntityTypeBuilder<Magazine> self)
    {
        ArgumentNullException.ThrowIfNull(self);

        self.ToTable("magazines")
            .HasKey(m => m.Id).HasName("pk_magazines");

        self.Property(m => m.CategoryId)
            .HasColumnType("INTEGER")
            .HasColumnName("category_id")
            .IsRequired();

        self.Property(m => m.IssueNumber)
            .HasColumnType("INTEGER")
            .HasColumnName("issue_number")
            .IsRequired();

        self.HasOne(m => m.Category)
            .WithMany(c => c.Magazines)
            .HasForeignKey(m => m.CategoryId)
            .HasConstraintName("fk_magazines_category")
            .OnDelete(DeleteBehavior.Cascade);

        self.HasIndex(m => new { m.CategoryId, m.IssueNumber })
            .IsUnique()
            .HasDatabaseName("ix_magazines_category_issue");
    }
}
