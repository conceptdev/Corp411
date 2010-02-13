
using System;
using System.Collections.Generic;
using System.Linq;
using MonoTouch.Foundation;
using MonoTouch.UIKit;

namespace CorporateDirectory1
{
	public partial class CustomCellController : UIViewController
	{
		#region Constructors

		// The IntPtr and NSCoder constructors are required for controllers that need 
		// to be able to be created from a xib rather than from managed code

		public CustomCellController (IntPtr handle) : base(handle)
		{
			Initialize ();
		}

		[Export("initWithCoder:")]
		public CustomCellController (NSCoder coder) : base(coder)
		{
			Initialize ();
		}

		public CustomCellController ()
		{
			Initialize ();
		}

		void Initialize ()
		{
		}

		#endregion

		public string Name
		{
		get { return labelName.Text; }
		set { labelName.Text = value; }
		}
		public string Department
		{
		get { return labelDepartment.Text; }
		set { labelDepartment.Text = value; }
		}
		public UITableViewCell Cell
		{
		get { return cell; }
		}

	}
}
