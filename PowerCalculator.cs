using System;
using System.ComponentModel;
using System.Drawing;
using System.Windows.Forms;
using TIger_A;

namespace TI.eLAB.EVM
{
	public partial class PowerCalculator : Form
	{
		public PowerCalculator()
		{
			InitializeComponent();
			pb_limit.SelectedIndex = 7;
			pb_freq.SelectedIndex = 50;
			pb_pulses.SelectedIndex = 10;
			pb_rec.SelectedIndex = 1;
			pb_vpwr.SelectedIndex = 3;
			cmdIntTextbox.Text = "500";
			pb_lpmt.SelectedIndex = 0;
			pb_idlet.SelectedIndex = 0;
		}

		private void button1_Click(object sender, EventArgs e)
		{
			if (double.Parse(cmdIntTextbox.Text) < double.Parse(pb_rec.Text))
			{
				MessageBox.Show("Command Interval cannot be shorter than Record Length!");
			}
			else
			{
				out_actburst.Text = Convert.ToString((pb_limit.SelectedIndex * 8 + 50) / 2);
				out_actburstTime.Text = Tools.Double_to_string(1.0 / ((double)pb_freq.SelectedIndex * 0.2 + 30.0) * (double)pb_pulses.SelectedIndex, 3);
				double num = 0.0;
				double input_d;
				if (pb_lpbc.Checked)
				{
					input_d = 0.3;
					switch (pb_lpmt.SelectedIndex)
					{
					case 0:
						num = 250.0;
						break;
					case 1:
						num = 500.0;
						break;
					case 2:
						num = 1000.0;
						break;
					case 3:
						num = 4000.0;
						break;
					}
					if (pb_disc.Checked)
					{
						if (double.Parse(pb_dist.Text) - double.Parse(pb_lpmt.Text) * 1000.0 < 0.0)
						{
							MessageBox.Show("Cannot cut power before entering Low Power Mode!\r\nUncheck 'Low Bower Mode Enabled', or increase time to cut power.");
							pb_dist.Text = Tools.Double_to_string(double.Parse(pb_lpmt.Text) * 1000.0, 3);
						}
						else
						{
							double num2 = double.Parse(pb_dist.Text) - num;
						}
					}
				}
				else if (pb_disc.Checked)
				{
					input_d = 0.0;
					num = double.Parse(pb_dist.Text);
				}
				else if (pb_idlec.Checked)
				{
					input_d = 7.0;
					switch (pb_idlet.SelectedIndex)
					{
					case 0:
						num = 2.5;
						break;
					case 1:
						num = 7.5;
						break;
					case 2:
						num = 5.0;
						break;
					case 3:
						num = 10.0;
						break;
					}
				}
				else
					input_d = 12.0;

				out_actlis.Text = "12";
				out_actlisTime.Text = Tools.Double_to_string(double.Parse(pb_rec.Text) - double.Parse(out_actburstTime.Text) + num, 3);
				out_inactive.Text = Tools.Double_to_string(input_d, 3);
				if (double.Parse(cmdIntTextbox.Text) - double.Parse(out_actlisTime.Text) < 0.0)
					out_inactiveTime.Text = "0";
				else if (pb_disc.Checked && pb_lpbc.Checked)
				{
					out_inactiveTime.Text = Tools.Double_to_string(double.Parse(pb_dist.Text) - double.Parse(out_actlisTime.Text) - double.Parse(out_actburstTime.Text), 3);
					if (double.Parse(out_inactiveTime.Text) < 0.0)
						out_inactiveTime.Text = "0";
				}
				else
					out_inactiveTime.Text = Tools.Double_to_string(double.Parse(cmdIntTextbox.Text) - double.Parse(out_actlisTime.Text) - double.Parse(out_actburstTime.Text), 3);

				avgCurrent.Text = Tools.Double_to_string((double.Parse(out_actburst.Text) * double.Parse(out_actburstTime.Text) + double.Parse(out_actlis.Text) * double.Parse(out_actlisTime.Text) + double.Parse(out_inactive.Text) * double.Parse(out_inactiveTime.Text)) / double.Parse(cmdIntTextbox.Text), 3);
				avgPower.Text = Tools.Double_to_string(double.Parse(avgCurrent.Text) * double.Parse(pb_vpwr.Text), 3);
			}
		}

		private void pb_lpbc_CheckedChanged(object sender, EventArgs e)
		{
			if (pb_lpbc.Checked)
				pb_idlec.Checked = false;
		}

		private void pb_idlec_CheckedChanged(object sender, EventArgs e)
		{
			if (pb_idlec.Checked)
				pb_lpbc.Checked = false;
		}

		private void onlyNumbers(object sender, KeyPressEventArgs e)
		{
			e.Handled = (!char.IsDigit(e.KeyChar) && !char.IsControl(e.KeyChar));
		}

		private void pb_dist_TextChanged(object sender, EventArgs e)
		{
			try
			{
				if (double.Parse(pb_dist.Text) > double.Parse(cmdIntTextbox.Text))
				{
					MessageBox.Show("Command Interval cannot be shorter than Cut Power time!");
					pb_dist.Text = cmdIntTextbox.Text;
				}
			}
			catch
			{
			}
		}
	}
}
