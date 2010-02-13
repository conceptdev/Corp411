using System;
using MonoTouch.Foundation;

namespace CorporateDirectory1
{
	[Preserve(AllMembers=true)]
	public class Employee
	{
		public Employee ()
		{}
		//public int Id {get;set;}
		public string Firstname {get;set;}
		public string Lastname {get;set;}
		public string Department {get;set;}
		public string Work {get;set;}
		public string Mobile {get;set;}
		public string Email {get;set;}
	}
}
