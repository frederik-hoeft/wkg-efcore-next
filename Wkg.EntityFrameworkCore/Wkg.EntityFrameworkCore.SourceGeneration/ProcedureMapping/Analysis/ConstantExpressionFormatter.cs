using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using System.Globalization;

namespace Wkg.EntityFrameworkCore.SourceGeneration.ProcedureMapping.Analysis;

internal static class ConstantExpressionFormatter
{
    public static bool TryFormat(
        SemanticModel semanticModel,
        ExpressionSyntax expression,
        CancellationToken cancellationToken,
        out string rendered)
    {
        expression = Unwrap(expression);
        if (expression is TypeOfExpressionSyntax typeOfExpression
            && semanticModel.GetTypeInfo(typeOfExpression.Type, cancellationToken).Type is { } typeArgument)
        {
            rendered = $"typeof({Qualify(typeArgument)})";
            return true;
        }

        if (expression is DefaultExpressionSyntax defaultExpression
            && semanticModel.GetTypeInfo(defaultExpression.Type, cancellationToken).Type is { } defaultType)
        {
            rendered = $"default({Qualify(defaultType)})";
            return true;
        }

        if (expression is LiteralExpressionSyntax { RawKind: (int)SyntaxKind.DefaultLiteralExpression })
        {
            rendered = "default";
            return true;
        }

        Optional<object?> constant = semanticModel.GetConstantValue(expression, cancellationToken);
        if (!constant.HasValue)
        {
            rendered = string.Empty;
            return false;
        }

        ITypeSymbol? expressionType = semanticModel.GetTypeInfo(expression, cancellationToken).Type
            ?? semanticModel.GetTypeInfo(expression, cancellationToken).ConvertedType;
        rendered = FormatValue(expressionType, constant.Value);
        return true;
    }

    public static string FormatValue(ITypeSymbol? type, object? value)
    {
        if (value is null)
        {
            return "null";
        }

        if (type is INamedTypeSymbol { TypeKind: TypeKind.Enum } enumType)
        {
            foreach (IFieldSymbol field in enumType.GetMembers().OfType<IFieldSymbol>())
            {
                if (field.HasConstantValue && Equals(field.ConstantValue, value))
                {
                    return $"{Qualify(enumType)}.{field.Name}";
                }
            }

            return $"({Qualify(enumType)})({Convert.ToInt64(value, CultureInfo.InvariantCulture)})";
        }

        return value switch
        {
            string text => SymbolDisplay.FormatLiteral(text, quote: true),
            char character => SymbolDisplay.FormatLiteral(character, quote: true),
            bool flag => flag ? "true" : "false",
            byte number => number.ToString(CultureInfo.InvariantCulture),
            sbyte number => number.ToString(CultureInfo.InvariantCulture),
            short number => number.ToString(CultureInfo.InvariantCulture),
            ushort number => number.ToString(CultureInfo.InvariantCulture),
            int number => number.ToString(CultureInfo.InvariantCulture),
            uint number => number.ToString(CultureInfo.InvariantCulture) + "u",
            long number => number.ToString(CultureInfo.InvariantCulture) + "L",
            ulong number => number.ToString(CultureInfo.InvariantCulture) + "uL",
            float number => number.ToString("R", CultureInfo.InvariantCulture) + "f",
            double number => number.ToString("R", CultureInfo.InvariantCulture) + "d",
            decimal number => number.ToString(CultureInfo.InvariantCulture) + "m",
            _ => value.ToString() ?? "null"
        };
    }

    public static string Qualify(ISymbol symbol) =>
        symbol.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat);

    private static ExpressionSyntax Unwrap(ExpressionSyntax expression)
    {
        while (expression is ParenthesizedExpressionSyntax parenthesized)
        {
            expression = parenthesized.Expression;
        }

        return expression;
    }
}
