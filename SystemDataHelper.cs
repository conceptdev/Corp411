
using System;
using System.Data;
using Mono.Data.Sqlite;
using System.IO;

namespace CorporateDirectory1
{
	public class SystemDataHelper
	{
		string _path;
		public SystemDataHelper (string path)
		{
			_path = path;
		}
		
		public SqliteConnection GetConnection()  
	    {  
	        //var documents = Environment.GetFolderPath (Environment.SpecialFolder.Personal);  
	        //string db = Path.Combine (documents, "mydb.db3");  
			string db = _path;
	        bool exists = File.Exists (db);  
	        var conn = new SqliteConnection("Data Source=" + db);  
	        return conn;  
	    }  
	}
}
