using DiGi.PostgreSQL.Table.Enums;
using System;

namespace DiGi.PostgreSQL.Table.Classes
{
    /// <summary>
    /// Thrown when a filtered read names a column the stored table does not hold, or supplies a filter value that
    /// cannot be converted to the column's type. A request-level failure a caller can catch and report, distinct
    /// from a <c>null</c> result (no connection, or a valid filter that matched nothing).
    /// </summary>
    public class InvalidFilterException : Exception
    {
        /// <summary>
        /// Gets the unique identifier of the offending column — the target column, or a filter condition's column.
        /// </summary>
        public string ColumnUniqueId { get; }

        /// <summary>
        /// Gets the filter operator when the failure is a filter condition; otherwise <c>null</c> (unknown target column).
        /// </summary>
        public FilterOperator? FilterOperator { get; }

        /// <summary>
        /// Gets the offending filter value when one could not be converted to the column's type; otherwise <c>null</c>.
        /// </summary>
        public object? Value { get; }

        /// <summary>
        /// Initializes a new instance for a target column that is not on the stored column list.
        /// </summary>
        /// <param name="columnUniqueId">The unique identifier of the column that the stored table does not hold.</param>
        public InvalidFilterException(string columnUniqueId)
            : base($"Invalid filter: column '{columnUniqueId}' is not on the stored column list.")
        {
            ColumnUniqueId = columnUniqueId;
        }

        /// <summary>
        /// Initializes a new instance for a filter condition that names an unknown column or carries an unconvertible value.
        /// </summary>
        /// <param name="columnUniqueId">The unique identifier of the offending filter column.</param>
        /// <param name="filterOperator">The filter operator of the offending condition.</param>
        /// <param name="value">The value that could not be converted to the column's type, when there is one; otherwise <c>null</c>.</param>
        public InvalidFilterException(string columnUniqueId, FilterOperator filterOperator, object? value = null)
            : base(value is null
                ? $"Invalid filter: column '{columnUniqueId}' is not on the stored column list (operator {filterOperator})."
                : $"Invalid filter: value '{value}' cannot be applied to column '{columnUniqueId}' (operator {filterOperator}).")
        {
            ColumnUniqueId = columnUniqueId;
            FilterOperator = filterOperator;
            Value = value;
        }
    }
}
