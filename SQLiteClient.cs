/*
 * Frank Krueger <fak@kruegersystems.com>
 * Tue, Sep 8, 2009 at 5:52 AM
 * */

using System;
using System.Runtime.InteropServices;
using System.Collections.Generic;

namespace SQLiteClient
{
	[Obsolete("This sample now uses System.Data")]
	public class SQLiteException : Exception
	{
		public SQLiteException (string message) : base(message)
		{
		}
	}

	[Obsolete("This sample now uses System.Data")]
	public class SQLiteConnection : IDisposable
	{
		private IntPtr _db;
		private bool _open;

		public string Database { get; set; }

		public SQLiteConnection (string database)
		{
			Database = database;
		}

		public void Open ()
		{
			if (SQLite3.Open (Database, out _db) != SQLite3.Result.OK) {
				throw new SQLiteException ("Could not open database file: " + Database);
			}
			_open = true;
		}

		public SQLiteCommand CreateCommand (string cmdText, params object[] ps)
		{
			if (!_open) {
				throw new SQLiteException ("Cannot create commands from unopened database");
			} else {
				var cmd = new SQLiteCommand (_db);
				cmd.CommandText = cmdText;
				foreach (var o in ps) {
					cmd.Bind (o);
				}
				return cmd;
			}
		}

		public int Execute (string query, params object[] ps)
		{
			var cmd = CreateCommand (query, ps);
			Console.Error.WriteLine("Executing " + cmd);
			return cmd.ExecuteNonQuery ();
		}

		public IEnumerable<T> Query<T> (string query, params object[] ps) where T : new()
		{
			var cmd = CreateCommand (query, ps);
			return cmd.ExecuteQuery<T> ();
		}

		public void Dispose ()
		{
			if (_open) {
				SQLite3.Close(_db);
				_db = IntPtr.Zero;
				_open = false;
			}
		}
	}

	[Obsolete("This sample now uses System.Data")]
	public class SQLiteCommand
	{
		private IntPtr _db;
		private List<Binding> _bindings;

		public string CommandText { get; set; }

		internal SQLiteCommand (IntPtr db)
		{
			_db = db;
			_bindings = new List<Binding> ();
			CommandText = "";
		}

		public int ExecuteNonQuery ()
		{
			var stmt = Prepare ();

			var r = SQLite3.Step (stmt);
			if (r == SQLite3.Result.Error) {
				string msg = SQLite3.Errmsg (_db);
				SQLite3.Finalize (stmt);
				throw new SQLiteException (msg);
			} else if (r == SQLite3.Result.Done) {
				int rowsAffected = SQLite3.Changes (_db);
				SQLite3.Finalize (stmt);
				return rowsAffected;
			} else {
				SQLite3.Finalize (stmt);
				throw new SQLiteException ("Unknown error");
			}
		}
		
		public IEnumerable<T> ExecuteQuery<T> () where T : new()
		{
			var stmt = Prepare ();

			var props = GetProps (typeof(T));
			var cols = new System.Reflection.PropertyInfo[SQLite3.ColumnCount (stmt)];
			for (int i = 0; i < cols.Length; i++) {
				cols[i] = MatchColProp (SQLite3.ColumnName (stmt, i), props);
			}

			while (SQLite3.Step (stmt) == SQLite3.Result.Row) {
				var obj = new T ();
				for (int i = 0; i < cols.Length; i++) {
					if (cols[i] == null)
						continue;
					var val = ReadCol (stmt, i, cols[i].PropertyType);
					cols[i].SetValue (obj, val, null);
				}
				yield return obj;
			}

			SQLite3.Finalize (stmt);
		}

		public void Bind (string name, object val)
		{
			_bindings.Add (new Binding {
				Name = name,
				Value = val
			});
		}
		public void Bind (object val)
		{
			Bind (null, val);
		}

		public override string ToString ()
		{
			return CommandText;
		}

		IntPtr Prepare ()
		{
			var stmt = SQLite3.Prepare (_db, CommandText);
			BindAll (stmt);
			return stmt;
		}

		void BindAll (IntPtr stmt)
		{
			int nextIdx = 1;
			foreach (var b in _bindings) {
				if (b.Name != null) {
					b.Index = SQLite3.BindParameterIndex (stmt, b.Name);
				} else {
					b.Index = nextIdx++;
				}
			}
			foreach (var b in _bindings) {
				if (b.Value == null) {
					SQLite3.BindNull (stmt, b.Index);
				} else {
					if (b.Value is Byte || b.Value is UInt16 || b.Value is SByte || b.Value is Int16 || b.Value is Int32) {
						SQLite3.BindInt (stmt, b.Index, Convert.ToInt32 (b.Value));
					} else if (b.Value is UInt32 || b.Value is Int64) {
						SQLite3.BindInt64 (stmt, b.Index, Convert.ToInt64 (b.Value));
					} else if (b.Value is Single || b.Value is Double || b.Value is Decimal) {
						SQLite3.BindDouble (stmt, b.Index, Convert.ToDouble (b.Value));
					} else if (b.Value is String) {
						SQLite3.BindText (stmt, b.Index, b.Value.ToString (), -1, new IntPtr (-1));
					}
				}
			}
		}

		class Binding
		{
			public string Name { get; set; }
			public object Value { get; set; }
			public int Index { get; set; }
		}

		object ReadCol (IntPtr stmt, int index, Type clrType)
		{
			var type = SQLite3.ColumnType (stmt, index);
			if (type == SQLite3.ColType.Null) {
				return null;
			} else {
				if (clrType == typeof(Byte) || clrType == typeof(UInt16) || clrType == typeof(SByte) || clrType == typeof(Int16) || clrType == typeof(Int32)) {
					return Convert.ChangeType (SQLite3.ColumnInt (stmt, index), clrType);
				} else if (clrType == typeof(UInt32) || clrType == typeof(Int64)) {
					return Convert.ChangeType (SQLite3.ColumnInt64 (stmt, index), clrType);
				} else if (clrType == typeof(Single) || clrType == typeof(Double) || clrType == typeof(Decimal)) {
					return Convert.ChangeType (SQLite3.ColumnDouble (stmt, index), clrType);
				} else if (clrType == typeof(String)) {
					return Convert.ChangeType (SQLite3.ColumnText (stmt, index), clrType);
				} else {
					throw new NotSupportedException ("Don't know how to read " + clrType);
				}
			}
		}

		static System.Reflection.PropertyInfo[] GetProps (Type t)
		{
			return t.GetProperties ();
		}
		static System.Reflection.PropertyInfo MatchColProp (string colName, System.Reflection.PropertyInfo[] props)
		{
			foreach (var p in props) {
				if (p.Name == colName) {
					return p;
				}
			}
			return null;
		}
	}

	[Obsolete("This sample now uses System.Data")]
	public static class SQLite3
	{
		public enum Result : int
		{
			OK = 0,
			Error = 1,
			Row = 100,
			Done = 101
		}

		[DllImport("sqlite3", EntryPoint = "sqlite3_open")]
		public static extern Result Open (string filename, out IntPtr db);
		[DllImport("sqlite3", EntryPoint = "sqlite3_close")]
		public static extern Result Close (IntPtr db);

		[DllImport("sqlite3", EntryPoint = "sqlite3_changes")]
		public static extern int Changes (IntPtr db);

		[DllImport("sqlite3", EntryPoint = "sqlite3_prepare_v2")]
		public static extern Result Prepare (IntPtr db, string sql, int numBytes, out IntPtr stmt, IntPtr pzTail);
		public static IntPtr Prepare (IntPtr db, string query)
		{
			IntPtr stmt;
			if (Prepare (db, query, query.Length, out stmt, IntPtr.Zero) != Result.OK)
				throw new SQLiteException (Errmsg (db));
			return stmt;
		}

		[DllImport("sqlite3", EntryPoint = "sqlite3_step")]
		public static extern Result Step (IntPtr stmt);

		[DllImport("sqlite3", EntryPoint = "sqlite3_finalize")]
		public static extern Result Finalize (IntPtr stmt);

		[DllImport("sqlite3", EntryPoint = "sqlite3_errmsg")]
		public static extern string Errmsg (IntPtr db);

		[DllImport("sqlite3", EntryPoint = "sqlite3_bind_parameter_index")]
		public static extern int BindParameterIndex (IntPtr stmt, string name);

		[DllImport("sqlite3", EntryPoint = "sqlite3_bind_null")]
		public static extern int BindNull (IntPtr stmt, int index);
		[DllImport("sqlite3", EntryPoint = "sqlite3_bind_int")]
		public static extern int BindInt (IntPtr stmt, int index, int val);
		[DllImport("sqlite3", EntryPoint = "sqlite3_bind_int64")]
		public static extern int BindInt64 (IntPtr stmt, int index, long val);
		[DllImport("sqlite3", EntryPoint = "sqlite3_bind_double")]
		public static extern int BindDouble (IntPtr stmt, int index, double val);
		[DllImport("sqlite3", EntryPoint = "sqlite3_bind_text")]
		public static extern int BindText (IntPtr stmt, int index, string val, int n, IntPtr free);

		[DllImport("sqlite3", EntryPoint = "sqlite3_column_count")]
		public static extern int ColumnCount (IntPtr stmt);
		[DllImport("sqlite3", EntryPoint = "sqlite3_column_name")]
		public static extern string ColumnName (IntPtr stmt, int index);
		[DllImport("sqlite3", EntryPoint = "sqlite3_column_type")]
		public static extern ColType ColumnType (IntPtr stmt, int index);
		[DllImport("sqlite3", EntryPoint = "sqlite3_column_int")]
		public static extern int ColumnInt (IntPtr stmt, int index);
		[DllImport("sqlite3", EntryPoint = "sqlite3_column_int64")]
		public static extern long ColumnInt64 (IntPtr stmt, int index);
		[DllImport("sqlite3", EntryPoint = "sqlite3_column_double")]
		public static extern double ColumnDouble (IntPtr stmt, int index);
		[DllImport("sqlite3", EntryPoint = "sqlite3_column_text")]
		public static extern string ColumnText (IntPtr stmt, int index);

		public enum ColType : int
		{
			Integer = 1,
			Float = 2,
			Text = 3,
			Blob = 4,
			Null = 5
		}
	}
}
