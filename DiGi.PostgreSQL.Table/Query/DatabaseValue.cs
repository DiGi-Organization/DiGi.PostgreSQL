namespace DiGi.PostgreSQL.Table
{
    public static partial class Query
    {
        /// <summary>
        /// Widens an unsigned integer value to the signed type its column is mapped to in the database, because Npgsql does not write unsigned CLR types.
        /// <para><see cref="ushort"/> becomes <see cref="int"/>, <see cref="uint"/> becomes <see cref="long"/> and <see cref="ulong"/> becomes <see cref="decimal"/>, matching <see cref="NpgsqlDbType(System.Type?)"/>.</para>
        /// </summary>
        /// <param name="value">The value returned by the column's value conversion.</param>
        /// <returns>The widened value, or <paramref name="value"/> unchanged when it is not an unsigned integer (including <see langword="null"/>).</returns>
        public static object? DatabaseValue(this object? value)
        {
            return value switch
            {
                ushort ushort_Value => (int)ushort_Value,
                uint uint_Value => (long)uint_Value,
                ulong ulong_Value => (decimal)ulong_Value,
                _ => value
            };
        }
    }
}
