using System.Reflection;
using Wkg.EntityFrameworkCore.Configuration.Reflection.Discovery;

namespace Wkg.EntityFrameworkCore.Tests.Model.Discovery;

public sealed class ModelAssembly : ITargetAssembly
{
    public static Assembly Assembly => typeof(ModelAssembly).Assembly;
}
