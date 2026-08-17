using System.Data;
using System.Data.Common;
using Wkg.EntityFrameworkCore.ProcedureMapping.Compiler.Output;

namespace Wkg.EntityFrameworkCore.Tests.Provider;

/// <summary>
/// Dummy compiled-parameter type required by the inherited builder pipeline. The test provider is source-generation only.
/// </summary>
public readonly struct TestCompiledParameter : ICompiledParameter
{
    public string Name => throw new NotSupportedException();
    public ParameterDirection Direction => throw new NotSupportedException();
    public PropertyGetter Getter => throw new NotSupportedException();
    public PropertySetter? Setter => throw new NotSupportedException();
    public bool IsOutput => throw new NotSupportedException();
    public void Load(ref DbParameter? parameter, object container) => throw new NotSupportedException();
    public void Store(ref DbParameter parameter, object container) => throw new NotSupportedException();
}
