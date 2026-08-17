using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.Text;
using System.Collections.Immutable;
using System.Data;
using System.Text;
using Wkg.EntityFrameworkCore.SourceGeneration.Helpers;
using Wkg.EntityFrameworkCore.SourceGeneration.ProcedureMapping.Analysis;
using Wkg.EntityFrameworkCore.SourceGeneration.ProcedureMapping.Contracts;
using Wkg.EntityFrameworkCore.SourceGeneration.ProcedureMapping.Diagnostics;
using Wkg.EntityFrameworkCore.SourceGeneration.ProcedureMapping.Grammar;
using Wkg.EntityFrameworkCore.SourceGeneration.ProcedureMapping.Model;

namespace Wkg.EntityFrameworkCore.SourceGeneration.ProcedureMapping.Emission;

internal static class ProcedurePlanEmitter
{
    public static EmittedProcedurePlan? Emit(
        SourceProductionContext context,
        ProcedurePlanModel model,
        ProcedureGenerationContractBindings contracts)
    {
        foreach (Diagnostic diagnostic in model.Diagnostics)
        {
            context.ReportDiagnostic(diagnostic);
        }

        if (model.Diagnostics.Count > 0)
        {
            return null;
        }

        IMethodSymbol? resultConstructor = null;
        if (model.Result is { } result)
        {
            resultConstructor = SelectConstructor(result);
            if (resultConstructor is null)
            {
                context.ReportDiagnostic(Diagnostic.Create(
                    ProcedureGenerationDiagnostics.MissingResultConstructor,
                    model.Location,
                    ConstantExpressionFormatter.Qualify(result.ResultType)));
                return null;
            }
        }

        string planName = $"{Sanitize(model.ProcedureType.Name)}ProcedurePlan";
        string ns = model.ProcedureType.ContainingNamespace.IsGlobalNamespace
            ? "GeneratedProcedurePlans"
            : $"{model.ProcedureType.ContainingNamespace.ToDisplayString()}.Generated";
        string procedureType = ConstantExpressionFormatter.Qualify(model.ProcedureType);
        string ioType = ConstantExpressionFormatter.Qualify(model.IOContainerType);
        string planInterface = ConstantExpressionFormatter.Qualify(contracts.ExecutionPlan);
        string compiledInterface = ConstantExpressionFormatter.Qualify(contracts.CompiledProcedure);
        string contextType = ConstantExpressionFormatter.Qualify(contracts.PlanExecutionContext);
        string attributeType = ConstantExpressionFormatter.Qualify(contracts.GeneratedProcedurePlanAttribute);

        StringBuilder accessors = new();
        StringBuilder bind = new();
        StringBuilder store = new();
        StringBuilder read = new();
        bool hasOutputs = model.Parameters.Any(static parameter =>
            parameter.Direction is ParameterDirection.Output or ParameterDirection.InputOutput or ParameterDirection.ReturnValue);

        bind.AppendLine($"        {ioType} io = ({ioType})container;");
        if (hasOutputs)
        {
            store.AppendLine($"        {ioType} io = ({ioType})container;");
        }

        for (int i = 0; i < model.Parameters.Count; i++)
        {
            ParameterPlanModel parameter = model.Parameters[i];
            AppendParameterBind(bind, parameter, i);
            if (parameter.Direction is ParameterDirection.Output or ParameterDirection.InputOutput or ParameterDirection.ReturnValue)
            {
                AppendParameterStore(store, accessors, parameter, i, ioType);
            }
        }

        if (model.Result is { } resultModel && resultConstructor is not null)
        {
            AppendResultRead(read, resultModel, resultConstructor);
        }
        else
        {
            read.AppendLine("        throw new global::System.InvalidOperationException(\"This procedure does not declare a result set.\");");
        }

        string commandTextExpression = BuildCommandTextExpression(model, out string? commandTextHelper);
        bool hasResult = model.Result is not null;
        bool isCollection = model.Result?.IsCollection ?? false;
        string executionContextInterface = ConstantExpressionFormatter.Qualify(GetExecutionContextInterface(contracts));

        StringBuilder builder = new();
        builder.AppendLine("#nullable enable");
        builder.AppendLine();
        builder.AppendLine($"namespace {ns};");
        builder.AppendLine();
        builder.AppendLine($"[{attributeType}(typeof({procedureType}))]");
        builder.AppendLine($"public sealed class {planName} : {planInterface}, {compiledInterface}");
        builder.AppendLine("{");
        builder.AppendLine($"    public global::System.Type ProcedureType => typeof({procedureType});");
        builder.AppendLine();
        builder.AppendLine($"    public string ProcedureName => {commandTextExpression};");
        builder.AppendLine();
        builder.AppendLine($"    public bool IsFunction => {(model.IsFunction ? "true" : "false")};");
        builder.AppendLine();
        builder.AppendLine($"    public int ParameterCount => {model.Parameters.Count};");
        builder.AppendLine();
        builder.AppendLine($"    public bool HasResult => {(hasResult ? "true" : "false")};");
        builder.AppendLine();
        builder.AppendLine($"    public bool IsCollectionResult => {(isCollection ? "true" : "false")};");
        builder.AppendLine();
        builder.AppendLine($"    public {executionContextInterface} CreateExecutionContext() => new {contextType}(this);");
        builder.AppendLine();
        builder.AppendLine("    public void BindParameters(global::System.Data.Common.DbParameter?[] parameters, object container)");
        builder.AppendLine("    {");
        builder.Append(bind);
        builder.AppendLine("    }");
        builder.AppendLine();
        builder.AppendLine("    public void StoreOutputs(global::System.Data.Common.DbParameter?[] parameters, object container, object? scalarReturn)");
        builder.AppendLine("    {");
        builder.Append(store);
        builder.AppendLine("    }");
        builder.AppendLine();
        builder.AppendLine("    public object ReadResult(global::System.Data.Common.DbDataReader reader)");
        builder.AppendLine("    {");
        builder.Append(read);
        builder.AppendLine("    }");
        if (commandTextHelper is not null)
        {
            builder.AppendLine();
            builder.Append(commandTextHelper);
        }
        if (accessors.Length > 0)
        {
            builder.AppendLine();
            builder.Append(accessors);
        }
        builder.AppendLine("}");

        context.AddSource($"{planName}.g.cs", SourceText.From(builder.ToString(), Encoding.UTF8));
        return new EmittedProcedurePlan(model.ProcedureType, $"{ns}.{planName}", model.IsDiscoverable);
    }

    private static INamedTypeSymbol GetExecutionContextInterface(ProcedureGenerationContractBindings contracts)
    {
        INamedTypeSymbol context = contracts.PlanExecutionContext;
        INamedTypeSymbol? iface = context.Interfaces.FirstOrDefault();
        return iface ?? context;
    }

    private static void AppendParameterBind(StringBuilder builder, ParameterPlanModel parameter, int index)
    {
        GrammarScopeBinding scope = parameter.Scope!;
        string stateType = InferStateType(scope);
        string getter = $"io.{parameter.Property.Name}";
        builder.AppendLine("        {");
        if (scope.Initializer is not null)
        {
            builder.AppendLine($"            {stateType} state = {Invoke(scope.Initializer)};");
        }

        foreach (BoundTerminal terminal in parameter.Terminals)
        {
            builder.AppendLine($"            {RenderIntrinsicCall(terminal, hasState: scope.Initializer is not null)};");
        }

        string finalizeArgs = BindKnownArguments(
            scope.Finalizer!,
            stateType,
            name: Quote(parameter.Name),
            direction: FormatDirection(parameter.Direction),
            size: parameter.Size.ToString(),
            clrType: $"typeof({ConstantExpressionFormatter.Qualify(parameter.Property.Type)})",
            value: getter,
            reader: null,
            isNullable: null,
            procedureName: null,
            isFunction: null,
            isCollection: null);
        builder.AppendLine($"            parameters[{index}] = {Invoke(scope.Finalizer!, finalizeArgs)};");
        builder.AppendLine("        }");
    }

    private static void AppendParameterStore(
        StringBuilder store,
        StringBuilder accessors,
        ParameterPlanModel parameter,
        int index,
        string ioType)
    {
        GrammarScopeBinding scope = parameter.Scope!;
        IMethodSymbol? storeMethod = scope.IntrinsicsType.GetMembers("Store")
            .OfType<IMethodSymbol>()
            .FirstOrDefault(static method => method.IsStatic)
            ?? scope.IntrinsicsType.GetMembers("ReadOutput")
                .OfType<IMethodSymbol>()
                .FirstOrDefault(static method => method.IsStatic);

        string valueExpression;
        string propertyType = ConstantExpressionFormatter.Qualify(parameter.Property.Type);
        if (storeMethod is { IsGenericMethod: true })
        {
            valueExpression = $"{ConstantExpressionFormatter.Qualify(storeMethod.ContainingType)}.{storeMethod.Name}<{propertyType}>(parameters[{index}]!)";
        }
        else if (storeMethod is not null)
        {
            valueExpression = $"({propertyType}){ConstantExpressionFormatter.Qualify(storeMethod.ContainingType)}.{storeMethod.Name}(parameters[{index}]!)!";
        }
        else
        {
            valueExpression = $"({propertyType})parameters[{index}]!.Value!";
        }

        IMethodSymbol? setter = parameter.Property.SetMethod;
        bool useUnsafeAccessor = setter is null
            || setter.IsInitOnly
            || setter.DeclaredAccessibility is not Accessibility.Public;
        if (useUnsafeAccessor)
        {
            string accessorName = $"Set{parameter.Property.Name}";
            store.AppendLine($"        {accessorName}(io, {valueExpression});");
            accessors.AppendLine($"    [global::System.Runtime.CompilerServices.UnsafeAccessor(global::System.Runtime.CompilerServices.UnsafeAccessorKind.Method, Name = \"set_{parameter.Property.Name}\")]");
            accessors.AppendLine($"    private static extern void {accessorName}({ioType} container, {propertyType} value);");
        }
        else
        {
            store.AppendLine($"        io.{parameter.Property.Name} = {valueExpression};");
        }
    }

    private static void AppendResultRead(StringBuilder builder, ResultPlanModel result, IMethodSymbol constructor)
    {
        List<string> ctorArgs = [];
        ImmutableArray<IParameterSymbol> ctorParameters = constructor.Parameters;
        for (int i = 0; i < ctorParameters.Length; i++)
        {
            IParameterSymbol ctorParameter = ctorParameters[i];
            ColumnPlanModel column = result.Columns.First(candidate =>
                candidate.Property.Name.Equals(ctorParameter.Name, StringComparison.OrdinalIgnoreCase)
                && SymbolEqualityComparer.Default.Equals(candidate.Property.Type, ctorParameter.Type));
            string local = $"column{i}";
            string propertyType = ConstantExpressionFormatter.Qualify(column.Property.Type);
            GrammarScopeBinding scope = column.Scope!;
            string stateType = InferStateType(scope);
            builder.AppendLine("        {");
            if (scope.Initializer is not null)
            {
                builder.AppendLine($"            {stateType} state = {Invoke(scope.Initializer)};");
            }

            foreach (BoundTerminal terminal in column.Terminals)
            {
                builder.AppendLine($"            {RenderIntrinsicCall(terminal, hasState: scope.Initializer is not null)};");
            }

            string readArgs = BindKnownArguments(
                scope.Finalizer!,
                stateType,
                name: Quote(column.Name ?? column.Property.Name),
                direction: null,
                size: null,
                clrType: $"typeof({propertyType})",
                value: null,
                reader: "reader",
                isNullable: column.IsNullable ? "true" : "false",
                procedureName: null,
                isFunction: null,
                isCollection: result.IsCollection ? "true" : "false");
            string raw = Invoke(scope.Finalizer!, readArgs);
            if (column.Conversion is { } conversion)
            {
                string sourceType = ConstantExpressionFormatter.Qualify(conversion.SourceType);
                builder.AppendLine($"            {sourceType} __value = ({sourceType}){raw}!;");
                builder.AppendLine($"            {propertyType} {local} = {conversion.RenderedExpression};");
            }
            else
            {
                builder.AppendLine($"            {propertyType} {local} = ({propertyType}){raw}!;");
            }

            builder.AppendLine("        }");
            ctorArgs.Add(local);
        }

        // Locals declared inside blocks are not visible; emit them in the outer scope instead.
        // Rebuild with outer locals.
        builder.Clear();
        ctorArgs.Clear();
        for (int i = 0; i < ctorParameters.Length; i++)
        {
            IParameterSymbol ctorParameter = ctorParameters[i];
            ColumnPlanModel column = result.Columns.First(candidate =>
                candidate.Property.Name.Equals(ctorParameter.Name, StringComparison.OrdinalIgnoreCase)
                && SymbolEqualityComparer.Default.Equals(candidate.Property.Type, ctorParameter.Type));
            string local = $"column{i}";
            string propertyType = ConstantExpressionFormatter.Qualify(column.Property.Type);
            GrammarScopeBinding scope = column.Scope!;
            string stateType = InferStateType(scope);
            string rawLocal = $"{local}Raw";
            if (scope.Initializer is not null)
            {
                builder.AppendLine($"        {stateType} {local}State = {Invoke(scope.Initializer)};");
            }

            foreach (BoundTerminal terminal in column.Terminals)
            {
                builder.AppendLine($"        {RenderIntrinsicCall(terminal, hasState: scope.Initializer is not null, stateIdentifier: $"{local}State")};");
            }

            string readArgs = BindKnownArguments(
                scope.Finalizer!,
                stateType,
                name: Quote(column.Name ?? column.Property.Name),
                direction: null,
                size: null,
                clrType: $"typeof({propertyType})",
                value: null,
                reader: "reader",
                isNullable: column.IsNullable ? "true" : "false",
                procedureName: null,
                isFunction: null,
                isCollection: result.IsCollection ? "true" : "false",
                stateIdentifier: $"{local}State");
            string raw = Invoke(scope.Finalizer!, readArgs);
            if (column.Conversion is { } conversion)
            {
                string sourceType = ConstantExpressionFormatter.Qualify(conversion.SourceType);
                builder.AppendLine($"        {sourceType} {rawLocal} = ({sourceType}){raw}!;");
                builder.AppendLine($"        {propertyType} {local} = {conversion.RenderedExpression.Replace("__value", rawLocal)};");
            }
            else
            {
                builder.AppendLine($"        {propertyType} {local} = ({propertyType}){raw}!;");
            }

            ctorArgs.Add(local);
        }

        builder.AppendLine($"        return new {ConstantExpressionFormatter.Qualify(result.ResultType)}({string.Join(", ", ctorArgs)});");
    }

    private static string BuildCommandTextExpression(ProcedurePlanModel model, out string? helper)
    {
        string rawName = Quote(model.ProcedureName ?? model.ProcedureType.Name);
        if (model.ProcedureScope?.Finalizer is null)
        {
            helper = null;
            return rawName;
        }

        GrammarScopeBinding scope = model.ProcedureScope;
        string stateType = InferStateType(scope);
        StringBuilder body = new();
        body.AppendLine("    private static string BuildProcedureName()");
        body.AppendLine("    {");
        if (scope.Initializer is not null)
        {
            body.AppendLine($"        {stateType} state = {Invoke(scope.Initializer)};");
        }

        foreach (BoundTerminal terminal in model.ProcedureTerminals)
        {
            body.AppendLine($"        {RenderIntrinsicCall(terminal, hasState: scope.Initializer is not null)};");
        }

        string args = BindKnownArguments(
            scope.Finalizer,
            stateType,
            name: rawName,
            direction: null,
            size: null,
            clrType: null,
            value: null,
            reader: null,
            isNullable: null,
            procedureName: rawName,
            isFunction: model.IsFunction ? "true" : "false",
            isCollection: null);
        body.AppendLine($"        return {Invoke(scope.Finalizer, args)};");
        body.AppendLine("    }");
        helper = body.ToString();
        return "BuildProcedureName()";
    }

    private static IMethodSymbol? SelectConstructor(ResultPlanModel result)
    {
        IMethodSymbol? match = null;
        foreach (IMethodSymbol constructor in result.ResultType.InstanceConstructors)
        {
            if (constructor.DeclaredAccessibility is not (Accessibility.Public or Accessibility.Internal))
            {
                continue;
            }

            if (constructor.Parameters.Length != result.Columns.Count)
            {
                continue;
            }

            bool compatible = constructor.Parameters.All(parameter =>
                result.Columns.Any(column =>
                    column.Property.Name.Equals(parameter.Name, StringComparison.OrdinalIgnoreCase)
                    && SymbolEqualityComparer.Default.Equals(column.Property.Type, parameter.Type)));
            if (!compatible)
            {
                continue;
            }

            if (match is not null)
            {
                return null;
            }

            match = constructor;
        }

        return match;
    }

    private static string InferStateType(GrammarScopeBinding scope)
    {
        if (scope.Initializer is not null)
        {
            return ConstantExpressionFormatter.Qualify(scope.Initializer.ReturnType);
        }

        if (scope.Finalizer?.Parameters is [{ Type: { } first }, ..])
        {
            return ConstantExpressionFormatter.Qualify(first);
        }

        return "object";
    }

    private static string RenderIntrinsicCall(BoundTerminal terminal, bool hasState, string stateIdentifier = "state")
    {
        List<string> arguments = [];
        ImmutableArray<IParameterSymbol> parameters = terminal.Intrinsic.Parameters;
        int extraIndex = 0;
        foreach (IParameterSymbol parameter in parameters)
        {
            if (IsStateParameter(parameter))
            {
                arguments.Add(parameter.RefKind is RefKind.Ref or RefKind.Out or RefKind.In
                    ? $"{RefPrefix(parameter.RefKind)}{stateIdentifier}"
                    : stateIdentifier);
                continue;
            }

            if (extraIndex < terminal.Arguments.Count)
            {
                arguments.Add(terminal.Arguments[extraIndex++]);
            }
        }

        string call = $"{ConstantExpressionFormatter.Qualify(terminal.Intrinsic.ContainingType)}.{terminal.Intrinsic.Name}({string.Join(", ", arguments)})";
        if (hasState && terminal.Intrinsic.ReturnsVoid is false && IsStateParameter(terminal.Intrinsic.Parameters.FirstOrDefault()))
        {
            return $"{stateIdentifier} = {call}";
        }

        return call;
    }

    private static string BindKnownArguments(
        IMethodSymbol method,
        string stateType,
        string? name,
        string? direction,
        string? size,
        string? clrType,
        string? value,
        string? reader,
        string? isNullable,
        string? procedureName,
        string? isFunction,
        string? isCollection,
        string stateIdentifier = "state")
    {
        List<string> arguments = [];
        foreach (IParameterSymbol parameter in method.Parameters)
        {
            if (IsStateParameter(parameter))
            {
                arguments.Add(parameter.RefKind is not RefKind.None
                    ? $"{RefPrefix(parameter.RefKind)}{stateIdentifier}"
                    : stateIdentifier);
                continue;
            }

            string parameterName = parameter.Name;
            if (Matches(parameterName, "name", "parameterName", "columnName") && name is not null)
            {
                arguments.Add(name);
            }
            else if (Matches(parameterName, "direction", "parameterDirection") && direction is not null)
            {
                arguments.Add(direction);
            }
            else if (Matches(parameterName, "size") && size is not null)
            {
                arguments.Add(size);
            }
            else if (Matches(parameterName, "clrType", "type") && clrType is not null)
            {
                arguments.Add(clrType);
            }
            else if (Matches(parameterName, "value", "runtimeValue") && value is not null)
            {
                arguments.Add(value);
            }
            else if (Matches(parameterName, "reader", "dataReader") && reader is not null)
            {
                arguments.Add(reader);
            }
            else if (Matches(parameterName, "isNullable", "nullable") && isNullable is not null)
            {
                arguments.Add(isNullable);
            }
            else if (Matches(parameterName, "procedureName") && procedureName is not null)
            {
                arguments.Add(procedureName);
            }
            else if (Matches(parameterName, "isFunction") && isFunction is not null)
            {
                arguments.Add(isFunction);
            }
            else if (Matches(parameterName, "isCollection") && isCollection is not null)
            {
                arguments.Add(isCollection);
            }
            else if (parameter.HasExplicitDefaultValue)
            {
                arguments.Add(ConstantExpressionFormatter.FormatValue(parameter.Type, parameter.ExplicitDefaultValue));
            }
            else
            {
                arguments.Add("default!");
            }
        }

        return string.Join(", ", arguments);
    }

    private static string Invoke(IMethodSymbol method, string? arguments = null)
    {
        string typeName = ConstantExpressionFormatter.Qualify(method.ContainingType);
        return arguments is null
            ? $"{typeName}.{method.Name}()"
            : $"{typeName}.{method.Name}({arguments})";
    }

    private static bool IsStateParameter(IParameterSymbol? parameter) =>
        parameter is not null
        && (parameter.RefKind is not RefKind.None
            || parameter.Name.Equals("state", StringComparison.OrdinalIgnoreCase));

    private static string RefPrefix(RefKind kind) => kind switch
    {
        RefKind.Ref => "ref ",
        RefKind.Out => "out ",
        RefKind.In => "in ",
        _ => string.Empty
    };

    private static bool Matches(string name, params string[] candidates) =>
        candidates.Any(candidate => name.Equals(candidate, StringComparison.OrdinalIgnoreCase));

    private static string FormatDirection(ParameterDirection direction) =>
        $"global::System.Data.ParameterDirection.{direction}";

    private static string Quote(string value) => SymbolDisplay.FormatLiteral(value, quote: true);

    private static string Sanitize(string name)
    {
        StringBuilder builder = new(name.Length);
        foreach (char character in name)
        {
            builder.Append(char.IsLetterOrDigit(character) ? character : '_');
        }

        return builder.Length is 0 ? "Procedure" : builder.ToString();
    }
}

internal sealed record EmittedProcedurePlan(INamedTypeSymbol ProcedureType, string FullyQualifiedName, bool IsDiscoverable);
