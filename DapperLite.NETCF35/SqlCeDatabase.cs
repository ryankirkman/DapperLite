using System;
using System.Collections.Generic;
using System.Data.SqlServerCe;
using System.Linq;

namespace DapperLite
{
    public class SqlCeDatabase<TId> : Database<TId>
    {
        public SqlCeDatabase(SqlCeConnection connection) : base(connection) { }

        public SqlCeDatabase(SqlCeConnection connection, DapperLiteException exceptionHandler, bool throwExceptions)
            : base(connection, exceptionHandler, throwExceptions)
        {
        }

        /// <summary>
        /// This query is specific to SQLCE 3.5 at the time of writing.
        /// A similar query can be used for SQL Server.
        /// </summary>
        protected override Dictionary<string, string> GetTableNameMap()
        {
            const string sql = @"
                SELECT TABLE_NAME
                FROM INFORMATION_SCHEMA.TABLES
                WHERE TABLE_TYPE = 'TABLE'";

            return GetConnection().Query<string>(sql).ToDictionary(name => name);
        }

        public new SqlCeConnection GetConnection()
        {
            return base.GetConnection() as SqlCeConnection;
        }

        /// <summary>
        /// Inserts the supplied object into the database. Infers table name from type name.
        /// Uses CommitMode.Immediate to ensure data is written to disk immediately.
        /// </summary>
        public override void Insert(object obj)
        {
            try
            {
                string tableName = GetTableName(obj);
                SqlCeConnection conn = GetConnection();
                using (SqlCeTransaction tran = conn.BeginTransaction())
                {
                    conn.Insert(tran, tableName, obj);
                    tran.Commit(CommitMode.Immediate);
                }
            }
            catch (Exception e)
            {
                ExceptionHandler(e);
                if (m_throwExceptions) throw;
            }
        }

        /// <summary>
        /// Updates the supplied object. Infers table name from type name.
        /// Uses CommitMode.Immediate to ensure data is written to disk immediately.
        /// </summary>
        public override void Update(object obj)
        {
            try
            {
                string tableName = GetTableName(obj);
                SqlCeConnection conn = GetConnection();
                using (SqlCeTransaction tran = conn.BeginTransaction())
                {
                    conn.Update(tran, tableName, obj);
                    tran.Commit(CommitMode.Immediate);
                }
            }
            catch (Exception e)
            {
                ExceptionHandler(e);
                if (m_throwExceptions) throw;
            }
        }
    }
}
