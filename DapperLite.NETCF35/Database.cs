using System;
using System.Data;
using System.Linq;
using System.Collections.Generic;
using System.Reflection;

namespace DapperLite
{
    public delegate void DapperLiteException(Exception e);

    /// <summary>
    /// A container for a database. Assumes all tables have an Id column named Id.
    /// </summary>
    /// <typeparam name="TId">The .NET equivalent type of the Id column (typically int or Guid).</typeparam>
    public abstract class Database<TId>
    {
        private readonly IDbConnection m_connection;
        private Dictionary<string, string> m_tableNameMap;
        private readonly DapperLiteException m_exceptionHandler;
        protected readonly bool m_throwExceptions;

        protected Database(IDbConnection connection)
        {
            if (connection == null) throw new ArgumentException("Connection cannot be null");

            m_connection = connection;
            m_throwExceptions = true;
        }

        /// <summary>
        /// Provides advanced configuration for the behaviour of the class when Exceptions are encountered.
        /// </summary>
        /// <param name="connection"></param>
        /// <param name="exceptionHandler">Deletegate which will receive any caught Exception objects.</param>
        /// <param name="throwExceptions">Indicate whether a caught exception should be re-thrown.</param>
        protected Database(IDbConnection connection, DapperLiteException exceptionHandler, bool throwExceptions)
        {
            if (connection == null) throw new ArgumentException("Connection cannot be null");

            m_connection = connection;
            m_exceptionHandler = exceptionHandler;
            m_throwExceptions = throwExceptions;
        }

        /// <summary>
        /// This method must be called 
        /// </summary>
        public void Init()
        {
            m_tableNameMap = GetTableNameMap();
        }

        /// <summary>
        /// Wrapper method for the delegate so we don't throw an exception if the delegate is null.
        /// </summary>
        protected void ExceptionHandler(Exception e)
        {
            if (m_exceptionHandler != null) m_exceptionHandler(e);
        }

        protected IDbConnection GetConnection()
        {
            if (m_connection.State != ConnectionState.Open)
            {
                m_connection.Open();
            }

            return m_connection;
        }

        private static string Pluralize(string s)
        {
            char lastchar = s[s.Length - 1];

            if (lastchar == 'y')
            {
                return s.Remove(s.Length - 1, 1) + "ies";
            }

            if (lastchar == 's')
            {
                return s;
            }

            return s + "s";
        }

        /// <summary>
        /// This abstract method must be implemented by deriving types as
        /// the implementation is specific to the SQL vendor (and possibly version).
        /// </summary>
        protected abstract Dictionary<string, string> GetTableNameMap();

        /// <summary>
        /// First checks if there is an exact match between type name and table name.
        /// Then checks if there is a match between the pluralized type name and table name.
        /// </summary>
        private string GetTableName(MemberInfo type)
        {
            string tableName;
            string typeName = type.Name;

            // First see if the type name exactly matches a table name.
            if (m_tableNameMap.TryGetValue(typeName, out tableName))
            {
                return tableName;
            }

            // Then check if when the type name is pluralized, it matches a table name.
            if (m_tableNameMap.TryGetValue(Pluralize(typeName), out tableName))
            {
                return tableName;
            }

            throw new Exception(string.Format("Cannot match the type name {0} to any table", typeName));
        }

        protected string GetTableName(object obj)
        {
            return GetTableName(obj.GetType());
        }

        /// <summary>
        /// A wrapper that uses the internally cached connection.
        /// </summary>
        public int Execute(string sql, object param, IDbTransaction transaction)
        {
            try
            {
                return GetConnection().Execute(sql, param, transaction);
            }
            catch (Exception e)
            {
                ExceptionHandler(e);
                if (m_throwExceptions) throw;
            }

            return -1;
        }

        /// <summary>
        /// A wrapper that uses the internally cached connection.
        /// </summary>
        public int Execute(string sql)
        {
            return Execute(sql, null, null);
        }

        /// <summary>
        /// A wrapper that uses the internally cached connection.
        /// </summary>
        public IEnumerable<T> Query<T>(string sql, object param)
        {
            try
            {
                return GetConnection().Query<T>(sql, param);
            }
            catch (Exception e)
            {
                ExceptionHandler(e);
                if (m_throwExceptions) throw;
            }

            return new List<T>();
        }

        /// <summary>
        /// A wrapper that uses the internally cached connection.
        /// </summary>
        public IEnumerable<T> Query<T>(string sql)
        {
            return Query<T>(sql, null);
        }

        /// <summary>
        /// Gets a single instance of a type by specifying the row Id.
        /// </summary>
        /// <returns>A specific instance of the specified type, or the default value for the type.</returns>
        public T Get<T>(TId id)
        {
            try
            {
                string tableName = GetTableName(typeof(T));
                return GetConnection().Query<T>("SELECT * FROM " + tableName + " WHERE Id = @id", new { id }).FirstOrDefault();
            }
            catch (Exception e)
            {
                ExceptionHandler(e);
                if (m_throwExceptions) throw;
            }

            return default(T);
        }

        /// <summary>
        /// Gets a single instance of a type. Filters by a single column.
        /// </summary>
        /// <param name="columnName">Used to generate a WHERE clause.</param>
        /// <param name="data">Input parameter for the WHERE clause.</param>
        /// <returns>A specific instance of the specified type, or the default value for the type.</returns>
        public T Get<T>(string columnName, object data)
        {
            try
            {
                return All<T>(columnName, data).FirstOrDefault();
            }
            catch (Exception e)
            {
                ExceptionHandler(e);
                if (m_throwExceptions) throw;
            }

            return default(T);
        }

        /// <summary>
        /// Gets all records in the table matching the supplied type after applying the supplied filter
        /// in a WHERE clause.
        /// </summary>
        /// <param name="columnName">Used to generate a WHERE clause.</param>
        /// <param name="data">Input parameter for the WHERE clause.</param>
        /// <returns>All records in the table matching the supplied type.</returns>
        public IEnumerable<T> All<T>(string columnName, object data)
        {
            try
            {
                string tableName = GetTableName(typeof(T));
                string sql = String.Format("SELECT * FROM {0} WHERE {1} = @param", tableName, columnName);
                return GetConnection().Query<T>(sql, new { param = data });
            }
            catch (Exception e)
            {
                ExceptionHandler(e);
                if (m_throwExceptions) throw;
            }

            return new List<T>();
        }

        /// <summary>
        /// Gets all records in the table matching the supplied type.
        /// </summary>
        /// <returns>All records in the table matching the supplied type.</returns>
        public IEnumerable<T> All<T>()
        {
            try
            {
                string tableName = GetTableName(typeof(T));
                return GetConnection().Query<T>("SELECT * FROM " + tableName);
            }
            catch (Exception e)
            {
                ExceptionHandler(e);
                if (m_throwExceptions) throw;
            }

            return new List<T>();
        }

        /// <summary>
        /// Inserts the supplied object into the database. Infers table name from type name.
        /// </summary>
        public virtual void Insert(object obj)
        {
            try
            {
                string tableName = GetTableName(obj);
                GetConnection().Insert(null, tableName, obj);
            }
            catch (Exception e)
            {
                ExceptionHandler(e);
                if (m_throwExceptions) throw;
            }
        }

        /// <summary>
        /// Updates the supplied object. Infers table name from type name.
        /// </summary>
        public virtual void Update(object obj)
        {
            try
            {
                string tableName = GetTableName(obj);
                GetConnection().Update(null, tableName, obj);
            }
            catch (Exception e)
            {
                ExceptionHandler(e);
                if (m_throwExceptions) throw;
            }
        }
    }
}
