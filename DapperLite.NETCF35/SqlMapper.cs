using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Reflection;

namespace DapperLite
{
    public static partial class SqlMapper
    {
        /// <summary>
        /// Pulled straight from the Dapper source.
        /// </summary>
        private static IDbCommand SetupCommand(IDbConnection cnn, IDbTransaction transaction, string sql, int? commandTimeout, CommandType? commandType)
        {
            IDbCommand cmd = cnn.CreateCommand();
            if (transaction != null)
                cmd.Transaction = transaction;
            cmd.CommandText = sql;
            if (commandTimeout.HasValue)
                cmd.CommandTimeout = commandTimeout.Value;
            if (commandType.HasValue)
                cmd.CommandType = commandType.Value;
            return cmd;
        }

        /// <summary>
        /// Modified from the Dapper.Rainbow source.
        /// </summary>
        private static IEnumerable<string> GetParamNames(object o)
        {
            return o.GetType().GetProperties(BindingFlags.GetProperty | BindingFlags.Instance | BindingFlags.Public).Select(prop => prop.Name);
        }

        /// <summary>
        /// Inject parameters from the supplied object into the command object.
        /// </summary>
        private static void AddParams(IDbCommand cmd, object data)
        {
            if (cmd == null || data == null) return;

            IEnumerable<string> propertyNames = GetParamNames(data);

            foreach (string propertyName in propertyNames)
            {
                IDbDataParameter param = cmd.CreateParameter();
                param.ParameterName = "@" + propertyName;
                param.Value = data.GetType().GetProperty(propertyName).GetValue(data, null) ?? DBNull.Value;
                cmd.Parameters.Add(param);
            }
        }

        private static IEnumerable<string> GetColumnNames(IDataRecord reader)
        {
            IList<string> columnNames = new List<string>();

            for (int i = 0; i < reader.FieldCount; i++)
            {
                columnNames.Add(reader.GetName(i));
            }

            return columnNames;
        }

        /// <summary>
        /// Populate a references type from an IDataRecord by matching column names to property names.
        /// </summary>
        private static void PopulateClass(object objectClass, IDataRecord reader)
        {
            Type theType = objectClass.GetType();
            PropertyInfo[] p = theType.GetProperties();

            // Only set properties which match column names in the result.
            foreach (string columnName in GetColumnNames(reader))
            {
                string colName = columnName;
                object value = reader[colName];
                PropertyInfo pi = p.FirstOrDefault(x => x.Name == colName);

                if (pi == null || value == DBNull.Value) continue;

                Type columnType = value.GetType();
                Type actualType = Nullable.GetUnderlyingType(pi.PropertyType) ?? pi.PropertyType;

                // Check for a directly assignable type
                if (actualType == columnType || actualType.Equals(columnType))
                {
                    pi.SetValue(objectClass, value, null);
                }
                else
                {
                    value = actualType.GetMethod("Parse", new[] { typeof(string) }).Invoke(null, new[] { value });
                    pi.SetValue(objectClass, value, null);
                }
            }
        }

        /// <summary>
        /// Encode a variable for use with the SQL LIKE operator.
        /// Modified from: http://stackoverflow.com/questions/6030099/does-dapper-support-the-like-operator
        /// </summary>
        public static string EncodeForLike(string term)
        {
            return "%" + term.Replace("%", "[%]").Replace("[", "[[]").Replace("]", "[]]") + "%";
        }

        /// <summary>
        /// Executes an SQL statement and returns the number of rows affected. Supports transactions.
        /// </summary>
        /// <returns>Number of rows affected.</returns>
        public static int Execute(this IDbConnection cnn, string sql, object param, IDbTransaction transaction)
        {
            using (IDbCommand cmd = SetupCommand(cnn, transaction, sql, null, null))
            {
                AddParams(cmd, param);
                return cmd.ExecuteNonQuery();
            }
        }

        /// <summary>
        /// Overload to allow a basic execute call.
        /// </summary>
        /// <returns>Number of rows affected.</returns>
        public static int Execute(this IDbConnection cnn, string sql)
        {
            return Execute(cnn, sql, null, null);
        }

        /// <summary>
        /// Basically a duplication of the Dapper interface.
        /// </summary>
        public static IEnumerable<T> Query<T>(this IDbConnection conn, string sql, object param)
        {
            IList<T> list = new List<T>();

            using (IDbCommand cmd = SetupCommand(conn, null, sql, null, null))
            {
                AddParams(cmd, param);

                using (IDataReader reader = cmd.ExecuteReader())
                {
                    if (reader == null) return list;

                    Type type = typeof(T);
                    if (type.IsValueType || type == typeof(string))
                    {
                        while (reader.Read())
                        {
                            if (reader.IsDBNull(0)) // Handles the case where the value is null.
                            {
                                list.Add(default(T));
                            }
                            else
                            {
                                list.Add((T)reader[0]);
                            }
                        }
                    }
                    else // Reference types
                    {
                        while (reader.Read())
                        {
                            T record = Activator.CreateInstance<T>();
                            PopulateClass(record, reader);
                            list.Add(record);
                        }
                    }

                    return list;
                }
            }
        }

        /// <summary>
        /// Overload for the times when we don't require a parameter.
        /// Because of .NET Compact Framework, we can't use optional parameters.
        /// </summary>
        public static IEnumerable<T> Query<T>(this IDbConnection conn, string sql)
        {
            return Query<T>(conn, sql, null);
        }
    }
}
