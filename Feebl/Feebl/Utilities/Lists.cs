namespace Feebl.Utilities
{
	public class Lists
	{
		public static string[] Directions = { "up", "left", "down", "right" };
		public static string[] Days = { "Sun", "Mon", "Tue", "Wed", "Thu", "Fri", "Sat" };

		public enum Status
		{
			Okay = 0,
			Warning = 500,
			Error = 900
		}

	  public enum Priority
	  {
      Trivial = -200, // NO SMS, NO REDMINE, NO PUSH TO FRONTPAGE
	    Low = -100, // YES SMS, NO REDMINE, NO PUSH TO FRONTPAGE
	    Normal = 0, // YES SMS, YES REDMINE
	    High = 100 // YES SMS, YES REDMINE (DIRECTLY, APPEND WITH URGENT PRIORITY DESCRIPTION)
	  }
	}
}