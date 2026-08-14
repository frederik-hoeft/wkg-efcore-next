using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Wkg.EntityFrameworkCore.Configuration;

namespace Wkg.EntityFrameworkCore.Tests.Discovery.TestData;

// Simple entity configuration test
internal sealed class Book : BaseProduct, IDiscoverableModelConfiguration<Book>
{
    public required string Author { get; set; }
    public required string ISBN { get; set; }
    public int PageCount { get; set; }

    public static void Configure(EntityTypeBuilder<Book> self)
    {
        ArgumentNullException.ThrowIfNull(self);

        self.ToTable("books")
            .HasKey(b => b.Id).HasName("pk_books");

        self.Property(b => b.Author)
            .HasColumnType("VARCHAR")
            .HasMaxLength(200)
            .HasColumnName("author")
            .IsRequired();

        self.Property(b => b.ISBN)
            .HasColumnType("VARCHAR")
            .HasMaxLength(20)
            .HasColumnName("isbn")
            .IsRequired();

        self.HasIndex(b => b.ISBN)
            .IsUnique()
            .HasDatabaseName("ix_books_isbn");

        self.Property(b => b.PageCount)
            .HasColumnType("INTEGER")
            .HasColumnName("page_count")
            .IsRequired();
    }
}
