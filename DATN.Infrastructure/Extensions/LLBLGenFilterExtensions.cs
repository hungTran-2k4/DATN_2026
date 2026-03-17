using System;
using System.Linq;
using System.Text.Json;
using DATN.Domain.Common.Models;
using SD.LLBLGen.Pro.ORMSupportClasses;
using SD.LLBLGen.Pro.QuerySpec;

namespace DATN.Infrastructure.Extensions;

public static class LLBLGenFilterExtensions
{
    public static IPredicateExpression ToPredicateExpression(this FilterDescriptor? filter, IEntityFieldsCore fields)
    {
        var predicateExpression = new PredicateExpression();

        if (filter == null)
            return predicateExpression;

        var predicate = BuildPredicate(filter, fields);
        if (predicate != null)
        {
            predicateExpression.Add(predicate);
        }

        return predicateExpression;
    }

    private static IPredicate? BuildPredicate(FilterDescriptor filter, IEntityFieldsCore fields)
    {
        // Composite filter
        if (filter.Filters != null && filter.Filters.Any())
        {
            var expression = new PredicateExpression();
            bool isOr = string.Equals(filter.Logic, "or", StringComparison.OrdinalIgnoreCase);

            foreach (var childFilter in filter.Filters)
            {
                var childPredicate = BuildPredicate(childFilter, fields);
                if (childPredicate != null)
                {
                    if (isOr)
                    {
                        expression.AddWithOr(childPredicate);
                    }
                    else
                    {
                        expression.AddWithAnd(childPredicate);
                    }
                }
            }

            return expression.Count > 0 ? expression : null;
        }

        // Single filter
        if (!string.IsNullOrWhiteSpace(filter.Field) && !string.IsNullOrWhiteSpace(filter.Operator))
        {
            // Find field case-insensitively
            var field = fields.FirstOrDefault(f => string.Equals(f.Name, filter.Field, StringComparison.OrdinalIgnoreCase));
            if (field == null)
            {
                // Optionally log that the field was not found
                return null;
            }

            // Convert value to correct type
            object? parsedValue = ParseValue(filter.Value, field.DataType);

            return BuildFieldPredicate(field, filter.Operator, parsedValue);
        }

        return null;
    }

    private static IPredicate? BuildFieldPredicate(IEntityFieldCore field, string op, object? value)
    {
        switch (op.ToLowerInvariant())
        {
            case "eq":
                return value == null ? field.IsNull() : field.Equal(value);
            case "neq":
                return value == null ? field.IsNotNull() : field.NotEqual(value);
            case "isnull":
                return field.IsNull();
            case "isnotnull":
                return field.IsNotNull();
            case "lt":
                return field.LesserThan(value);
            case "lte":
                return field.LesserEqual(value);
            case "gt":
                return field.GreaterThan(value);
            case "gte":
                return field.GreaterEqual(value);
            case "startswith":
                return value != null ? field.StartsWith(value.ToString()) : null;
            case "endswith":
                return value != null ? field.EndsWith(value.ToString()) : null;
            case "contains":
                return value != null ? field.Contains(value.ToString()) : null;
            case "doesnotcontain":
                if (value == null) return null;
                var notContains = field.Contains(value.ToString());
                notContains.Negate = true;
                return notContains;
            case "isempty":
                return field.Equal(string.Empty);
            case "isnotempty":
                return field.NotEqual(string.Empty);
            default:
                return null;
        }
    }

    private static object? ParseValue(object? value, Type targetType)
    {
        if (value == null) return null;

        try
        {
            var underlyingType = Nullable.GetUnderlyingType(targetType) ?? targetType;

            if (value is JsonElement element)
            {
                switch (element.ValueKind)
                {
                    case JsonValueKind.String:
                        var strVal = element.GetString();
                        if (underlyingType == typeof(Guid) && Guid.TryParse(strVal, out var guidGuid))
                            return guidGuid;
                        if (underlyingType == typeof(DateTime) && DateTime.TryParse(strVal, out var dtDate))
                            return dtDate;
                        if (underlyingType == typeof(DateTimeOffset) && DateTimeOffset.TryParse(strVal, out var dtOffset))
                            return dtOffset;
                        if (underlyingType == typeof(TimeSpan) && TimeSpan.TryParse(strVal, out var tsVal))
                            return tsVal;
                        return Convert.ChangeType(strVal, underlyingType);
                        
                    case JsonValueKind.Number:
                        var rawText = element.GetRawText();
                        if (underlyingType == typeof(int)) return element.GetInt32();
                        if (underlyingType == typeof(long)) return element.GetInt64();
                        if (underlyingType == typeof(double)) return element.GetDouble();
                        if (underlyingType == typeof(decimal)) return element.GetDecimal();
                        if (underlyingType == typeof(short)) return element.GetInt16();
                        if (underlyingType == typeof(float)) return element.GetSingle();
                        return Convert.ChangeType(rawText, underlyingType);
                        
                    case JsonValueKind.True:
                        return true;
                    case JsonValueKind.False:
                        return false;
                    case JsonValueKind.Null:
                        return null;
                }
            }

            // Fallback for non-JsonElement objects (e.g., already deserialized directly or simple types)
            if (underlyingType == typeof(Guid) && value is string strGuid && Guid.TryParse(strGuid, out var parsedGuid))
            {
                return parsedGuid;
            }

            return Convert.ChangeType(value, underlyingType);
        }
        catch
        {
            // If conversion fails, return the original value and let LLBLGen handle or throw
            return value;
        }
    }
}
