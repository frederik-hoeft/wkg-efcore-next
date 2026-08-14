using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Wkg.EntityFrameworkCore.Configuration;

namespace Wkg.EntityFrameworkCore.Tests.Model.Discovery.Entities;

// Base model for inheritance testing
public abstract class BaseProduct : IDiscoverableBaseModelConfiguration<BaseProduct>
{
    public int Id { get; set; }
    public required string Name { get; set; }
    public decimal Price { get; set; }
    public DateTime CreatedAt { get; set; }

    static void IBaseModelConfiguration<BaseProduct>.ConfigureBaseModel<TChildClass>(EntityTypeBuilder<TChildClass> self)
    {
        ArgumentNullException.ThrowIfNull(self);

        self.Property(p => p.Id)
            .HasColumnType("INTEGER")
            .HasColumnName("id")
            .IsRequired()
            .ValueGeneratedOnAdd();

        self.Property(p => p.Name)
            .HasColumnType("VARCHAR")
            .HasMaxLength(100)
            .HasColumnName("name")
            .IsRequired();

        self.Property(p => p.Price)
            .HasColumnType("DECIMAL")
            .HasPrecision(18, 2)
            .HasColumnName("price")
            .IsRequired();

        self.Property(p => p.CreatedAt)
            .HasColumnType("DATETIME")
            .HasColumnName("created_at")
            .IsRequired()
            .HasDefaultValueSql("CURRENT_TIMESTAMP");
    }
}
