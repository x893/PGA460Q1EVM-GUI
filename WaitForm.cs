using System;
using System.ComponentModel;
using System.Drawing;
using System.Threading;
using System.Windows.Forms;

namespace TI.eLAB.EVM
{
	public partial class WaitForm : Form
	{
		public WaitForm()
		{
			InitializeComponent();
		}

		protected void ShowWaitForm(string message)
		{
			if (_waitForm == null || _waitForm.IsDisposed)
			{
				_waitForm = new WaitForm();
				_waitForm.TopMost = true;
				_waitForm.StartPosition = FormStartPosition.CenterScreen;
				_waitForm.Show();
				_waitForm.Refresh();
				Thread.Sleep(700);
				Application.Idle += OnLoaded;
			}
		}

		private void OnLoaded(object sender, EventArgs e)
		{
			Application.Idle -= OnLoaded;
			_waitForm.Close();
		}

		private WaitForm _waitForm;
	}
}
