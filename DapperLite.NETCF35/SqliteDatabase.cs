using System.Collections.Generic;
using System.Linq;
using System.Data.SQLite;

namespace DapperLite
{
	public class SqliteDatabase<TId> : Database<TId>
	{
		public SqliteDatabase(SQLiteConnection connection) : base(connection) { }

		public SqliteDatabase(SQLiteConnection connection, DapperLiteException exceptionHandler, bool throwExceptions)
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
                SELECT tbl_name
                FROM main.sqlite_master
                WHERE type = 'table'";
				
			return GetConnection().Query<string>(sql).ToDictionary(name => name);
		}

		public new SQLiteConnection GetConnection()
		{
			return base.GetConnection() as SQLiteConnection;
		}
	}
}