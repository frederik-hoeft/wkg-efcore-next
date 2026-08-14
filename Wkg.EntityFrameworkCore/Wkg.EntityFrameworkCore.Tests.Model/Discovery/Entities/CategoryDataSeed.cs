using Wkg.EntityFrameworkCore.Configuration;

namespace Wkg.EntityFrameworkCore.Tests.Model.Discovery.Entities;

public sealed class CategoryDataSeed : IDiscoverableModelDataSeed<Category>
{
    public static IEnumerable<Category> GetSeedData()
    {
        return new List<Category>
        {
            new() { Id = 1, Name = "Technology", Description = "Magazines about technology and gadgets." },
            new() { Id = 2, Name = "Health", Description = "Magazines focusing on health and wellness." },
            new() { Id = 3, Name = "Travel", Description = "Magazines about travel destinations and tips." }
        };
    }
}
