using System.Collections.Generic;

namespace DATN.Domain.Common.Models;

/// <summary>
/// Represents a Kendo UI DataSource filter descriptor.
/// Can be a single filter (Field, Operator, Value) or a composite filter (Logic, Filters).
/// </summary>
public class FilterDescriptor
{
    /// <summary>
    /// The logic operator (e.g., "and", "or"). Used for composite filters.
    /// </summary>
    public string? Logic { get; set; }

    /// <summary>
    /// The collection of child filters. Used for composite filters.
    /// </summary>
    public IEnumerable<FilterDescriptor>? Filters { get; set; }

    /// <summary>
    /// The field name to filter on. Used for single filters.
    /// </summary>
    public string? Field { get; set; }

    /// <summary>
    /// The comparison operator (e.g., "eq", "neq", "contains", "startswith", "endswith", "gte", "gt", "lte", "lt").
    /// </summary>
    public string? Operator { get; set; }

    /// <summary>
    /// The value to compare the field against.
    /// </summary>
    public object? Value { get; set; }
}
