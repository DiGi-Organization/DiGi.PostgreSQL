using DiGi.Core.IO.Table.Interfaces;
using DiGi.PostgreSQL.Table.Classes;
using DiGi.PostgreSQL.Table.Enums;
using Npgsql;
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
        /// Recursively builds the SQL query condition and parameters from the specified <see cref="FilterGroup"/>.
        /// <para>Keys <paramref name="existingColumns"/> by unique identifier once and delegates to the overload taking that lookup. A caller that builds more than one statement from the same columns keys them itself and calls that overload directly.</para>
        /// </summary>
        /// <typeparam name="UColumn">The base column type deriving from <see cref="IColumn"/>.</typeparam>
        /// <param name="filterGroup">The filter group instance to build SQL for.</param>
        /// <param name="existingColumns">The list of valid whitelisted database columns.</param>
        /// <param name="stringBuilder_Sql">The string builder to append the resulting SQL condition to.</param>
        /// <param name="npgsqlParameterCollection">The parameter collection to bind Npgsql parameters to.</param>
        /// <param name="parameterIndex">A reference counter for unique query parameter names.</param>
        /// <returns><c>true</c> when the SQL condition was built; <c>false</c> only when a whitelisted column's stored type cannot be resolved to an Npgsql data type.</returns>
        /// <exception cref="InvalidFilterException">A filter names a column the stored table does not hold, or carries a value that cannot be converted to the column's type.</exception>
        public static bool TryBuildFilterGroupSql<UColumn>(this FilterGroup? filterGroup, List<UColumn> existingColumns, StringBuilder stringBuilder_Sql, NpgsqlParameterCollection npgsqlParameterCollection, ref int parameterIndex)
            where UColumn : IColumn
        {
            if (filterGroup is null)
            {
                return true;
            }

            // The first column carrying a unique identifier wins, as the linear search this replaced did.
            Dictionary<string, UColumn> dictionary_Columns = new(existingColumns.Count);
            foreach (UColumn column in existingColumns)
            {
                if (column.UniqueId() is string uniqueId)
                {
                    dictionary_Columns.TryAdd(uniqueId, column);
                }
            }

            return filterGroup.TryBuildFilterGroupSql(dictionary_Columns, stringBuilder_Sql, npgsqlParameterCollection, ref parameterIndex);
        }

        /// <summary>
        /// Recursively builds the SQL query condition and parameters from the specified <see cref="FilterGroup"/>, resolving every filter column through a lookup keyed by unique identifier.
        /// <para>The condition is appended straight to <paramref name="stringBuilder_Sql"/>; when the build answers <c>false</c> the builder is restored to its length on entry.</para>
        /// </summary>
        /// <typeparam name="UColumn">The base column type deriving from <see cref="IColumn"/>.</typeparam>
        /// <param name="filterGroup">The filter group instance to build SQL for.</param>
        /// <param name="dictionary_Columns">The valid whitelisted database columns, keyed by unique identifier.</param>
        /// <param name="stringBuilder_Sql">The string builder to append the resulting SQL condition to.</param>
        /// <param name="npgsqlParameterCollection">The parameter collection to bind Npgsql parameters to.</param>
        /// <param name="parameterIndex">A reference counter for unique query parameter names.</param>
        /// <returns><c>true</c> when the SQL condition was built; <c>false</c> only when a whitelisted column's stored type cannot be resolved to an Npgsql data type.</returns>
        /// <exception cref="InvalidFilterException">A filter names a column the stored table does not hold, or carries a value that cannot be converted to the column's type.</exception>
        public static bool TryBuildFilterGroupSql<UColumn>(this FilterGroup? filterGroup, Dictionary<string, UColumn> dictionary_Columns, StringBuilder stringBuilder_Sql, NpgsqlParameterCollection npgsqlParameterCollection, ref int parameterIndex)
            where UColumn : IColumn
        {
            if (filterGroup is null)
            {
                return true;
            }

            // Conditions are appended in place and counted; the surrounding parentheses are only known to be needed
            // once the second one has been written, so the opening one is inserted at the end.
            int start = stringBuilder_Sql.Length;
            int count = 0;
            string logicalOperator = filterGroup.LogicalOperator == FilterLogicalOperator.Or ? " OR " : " AND ";

            if (filterGroup.FilterConditions is not null)
            {
                foreach (FilterCondition filterCondition in filterGroup.FilterConditions)
                {
                    if (filterCondition is null || string.IsNullOrWhiteSpace(filterCondition.ColumnUniqueId))
                    {
                        continue;
                    }

                    string columnUniqueId = filterCondition.ColumnUniqueId;

                    if (!dictionary_Columns.TryGetValue(columnUniqueId, out UColumn? column) || column is null)
                    {
                        throw new InvalidFilterException(columnUniqueId, filterCondition.FilterOperator);
                    }

                    if (column.NpgsqlDbType() is not NpgsqlTypes.NpgsqlDbType npgsqlDbType)
                    {
                        stringBuilder_Sql.Length = start;
                        return false;
                    }

                    // Every branch below writes exactly one condition or throws, so the separator goes in first.
                    if (count > 0)
                    {
                        stringBuilder_Sql.Append(logicalOperator);
                    }

                    count++;

                    switch (filterCondition.FilterOperator)
                    {
                        case FilterOperator.IsNull:
                            stringBuilder_Sql.Append('"').Append(columnUniqueId).Append("\" IS NULL");
                            break;

                        case FilterOperator.IsNotNull:
                            stringBuilder_Sql.Append('"').Append(columnUniqueId).Append("\" IS NOT NULL");
                            break;

                        case FilterOperator.Contains:
                            if (filterCondition.Value is not null)
                            {
                                if (!column.TryGetValidValue(filterCondition.Value, out object? validValue_Contains))
                                {
                                    throw new InvalidFilterException(columnUniqueId, FilterOperator.Contains, filterCondition.Value);
                                }
                                else if (validValue_Contains is not null)
                                {
                                    string paramName = $"filterParam_{parameterIndex}";
                                    parameterIndex++;
                                    stringBuilder_Sql.Append('"').Append(columnUniqueId).Append("\" ILIKE @").Append(paramName);
                                    npgsqlParameterCollection.Add(new NpgsqlParameter(paramName, npgsqlDbType) { Value = $"%{validValue_Contains}%" });
                                }
                                else
                                {
                                    stringBuilder_Sql.Append("1=0");
                                }
                            }
                            else
                            {
                                stringBuilder_Sql.Append("1=0");
                            }
                            break;

                        case FilterOperator.In:
                        case FilterOperator.NotIn:
                            System.Collections.IEnumerable? enumerable = null;

                            if (filterCondition.Value is System.Collections.IEnumerable enumerable_Collection && filterCondition.Value is not string)
                            {
                                enumerable = enumerable_Collection;
                            }
                            else if (filterCondition.Value is System.Text.Json.JsonElement jsonElement && jsonElement.ValueKind == System.Text.Json.JsonValueKind.Array)
                            {
                                enumerable = jsonElement.EnumerateArray();
                            }

                            if (enumerable is not null)
                            {
                                List<object> validValues = [];
                                foreach (object item in enumerable)
                                {
                                    bool itemIsNull = item is null
                                        || (item is System.Text.Json.JsonElement itemJson && itemJson.ValueKind == System.Text.Json.JsonValueKind.Null);
                                    if (itemIsNull)
                                    {
                                        continue;
                                    }
                                    if (!column.TryGetValidValue(item, out object? validItem))
                                    {
                                        throw new InvalidFilterException(columnUniqueId, filterCondition.FilterOperator, item);
                                    }
                                    if (validItem is not null)
                                    {
                                        validValues.Add(validItem.DatabaseValue()!);
                                    }
                                }

                                if (validValues.Count == 0)
                                {
                                    stringBuilder_Sql.Append(filterCondition.FilterOperator == FilterOperator.In ? "1=0" : "1=1");
                                }
                                else if (validValues.Count == 1)
                                {
                                    string paramName = $"filterParam_{parameterIndex}";
                                    parameterIndex++;
                                    string opSymbol = filterCondition.FilterOperator == FilterOperator.In ? "=" : "!=";

                                    stringBuilder_Sql.Append('"').Append(columnUniqueId).Append("\" ").Append(opSymbol).Append(" @").Append(paramName);
                                    npgsqlParameterCollection.Add(new NpgsqlParameter(paramName, npgsqlDbType) { Value = validValues[0] });
                                }
                                else
                                {
                                    string paramName = $"filterParam_{parameterIndex}";
                                    parameterIndex++;
                                    string arrayFunc = filterCondition.FilterOperator == FilterOperator.In ? "= ANY" : "!= ALL";

                                    stringBuilder_Sql.Append('"').Append(columnUniqueId).Append("\" ").Append(arrayFunc).Append("(@").Append(paramName).Append(')');
                                    npgsqlParameterCollection.Add(new NpgsqlParameter(paramName, NpgsqlTypes.NpgsqlDbType.Array | npgsqlDbType) { Value = validValues.ToArray() });
                                }
                            }
                            else
                            {
                                if (filterCondition.Value is System.Text.Json.JsonElement jsonElement_Scalar &&
                                    jsonElement_Scalar.ValueKind is System.Text.Json.JsonValueKind.Object or System.Text.Json.JsonValueKind.Undefined)
                                {
                                    throw new InvalidFilterException(columnUniqueId, filterCondition.FilterOperator, filterCondition.Value);
                                }
                                else if (column.TryGetValidValue(filterCondition.Value, out object? validValue) && validValue is not null)
                                {
                                    string paramName = $"filterParam_{parameterIndex}";
                                    parameterIndex++;
                                    string opSymbol = filterCondition.FilterOperator == FilterOperator.In ? "=" : "!=";

                                    stringBuilder_Sql.Append('"').Append(columnUniqueId).Append("\" ").Append(opSymbol).Append(" @").Append(paramName);
                                    npgsqlParameterCollection.Add(new NpgsqlParameter(paramName, npgsqlDbType) { Value = validValue.DatabaseValue() });
                                }
                                else if (filterCondition.Value is null)
                                {
                                    stringBuilder_Sql.Append(filterCondition.FilterOperator == FilterOperator.In ? "1=0" : "1=1");
                                }
                                else
                                {
                                    throw new InvalidFilterException(columnUniqueId, filterCondition.FilterOperator, filterCondition.Value);
                                }
                            }
                            break;

                        default: // Equals, NotEquals, GreaterThan, GreaterThanOrEqual, LessThan, LessThanOrEqual
                            if (column.TryGetValidValue(filterCondition.Value, out object? validValue_Comp))
                            {
                                if (validValue_Comp is not null && validValue_Comp != System.DBNull.Value)
                                {
                                    string paramName = $"filterParam_{parameterIndex}";
                                    parameterIndex++;
                                    string opSymbol = filterCondition.FilterOperator switch
                                    {
                                        FilterOperator.Equals => "=",
                                        FilterOperator.NotEquals => "!=",
                                        FilterOperator.GreaterThan => ">",
                                        FilterOperator.GreaterThanOrEqual => ">=",
                                        FilterOperator.LessThan => "<",
                                        FilterOperator.LessThanOrEqual => "<=",
                                        _ => "="
                                    };

                                    stringBuilder_Sql.Append('"').Append(columnUniqueId).Append("\" ").Append(opSymbol).Append(" @").Append(paramName);
                                    npgsqlParameterCollection.Add(new NpgsqlParameter(paramName, npgsqlDbType) { Value = validValue_Comp.DatabaseValue() });
                                }
                                else
                                {
                                    if (filterCondition.FilterOperator == FilterOperator.Equals)
                                    {
                                        stringBuilder_Sql.Append('"').Append(columnUniqueId).Append("\" IS NULL");
                                    }
                                    else if (filterCondition.FilterOperator == FilterOperator.NotEquals)
                                    {
                                        stringBuilder_Sql.Append('"').Append(columnUniqueId).Append("\" IS NOT NULL");
                                    }
                                    else
                                    {
                                        stringBuilder_Sql.Append("1=0");
                                    }
                                }
                            }
                            else if (filterCondition.Value is null)
                            {
                                stringBuilder_Sql.Append("1=0");
                            }
                            else
                            {
                                throw new InvalidFilterException(columnUniqueId, filterCondition.FilterOperator, filterCondition.Value);
                            }
                            break;
                    }
                }
            }

            if (filterGroup.FilterGroups is not null)
            {
                foreach (FilterGroup filterGroup_Child in filterGroup.FilterGroups)
                {
                    if (filterGroup_Child is null)
                    {
                        continue;
                    }

                    int length = stringBuilder_Sql.Length;
                    if (count > 0)
                    {
                        stringBuilder_Sql.Append(logicalOperator);
                    }

                    int length_Child = stringBuilder_Sql.Length;
                    if (!filterGroup_Child.TryBuildFilterGroupSql(dictionary_Columns, stringBuilder_Sql, npgsqlParameterCollection, ref parameterIndex))
                    {
                        stringBuilder_Sql.Length = start;
                        return false;
                    }

                    // A child that wrote nothing is not a condition: the separator written for it is taken back.
                    if (stringBuilder_Sql.Length == length_Child)
                    {
                        stringBuilder_Sql.Length = length;
                        continue;
                    }

                    count++;
                }
            }

            if (count > 1)
            {
                stringBuilder_Sql.Insert(start, '(').Append(')');
            }

            return true;
        }
    }
}
