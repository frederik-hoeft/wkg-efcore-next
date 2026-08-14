using Wkg.EntityFrameworkCore.SourceGeneration.Contracts;
using Wkg.EntityFrameworkCore.SourceGeneration.Discovery.Contracts;

namespace Wkg.EntityFrameworkCore.Configuration.Reflection.Attributes;

/// <summary>
/// Represents an attribute to indicate which database engine should be used to configure the entity if multiple database engines are supported.
/// </summary>
[AttributeUsage(AttributeTargets.Class, AllowMultiple = false, Inherited = true)]
[GeneratorContractRegistration<ModelDiscoveryContract>(ModelDiscoveryContract.DatabaseEngineModelAttribute)]
public abstract class DatabaseEngineModelAttribute : Attribute;
