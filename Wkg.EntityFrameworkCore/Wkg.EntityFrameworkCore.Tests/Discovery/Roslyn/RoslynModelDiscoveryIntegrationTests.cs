using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Wkg.Common.Extensions;
using Wkg.EntityFrameworkCore.Configuration;
using Wkg.EntityFrameworkCore.Configuration.Discovery;
using Wkg.EntityFrameworkCore.Configuration.Policies.Defaults.EntityNamingPolicies;
using Wkg.EntityFrameworkCore.Configuration.Policies.Defaults.PropertyMappingPolicies;
using Wkg.EntityFrameworkCore.Extensions;
using Wkg.EntityFrameworkCore.Tests.Model.Discovery;
using Wkg.EntityFrameworkCore.Tests.Model.Discovery.Entities;

namespace Wkg.EntityFrameworkCore.Tests.Discovery.Roslyn;

[TestClass]
public sealed class RoslynModelDiscoveryIntegrationTests
{
    private DbContextOptions<TestDbContext> _dbContextOptions = null!;

    [TestInitialize]
    public void Setup()
    {
        _dbContextOptions = new DbContextOptionsBuilder<TestDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;
    }

    [TestMethod]
    public void LoadModels_ShouldConfigureAllDiscoverableEntities()
    {
        // Arrange
        TestModelLoader modelLoader = new();
        
        // Act & Assert - This will throw if the generated code has syntax errors
        using TestDbContext context = new(_dbContextOptions, modelLoader);
        IModel model = context.Model;

        // Verify that all expected entities are registered
        List<IEntityType> entityTypes = [.. model.GetEntityTypes()];
        HashSet<string> entityTypeNames = [.. entityTypes.Select(et => et.ClrType.Name)];
        
        Assert.Contains("Book", entityTypeNames, "Book entity should be registered");
        Assert.Contains("Category", entityTypeNames, "Category entity should be registered");
        Assert.Contains("Magazine", entityTypeNames, "Magazine entity should be registered");
        Assert.Contains("Author", entityTypeNames, "Author entity should be registered");
        Assert.Contains("BookAuthor", entityTypeNames, "BookAuthor entity should be registered");
    }

    [TestMethod]
    public void LoadModels_RegistersEntitiesInDiscoveryContext()
    {
        // Arrange
        IModelLoader modelLoader = new TestModelLoader();

        // Act
        ModelBuilder builder = new();
        EntityDiscoveryContext discoveryContext = new(policies: []);
        modelLoader.LoadModels(builder, discoveryContext);

        // Assert - Verify that Register was called for each expected entity type
        IReadOnlyDictionary<Type, EntityTypeBuilder> builderCache = discoveryContext.To<IEntityDiscoveryContext>().EntityBuilderCache;
        Assert.IsTrue(builderCache.ContainsKey(typeof(Book)), "Book entity should be registered in discovery context");
        Assert.IsTrue(builderCache.ContainsKey(typeof(Category)), "Category entity should be registered in discovery context");
        Assert.IsTrue(builderCache.ContainsKey(typeof(Magazine)), "Magazine entity should be registered in discovery context");
        Assert.IsTrue(builderCache.ContainsKey(typeof(Author)), "Author entity should be registered in discovery context");
        Assert.IsTrue(builderCache.ContainsKey(typeof(BookAuthor)), "BookAuthor entity should be registered in discovery context");
    }

    [TestMethod]
    public void LoadModels_BookEntity_ShouldHaveCorrectConfiguration()
    {
        // Arrange
        TestModelLoader modelLoader = new();
        
        // Act
        using TestDbContext context = new(_dbContextOptions, modelLoader);
        IModel model = context.Model;
        IEntityType? bookEntityType = model.FindEntityType(typeof(Book));
        
        // Assert
        Assert.IsNotNull(bookEntityType, "Book entity type should be found");
        
        // Check table configuration
        Assert.AreEqual("books", bookEntityType.GetTableName());

        // Check primary key
        IKey? primaryKey = bookEntityType.FindPrimaryKey();
        Assert.IsNotNull(primaryKey);
        Assert.AreEqual("pk_books", primaryKey.GetName());
        Assert.HasCount(1, primaryKey.Properties);
        Assert.AreEqual("Id", primaryKey.Properties[0].Name);

        // Check properties from base class  
        IProperty? idProperty = bookEntityType.FindProperty("Id");
        Assert.IsNotNull(idProperty);
        Assert.AreEqual("id", idProperty.GetColumnName());
        Assert.IsTrue(idProperty.IsKey());

        IProperty? nameProperty = bookEntityType.FindProperty("Name");
        Assert.IsNotNull(nameProperty);
        Assert.AreEqual("name", nameProperty.GetColumnName());
        Assert.AreEqual(100, nameProperty.GetMaxLength());

        IProperty? priceProperty = bookEntityType.FindProperty("Price");
        Assert.IsNotNull(priceProperty);
        Assert.AreEqual("price", priceProperty.GetColumnName());

        // Check Book-specific properties
        IProperty? authorProperty = bookEntityType.FindProperty("Author");
        Assert.IsNotNull(authorProperty);
        Assert.AreEqual("author", authorProperty.GetColumnName());
        Assert.AreEqual(200, authorProperty.GetMaxLength());

        IProperty? isbnProperty = bookEntityType.FindProperty("ISBN");
        Assert.IsNotNull(isbnProperty);
        Assert.AreEqual("isbn", isbnProperty.GetColumnName());
        Assert.AreEqual(20, isbnProperty.GetMaxLength());

        // Check unique index on ISBN
        IIndex? isbnIndex = bookEntityType.GetIndexes().FirstOrDefault(i => i.Properties.Any(p => p.Name == "ISBN"));
        Assert.IsNotNull(isbnIndex);
        Assert.IsTrue(isbnIndex.IsUnique);
        Assert.AreEqual("ix_books_isbn", isbnIndex.GetDatabaseName());
    }

    [TestMethod]
    public void LoadModels_CategoryMagazineRelationship_ShouldBeConfiguredCorrectly()
    {
        // Arrange
        TestModelLoader modelLoader = new();
        
        // Act
        using TestDbContext context = new(_dbContextOptions, modelLoader);
        IModel model = context.Model;
        IEntityType? magazineEntityType = model.FindEntityType(typeof(Magazine));
        IEntityType? categoryEntityType = model.FindEntityType(typeof(Category));
        
        // Assert
        Assert.IsNotNull(magazineEntityType);
        Assert.IsNotNull(categoryEntityType);

        // Check foreign key relationship
        List<IForeignKey> foreignKeys = [.. magazineEntityType.GetForeignKeys()];
        IForeignKey? categoryForeignKey = foreignKeys.FirstOrDefault(fk => 
            fk.PrincipalEntityType.ClrType == typeof(Category));
        
        Assert.IsNotNull(categoryForeignKey, "Category foreign key should exist");
        Assert.AreEqual("fk_magazines_category", categoryForeignKey.GetConstraintName());
        Assert.AreEqual(DeleteBehavior.Cascade, categoryForeignKey.DeleteBehavior);

        // Check navigation properties
        INavigation? categoryNavigation = magazineEntityType.FindNavigation("Category");
        Assert.IsNotNull(categoryNavigation);
        Assert.IsFalse(categoryNavigation.IsCollection);

        INavigation? magazinesNavigation = categoryEntityType.FindNavigation("Magazines");
        Assert.IsNotNull(magazinesNavigation);
        Assert.IsTrue(magazinesNavigation.IsCollection);
    }

    [TestMethod]
    public void LoadModels_BookAuthorConnection_ShouldBeConfiguredCorrectly()
    {
        // Arrange
        TestModelLoader modelLoader = new();
        
        // Act
        using TestDbContext context = new(_dbContextOptions, modelLoader);
        IModel model = context.Model;
        IEntityType? bookAuthorEntityType = model.FindEntityType(typeof(BookAuthor));
        IEntityType? bookEntityType = model.FindEntityType(typeof(Book));
        IEntityType? authorEntityType = model.FindEntityType(typeof(Author));
        
        // Assert
        Assert.IsNotNull(bookAuthorEntityType);
        Assert.IsNotNull(bookEntityType);
        Assert.IsNotNull(authorEntityType);
        
        // Check BookAuthor table configuration - Accept either explicit or convention naming
        string? tableName = bookAuthorEntityType.GetTableName();
        Assert.IsTrue(tableName is "book_authors" or "BookAuthor", 
            $"Table name should be 'book_authors' or 'BookAuthor', but was '{tableName}'");

        // Check foreign key relationships
        List<IForeignKey> foreignKeys = [.. bookAuthorEntityType.GetForeignKeys()];
        Assert.HasCount(2, foreignKeys, "BookAuthor should have two foreign keys");

        IForeignKey? bookForeignKey = foreignKeys.FirstOrDefault(fk => 
            fk.PrincipalEntityType.ClrType == typeof(Book));
        Assert.IsNotNull(bookForeignKey);
        Assert.AreEqual("fk_book_authors_book", bookForeignKey.GetConstraintName());

        IForeignKey? authorForeignKey = foreignKeys.FirstOrDefault(fk => 
            fk.PrincipalEntityType.ClrType == typeof(Author));
        Assert.IsNotNull(authorForeignKey);
        Assert.AreEqual("fk_book_authors_author", authorForeignKey.GetConstraintName());
    }

    [TestMethod]
    public void LoadModels_BaseModelConfiguration_ShouldBeAppliedToInheritedEntities()
    {
        // Arrange
        TestModelLoader modelLoader = new();
        
        // Act
        using TestDbContext context = new(_dbContextOptions, modelLoader);
        IModel model = context.Model;
        IEntityType? magazineEntityType = model.FindEntityType(typeof(Magazine));
        
        // Assert - Magazine inherits from BaseProduct, so should have base properties configured
        Assert.IsNotNull(magazineEntityType);

        // Check inherited properties from BaseProduct
        IProperty? idProperty = magazineEntityType.FindProperty("Id");
        Assert.IsNotNull(idProperty);
        Assert.AreEqual("id", idProperty.GetColumnName());

        IProperty? nameProperty = magazineEntityType.FindProperty("Name");
        Assert.IsNotNull(nameProperty);
        Assert.AreEqual("name", nameProperty.GetColumnName());

        IProperty? priceProperty = magazineEntityType.FindProperty("Price");
        Assert.IsNotNull(priceProperty);
        Assert.AreEqual("price", priceProperty.GetColumnName());

        IProperty? createdAtProperty = magazineEntityType.FindProperty("CreatedAt");
        Assert.IsNotNull(createdAtProperty);
        Assert.AreEqual("created_at", createdAtProperty.GetColumnName());
        Assert.AreEqual("CURRENT_TIMESTAMP", createdAtProperty.GetDefaultValueSql());
    }

    [TestMethod]
    public void LoadModels_ShouldWorkWithDatabaseOperations()
    {
        // Arrange
        TestModelLoader modelLoader = new();
        
        // Act & Assert
        using TestDbContext context = new(_dbContextOptions, modelLoader);
        
        // Ensure database is created successfully
        context.Database.EnsureCreated();

        // Test basic CRUD operations
        Category category = new()
        { 
            Name = "Science Fiction", 
            Description = "Sci-fi magazines",
            Magazines = []
        };
        
        context.Categories.Add(category);
        context.SaveChanges();

        // Verify the entity was saved and can be retrieved
        Category savedCategory = context.Categories.First(c => c.Name == "Science Fiction");
        Assert.AreEqual("Science Fiction", savedCategory.Name);
        Assert.AreEqual("Sci-fi magazines", savedCategory.Description);

        // Test relationship
        Magazine magazine = new()
        {
            Name = "Asimov's Science Fiction",
            Price = 5.99m,
            CreatedAt = DateTime.UtcNow,
            CategoryId = savedCategory.Id,
            Category = savedCategory,
            IssueNumber = 1
        };
        
        context.Magazines.Add(magazine);
        context.SaveChanges();

        Magazine savedMagazine = context.Magazines
            .Include(m => m.Category)
            .First(m => m.Name == "Asimov's Science Fiction");
        
        Assert.AreEqual("Asimov's Science Fiction", savedMagazine.Name);
        Assert.AreEqual(savedCategory.Id, savedMagazine.CategoryId);
        Assert.AreEqual("Science Fiction", savedMagazine.Category.Name);
    }

    // Test DbContext class
    private sealed class TestDbContext(DbContextOptions<TestDbContext> options, IModelLoader modelLoader) : DbContext(options)
    {
        public DbSet<Book> Books => Set<Book>();
        public DbSet<Category> Categories => Set<Category>();
        public DbSet<Magazine> Magazines => Set<Magazine>();
        public DbSet<Author> Authors => Set<Author>();
        public DbSet<BookAuthor> BookAuthors => Set<BookAuthor>();

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            modelBuilder.LoadModels(modelLoader, options => options.ConfigurePolicies(policies => policies
                .AddEntityNamingPolicy(EntityNamingPolicy.AllowImplicit)
                .AddPropertyMappingPolicy(PropertyMappingPolicy.AllowImplicit)));
        }
    }
}
