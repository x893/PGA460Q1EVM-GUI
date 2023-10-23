using System;
using System.Globalization;
using System.Windows.Forms;

namespace TI.eLAB.EVM
{
	internal static class Program
	{
		[STAThread]
		private static void Main()
		{
			Application.EnableVisualStyles();
			Application.CurrentCulture = CultureInfo.CreateSpecificCulture("en-US");
			Application.SetCompatibleTextRenderingDefault(false);
			Application.Run(new MainForm(0));
		}
	}
}
