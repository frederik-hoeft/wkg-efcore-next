using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using System.Collections.Immutable;
using System.Data;
using Wkg.EntityFrameworkCore.ProcedureMapping.Generation;
using Wkg.EntityFrameworkCore.SourceGeneration.ProcedureMapping.Contracts;
using Wkg.EntityFrameworkCore.SourceGeneration.ProcedureMapping.Diagnostics;
using Wkg.EntityFrameworkCore.SourceGeneration.ProcedureMapping.Grammar;
using Wkg.EntityFrameworkCore.SourceGeneration.ProcedureMapping.Model;

namespace Wkg.EntityFrameworkCore.SourceGeneration.ProcedureMapping.Analysis;

internal sealed class ConfigureSyntaxAnalyzer(
    SemanticModel semanticModel,
    ProviderGrammar grammar,
    ProcedureGenerationContractBindings contracts)
{
    private readonly Dictionary<string, AnalysisFrame> _locals = new(StringComparer.Ordinal);

    public ProcedurePlanModel Analyze(IMethodSymbol configureMethod, MethodDeclarationSyntax syntax, CancellationToken cancellationToken)
    {
        INamedTypeSymbol procedureType = configureMethod.ContainingType;
        INamedTypeSymbol ioContainer = ResolveIOContainer(configureMethod.Parameters[0].Type) ?? procedureType;
        ProcedurePlanModel model = new()
        {
            ProcedureType = procedureType,
            IOContainerType = ioContainer,
            Location = syntax.GetLocation(),
            ProcedureScope = grammar.FindScope(configureMethod.Parameters[0].Type, GrammarScopeKind.Procedure),
            IsDiscoverable = Implements(procedureType, contracts.DiscoverableProcedureConfiguration)
        };

        if (syntax.Body is null)
        {
            Report(model, syntax, ProcedureGenerationDiagnostics.UnsupportedSyntax, "Expression-bodied Configure methods are not supported; use a statement body.");
            return model;
        }

        AnalysisFrame procedureFrame = new(AnalysisKind.Procedure, model, Parameter: null, Result: null, Column: null);
        _locals[configureMethod.Parameters[0].Name] = procedureFrame;

        foreach (StatementSyntax statement in syntax.Body.Statements)
        {
            AnalyzeStatement(statement, model, cancellationToken);
        }

        Validate(model);
        return model;
    }

    private void AnalyzeStatement(StatementSyntax statement, ProcedurePlanModel model, CancellationToken cancellationToken)
    {
        switch (statement)
        {
            case EmptyStatementSyntax:
                return;
            case BlockSyntax block:
                foreach (StatementSyntax child in block.Statements)
                {
                    AnalyzeStatement(child, model, cancellationToken);
                }
                return;
            case LocalDeclarationStatementSyntax declaration:
                AnalyzeLocalDeclaration(declaration, model, cancellationToken);
                return;
            case ExpressionStatementSyntax expressionStatement:
                AnalyzeExpressionStatement(expressionStatement, model, cancellationToken);
                return;
            default:
                Report(model, statement, ProcedureGenerationDiagnostics.UnsupportedSyntax,
                    $"Statement kind '{statement.Kind()}' is not allowed. Local state and control flow are forbidden.");
                return;
        }
    }

    private void AnalyzeLocalDeclaration(LocalDeclarationStatementSyntax declaration, ProcedurePlanModel model, CancellationToken cancellationToken)
    {
        if (declaration.Declaration.Variables is not [VariableDeclaratorSyntax variable] || variable.Initializer is null)
        {
            Report(model, declaration, ProcedureGenerationDiagnostics.UnsupportedSyntax,
                "Only a single builder local with an initializer is allowed.");
            return;
        }

        if (AnalyzeChain(variable.Initializer.Value, model, cancellationToken) is { } frame)
        {
            _locals[variable.Identifier.ValueText] = frame;
            return;
        }

        Report(model, declaration, ProcedureGenerationDiagnostics.UnsupportedSyntax,
            "Locals may only hold fluent builder instances used to continue configuration.");
    }

    private void AnalyzeExpressionStatement(ExpressionStatementSyntax statement, ProcedurePlanModel model, CancellationToken cancellationToken)
    {
        ExpressionSyntax expression = statement.Expression;
        if (expression is AssignmentExpressionSyntax assignment)
        {
            if (assignment.Left is IdentifierNameSyntax { Identifier.ValueText: "_" })
            {
                _ = AnalyzeChain(assignment.Right, model, cancellationToken);
                return;
            }

            if (assignment.Left is IdentifierNameSyntax identifier
                && AnalyzeChain(assignment.Right, model, cancellationToken) is { } frame)
            {
                _locals[identifier.Identifier.ValueText] = frame;
                return;
            }

            Report(model, statement, ProcedureGenerationDiagnostics.UnsupportedSyntax,
                "Assignments may only update builder locals that continue a fluent chain.");
            return;
        }

        _ = AnalyzeChain(expression, model, cancellationToken);
    }

    private AnalysisFrame? AnalyzeChain(ExpressionSyntax expression, ProcedurePlanModel model, CancellationToken cancellationToken)
    {
        if (expression is AssignmentExpressionSyntax
            {
                Left: IdentifierNameSyntax { Identifier.ValueText: "_" },
                Right: { } discarded
            })
        {
            expression = discarded;
        }

        List<InvocationExpressionSyntax> invocations = [];
        ExpressionSyntax current = expression;
        while (current is InvocationExpressionSyntax invocation)
        {
            invocations.Add(invocation);
            if (invocation.Expression is MemberAccessExpressionSyntax memberAccess)
            {
                current = memberAccess.Expression;
                continue;
            }

            break;
        }

        invocations.Reverse();
        AnalysisFrame? frame = ResolveRoot(current, model);
        if (frame is null)
        {
            Report(model, expression, ProcedureGenerationDiagnostics.UnsupportedSyntax,
                "Fluent chains must start from the Configure parameter or a builder local.");
            return null;
        }

        foreach (InvocationExpressionSyntax invocation in invocations)
        {
            frame = ApplyInvocation(invocation, frame, model, cancellationToken);
            if (frame is null)
            {
                return null;
            }
        }

        return frame;
    }

    private AnalysisFrame? ResolveRoot(ExpressionSyntax expression, ProcedurePlanModel model)
    {
        if (expression is IdentifierNameSyntax identifier && _locals.TryGetValue(identifier.Identifier.ValueText, out AnalysisFrame? frame))
        {
            return frame;
        }

        if (expression is InvocationExpressionSyntax)
        {
            return null;
        }

        Report(model, expression, ProcedureGenerationDiagnostics.UnsupportedSyntax,
            "Configuration receivers must be the Configure parameter or a previously assigned builder local.");
        return null;
    }

    private AnalysisFrame? ApplyInvocation(
        InvocationExpressionSyntax invocation,
        AnalysisFrame frame,
        ProcedurePlanModel model,
        CancellationToken cancellationToken)
    {
        if (semanticModel.GetSymbolInfo(invocation, cancellationToken).Symbol is not IMethodSymbol method)
        {
            Report(model, invocation, ProcedureGenerationDiagnostics.UnknownInvocation, invocation.ToString());
            return null;
        }

        if (!grammar.TryGetOperation(method, out GrammarOperation operation))
        {
            Report(model, invocation, ProcedureGenerationDiagnostics.UnknownInvocation, method.Name);
            return null;
        }

        if (operation.IsComposite)
        {
            ApplyComposite(invocation, method, frame, model, cancellationToken);
        }

        if (operation.Intrinsic is not null)
        {
            List<string> arguments = BindTerminalArguments(invocation, method, model, cancellationToken);
            BoundTerminal terminal = new(operation.Intrinsic, arguments);
            switch (frame.Kind)
            {
                case AnalysisKind.Procedure:
                    model.ProcedureTerminals.Add(terminal);
                    break;
                case AnalysisKind.Parameter when frame.Parameter is not null:
                    frame.Parameter.Terminals.Add(terminal);
                    break;
                case AnalysisKind.Result when frame.Result is not null:
                    frame.Result.Terminals.Add(terminal);
                    break;
                case AnalysisKind.Column when frame.Column is not null:
                    frame.Column.Terminals.Add(terminal);
                    break;
            }
        }

        if (operation.Role is not { } role)
        {
            return frame;
        }

        return ApplyStructuralRole(role, invocation, method, frame, model, cancellationToken);
    }

    private AnalysisFrame ApplyStructuralRole(
        StructuralRole role,
        InvocationExpressionSyntax invocation,
        IMethodSymbol method,
        AnalysisFrame frame,
        ProcedurePlanModel model,
        CancellationToken cancellationToken)
    {
        switch (role)
        {
            case StructuralRole.ToDatabaseProcedure:
                model.ProcedureName = ReadRequiredString(invocation, method, model, cancellationToken);
                model.IsFunction = false;
                return frame;
            case StructuralRole.ToDatabaseFunction:
                model.ProcedureName = ReadRequiredString(invocation, method, model, cancellationToken);
                model.IsFunction = true;
                return frame;
            case StructuralRole.IsFunction:
                model.IsFunction = ReadOptionalBoolean(invocation, defaultValue: true, model, cancellationToken);
                return frame;
            case StructuralRole.Parameter:
            case StructuralRole.ReturnsScalar:
                return BeginParameter(invocation, method, model, cancellationToken, role is StructuralRole.ReturnsScalar);
            case StructuralRole.Returns:
                return BeginResult(invocation, method, model);
            case StructuralRole.Column:
                return BeginColumn(invocation, method, frame, model, cancellationToken);
            case StructuralRole.AsCollection when frame.Result is not null:
                frame.Result.IsCollection = true;
                return frame;
            case StructuralRole.AsSingle when frame.Result is not null:
                frame.Result.IsCollection = false;
                return frame;
            case StructuralRole.HasName:
                ApplyName(invocation, frame, model, cancellationToken);
                return frame;
            case StructuralRole.HasDirection when frame.Parameter is not null:
                frame.Parameter.Direction = ReadDirection(invocation, model, cancellationToken);
                return frame;
            case StructuralRole.HasSize when frame.Parameter is not null:
                frame.Parameter.Size = ReadInt32(invocation, model, cancellationToken);
                return frame;
            case StructuralRole.MayBeNull when frame.Column is not null:
                frame.Column.IsNullable = true;
                return frame;
            case StructuralRole.RequiresConversion when frame.Column is not null:
                frame.Column.Conversion = ReadConversion(invocation, frame.Column, model, cancellationToken);
                return frame;
            default:
                Report(model, invocation, ProcedureGenerationDiagnostics.UnknownInvocation, method.Name);
                return frame;
        }
    }

    private AnalysisFrame BeginParameter(
        InvocationExpressionSyntax invocation,
        IMethodSymbol method,
        ProcedurePlanModel model,
        CancellationToken cancellationToken,
        bool isReturnValue)
    {
        IPropertySymbol? property = ReadPropertySelector(invocation, model, cancellationToken);
        if (property is null)
        {
            return new AnalysisFrame(AnalysisKind.Parameter, model, Parameter: null, Result: null, Column: null);
        }

        ParameterPlanModel parameter = new()
        {
            Property = property,
            Name = property.Name,
            Direction = isReturnValue ? ParameterDirection.ReturnValue : ParameterDirection.Input,
            Scope = grammar.FindScope(method.ReturnType, GrammarScopeKind.Parameter)
                ?? grammar.FindScopeForBuilder(method.ReturnType)
        };
        if (parameter.Scope is null || parameter.Scope.Kind != GrammarScopeKind.Parameter)
        {
            parameter.Scope = grammar.FindScope(method.ReturnType, GrammarScopeKind.Parameter);
        }

        model.Parameters.Add(parameter);
        return new AnalysisFrame(AnalysisKind.Parameter, model, parameter, null, null);
    }

    private AnalysisFrame BeginResult(InvocationExpressionSyntax invocation, IMethodSymbol method, ProcedurePlanModel model)
    {
        INamedTypeSymbol? resultType = method.TypeArguments is [INamedTypeSymbol named]
            ? named
            : invocation.Expression is MemberAccessExpressionSyntax { Name: GenericNameSyntax generic }
                && generic.TypeArgumentList.Arguments is [TypeSyntax typeSyntax]
                && semanticModel.GetTypeInfo(typeSyntax).Type is INamedTypeSymbol fromSyntax
                ? fromSyntax
                : null;

        if (resultType is null)
        {
            Report(model, invocation, ProcedureGenerationDiagnostics.InvalidTopology, "Returns<TResult>() requires a result type argument.");
            return new AnalysisFrame(AnalysisKind.Result, model, Parameter: null, Result: null, Column: null);
        }

        ResultPlanModel result = new()
        {
            ResultType = resultType,
            Scope = grammar.FindScope(method.ReturnType, GrammarScopeKind.Result)
                ?? grammar.FindScopeForBuilder(method.ReturnType)
        };
        model.Result = result;
        return new AnalysisFrame(AnalysisKind.Result, model, null, result, null);
    }

    private AnalysisFrame BeginColumn(
        InvocationExpressionSyntax invocation,
        IMethodSymbol method,
        AnalysisFrame frame,
        ProcedurePlanModel model,
        CancellationToken cancellationToken)
    {
        ResultPlanModel? result = frame.Result ?? model.Result;
        if (result is null)
        {
            Report(model, invocation, ProcedureGenerationDiagnostics.InvalidTopology, "Column() must appear on a result builder.");
            return frame;
        }

        IPropertySymbol? property = ReadPropertySelector(invocation, model, cancellationToken);
        if (property is null)
        {
            return frame;
        }

        ColumnPlanModel column = new()
        {
            Property = property,
            Scope = grammar.FindScope(method.ReturnType, GrammarScopeKind.Column)
                ?? grammar.FindScopeForBuilder(method.ReturnType)
        };
        result.Columns.Add(column);
        return new AnalysisFrame(AnalysisKind.Column, model, null, result, column);
    }

    private void ApplyName(InvocationExpressionSyntax invocation, AnalysisFrame frame, ProcedurePlanModel model, CancellationToken cancellationToken)
    {
        string? name = ReadRequiredString(invocation, invocation.ArgumentList.Arguments.Count > 0 ? null : null, model, cancellationToken);
        if (name is null)
        {
            return;
        }

        if (frame.Parameter is not null)
        {
            frame.Parameter.Name = name;
        }
        else if (frame.Column is not null)
        {
            frame.Column.Name = name;
        }
    }

    private void ApplyComposite(
        InvocationExpressionSyntax invocation,
        IMethodSymbol method,
        AnalysisFrame frame,
        ProcedurePlanModel model,
        CancellationToken cancellationToken)
    {
        ArgumentSyntax? lambdaArgument = invocation.ArgumentList.Arguments.FirstOrDefault(argument =>
            argument.Expression is LambdaExpressionSyntax);
        if (lambdaArgument?.Expression is not LambdaExpressionSyntax lambda)
        {
            Report(model, invocation, ProcedureGenerationDiagnostics.UnsupportedNestedSyntax,
                $"Composite operation '{method.Name}' requires a nested builder lambda.");
            return;
        }

        string? nestedParameter = lambda switch
        {
            SimpleLambdaExpressionSyntax simple => simple.Parameter.Identifier.ValueText,
            ParenthesizedLambdaExpressionSyntax { ParameterList.Parameters: [ParameterSyntax parameter] } => parameter.Identifier.ValueText,
            _ => null
        };
        if (nestedParameter is not null)
        {
            _locals[nestedParameter] = frame;
        }

        if (lambda.Body is ExpressionSyntax expressionBody)
        {
            _ = AnalyzeChain(expressionBody, model, cancellationToken);
            return;
        }

        foreach (StatementSyntax statement in GetLambdaStatements(lambda))
        {
            AnalyzeStatement(statement, model, cancellationToken);
        }
    }

    private List<string> BindTerminalArguments(
        InvocationExpressionSyntax invocation,
        IMethodSymbol method,
        ProcedurePlanModel model,
        CancellationToken cancellationToken)
    {
        List<string> arguments = [];
        ImmutableArray<IParameterSymbol> parameters = method.ReducedFrom?.Parameters ?? method.Parameters;
        int argumentOffset = method.ReducedFrom is not null || method.IsExtensionMethod && method.Parameters.Length == invocation.ArgumentList.Arguments.Count + 1
            ? 1
            : 0;

        int argumentIndex = 0;
        for (int parameterIndex = argumentOffset; parameterIndex < parameters.Length; parameterIndex++)
        {
            IParameterSymbol parameter = parameters[parameterIndex];
            if (IsBuilderOrDelegate(parameter.Type))
            {
                if (argumentIndex < invocation.ArgumentList.Arguments.Count)
                {
                    argumentIndex++;
                }
                continue;
            }

            if (argumentIndex >= invocation.ArgumentList.Arguments.Count)
            {
                if (parameter.HasExplicitDefaultValue)
                {
                    arguments.Add(ConstantExpressionFormatter.FormatValue(parameter.Type, parameter.ExplicitDefaultValue));
                    continue;
                }

                Report(model, invocation, ProcedureGenerationDiagnostics.NonConstantArgument, parameter.Name, method.Name);
                continue;
            }

            ExpressionSyntax argument = invocation.ArgumentList.Arguments[argumentIndex++].Expression;
            if (IsPropertySelector(argument) || argument is LambdaExpressionSyntax)
            {
                continue;
            }

            if (ConstantExpressionFormatter.TryFormat(semanticModel, argument, cancellationToken, out string rendered))
            {
                arguments.Add(rendered);
                continue;
            }

            Report(model, argument, ProcedureGenerationDiagnostics.NonConstantArgument, argument.ToString(), method.Name);
        }

        return arguments;
    }

    private IPropertySymbol? ReadPropertySelector(InvocationExpressionSyntax invocation, ProcedurePlanModel model, CancellationToken cancellationToken)
    {
        ExpressionSyntax? selector = invocation.ArgumentList.Arguments
            .Select(static argument => argument.Expression)
            .FirstOrDefault(static expression => expression is LambdaExpressionSyntax);
        if (selector is null)
        {
            Report(model, invocation, ProcedureGenerationDiagnostics.InvalidPropertySelector, invocation.ToString());
            return null;
        }

        ExpressionSyntax? body = selector switch
        {
            SimpleLambdaExpressionSyntax simple => simple.ExpressionBody,
            ParenthesizedLambdaExpressionSyntax parenthesized => parenthesized.ExpressionBody,
            _ => null
        };
        if (body is not MemberAccessExpressionSyntax memberAccess
            || semanticModel.GetSymbolInfo(memberAccess, cancellationToken).Symbol is not IPropertySymbol property)
        {
            Report(model, selector, ProcedureGenerationDiagnostics.InvalidPropertySelector, selector.ToString());
            return null;
        }

        return property;
    }

    private ConversionPlan? ReadConversion(
        InvocationExpressionSyntax invocation,
        ColumnPlanModel column,
        ProcedurePlanModel model,
        CancellationToken cancellationToken)
    {
        if (invocation.ArgumentList.Arguments is not [{ Expression: LambdaExpressionSyntax lambda }])
        {
            Report(model, invocation, ProcedureGenerationDiagnostics.InvalidConversion, invocation.ToString());
            return null;
        }

        if (lambda.Body is not ExpressionSyntax body)
        {
            Report(model, lambda, ProcedureGenerationDiagnostics.InvalidConversion, lambda.ToString());
            return null;
        }

        if (ContainsForbiddenSyntax(body))
        {
            Report(model, lambda, ProcedureGenerationDiagnostics.InvalidConversion, lambda.ToString());
            return null;
        }

        string parameterName = lambda switch
        {
            SimpleLambdaExpressionSyntax simple => simple.Parameter.Identifier.ValueText,
            ParenthesizedLambdaExpressionSyntax { ParameterList.Parameters: [ParameterSyntax parameter] } => parameter.Identifier.ValueText,
            _ => "value"
        };

        ITypeSymbol? sourceType = semanticModel.GetTypeInfo(body, cancellationToken).ConvertedType
            ?? column.Property.Type;
        if (lambda is SimpleLambdaExpressionSyntax simpleLambda
            && semanticModel.GetDeclaredSymbol(simpleLambda.Parameter, cancellationToken) is IParameterSymbol declared)
        {
            sourceType = declared.Type;
        }
        else if (methodTypeArgument(invocation) is { } typeArgument)
        {
            sourceType = typeArgument;
        }

        string rendered = RewriteConversion(body, parameterName, semanticModel, cancellationToken);
        return new ConversionPlan(sourceType ?? column.Property.Type, rendered);

        static ITypeSymbol? methodTypeArgument(InvocationExpressionSyntax call) =>
            call.Expression is MemberAccessExpressionSyntax { Name: GenericNameSyntax { TypeArgumentList.Arguments: [TypeSyntax typeSyntax] } }
                ? null
                : null;
    }

    private string? ReadRequiredString(
        InvocationExpressionSyntax invocation,
        IMethodSymbol? _,
        ProcedurePlanModel model,
        CancellationToken cancellationToken)
    {
        if (invocation.ArgumentList.Arguments is not [{ Expression: { } argument }])
        {
            Report(model, invocation, ProcedureGenerationDiagnostics.NonConstantArgument, "name", invocation.ToString());
            return null;
        }

        if (!ConstantExpressionFormatter.TryFormat(semanticModel, argument, cancellationToken, out string rendered))
        {
            Report(model, argument, ProcedureGenerationDiagnostics.NonConstantArgument, argument.ToString(), "HasName");
            return null;
        }

        Optional<object?> value = semanticModel.GetConstantValue(argument, cancellationToken);
        return value.Value as string;
    }

    private bool ReadOptionalBoolean(InvocationExpressionSyntax invocation, bool defaultValue, ProcedurePlanModel model, CancellationToken cancellationToken)
    {
        if (invocation.ArgumentList.Arguments.Count is 0)
        {
            return defaultValue;
        }

        ExpressionSyntax argument = invocation.ArgumentList.Arguments[0].Expression;
        Optional<object?> value = semanticModel.GetConstantValue(argument, cancellationToken);
        if (value is { HasValue: true, Value: bool flag })
        {
            return flag;
        }

        Report(model, argument, ProcedureGenerationDiagnostics.NonConstantArgument, argument.ToString(), "IsFunction");
        return defaultValue;
    }

    private int ReadInt32(InvocationExpressionSyntax invocation, ProcedurePlanModel model, CancellationToken cancellationToken)
    {
        if (invocation.ArgumentList.Arguments.Count is 0)
        {
            Report(model, invocation, ProcedureGenerationDiagnostics.NonConstantArgument, "size", "HasSize");
            return 0;
        }

        ExpressionSyntax argument = invocation.ArgumentList.Arguments[0].Expression;
        Optional<object?> value = semanticModel.GetConstantValue(argument, cancellationToken);
        if (value.HasValue && value.Value is IConvertible convertible)
        {
            return convertible.ToInt32(System.Globalization.CultureInfo.InvariantCulture);
        }

        Report(model, argument, ProcedureGenerationDiagnostics.NonConstantArgument, argument.ToString(), "HasSize");
        return 0;
    }

    private ParameterDirection ReadDirection(InvocationExpressionSyntax invocation, ProcedurePlanModel model, CancellationToken cancellationToken)
    {
        if (invocation.ArgumentList.Arguments.Count is 0)
        {
            Report(model, invocation, ProcedureGenerationDiagnostics.NonConstantArgument, "direction", "HasDirection");
            return ParameterDirection.Input;
        }

        ExpressionSyntax argument = invocation.ArgumentList.Arguments[0].Expression;
        Optional<object?> value = semanticModel.GetConstantValue(argument, cancellationToken);
        if (value.HasValue && value.Value is not null)
        {
            return (ParameterDirection)Convert.ToInt32(value.Value);
        }

        Report(model, argument, ProcedureGenerationDiagnostics.NonConstantArgument, argument.ToString(), "HasDirection");
        return ParameterDirection.Input;
    }

    private void Validate(ProcedurePlanModel model)
    {
        if (string.IsNullOrWhiteSpace(model.ProcedureName))
        {
            model.Diagnostics.Add(Diagnostic.Create(
                ProcedureGenerationDiagnostics.MissingProcedureName,
                model.Location,
                model.ProcedureType.Name));
        }

        if (model.Parameters.Count(static parameter => parameter.Direction is ParameterDirection.ReturnValue) > 1)
        {
            model.Diagnostics.Add(Diagnostic.Create(
                ProcedureGenerationDiagnostics.MultipleReturnValues,
                model.Location,
                model.ProcedureType.Name));
        }

        if (model.IsFunction && model.Result is not null)
        {
            model.Diagnostics.Add(Diagnostic.Create(
                ProcedureGenerationDiagnostics.InvalidTopology,
                model.Location,
                "A function cannot have a result set."));
        }

        if (model.Result is not null && model.Parameters.Any(static parameter => parameter.Direction is ParameterDirection.ReturnValue))
        {
            model.Diagnostics.Add(Diagnostic.Create(
                ProcedureGenerationDiagnostics.InvalidTopology,
                model.Location,
                "A procedure cannot have both a ReturnValue parameter and a result set."));
        }

        bool expectsResult = ExtendsStoredProcedureWithResult(model.ProcedureType);
        if (expectsResult && model.Result is null)
        {
            model.Diagnostics.Add(Diagnostic.Create(
                ProcedureGenerationDiagnostics.InvalidTopology,
                model.Location,
                $"Procedure '{model.ProcedureType.Name}' inherits a result-bearing stored-procedure base type but does not declare a result set."));
        }

        foreach (ParameterPlanModel parameter in model.Parameters)
        {
            if (parameter.Scope is null)
            {
                model.Diagnostics.Add(Diagnostic.Create(
                    ProcedureGenerationDiagnostics.MissingGrammar,
                    model.Location,
                    parameter.Property.ContainingType.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat),
                    GrammarScopeKind.Parameter));
            }
            else if (parameter.Scope.Finalizer is null)
            {
                model.Diagnostics.Add(Diagnostic.Create(
                    ProcedureGenerationDiagnostics.MissingInitializerOrFinalizer,
                    model.Location,
                    GrammarScopeKind.Parameter,
                    parameter.Scope.BuilderType.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat),
                    "Finalizer"));
            }

            if (parameter.Direction is ParameterDirection.Output or ParameterDirection.InputOutput or ParameterDirection.ReturnValue
                && parameter.Property.SetMethod is null)
            {
                model.Diagnostics.Add(Diagnostic.Create(
                    ProcedureGenerationDiagnostics.UnwritableOutput,
                    model.Location,
                    parameter.Property.Name));
            }
        }

        if (model.Result is { } result)
        {
            foreach (ColumnPlanModel column in result.Columns)
            {
                if (string.IsNullOrWhiteSpace(column.Name))
                {
                    model.Diagnostics.Add(Diagnostic.Create(
                        ProcedureGenerationDiagnostics.InvalidTopology,
                        model.Location,
                        $"Result column '{column.Property.Name}' does not specify a database name."));
                }

                if (column.Scope?.Finalizer is null)
                {
                    model.Diagnostics.Add(Diagnostic.Create(
                        ProcedureGenerationDiagnostics.MissingInitializerOrFinalizer,
                        model.Location,
                        GrammarScopeKind.Column,
                        column.Scope?.BuilderType.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat) ?? "<unknown>",
                        "Finalizer"));
                }
            }
        }
    }

    private INamedTypeSymbol? ResolveIOContainer(ITypeSymbol builderType)
    {
        if (builderType is INamedTypeSymbol { TypeArguments: [_, INamedTypeSymbol io] })
        {
            return io;
        }

        return builderType.BaseType is { } baseType
            ? ResolveIOContainer(baseType)
            : null;
    }

    private bool ExtendsStoredProcedureWithResult(INamedTypeSymbol procedureType)
    {
        for (INamedTypeSymbol? current = procedureType; current is not null; current = current.BaseType)
        {
            if (SymbolEqualityComparer.Default.Equals(current.OriginalDefinition, contracts.StoredProcedure)
                && current.TypeArguments.Length is 2)
            {
                return true;
            }
        }

        return false;
    }

    private static bool Implements(INamedTypeSymbol type, INamedTypeSymbol marker) =>
        type.AllInterfaces.Any(iface => SymbolEqualityComparer.Default.Equals(iface.OriginalDefinition, marker));

    private static bool IsBuilderOrDelegate(ITypeSymbol type) =>
        type.TypeKind is TypeKind.Delegate
        || type.Name.Contains("Builder", StringComparison.Ordinal);

    private static bool IsPropertySelector(ExpressionSyntax expression) =>
        expression is LambdaExpressionSyntax { Body: MemberAccessExpressionSyntax };

    private static IReadOnlyList<StatementSyntax> GetLambdaStatements(LambdaExpressionSyntax lambda) => lambda.Body switch
    {
        BlockSyntax block => block.Statements,
        _ => []
    };

    private static bool ContainsForbiddenSyntax(SyntaxNode node) => node.DescendantNodesAndSelf().Any(static descendant =>
        descendant is IfStatementSyntax
            or ForStatementSyntax
            or ForEachStatementSyntax
            or WhileStatementSyntax
            or DoStatementSyntax
            or SwitchStatementSyntax
            or TryStatementSyntax
            or AwaitExpressionSyntax
            or AssignmentExpressionSyntax);

    private static string RewriteConversion(
        ExpressionSyntax body,
        string parameterName,
        SemanticModel semanticModel,
        CancellationToken cancellationToken)
    {
        ConversionRewriter rewriter = new(parameterName, semanticModel, cancellationToken);
        return rewriter.Visit(body)!.ToFullString();
    }

    private static void Report(ProcedurePlanModel model, SyntaxNode node, DiagnosticDescriptor descriptor, params object[] args) =>
        model.Diagnostics.Add(Diagnostic.Create(descriptor, node.GetLocation(), args));

    private sealed record AnalysisFrame(
        AnalysisKind Kind,
        ProcedurePlanModel Model,
        ParameterPlanModel? Parameter,
        ResultPlanModel? Result,
        ColumnPlanModel? Column);

    private enum AnalysisKind
    {
        Procedure,
        Parameter,
        Result,
        Column
    }

    private sealed class ConversionRewriter(string parameterName, SemanticModel semanticModel, CancellationToken cancellationToken)
        : CSharpSyntaxRewriter
    {
        public override SyntaxNode? VisitIdentifierName(IdentifierNameSyntax node)
        {
            if (node.Identifier.ValueText == parameterName)
            {
                return SyntaxFactory.IdentifierName("__value");
            }

            if (semanticModel.GetSymbolInfo(node, cancellationToken).Symbol is INamedTypeSymbol type)
            {
                return SyntaxFactory.ParseName(ConstantExpressionFormatter.Qualify(type)).WithTriviaFrom(node);
            }

            return base.VisitIdentifierName(node);
        }

        public override SyntaxNode? VisitQualifiedName(QualifiedNameSyntax node)
        {
            if (semanticModel.GetSymbolInfo(node, cancellationToken).Symbol is INamedTypeSymbol type)
            {
                return SyntaxFactory.ParseName(ConstantExpressionFormatter.Qualify(type)).WithTriviaFrom(node);
            }

            return base.VisitQualifiedName(node);
        }
    }
}
