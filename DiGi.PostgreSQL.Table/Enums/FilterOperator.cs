namespace DiGi.PostgreSQL.Table.Enums
{
    /// <summary>
    /// Specifies the comparison operator to be applied to a database column filter.
    /// </summary>
    public enum FilterOperator
    {
        /// <summary>
        /// Checks if the column value is equal to the filter value. Applicable to both text and numeric columns.
        /// <para>Takes a scalar value; a <c>null</c> value matches <c>IS NULL</c>.</para>
        /// </summary>
        Equals,

        /// <summary>
        /// Checks if the column value is not equal to the filter value. Applicable to both text and numeric columns.
        /// <para>Takes a scalar value; a <c>null</c> value matches <c>IS NOT NULL</c>.</para>
        /// </summary>
        NotEquals,

        /// <summary>
        /// Checks if the column value is greater than the filter value. Applicable to numeric columns only.
        /// <para>Takes a scalar value.</para>
        /// </summary>
        GreaterThan,

        /// <summary>
        /// Checks if the column value is greater than or equal to the filter value. Applicable to numeric columns only.
        /// <para>Takes a scalar value.</para>
        /// </summary>
        GreaterThanOrEqual,

        /// <summary>
        /// Checks if the column value is less than the filter value. Applicable to numeric columns only.
        /// <para>Takes a scalar value.</para>
        /// </summary>
        LessThan,

        /// <summary>
        /// Checks if the column value is less than or equal to the filter value. Applicable to numeric columns only.
        /// <para>Takes a scalar value.</para>
        /// </summary>
        LessThanOrEqual,

        /// <summary>
        /// Checks if the column value matches any of the values in the specified collection parameter. Applicable to both text and numeric columns.
        /// <para>Takes a JSON array of values, or a single scalar treated as a one-element list; <c>null</c> items are skipped and an empty list matches nothing.</para>
        /// </summary>
        In,

        /// <summary>
        /// Checks if the column value does not match any of the values in the specified collection parameter. Applicable to both text and numeric columns.
        /// <para>Takes a JSON array of values, or a single scalar treated as a one-element list; <c>null</c> items are skipped and an empty list matches everything.</para>
        /// </summary>
        NotIn,

        /// <summary>
        /// Checks if the column value contains the filter string value as a substring (case-insensitive search). Applicable to text and string columns only.
        /// <para>Takes a string, matched as a case-insensitive substring (<c>ILIKE</c>, wrapped in <c>%</c> server-side).</para>
        /// </summary>
        Contains,

        /// <summary>
        /// Checks if the column value is null. Applicable to all column types.
        /// <para>The filter value is ignored.</para>
        /// </summary>
        IsNull,

        /// <summary>
        /// Checks if the column value is not null. Applicable to all column types.
        /// <para>The filter value is ignored.</para>
        /// </summary>
        IsNotNull
    }
}