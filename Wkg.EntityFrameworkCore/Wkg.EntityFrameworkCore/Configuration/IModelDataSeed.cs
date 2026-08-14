namespace Wkg.EntityFrameworkCore.Configuration;

/// <summary>
/// Represents seed data for a model.
/// </summary>
/// <typeparam name="T">The type of the model.</typeparam>
public interface IModelDataSeed<T> where T : class
{
    /// <summary>
    /// Gets the seed data for the model. Note that this method must return deterministic data (e.g., fixed primary keys) for code-first migrations to work correctly.
    /// </summary>
    /// <returns>The seed data.</returns>
    static abstract IEnumerable<T> GetSeedData();
}