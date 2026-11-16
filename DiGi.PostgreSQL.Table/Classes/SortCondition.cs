using System.ComponentModel.DataAnnotations;

namespace DiGi.PostgreSQL.Table.Classes
{
    /// <summary>
    /// Represents a single sort instruction for an ordered read: a database column and its direction.
    /// </summary>
    public class SortCondition
    {
        /// <summary>
        /// Gets or sets the unique identifier of the column to sort by.
        /// <para>Must name a column present on the stored table (obtainable from the building-data <c>columnuniqueids</c> endpoint); a column the table does not hold throws <see cref="InvalidFilterException"/>.</para>
        /// </summary>
        [Required]
        public string? ColumnUniqueId { get; set; }

        /// <summary>
        /// Gets or sets the sort direction: <c>true</c> sorts descending, <c>false</c> ascending.
        /// <para>Null values sort last in both directions (<c>NULLS LAST</c>), overriding PostgreSQL's default of nulls first when descending.</para>
        /// </summary>
        public bool Descending { get; set; }
    }
}
