using DiGi.PostgreSQL.Table.Enums;
using System.ComponentModel.DataAnnotations;

namespace DiGi.PostgreSQL.Table.Classes
{
    /// <summary>
    /// Represents a single comparison filter condition on a database column.
    /// </summary>
    public class FilterCondition
    {
        /// <summary>
        /// Gets or sets the unique identifier of the column to filter.
        /// <para>Must name a column present on the stored table (obtainable from the building-data <c>columnuniqueids</c> endpoint); a column the table does not hold throws <see cref="InvalidFilterException"/>.</para>
        /// </summary>
        [Required]
        public string? ColumnUniqueId { get; set; }

        /// <summary>
        /// Gets or sets the comparison operator to apply.
        /// </summary>
        [Required]
        public FilterOperator FilterOperator { get; set; }

        /// <summary>
        /// Gets or sets the value to compare against; the accepted JSON shape depends on <see cref="FilterOperator"/>.
        /// <para>Comparison operators (<see cref="FilterOperator.Equals"/>, <see cref="FilterOperator.NotEquals"/>, <see cref="FilterOperator.GreaterThan"/>, <see cref="FilterOperator.GreaterThanOrEqual"/>, <see cref="FilterOperator.LessThan"/>, <see cref="FilterOperator.LessThanOrEqual"/>) take a scalar convertible to the column's type. <see cref="FilterOperator.In"/> and <see cref="FilterOperator.NotIn"/> take a JSON array of values, or a single scalar treated as a one-element list. <see cref="FilterOperator.Contains"/> takes a string matched as a case-insensitive substring (<c>ILIKE</c>, wrapped in <c>%</c> server-side). <see cref="FilterOperator.IsNull"/> and <see cref="FilterOperator.IsNotNull"/> ignore this value.</para>
        /// <para>A value that cannot be converted to the column's type throws <see cref="InvalidFilterException"/>. A <c>null</c> value and an empty <see cref="FilterOperator.In"/>/<see cref="FilterOperator.NotIn"/> list are handled without an error: <see cref="FilterOperator.Equals"/> becomes <c>IS NULL</c>, <see cref="FilterOperator.NotEquals"/> becomes <c>IS NOT NULL</c>, <see cref="FilterOperator.NotIn"/> becomes <c>1=1</c> (matches everything), and every other operator degrades to <c>1=0</c> (no match).</para>
        /// </summary>
        public object? Value { get; set; }
    }
}