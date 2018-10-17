using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations.Schema;
using System.Reflection;
using System.Text;
using Tremblay.DatabaseUtilities.Sql.Attributes;
using ColumnAttribute = Tremblay.DatabaseUtilities.Sql.Attributes.ColumnAttribute;

namespace Tremblay.DatabaseUtilities.Sql
{
    public static class ObjectExtensions
    {

        public static string GenerateInsertStatement(this object obj, IList<object> parameters)
        {
            var columnList = new StringBuilder();
            var valueList = new StringBuilder();
            var type = obj.GetType();
            var tableAttribute = (TableAttribute)type.GetCustomAttribute(typeof(TableAttribute));
            var tableName = tableAttribute?.Name ?? $"{type.Name}s";
            var schema = tableAttribute?.Schema ?? "dbo";

            //sb.Append($"INSERT INTO {schema}.{tableName} (");

            foreach (var property in type.GetProperties())
            {
                if (property.CanRead)
                {
                    //Skip property if ignored.
                    var ignoreAttribute = (IgnoreAttribute)property.GetCustomAttribute(typeof(IgnoreAttribute));

                    if (ignoreAttribute?.IsIgnored(SqlAction.Insert) == true)
                        continue;

                    //Include Property
                    var columnAttribute = (ColumnAttribute)property.GetCustomAttribute(typeof(ColumnAttribute));
                    var columnName = columnAttribute?.Name ?? property.Name;

                    columnList.Append($"{columnName}, ");
                    valueList.Append($"{{{parameters.Count}}}, ");

                    parameters.Add(property.GetValue(obj));
                }
            }

            if (columnList.Length >= 1) columnList.Length -= 2;
            if (valueList.Length >= 1) valueList.Length -= 2;

            return $"INSERT INTO {schema}.{tableName} ({columnList}) VALUES ({valueList})";
        }

        public static string GenerateUpdateStatement<TType>(this TType obj, IList<object> parameters)
        {
            var update = new StringBuilder();
            var type = obj.GetType();
            var tableAttribute = (TableAttribute)type.GetCustomAttribute(typeof(TableAttribute));
            var tableName = tableAttribute?.Name ?? $"{type.Name}s";
            var schema = tableAttribute?.Schema ?? "dbo";
            var where = new StringBuilder(" WHERE ");

            update.Append($"UPDATE {schema}.{tableName} SET ");

            foreach (var property in type.GetProperties())
            {
                if (property.CanRead)
                {
                    //Skip property if ignored.
                    var ignoreAttribute = (IgnoreAttribute)property.GetCustomAttribute(typeof(IgnoreAttribute));
                    var primaryKeyAttribute = (PrimaryKeyAttribute)property.GetCustomAttribute(typeof(PrimaryKeyAttribute));

                    if (ignoreAttribute?.IsIgnored(SqlAction.Update) == true)
                        continue;

                    var columnAttribute = (ColumnAttribute)property.GetCustomAttribute(typeof(ColumnAttribute));
                    var columnName = columnAttribute?.Name ?? property.Name;

                    if (primaryKeyAttribute != null)
                    {
                        where.Append($"{columnName} = {{{parameters.Count}}} AND ");
                        parameters.Add(property.GetValue(obj));
                    }
                    else
                    {
                        update.Append($"{columnName} = {{{parameters.Count}}}, ");
                        parameters.Add(property.GetValue(obj));
                    }
                }
            }

            if (parameters.Count == 0)
                return "";

            if (update.Length >= 1) update.Length -= 2;

            if (where.Length >= 1) where.Length -= 5;

            /****************************************************************************
             * Get the update key.                                                      *
             ***************************************************************************/

            return $"{update} {where}";
        }

    }
}
