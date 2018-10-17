using System;
using System.Collections;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations.Schema;
using System.Data;
using System.Data.SqlClient;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace Tremblay.DatabaseUtilities.Sql
{
    public static class SqlDatabaseExtensions
    {

        #region Fields

        private static readonly Random Rand = new Random();

        #endregion

        public static SqlDataReader CreateDataReader(this ISqlDatabase db, SqlConnection cnn, string query, params object[] parameters)
            => db.CreateDataReader(cnn, null, query, parameters);

        public static SqlDataReader CreateDataReader(this ISqlDatabase db, SqlTransaction transaction, string query, params object[] parameters)
            => db.CreateDataReader(transaction.Connection, transaction, query, parameters);

        public static SqlDataReader CreateDataReader(this ISqlDatabase db, SqlConnection cnn, SqlTransaction transaction, string query, params object[] parameters)
        {
            using (var command = db.CreateCommand(cnn, transaction, query, parameters))
            {
                if (command.Connection.State == ConnectionState.Closed)
                    command.Connection.Open();

                return command.ExecuteReader();
            }
        }

        public static SqlParameter CreateBinaryParam(string paramName, byte[] data)
        {
            var fileParam = new SqlParameter(string.Concat("@", paramName), null);

            fileParam.SqlDbType = SqlDbType.VarBinary;

            if (data == null)
            {
                fileParam.Value = DBNull.Value;
                fileParam.Size = -1;
            }
            else
                fileParam.Value = data.ToArray();

            return fileParam;
        }

        /// <summary>
        ///Creates a new Parameterized SqlCommand object. Query should be structured as it would if String.Format was used. Parameters should be referenced by their index in the <paramref  name="Parameters"/> argument. (ex: {0}, {1}, etc...)
        ///</summary>
        ///<param name="query"></param>
        ///<param name="parameters"></param>
        ///<returns>Using this function is safe from SQL injection attaks because it creates a parameterized command.</returns>
        ///<remarks></remarks>
        public static SqlCommand CreateSqlCommand(this ISqlDatabase db, string query, params object[] parameters)
            => db.CreateCommand(null, null, query, parameters);

        public static SqlCommand CreateCommand(this ISqlDatabase db, SqlConnection cnn, string query, params object[] parameters)
            => db.CreateCommand(cnn, null, query, parameters);

        public static SqlCommand CreateCommand(this ISqlDatabase db, SqlTransaction transaction, string query, params object[] parameters)
            => db.CreateCommand(transaction.Connection, transaction, query, parameters);

        public static SqlCommand CreateCommand(this ISqlDatabase db, SqlConnection cnn, SqlTransaction transaction, string query, params object[] parameters)
        {
            var command = new SqlCommand(query);
            var ex = new Regex(@"[\\\!\@\#|$\^\&\*\(\)\{\}\[\]\:\;\""\'\<\>\,\.\?\/\~\-\+\=\`]", RegexOptions.Compiled & RegexOptions.Singleline);

            command.Connection = (cnn == null)
                ? new SqlConnection(db.ConnectionString)
                : command.Connection = cnn;

            command.Transaction = transaction;

            if (parameters != null)
            {
                var parameterNames = new object[parameters.Length - 1 + 1];

                for (var counter = 0; counter <= parameters.Length - 1; counter++)
                {
                    if (parameters[counter] is Dictionary<string, object> index)
                    {
                        foreach (var entry in index)
                            command.Parameters.AddWithValue(entry.Key.StartsWith("@") ? entry.Key : $"@{entry.Key}", GetParameter(entry.Value));
                    }
                    else if (parameters[counter] is KeyValuePair<string, object> entry)
                    {
                        command.Parameters.AddWithValue(entry.Key.StartsWith("@") ? entry.Key : $"@{entry.Key}", GetParameter(entry.Value));
                    }
                    else if (parameters[counter] is IEnumerable items && !(parameters[counter] is string))
                    {
                        var sb = new StringBuilder();

                        foreach (object item in items)
                        {
                            if (IsNumber(item))
                                sb.Append($"{item},");
                            else if (item is string s && !ex.IsMatch(s))
                                sb.Append($"'{s}',");
                            else
                            {
                                // dim parameterName = CreateParameterName()
                                // sb.Append($"@P{counter}_{counter2},")
                                // command.Parameters.AddWithValue(String.Concat("@P", counter, "_", counter2), GetParameter(item))
                                var parameterName = CreateParameterName();
                                sb.Append($"{parameterName},");
                                command.Parameters.AddWithValue(parameterName, GetParameter(item));
                            }
                        }

                        if (sb.Length > 0)
                            sb.Length -= 1;

                        parameterNames[counter] = sb.ToString();
                    }
                    else if (parameters[counter] is SqlParameter p)
                    {
                        if (string.IsNullOrEmpty(p.ParameterName))
                            p.ParameterName = string.Concat("@P", counter);

                        parameterNames[counter] = p.ParameterName;
                        command.Parameters.Add(p);

                    }
                    else
                    {
                        var item = parameters[counter];

                        if (IsNumber(item))
                            parameterNames[counter] = item;
                        else if (item is string s && !ex.IsMatch(s))
                            parameterNames[counter] = $"'{s}'";
                        else
                        {
                            var parameterName = CreateParameterName();
                            command.Parameters.AddWithValue(parameterName, GetParameter(item));
                            parameterNames[counter] = parameterName;
                        }
                    }
                }

                command.CommandText = string.Format(command.CommandText, parameterNames);
            }

            return command;
        }

        private static string CreateParameterName()
        {
            var value = new StringBuilder("@");
            var characters = new[] { "A", "B", "C", "D", "E", "F", "G", "H", "I", "J", "K", "L", "M", "N", "O", "P", "Q", "R", "S", "T", "U", "V", "W", "X", "Y", "Z", "0", "1", "2", "3", "4", "5", "6", "7", "8", "9" };

            lock (Rand)
            {
                value.Append(characters[Rand.Next(0, 27)]);

                for (var i = 0; i <= 9; i++)
                    value.Append(characters[Rand.Next(0, 36)]);
            }

            return value.ToString();
        }

        public static void Insert(this ISqlDatabase db, SqlConnection cnn, object obj)
        {
            var parameters = new List<object>();
            db.ExecuteNonQuery(cnn, obj.GenerateInsertStatement(parameters), parameters);
        }

        public static void Insert(this ISqlDatabase db, SqlTransaction trans, object obj)
        {
            var parameters = new List<object>();
            db.ExecuteNonQuery(trans, obj.GenerateInsertStatement(parameters), parameters);
        }

        public static int InsertIdentity(this ISqlDatabase db, SqlConnection cnn, object obj)
        {
            var parameters = new List<object>();
            return (int)db.ExecuteScalar(cnn, $"{obj.GenerateInsertStatement(parameters)};SELECT SCOPE_IDENTITY();", parameters.ToArray());
        }

        public static int InsertIdentity(this ISqlDatabase db, SqlTransaction trans, object obj)
        {
            var parameters = new List<object>();
            return (int)db.ExecuteScalar(trans.Connection, trans, $"{obj.GenerateInsertStatement(parameters)};SELECT SCOPE_IDENTITY();", parameters.ToArray());
        }

        private static bool IsNumber(this object obj)
            => obj is sbyte
                || obj is byte
                || obj is short
                || obj is ushort
                || obj is int
                || obj is uint
                || obj is long
                || obj is ulong
                || obj is float
                || obj is double
                || obj is decimal;

        public static int ExecuteNonQuery(this ISqlDatabase db, string query, params object[] parameters)
        {
            using (var command = db.CreateSqlCommand(query, parameters))
            using (command.Connection)
                return db.ExecuteNonQuery(command);

        }

        public static int ExecuteNonQuery(this ISqlDatabase db, SqlConnection cnn, string sql, params object[] parameters)
        {
            using (var command = db.CreateCommand(cnn, sql, parameters))
                return db.ExecuteNonQuery(command);
        }

        public static int ExecuteNonQuery(this ISqlDatabase db, SqlTransaction transaction, string sql, params object[] parameters)
        {
            using (var command = db.CreateCommand(transaction.Connection, transaction, sql, parameters))
                return db.ExecuteNonQuery(command);
        }

        public static int ExecuteNonQuery(this ISqlDatabase db, SqlConnection cnn, SqlTransaction transaction, string sql, params object[] parameters)
        {
            using (var command = db.CreateCommand(cnn, transaction, sql, parameters))
                return db.ExecuteNonQuery(command);
        }

        public static int ExecuteNonQuery(this ISqlDatabase db, SqlCommand command)
        {
            if (command.Connection == null)
            {
                using (var cnn = new SqlConnection(db.ConnectionString))
                {
                    command.Connection = cnn;

                    cnn.Open();
                    return command.ExecuteNonQuery();
                }
            }

            var closeConnection = command.Connection.State == ConnectionState.Closed;

            try
            {
                if (closeConnection)
                    command.Connection.Open();
                return command.ExecuteNonQuery();
            }
            finally
            {
                if (closeConnection)
                    command.Connection.Close();
            }
        }


        public static object ExecuteScalar(this ISqlDatabase db, string query, params object[] parameters)
        {
            using (var command = db.CreateSqlCommand(query, parameters))
            using (command.Connection)
                return db.ExecuteScalar(command);
        }

        public static object ExecuteScalar(this ISqlDatabase db, SqlConnection cnn, string sql, params object[] parameters)
        {
            using (var command = db.CreateCommand(cnn, sql, parameters))
                return db.ExecuteScalar(command);
        }

        public static object ExecuteScalar(this ISqlDatabase db, SqlConnection cnn, SqlTransaction transaction, string sql, params object[] parameters)
        {
            using (var command = db.CreateCommand(cnn, transaction, sql, parameters))
                return db.ExecuteScalar(command);
        }

        public static object ExecuteScalar(this ISqlDatabase db, SqlCommand command)
        {
            if (command.Connection == null)
            {
                using (var cnn = new SqlConnection(db.ConnectionString))
                {
                    command.Connection = cnn;

                    cnn.Open();
                    return command.ExecuteScalar();
                }
            }

            var closeConnection = command.Connection.State == ConnectionState.Closed;

            try
            {
                if (command.Connection.State != ConnectionState.Open)
                    command.Connection.Open();
                return command.ExecuteScalar();
            }
            finally
            {
                if (command.Connection.State == ConnectionState.Open && closeConnection)
                    command.Connection.Close();
            }
        }


        public static DataTable CreateDataTable(this ISqlDatabase db, string name, SqlCommand command)
        {
            var value = new DataTable(name);

            if (command.Connection == null)
            {
                using (var cnn = new SqlConnection(db.ConnectionString))
                {
                    command.Connection = cnn;

                    using (var adpt = new SqlDataAdapter(command))
                        adpt.Fill(value);
                }
            }
            else
            {
                var closeConnection = command.Connection.State == ConnectionState.Closed;

                try
                {
                    if (command.Connection.State != ConnectionState.Open)
                        command.Connection.Open();

                    using (var adpt = new SqlDataAdapter(command))
                        adpt.Fill(value);
                }
                finally
                {
                    if (command.Connection.State == ConnectionState.Open && closeConnection)
                        command.Connection.Close();
                }
            }

            return value;
        }

        private static object GetParameter(object value)
        {
            if (value == null | value == DBNull.Value)
                return DBNull.Value;

            // If Nullable.GetUnderlyingType(Value) Is Nothing Then
            return value;
        }


        public static TValueType GetObject<TValueType>(this ISqlDatabase db, string commandText, params object[] parameters) where TValueType : class, IDbObject<TValueType>, new()
        {
            using (var command = db.CreateSqlCommand(commandText, parameters))
            using (command.Connection)
                return db.GetObject<TValueType>(command);
        }

        /// <summary>
        /// Creates an object of the specified type from the values contained within the results of the specified Command.
        /// </summary>
        /// <typeparam name="TValueType"></typeparam>
        /// <param name="command"></param>
        /// <returns></returns>
        /// <remarks></remarks>
        public static TValueType GetObject<TValueType>(this ISqlDatabase db, SqlCommand command) where TValueType : class, IDbObject<TValueType>, new()
        {
            var value = new TValueType();
            var closeConnection = false;

            try
            {
                if (command.Connection == null)
                    command.Connection = new SqlConnection(db.ConnectionString);
                if (command.Connection.State == ConnectionState.Closed)
                {
                    closeConnection = true;
                    command.Connection.Open();
                }

                using (var reader = command.ExecuteReader())
                    return reader.Read()
                        ? value.FromDataReaderInstance(reader)
                        : null;

            }
            finally
            {
                if (closeConnection)
                    command.Connection.Close();
            }
        }

        public static List<TValueType> GetObjectList<TValueType>(this ISqlDatabase db, string commandText, params object[] parameters) where TValueType : class, new()
        {
            using (var command = db.CreateSqlCommand(commandText, parameters))
                using (command.Connection)
                    return db.GetObjectList<List<TValueType>, TValueType>(command);
        }

        public static async Task<List<TValueType>> GetObjectListAsync<TValueType>(this ISqlDatabase db, string commandText, params object[] parameters) where TValueType : class, new()
        {
            using (var command = db.CreateSqlCommand(commandText, parameters))
                using (command.Connection)
                    return await db.GetObjectListAsync<List<TValueType>, TValueType>(command);
        }

        public static List<TValueType> GetObjectList<TValueType>(this ISqlDatabase db, SqlCommand command) where TValueType : class, new()
            => db.GetObjectList<List<TValueType>, TValueType>(command);


        public static async Task<List<TValueType>> GetObjectListAsync<TValueType>(this ISqlDatabase db, SqlCommand command) where TValueType : class, new()
            => await db.GetObjectListAsync<List<TValueType>, TValueType>(command);

        public static TListType GetObjectList<TListType, TValueType>(this ISqlDatabase db, string commandText, params object[] parameters)
            where TListType : class, IList<TValueType>, new()
            where TValueType : class, new()
        {
            using (SqlCommand command = db.CreateSqlCommand(commandText, parameters))
            {
                using (command.Connection)
                    return db.GetObjectList<TListType, TValueType>(command);
            }
        }

        public static async Task<TListType> GetObjectListAsync<TListType, TValueType>(this ISqlDatabase db, string commandText, params object[] parameters)
            where TListType : class, IList<TValueType>, new()
            where TValueType : class, new()
        {
            using (var command = db.CreateSqlCommand(commandText, parameters))
            using (command.Connection)
                return await db.GetObjectListAsync<TListType, TValueType>(command);
        }

        public static TListType GetObjectList<TListType, TValueType>(this ISqlDatabase db, SqlCommand command)
            where TListType : class, IList<TValueType>, new()
            where TValueType : class, new()
        {
            var value = new TListType();
            var closeConnection = false;

            if (command.Connection == null)
                command.Connection = new SqlConnection(db.ConnectionString);

            try
            {
                if (command.Connection.State == ConnectionState.Closed)
                {
                    command.Connection.Open();
                    closeConnection = true;
                }

                using (var reader = command.ExecuteReader())
                    while (reader.Read())
                        value.Add(CreateObject<TValueType>(reader));
            }
            finally
            {
                if (closeConnection & command.Connection.State == ConnectionState.Open)
                    command.Connection.Close();
            }

            return value;
        }

        public static async Task<TListType> GetObjectListAsync<TListType, TValueType>(this ISqlDatabase db, SqlCommand command)
            where TListType : class, IList<TValueType>, new()
            where TValueType : class, new()
        {
            var value = new TListType();
            var closeConnection = false;

            if (command.Connection == null)
                command.Connection = new SqlConnection(db.ConnectionString);

            try
            {
                if (command.Connection.State == ConnectionState.Closed)
                {
                    await command.Connection.OpenAsync();
                    closeConnection = true;
                }

                using (var reader = await command.ExecuteReaderAsync())
                    while (await reader.ReadAsync())
                        value.Add(CreateObject<TValueType>(reader));

            }
            finally
            {
                if (closeConnection & command.Connection.State == ConnectionState.Open)
                    command.Connection.Close();
            }

            return value;
        }

        public static IEnumerable<TValueType> GetObjectListFromFactory<TValueType, TFactoryType>(this ISqlDatabase db, string commandText, params object[] parameters)
            where TValueType : new()
            where TFactoryType : IObjectFactory<TValueType>, new()
        {
            using (var command = db.CreateSqlCommand(commandText, parameters))
            using (command.Connection)
                return db.GetObjectListFromFactory<TValueType, TFactoryType>(command);
        }

        public static async Task<IEnumerable<TValueType>> GetObjectListFromFactoryAsync<TValueType, TFactoryType>(this ISqlDatabase db, string commandText, params object[] parameters)
            where TValueType : new()
            where TFactoryType : IObjectFactory<TValueType>, new()
        {
            using (var command = db.CreateSqlCommand(commandText, parameters))
            {
                using (command.Connection)
                    return await db.GetObjectListFromFactoryAsync<TValueType, TFactoryType>(command);
            }
        }

        public static IEnumerable<TValueType> GetObjectListFromFactory<TValueType, TFactoryType>(this ISqlDatabase db, SqlCommand command)
            where TValueType : new()
            where TFactoryType : IObjectFactory<TValueType>, new()
        {
            var value = new List<TValueType>();
            var closeConnection = false;
            var factory = new TFactoryType();

            if (command.Connection == null)
                command.Connection = new SqlConnection(db.ConnectionString);

            try
            {
                if (command.Connection.State == ConnectionState.Closed)
                {
                    command.Connection.Open();
                    closeConnection = true;
                }

                // command.CommandType = CommandType.StoredProcedure

                using (var reader = command.ExecuteReader())
                {
                    while (reader.Read())
                        value.Add(factory.FromDataReader(reader));
                }
            }
            catch (Exception ex)
            {
                var a = ex;
            }
            finally
            {
                if (closeConnection & command.Connection.State == ConnectionState.Open)
                    command.Connection.Close();
            }

            return value;
        }

        public static async Task<IEnumerable<TValueType>> GetObjectListFromFactoryAsync<TValueType, TFactoryType>(this ISqlDatabase db, SqlCommand command)
            where TValueType : new()
            where TFactoryType : IObjectFactory<TValueType>, new()
        {
            var value = new List<TValueType>();
            var closeConnection = false;
            var factory = new TFactoryType();

            if (command.Connection == null)
                command.Connection = new SqlConnection(db.ConnectionString);

            try
            {
                if (command.Connection.State == ConnectionState.Closed)
                {
                    await command.Connection.OpenAsync();
                    closeConnection = true;
                }

                // command.CommandType = CommandType.StoredProcedure

                using (var reader = await command.ExecuteReaderAsync())
                {
                    while (await reader.ReadAsync())
                        value.Add(factory.FromDataReader(reader));
                }
            }
            // Catch ex As Exception
            // Dim a = ex
            finally
            {
                if (closeConnection & command.Connection.State == ConnectionState.Open)
                    command.Connection.Close();
            }

            return value;
        }


        public static List<TValueType> GetValueList<TValueType>(this ISqlDatabase db, string commandText, params object[] parameters)
        {
            using (var command = db.CreateSqlCommand(commandText, parameters))
            using (command.Connection)
                return db.GetValueList<TValueType>(command);
        }

        public static List<TValueType> GetValueList<TValueType>(this ISqlDatabase db, SqlCommand command)
        {
            var value = new List<TValueType>();
            var closeConnection = false;

            if (command.Connection == null)
                command.Connection = new SqlConnection(db.ConnectionString);

            try
            {
                if (command.Connection.State == ConnectionState.Closed)
                {
                    command.Connection.Open();
                    closeConnection = true;
                }

                using (var reader = command.ExecuteReader())
                {
                    while (reader.Read())
                        value.Add((TValueType)reader[0]);
                }
            }
            finally
            {
                if (closeConnection & command.Connection.State == ConnectionState.Open)
                    command.Connection.Close();
            }

            return value;
        }

        public static Dictionary<TKeyType, TValueType> GetDictionary<TKeyType, TValueType>(this ISqlDatabase db, string commandText, string keyColumn, string valueColumn, params object[] parameters)
        {
            using (var command = db.CreateSqlCommand(commandText, parameters))
            using (command.Connection)
                return db.GetDictionary<TKeyType, TValueType>(command, keyColumn, valueColumn);
        }

        public static Dictionary<TKeyType, TValueType> GetDictionary<TKeyType, TValueType>(this ISqlDatabase db, SqlCommand command, string keyColumn, string valueColumn)
        {
            var value = new Dictionary<TKeyType, TValueType>();
            var closeConnection = false;

            if (command.Connection == null)
                command.Connection = new SqlConnection(db.ConnectionString);

            try
            {
                if (command.Connection.State == ConnectionState.Closed)
                {
                    command.Connection.Open();
                    closeConnection = true;
                }

                using (var reader = command.ExecuteReader())
                    while (reader.Read())
                        value.Add((TKeyType)reader[keyColumn], (TValueType)reader[valueColumn]);
            }
            finally
            {
                if (closeConnection & command.Connection.State == ConnectionState.Open)
                    command.Connection.Close();
            }

            return value;
        }



        /// <summary>
        ///Determines if any records are returned from the specified query.
        ///</summary>
        ///<param name="commandText"></param>
        ///<param name="parameters"></param>
        ///<returns></returns>
        ///<remarks></remarks>
        public static bool Exists(this ISqlDatabase db, string commandText, params object[] parameters)
        {
            using (var command = db.CreateCommand(null, null, commandText, parameters))
            using (command.Connection)
                return db.Exists(command);
        }

        public static bool Exists(this ISqlDatabase db, SqlConnection cnn, string commandText, params object[] parameters)
        {
            using (var command = db.CreateCommand(cnn, commandText, parameters))
                return db.Exists(command);

        }

        public static bool Exists(this ISqlDatabase db, SqlConnection cnn, SqlTransaction transaction, string commandText, params object[] parameters)
        {
            using (var command = db.CreateCommand(cnn, transaction, commandText, parameters))
                return db.Exists(command);
        }

        public static bool Exists(this ISqlDatabase db, SqlCommand command)
        {
            if (command.Connection == null)
                command.Connection = new SqlConnection(db.ConnectionString);

            var closeConnection = command.Connection.State == ConnectionState.Closed;

            try
            {
                if (closeConnection)
                    command.Connection.Open();

                command.CommandText = string.Concat("SELECT CAST( CASE WHEN EXISTS(", command.CommandText, ") THEN 1 ELSE 0 End AS BIT)");

                return (bool)command.ExecuteScalar();
            }
            finally
            {
                if (closeConnection)
                    command.Connection.Close();
            }
        }

        public static void CreateParameters(string parameterName, ICollection items, ref string commandText, ref SqlParameterCollection parameters)
            => commandText = CreateParameters(parameterName, items, ref parameters);

        public static string CreateParameters(ICollection items, ref SqlParameterCollection parameters)
            => CreateParameters("IndexedParam", items, ref parameters);


        public static string CreateParameters(string parameterName, ICollection items, ref SqlParameterCollection parameters)
        {
            var value = new StringBuilder();
            var index = 0;

            foreach (var item in items)
            {
                value.AppendFormat("@{0}{1},", parameterName, index);
                parameters.AddWithValue(string.Concat("@", parameterName, index++), item);
            }

            if (value[value.Length - 1] == ',')
                value.Remove(value.Length - 1, 1);

            return value.ToString();
        }


        public static TValueType CreateObject<TValueType>(IDataReader reader) where TValueType : class, new()
        {
            var obj = new TValueType();

            if (obj is IDbObject<TValueType> val)
                return val.FromDataReaderInstance(reader);

            var props = obj.GetType().GetProperties();

            foreach (var p in props)
            {
                if (p.CanWrite)
                {
                    var attr = (ColumnAttribute)p.GetCustomAttribute(typeof(ColumnAttribute));

                    if (attr != null)
                    {
                        if (!string.IsNullOrEmpty(attr.Name) && reader[attr.Name] != DBNull.Value)
                            p.SetValue(obj, reader[attr.Name]);
                    }
                }
            }

            return obj;
        }
    }
}