using Microsoft.EntityFrameworkCore;
using Wkg.EntityFrameworkCore.Configuration.Discovery;
using Wkg.EntityFrameworkCore.Configuration.Reflection.Discovery;

namespace Wkg.EntityFrameworkCore.Configuration.Reflection;

internal interface IReflectiveModelLoader
{
    void LoadModels(ModelBuilder builder, IEntityDiscoveryContext discoveryContext, DiscoveryOptions options);
}
