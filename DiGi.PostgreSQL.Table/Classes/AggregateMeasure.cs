namespace DiGi.PostgreSQL.Table.Classes
{
    /// <summary>
    /// One measure of a multi-measure aggregate request: the column to aggregate and the single-value aggregate function to apply to it.
    /// </summary>
    /// <param name="ColumnUniqueId">The unique identifier of the column to aggregate.</param>
    /// <param name="SinglevalueAggregateFunction">The single-value aggregate function to apply to the column.</param>
    public readonly record struct AggregateMeasure(string ColumnUniqueId, Enums.SinglevalueAggregateFunction SinglevalueAggregateFunction)
    {
    }
}
