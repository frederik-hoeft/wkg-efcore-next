using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Wkg.EntityFrameworkCore.Configuration;

namespace Wkg.EntityFrameworkCore.Tests.Model.Discovery.Entities;

// Junction table for many-to-many relationship using IDiscoverableModelConnection
public sealed class BookAuthor : IDiscoverableModelConnection<BookAuthor, Book, Author>
{
    public int Id { get; set; }
    public int BookId { get; set; }
    public int AuthorId { get; set; }
    public required Book Book { get; set; }
    public required Author Author { get; set; }
    public bool IsPrimary { get; set; }

    public static void ConfigureConnection(EntityTypeBuilder<BookAuthor> self)
    {
        ArgumentNullException.ThrowIfNull(self);

        self.ToTable("book_authors")
            .HasKey(ba => ba.Id).HasName("pk_book_authors");

        self.Property(ba => ba.Id)
            .HasColumnType("INTEGER")
            .HasColumnName("id")
            .IsRequired()
            .ValueGeneratedOnAdd();

        self.Property(ba => ba.BookId)
            .HasColumnType("INTEGER")
            .HasColumnName("book_id")
            .IsRequired();

        self.Property(ba => ba.AuthorId)
            .HasColumnType("INTEGER")
            .HasColumnName("author_id")
            .IsRequired();

        self.Property(ba => ba.IsPrimary)
            .HasColumnType("BIT")
            .HasColumnName("is_primary")
            .IsRequired()
            .HasDefaultValue(false);

        self.HasIndex(ba => new { ba.BookId, ba.AuthorId })
            .IsUnique()
            .HasDatabaseName("ix_book_authors_book_author");
    }

    public static void Connect(EntityTypeBuilder<Book> left, EntityTypeBuilder<Author> right)
    {
        ArgumentNullException.ThrowIfNull(left);
        ArgumentNullException.ThrowIfNull(right);

        // Configure the many-to-many relationship through the junction table
        left.HasMany<BookAuthor>()
            .WithOne(ba => ba.Book)
            .HasForeignKey(ba => ba.BookId)
            .HasConstraintName("fk_book_authors_book")
            .OnDelete(DeleteBehavior.Cascade);

        right.HasMany(a => a.BookAuthors)
            .WithOne(ba => ba.Author)
            .HasForeignKey(ba => ba.AuthorId)
            .HasConstraintName("fk_book_authors_author")
            .OnDelete(DeleteBehavior.Cascade);
    }
}
