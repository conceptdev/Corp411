using System;
using System.Collections.Generic;
using System.Linq;
using MonoTouch.Foundation;
using MonoTouch.UIKit;
using SQLiteClient;

namespace CorporateDirectory1
{
	/// <summary>
	/// Example using SQLite and a custom cell XIB 
	/// </summary>
	public class Application
	{
		static void Main (string[] args)
		{
			try 
			{
				UIApplication.Main (args);
			} 
			catch (Exception ex)
			{
				Console.WriteLine(ex);
			}
		}
	}

	// The name AppDelegate is referenced in the MainWindow.xib file.
	public partial class AppDelegate : UIApplicationDelegate
	{		
		static NSString kCellIdentifier = new NSString ("employeeCell");
	
		private List<Employee> listData;
		
		public int ListCount {get{return listData.Count();}}
		
		public List<Employee> Employees {get{return listData;}}
		
		public Employee SelectedEmployee = null;
		
		// This method is invoked when the application has loaded its UI and its ready to run
		public override bool FinishedLaunching (UIApplication app, NSDictionary options)
		{
			tableviewEmployee.DataSource = new EmployeeDataSource(this);
			tableviewEmployee.Delegate = new EmployeeListDelegate(this);
			
#if ORM
			using (var db = new SQLiteClient.SQLiteConnection("phonebook")) {
			    db.Open();
	
				// Perform strongly typed queries
			    var users = db.Query<Employee>("SELECT Firstname, Lastname, Work, Mobile, Department, Email FROM Phonebook ORDER BY Lastname", 1000);
	
				listData = users.ToList();
			}
#else
			listData = new List<Employee>();
			// System.Data test code from http://monotouch.net/Documentation/System.Data
			var sd = new SystemDataHelper("phonebook");
			var connection = sd.GetConnection();
			using (var cmd = connection.CreateCommand())
			{
				connection.Open ();  
	            cmd.CommandText = "SELECT Firstname, Lastname, Work, Mobile, Department, Email " +
	            	"FROM Phonebook ORDER BY Lastname";  

				using (var reader = cmd.ExecuteReader ()) 
				{   
                		while (reader.Read ()) 
					{  
						var emp = new Employee();
						emp.Firstname = (string)reader["Firstname"]; 
						emp.Lastname = (string)reader["Lastname"]; 
						emp.Work = (string)reader["Work"];
						emp.Mobile = (string)reader["Mobile"]; 
						emp.Department = (string)reader["Department"]; 
						emp.Email = (string)reader["Email"];
						Console.WriteLine("Column {0}",reader["Lastname"]);
	                    listData.Add(emp);
	                }  
	            } 
			}
#endif			
			window.MakeKeyAndVisible ();
			return true;
		}
		
		/// <summary>
		/// Show alert to initiate call (from Delegate.RowSelected)
		/// </summary>
		void DialogCall (string name, string work, string mobile, string email)
		{
			using (var alert = new UIAlertView ("Make Contact"
				                                    , "Call or email " + name + " now?"
				                                    , new CallAlert (this)
				                                    , "Cancel"
				                                    , work
				                           			, mobile
			                                    , email))
				{
			       alert.Show ();
				}
		}
		// This method is required in iPhoneOS 3.0
		public override void OnActivated (UIApplication application)
		{
		}
		
		/// <summary>
		/// Uses OpenUrl to trigger an action on the phone (if supported)
		/// </summary>
		public class CallAlert : UIAlertViewDelegate
		{
			private AppDelegate _appd;
			public CallAlert (AppDelegate appd)
			{
				_appd = appd;
			}
			public override void Clicked (UIAlertView alertview, int buttonIndex)
			{
				Console.WriteLine("Clicked " + buttonIndex);
				NSUrl u=null;
				if (buttonIndex == 1)
				{
					Console.WriteLine("tel:" + _appd.SelectedEmployee.Work);
					u = new NSUrl("tel:" + _appd.SelectedEmployee.Work);
				} 
				else if (buttonIndex == 2)
				{
					Console.WriteLine("tel:" + _appd.SelectedEmployee.Mobile);
					u = new NSUrl("tel:" + _appd.SelectedEmployee.Mobile);
				}
				else if (buttonIndex == 3)
				{
					Console.WriteLine("mailto:" + _appd.SelectedEmployee.Email);
					u = new NSUrl("mailto:" + _appd.SelectedEmployee.Email);
				}
				if (u != null)
				{
					if (!UIApplication.SharedApplication.OpenUrl(u))
					{
						Console.WriteLine("Not Supported");
						NotSupportedAlert(u.Scheme);
					}
				}
			}
			private void NotSupportedAlert(string scheme)
			{	
				var av = new UIAlertView("Not supported"
			                         , "Scheme '"+scheme+"' is not supported on this device"
			                         , null
			                         , "k thanks"
			                         , null);
				av.Show();
	     	}
		}
		/// <summary>
		/// Links the UITableView to the Employees collection
		/// on the AppDelegate instance passed in
		/// </summary>
		public class EmployeeDataSource : UITableViewDataSource
		{
			private AppDelegate _appd;
			public EmployeeDataSource (AppDelegate appd)
			{
				_appd = appd;
				controllers = new Dictionary<int, CustomCellController>();
			}
			public override int RowsInSection (UITableView tableview, int section)
			{
				return _appd.ListCount;
			}
			
			private Dictionary<int, CustomCellController> controllers = null;
			/// <summary>
			/// Thanks to Simon http://simon.nureality.ca/?p=91
			/// for the two-row cell sample
			/// </summary>
			public override UITableViewCell GetCell (UITableView tableView, NSIndexPath indexPath)
			{
				var cell = tableView.DequeueReusableCell (kCellIdentifier);
				CustomCellController cellController = null;
				int row = indexPath.Row;
				Console.WriteLine(row);
				if (cell == null)
				{
					cellController = new CustomCellController();
					NSBundle.MainBundle.LoadNib("CustomCellController", cellController, null);
					Console.WriteLine(cellController == null);
					cell = cellController.Cell;
					cell.Tag = row; // choose something unique here!
					//cell.Accessory = UITableViewCellAccessory.DetailDisclosureButton;
					controllers.Add(cell.Tag, cellController); // otherwise you get "An element with the same key already exists in the dictionary" here!
				}
				else
				{
					cellController = controllers[cell.Tag];
				}
 				Employee e = _appd.Employees[row];
				cellController.Name = e.Firstname + " " + e.Lastname;
				cellController.Department = e.Department;
				return cell;
			}
		}
		
		/// <summary>
		/// Delegate for UITableView - handles RowSelected
		/// </summary>
		public class EmployeeListDelegate : UITableViewDelegate
		{
			AppDelegate _appd;
			public EmployeeListDelegate (AppDelegate appd)
			{
				_appd = appd;
			}
			public override void RowSelected (UITableView tableView, NSIndexPath indexPath)
			{
				int row = indexPath.Row;
					Employee e = _appd.Employees[row];
				string rowValue = e.Firstname + " " + e.Lastname;
				//Console.WriteLine("selected " + rowValue);
				_appd.SelectedEmployee = e;
				_appd.DialogCall(rowValue, e.Work.ToString(), e.Mobile.ToString(), e.Email);
				tableView.DeselectRow(indexPath,true);
			}
		}
	}
}