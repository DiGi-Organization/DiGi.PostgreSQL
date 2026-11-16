using DiGi.Core.IO.Table.Interfaces;
using DiGi.PostgreSQL.Table.Classes;
using System.Collections.Generic;
using System.Text;

namespace DiGi.PostgreSQL.Table
{
    /// <summary>
    /// Static partial class containing query extension methods for database operations.
    /// </summary>
    public static partial class Query
    {
        /// <summary>
        /// Builds the SQL <c>ORDER BY</c> clause from the specified sort conditions, resolving every column through a lookup keyed by unique identifier.
        /// <para>Each condition renders as <c>"column" ASC|DESC NULLS LAST</c>, joined by <c>, </c> in list order; null values sort last in both directions. A condition naming no column is skipped. A caller wanting deterministic ties under equal sort values appends its own tie-break condition (the building-data reads append an ascending <c>reference</c> sort).</para>
        /// <para>The clause is appended straight to <paramref name="stringBuilder_Sql"/>; an identifier cannot be a parameter, so the lookup is the whitelist — a column it does not hold throws instead of reaching the statement.</para>
        /// </summary>
        /// <typeparam name="UColumn">The base column type deriving from <see cref="IColumn"/>.</typeparam>
        /// <param name="sortConditions">The sort conditions to render, in order. May be null or empty.</param>
        /// <param name="dictionary_Columns">The valid whitelisted database columns, keyed by unique identifier.</param>
        /// <param name="stringBuilder_Sql">The string builder to append the resulting SQL clause to.</param>
        /// <returns><c>true</c> when at least one sort condition was rendered; <c>false</c> when <paramref name="sortConditions"/> is null, empty, or names no column.</returns>
        /// <exception cref="InvalidFilterException">A sort condition names a column the stored table does not hold.</exception>
        public static bool TryBuildOrderBySql<UColumn>(this IEnumerable<SortCondition>? sortConditions, Dictionary<string, UColumn> dictionary_Columns, StringBuilder stringBuilder_Sql)
            where UColumn : IColumn
        {
            if (sortConditions is null)
            {
                return false;
            }

            bool result = false;
            foreach (SortCondition? sortCondition in sortConditions)
            {
                if (sortCondition is null || string.IsNullOrWhiteSpace(sortCondition.ColumnUniqueId))
                {
                    continue;
                }

                string columnUniqueId = sortCondition.ColumnUniqueId;

                if (!dictionary_Columns.TryGetValue(columnUniqueId, out UColumn? column) || column is null)
                {
                    throw new InvalidFilterException(columnUniqueId);
                }

                if (result)
                {
                    stringBuilder_Sql.Append(", ");
                }

                stringBuilder_Sql.Append('"').Append(columnUniqueId).Append(sortCondition.Descending ? "\" DESC NULLS LAST" : "\" ASC NULLS LAST");

                result = true;
            }

            return result;
        }
    }
}
