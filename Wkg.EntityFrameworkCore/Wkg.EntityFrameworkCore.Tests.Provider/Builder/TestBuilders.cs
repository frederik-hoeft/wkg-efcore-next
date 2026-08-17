using System.Data;
using System.Data.Common;
using System.Linq.Expressions;
using Wkg.EntityFrameworkCore.ProcedureMapping;
using Wkg.EntityFrameworkCore.ProcedureMapping.Builder;
using Wkg.EntityFrameworkCore.ProcedureMapping.Builder.ResultBinding;
using Wkg.EntityFrameworkCore.ProcedureMapping.Builder.ThrowHelpers;
using Wkg.EntityFrameworkCore.ProcedureMapping.Compiler;
using Wkg.EntityFrameworkCore.ProcedureMapping.Compiler.Output;
using Wkg.EntityFrameworkCore.ProcedureMapping.Compiler.ResultBinding;
using Wkg.EntityFrameworkCore.ProcedureMapping.Configuration;
using Wkg.EntityFrameworkCore.ProcedureMapping.Generation;
using Wkg.EntityFrameworkCore.Tests.Provider.Generation;

namespace Wkg.EntityFrameworkCore.Tests.Provider.Builder;

public abstract class TestStoredProcedure<TIOContainer> : StoredProcedure<TIOContainer>
    where TIOContainer : class;

public abstract class TestStoredProcedure<TIOContainer, TResult> : StoredProcedure<TIOContainer, TResult>
    where TIOContainer : class
    where TResult : class;

public interface ITestProcedureConfiguration<TProcedure, TIOContainer> : IDiscoverableProcedureConfiguration
    where TProcedure : StoredProcedure, ITestProcedureConfiguration<TProcedure, TIOContainer>
    where TIOContainer : class
{
    static abstract void Configure(TestProcedureBuilder<TProcedure, TIOContainer> self);
}

[ProcedureGrammarScope(GrammarScopeKind.Procedure, typeof(TestProcedureIntrinsics), Initializer = nameof(TestProcedureIntrinsics.Create), Finalizer = nameof(TestProcedureIntrinsics.BuildCommandText))]
public class TestProcedureBuilder<TProcedure, TIOContainer>
    : ProcedureBuilder<TProcedure, TIOContainer, TestCompiledParameter, DbDataReader, TestProcedureBuilder<TProcedure, TIOContainer>>
    where TProcedure : StoredProcedure, ITestProcedureConfiguration<TProcedure, TIOContainer>
    where TIOContainer : class
{
    [TerminalIntrinsic(typeof(TestProcedureIntrinsics), nameof(TestProcedureIntrinsics.HasSchema))]
    public TestProcedureBuilder<TProcedure, TIOContainer> HasSchema(string schema) => this;

    [StructuralOperation(StructuralRole.Parameter)]
    public TestParameterBuilder<TIOContainer, TParameter> Parameter<TParameter>(Expression<Func<TIOContainer, TParameter>> parameterExpression)
    {
        TestParameterBuilder<TIOContainer, TParameter> builder = new(parameterExpression, ThrowHelper);
        ParameterBuilders.Add(builder);
        return builder;
    }

    [StructuralOperation(StructuralRole.Returns)]
    public TestResultBuilder<TResult> Returns<TResult>() where TResult : class
    {
        TestResultBuilder<TResult> builder = new(ThrowHelper);
        ResultBuilder = builder;
        return builder;
    }

    [StructuralOperation(StructuralRole.ReturnsScalar)]
    public TestParameterBuilder<TIOContainer, TParameter> ReturnsScalar<TParameter>(Expression<Func<TIOContainer, TParameter>> parameterExpression)
    {
        TestParameterBuilder<TIOContainer, TParameter> builder = Parameter(parameterExpression);
        builder.HasDirection(ParameterDirection.ReturnValue);
        return builder;
    }

    protected override IProcedureCompiler<TestCompiledParameter> Build() =>
        throw new NotSupportedException("The test provider is source-generation only.");
}

[ProcedureGrammarScope(GrammarScopeKind.Parameter, typeof(TestParameterIntrinsics), Initializer = nameof(TestParameterIntrinsics.Create), Finalizer = nameof(TestParameterIntrinsics.Finalize))]
public class TestParameterBuilder<TIOContainer, TParameter>
    : ParameterBuilder<TIOContainer, TParameter, TestCompiledParameter, TestParameterBuilder<TIOContainer, TParameter>>
    where TIOContainer : class
{
    public TestParameterBuilder(Expression<Func<TIOContainer, TParameter>> parameterSelector, IProcedureThrowHelper throwHelper)
        : base(parameterSelector, throwHelper)
    {
    }

    [TerminalIntrinsic(typeof(TestParameterIntrinsics), nameof(TestParameterIntrinsics.HasDbType))]
    public TestParameterBuilder<TIOContainer, TParameter> HasDbType(DbType dbType) => this;

    [CompositeBuilder]
    public TestParameterBuilder<TIOContainer, TParameter> HasPrecision(Action<TestPrecisionBuilder> configure)
    {
        configure(new TestPrecisionBuilder());
        return this;
    }

    protected override IParameterCompiler<TestCompiledParameter> Build() =>
        throw new NotSupportedException("The test provider is source-generation only.");
}

public class TestPrecisionBuilder
{
    [TerminalIntrinsic(typeof(TestParameterIntrinsics), nameof(TestParameterIntrinsics.Precision))]
    public TestPrecisionBuilder Precision(int precision) => this;

    [TerminalIntrinsic(typeof(TestParameterIntrinsics), nameof(TestParameterIntrinsics.Scale))]
    public TestPrecisionBuilder Scale(int scale) => this;
}

[ProcedureGrammarScope(GrammarScopeKind.Result, typeof(object))]
public class TestResultBuilder<TResult>
    : ResultBuilder<TResult, DbDataReader, TestResultBuilder<TResult>>
    where TResult : class
{
    public TestResultBuilder(IProcedureThrowHelper throwHelper) : base(throwHelper, typeof(TResult))
    {
    }

    [StructuralOperation(StructuralRole.Column)]
    public TestColumnBuilder<TResult, TProperty> Column<TProperty>(Expression<Func<TResult, TProperty>> propertySelector)
    {
        TestColumnBuilder<TResult, TProperty> builder = new(propertySelector, ThrowHelper);
        ColumnBuilders.Add(builder);
        return builder;
    }

    protected override IResultCompiler<DbDataReader> Build() =>
        throw new NotSupportedException("The test provider is source-generation only.");
}

[ProcedureGrammarScope(GrammarScopeKind.Column, typeof(TestColumnIntrinsics), Initializer = nameof(TestColumnIntrinsics.Create), Finalizer = nameof(TestColumnIntrinsics.Read))]
public class TestColumnBuilder<TResult, TProperty>
    : ResultColumnBuilder<TResult, TProperty, TestColumnBuilder<TResult, TProperty>>
{
    public TestColumnBuilder(Expression<Func<TResult, TProperty>> columnSelector, IResultThrowHelper throwHelper)
        : base(columnSelector, throwHelper)
    {
    }

    [TerminalIntrinsic(typeof(TestColumnIntrinsics), nameof(TestColumnIntrinsics.GetAsInt32))]
    public TestColumnBuilder<TResult, TProperty> GetAsInt32() => this;

    [TerminalIntrinsic(typeof(TestColumnIntrinsics), nameof(TestColumnIntrinsics.GetAsString))]
    public TestColumnBuilder<TResult, TProperty> GetAsString() => this;

    [TerminalIntrinsic(typeof(TestColumnIntrinsics), nameof(TestColumnIntrinsics.GetAsBytes))]
    public TestColumnBuilder<TResult, TProperty> GetAsBytes() => this;

    protected override void AttemptAutoConfiguration()
    {
    }

    protected override IResultColumnCompiler Build() =>
        throw new NotSupportedException("The test provider is source-generation only.");
}
