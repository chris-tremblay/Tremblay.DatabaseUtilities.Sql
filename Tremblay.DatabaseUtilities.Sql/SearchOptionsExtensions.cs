using System.Collections;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using System.Net.Sockets;
using System.Reflection;
using System.Text;
using Tremblay.DatabaseUtilities.Sql.Attributes;
using Tremblay.DatabaseUtilities.Sql.Extensions;

namespace Tremblay.DatabaseUtilities.Sql
{
    public static class SearchOptionsExtensions
    {


        public static string GenerateSearchString(this ISearchOptions searchOptions, IList<object> parameters)
        {
            var sb = new StringBuilder();
            var type = searchOptions.GetType();

            /***************************************************************************************
             * Create the Select statement. Determine the table name and schema.                   *
             **************************************************************************************/
            var tableAttribute = type.GetCustomAttribute(typeof(TableAttribute)) as TableAttribute;
            var schema = tableAttribute?.Schema ?? "dbo";
            var tableName = tableAttribute?.Name ?? searchOptions.GetType().Name.Replace("SearchOptions", "");
            
            sb.Append($"SELECT * FROM {schema}.{tableName} WHERE ");

            /***************************************************************************************
             * Add the WHERE statements.                                                           *
             **************************************************************************************/
            foreach (var property in type.GetProperties())
            {
                if (!property.CanRead)
                    continue;

                //Ignore the current property if necessary.
                var ignoreAttribute = property.GetCustomAttribute(typeof(IgnoreAttribute));

                if (ignoreAttribute != null)
                    continue;

                //Determine the column name.
                var columnAttribute = (Attributes.ColumnAttribute)property.GetCustomAttribute(typeof(Attributes.ColumnAttribute));
                var columnName = columnAttribute != null
                    ? columnAttribute.Name
                    : property.Name;

                var equalityMethod = columnAttribute != null
                    ? columnAttribute.EqualityMethod
                    : "=";

                if (property.GetValue(searchOptions) != null)
                {
                    var val = property.GetValue(searchOptions);
                    if (val is IEnumerable list)
                    {
                        switch (list.Count())
                        {
                            case 0:
                                sb.Append("1=0");
                                break;
                            case 1:
                                sb.Append($"{columnName} = {{{parameters.Count}}}");
                                parameters.Add(list.FirstOrDefault());
                                break;
                            default:
                                sb.Append($"{columnName} IN ({{{parameters.Count}}})");
                                parameters.Add(list);
                                break;
                        }
                    }
                    else if (property.GetValue(searchOptions) is string str)
                    {
                        if (!string.IsNullOrEmpty(str))
                        {
                            switch (equalityMethod)
                            {
                                case "LIKE":
                                    sb.Append($"{columnName} LIKE {{{parameters.Count}}}");
                                    parameters.Add($"%{str}%");
                                    break;
                                case "=":
                                    sb.Append($"{columnName} = {{{parameters.Count}}}");
                                    parameters.Add(str);
                                    break;
                            }
                        }
                    }
                    else
                    {
                        sb.Append($"{columnName} {equalityMethod} {{{parameters.Count}}}");
                        parameters.Add(property.GetValue(searchOptions));
                    }


                    sb.Append(" AND ");
                }
            }

            sb.Length -= sb.ToString().EndsWith(" WHERE ")
                ? 7
                : 5;

            return sb.ToString();
        }



    }
}
