using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Wkg.EntityFrameworkCore.Configuration;

namespace Wkg.EntityFrameworkCore.Tests.Discovery.TestData;

// Many-to-many relationship entities
internal sealed class Author : IDiscoverableModelConfiguration<Author>
{
    public int Id { get; set; }
    public required string FirstName { get; set; }
    public required string LastName { get; set; }
    public string? Biography { get; set; }
    public required ICollection<BookAuthor> BookAuthors { get; set; }

    public static void Configure(EntityTypeBuilder<Author> self)
    {
        ArgumentNullException.ThrowIfNull(self);

        self.ToTable("authors")
            .HasKey(a => a.Id).HasName("pk_authors");

        self.Property(a => a.Id)
            .HasColumnType("INTEGER")
            .HasColumnName("id")
            .IsRequired()
            .ValueGeneratedOnAdd();

        self.Property(a => a.FirstName)
            .HasColumnType("VARCHAR")
            .HasMaxLength(100)
            .HasColumnName("first_name")
            .IsRequired();

        self.Property(a => a.LastName)
            .HasColumnType("VARCHAR")
            .HasMaxLength(100)
            .HasColumnName("last_name")
            .IsRequired();

        self.Property(a => a.Biography)
            .HasColumnType("TEXT")
            .HasColumnName("biography");
    }
}
