using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Wkg.EntityFrameworkCore.Configuration;

namespace Wkg.EntityFrameworkCore.Tests.Model.Discovery.Entities;

// Another simple entity for relationship testing
public sealed class Category : IDiscoverableModelConfiguration<Category>
{
    public int Id { get; set; }
    public required string Name { get; set; }
    public string? Description { get; set; }
    public ICollection<Magazine> Magazines { get; set; } = null!;

    public static void Configure(EntityTypeBuilder<Category> self)
    {
        ArgumentNullException.ThrowIfNull(self);

        self.ToTable("categories")
            .HasKey(c => c.Id).HasName("pk_categories");

        self.Property(c => c.Id)
            .HasColumnType("INTEGER")
            .HasColumnName("id")
            .IsRequired()
            .ValueGeneratedOnAdd();

        self.Property(c => c.Name)
            .HasColumnType("VARCHAR")
            .HasMaxLength(50)
            .HasColumnName("name")
            .IsRequired();

        self.Property(c => c.Description)
            .HasColumnType("TEXT")
            .HasColumnName("description");

        self.HasIndex(c => c.Name)
            .IsUnique()
            .HasDatabaseName("ix_categories_name");
    }
}
