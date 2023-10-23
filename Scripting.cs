using System;
using System.ComponentModel;
using System.Drawing;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace TI.eLAB.EVM
{
	public partial class Scripting : Form
	{
		public Scripting(MainForm parentForm1)
		{
			InitializeComponent();
			_parentForm = parentForm1;
		}

		private void script_run_btn_Click(object sender, EventArgs e)
		{
			abortFlag = false;
			if (script_input.Text == "" || !script_input.Text.Contains(";"))
				script_status.Text = "\n\rInvalid input...\n\r";
			else
				run_script();
		}

		private async void run_script()
		{
			for (int i = 0; i < int.Parse(script_loop_count.Text); i++)
			{
				char[] delimiterChars = new char[] { ';' };
				string script_inputClean = script_input.Text.Replace("\n", "").Replace("\r", "").Replace(" ", "").Replace("\t", "");
				Regex regex = new Regex(string.Format("\\{0}.*?\\{1}", '[', ']'));
				script_inputClean = regex.Replace(script_inputClean, string.Empty);
				string[] inputParsed = script_inputClean.Split(delimiterChars);
				int parseCount = inputParsed.Length;
				while (!abortFlag)
				{
					for (int j = 0; j < parseCount - 1; j++)
					{
						if (!abortFlag)
						{
							try
							{
								string inp = inputParsed[j].Substring(0, inputParsed[j].IndexOf("("));
								string val = inputParsed[j].Split(new char[] { '(', ')' })[1];
								string text = inp;
								switch (text)
								{
									case "kbenter":
										SendKeys.Send("{ENTER}");
										script_status.AppendText(string.Concat(new object[] { "\n", j, "-P ", inputParsed[j] }));
										break;
									case "scripttimeout":
										await Task.Delay(Convert.ToInt32(val));
										script_status.AppendText(string.Concat(new object[] { "\n", j, "-P ", inputParsed[j] }));
										break;
									case "commandint":
										comIntTime = Convert.ToInt32(val);
										script_status.AppendText(string.Concat(new object[]
										{
										"\n",
										j,
										"-P ",
										inputParsed[j]
										}));
										break;
									case "togglepauseresumescript":
										script_pause_btn_Click(null, null);
										script_status.AppendText(string.Concat(new object[]
										{
										"\n",
										j,
										"-P ",
										inputParsed[j]
										}));
										break;
									case "scripttimestampint":
										timestampint = Convert.ToInt32(val);
										script_status.AppendText(string.Concat(new object[]
										{
										"\n",
										j,
										"-P ",
										inputParsed[j]
										}));
										break;
									case "ds1led":
										common.u2a.GPIO_SetPort(10, 1);
										if (val == "1")
										{
											common.u2a.GPIO_WritePort(10, 2);
										}
										else
										{
											common.u2a.GPIO_WritePort(10, 1);
										}
										script_status.AppendText(string.Concat(new object[]
										{
										"\n",
										j,
										"-P ",
										inputParsed[j]
										}));
										break;
									case "fdled":
										common.u2a.GPIO_SetPort(10, 1);
										if (val == "1")
										{
											common.u2a.GPIO_WritePort(11, 2);
										}
										else
										{
											common.u2a.GPIO_WritePort(11, 1);
										}
										script_status.AppendText(string.Concat(new object[]
										{
										"\n",
										j,
										"-P ",
										inputParsed[j]
										}));
										break;
									case "vdled":
										common.u2a.GPIO_SetPort(10, 1);
										if (val == "1")
										{
											common.u2a.GPIO_WritePort(12, 2);
										}
										else
										{
											common.u2a.GPIO_WritePort(12, 1);
										}
										script_status.AppendText(string.Concat(new object[]
										{
										"\n",
										j,
										"-P ",
										inputParsed[j]
										}));
										break;
									case "drfreq":
										_parentForm.freqCombo.Text = val;
										script_status.AppendText(string.Concat(new object[]
										{
										"\n",
										j,
										"-P ",
										inputParsed[j]
										}));
										break;
									case "afgr":
										_parentForm.AFEGainRngCombo.Text = val;
										script_status.AppendText(string.Concat(new object[]
										{
										"\n",
										j,
										"-P ",
										inputParsed[j]
										}));
										break;
									case "gainCombo":
										_parentForm.gainCombo.Text = val;
										script_status.AppendText(string.Concat(new object[]
										{
										"\n",
										j,
										"-P ",
										inputParsed[j]
										}));
										break;
									case "bpbw":
										_parentForm.bpbwCombo.Text = val;
										script_status.AppendText(string.Concat(new object[]
										{
										"\n",
										j,
										"-P ",
										inputParsed[j]
										}));
										break;
									case "cutoff":
										_parentForm.cutoffCombo.Text = val;
										script_status.AppendText(string.Concat(new object[]
										{
										"\n",
										j,
										"-P ",
										inputParsed[j]
										}));
										break;
									case "deglitch":
										_parentForm.thrCmpDeglitchCombo.Text = val;
										script_status.AppendText(string.Concat(new object[]
										{
										"\n",
										j,
										"-P ",
										inputParsed[j]
										}));
										break;
									case "p1bp":
										_parentForm.p1PulsesCombo.Text = val;
										script_status.AppendText(string.Concat(new object[]
										{
										"\n",
										j,
										"-P ",
										inputParsed[j]
										}));
										break;
									case "p1dcl":
										_parentForm.p1DriveCombo.Text = val;
										script_status.AppendText(string.Concat(new object[]
										{
										"\n",
										j,
										"-P ",
										inputParsed[j]
										}));
										break;
									case "p1rtl":
										_parentForm.p1RecordCombo.Text = val;
										script_status.AppendText(string.Concat(new object[]
										{
										"\n",
										j,
										"-P ",
										inputParsed[j]
										}));
										break;
									case "p2bp":
										_parentForm.p2PulsesCombo.Text = val;
										script_status.AppendText(string.Concat(new object[]
										{
										"\n",
										j,
										"-P ",
										inputParsed[j]
										}));
										break;
									case "p2dcl":
										_parentForm.p2DriveCombo.Text = val;
										script_status.AppendText(string.Concat(new object[]
										{
										"\n",
										j,
										"-P ",
										inputParsed[j]
										}));
										break;
									case "p2rtl":
										_parentForm.p2RecordCombo.Text = val;
										script_status.AppendText(string.Concat(new object[]
										{
										"\n",
										j,
										"-P ",
										inputParsed[j]
										}));
										break;
									case "discl":
										if (val == "1")
										{
											_parentForm.disableCurrentLimitBox.Checked = true;
										}
										else
										{
											_parentForm.disableCurrentLimitBox.Checked = false;
										}
										script_status.AppendText(string.Concat(new object[]
										{
										"\n",
										j,
										"-P ",
										inputParsed[j]
										}));
										break;
									case "nlsnl":
										_parentForm.nlsNoiseCombo.Text = val;
										script_status.AppendText(string.Concat(new object[]
										{
										"\n",
										j,
										"-P ",
										inputParsed[j]
										}));
										break;
									case "nlssl":
										_parentForm.nlsSECombo.Text = val;
										script_status.AppendText(string.Concat(new object[]
										{
										"\n",
										j,
										"-P ",
										inputParsed[j]
										}));
										break;
									case "nlstop":
										_parentForm.nlsTOPCombo.Text = val;
										script_status.AppendText(string.Concat(new object[]
										{
										"\n",
										j,
										"-P ",
										inputParsed[j]
										}));
										break;
									case "p1nlsen":
										if (val == "1")
										{
											_parentForm.p1NLSEnBox.Checked = true;
										}
										else
										{
											_parentForm.p1NLSEnBox.Checked = false;
										}
										script_status.AppendText(string.Concat(new object[]
										{
										"\n",
										j,
										"-P ",
										inputParsed[j]
										}));
										break;
									case "p2nlsen":
										if (val == "1")
										{
											_parentForm.p2NLSEnBox.Checked = true;
										}
										else
										{
											_parentForm.p2NLSEnBox.Checked = false;
										}
										script_status.AppendText(string.Concat(new object[]
										{
										"\n",
										j,
										"-P ",
										inputParsed[j]
										}));
										break;
									case "entimedec":
										if (val == "1")
										{
											_parentForm.decoupletimeRadio.Checked = true;
										}
										else
										{
											_parentForm.decoupletimeRadio.Checked = false;
										}
										script_status.AppendText(string.Concat(new object[]
										{
										"\n",
										j,
										"-P ",
										inputParsed[j]
										}));
										break;
									case "timedec":
										_parentForm.decoupletimeBox.Text = val;
										script_status.AppendText(string.Concat(new object[]
										{
										"\n",
										j,
										"-P ",
										inputParsed[j]
										}));
										break;
									case "entempdec":
										if (val == "1")
										{
											_parentForm.decoupletempRadio.Checked = true;
										}
										else
										{
											_parentForm.decoupletempRadio.Checked = false;
										}
										script_status.AppendText(string.Concat(new object[]
										{
										"\n",
										j,
										"-P ",
										inputParsed[j]
										}));
										break;
									case "tempdec":
										_parentForm.decoupletempBox.Text = val;
										script_status.AppendText(string.Concat(new object[]
										{
										"\n",
										j,
										"-P ",
										inputParsed[j]
										}));
										break;
									case "p1dgsr":
										_parentForm.p1DigGainSr.Text = val;
										script_status.AppendText(string.Concat(new object[]
										{
										"\n",
										j,
										"-P ",
										inputParsed[j]
										}));
										break;
									case "p1dglr":
										_parentForm.p1DigGainLr.Text = val;
										script_status.AppendText(string.Concat(new object[]
										{
										"\n",
										j,
										"-P ",
										inputParsed[j]
										}));
										break;
									case "p1dglrst":
										_parentForm.p1DigGainLrSt.Text = val;
										script_status.AppendText(string.Concat(new object[]
										{
										"\n",
										j,
										"-P ",
										inputParsed[j]
										}));
										break;
									case "p2dgsr":
										_parentForm.p2DigGainSr.Text = val;
										script_status.AppendText(string.Concat(new object[]
										{
										"\n",
										j,
										"-P ",
										inputParsed[j]
										}));
										break;
									case "p2dglr":
										_parentForm.p2DigGainLr.Text = val;
										script_status.AppendText(string.Concat(new object[]
										{
										"\n",
										j,
										"-P ",
										inputParsed[j]
										}));
										break;
									case "p2dglrst":
										_parentForm.p2DigGainLrSt.Text = val;
										script_status.AppendText(string.Concat(new object[]
										{
										"\n",
										j,
										"-P ",
										inputParsed[j]
										}));
										break;
									case "xfapdft":
										if (val == "1")
										{
											_parentForm.defaultAllGeneralBtn_Click(null, null);
										}
										script_status.AppendText(string.Concat(new object[]
										{
										"\n",
										j,
										"-P ",
										inputParsed[j]
										}));
										break;
									case "xfipdft":
										if (val == "1")
										{
											_parentForm.ISOClosedDefaultsBtn_Click(null, null);
										}
										script_status.AppendText(string.Concat(new object[]
										{
										"\n",
										j,
										"-P ",
										inputParsed[j]
										}));
										break;
									case "ddapdft":
										if (val == "1")
										{
											_parentForm.defaultAllGeneralDDBtn_Click(null, null);
										}
										script_status.AppendText(string.Concat(new object[]
										{
										"\n",
										j,
										"-P ",
										inputParsed[j]
										}));
										break;
									case "ddipdft":
										if (val == "1")
										{
											_parentForm.ISOOpenDefaultsBtn_Click(null, null);
										}
										script_status.AppendText(string.Concat(new object[]
										{
										"\n",
										j,
										"-P ",
										inputParsed[j]
										}));
										break;
									case "xfbddft":
										if (val == "1")
										{
											_parentForm.dftBDBtn_Click(null, null);
										}
										script_status.AppendText(string.Concat(new object[]
										{
										"\n",
										j,
										"-P ",
										inputParsed[j]
										}));
										break;
									case "ddbddft":
										if (val == "1")
										{
											_parentForm.dftBDDDBtn_Click(null, null);
										}
										script_status.AppendText(string.Concat(new object[]
										{
										"\n",
										j,
										"-P ",
										inputParsed[j]
										}));
										break;
									case "p1t1":
										_parentForm.p1t1.Text = val;
										script_status.AppendText(string.Concat(new object[] { "\n", j, "-P ", inputParsed[j] }));
										break;
									case "p1t2":
										_parentForm.p1t2.Text = val;
										script_status.AppendText(string.Concat(new object[] { "\n", j, "-P ", inputParsed[j] }));
										break;
									case "p1t3":
										_parentForm.p1t3.Text = val;
										script_status.AppendText(string.Concat(new object[] { "\n", j, "-P ", inputParsed[j] }));
										break;
									case "p1t4":
										_parentForm.p1t4.Text = val;
										script_status.AppendText(string.Concat(new object[] { "\n", j, "-P ", inputParsed[j] }));
										break;
									case "p1t5":
										_parentForm.p1t5.Text = val;
										script_status.AppendText(string.Concat(new object[] { "\n", j, "-P ", inputParsed[j] }));
										break;
									case "p1t6":
										_parentForm.p1t6.Text = val;
										script_status.AppendText(string.Concat(new object[] { "\n", j, "-P ", inputParsed[j] }));
										break;
									case "p1t7":
										_parentForm.p1t7.Text = val;
										script_status.AppendText(string.Concat(new object[] { "\n", j, "-P ", inputParsed[j] }));
										break;
									case "p1t8":
										_parentForm.p1t8.Text = val;
										script_status.AppendText(string.Concat(new object[]
										{
										"\n",
										j,
										"-P ",
										inputParsed[j]
										}));
										break;
									case "p1t9":
										_parentForm.p1t9.Text = val;
										script_status.AppendText(string.Concat(new object[]
										{
										"\n",
										j,
										"-P ",
										inputParsed[j]
										}));
										break;
									case "p1t10":
										_parentForm.p1t10.Text = val;
										script_status.AppendText(string.Concat(new object[]
										{
										"\n",
										j,
										"-P ",
										inputParsed[j]
										}));
										break;
									case "p1t11":
										_parentForm.p1t11.Text = val;
										script_status.AppendText(string.Concat(new object[]
										{
										"\n",
										j,
										"-P ",
										inputParsed[j]
										}));
										break;
									case "p1t12":
										_parentForm.p1t12.Text = val;
										script_status.AppendText(string.Concat(new object[]
										{
										"\n",
										j,
										"-P ",
										inputParsed[j]
										}));
										break;
									case "p1l1":
										_parentForm.p1l1.Text = val;
										script_status.AppendText(string.Concat(new object[]
										{
										"\n",
										j,
										"-P ",
										inputParsed[j]
										}));
										break;
									case "p1l2":
										_parentForm.p1l2.Text = val;
										script_status.AppendText(string.Concat(new object[]
										{
										"\n",
										j,
										"-P ",
										inputParsed[j]
										}));
										break;
									case "p1l3":
										_parentForm.p1l3.Text = val;
										script_status.AppendText(string.Concat(new object[]
										{
										"\n",
										j,
										"-P ",
										inputParsed[j]
										}));
										break;
									case "p1l4":
										_parentForm.p1l4.Text = val;
										script_status.AppendText(string.Concat(new object[]
										{
										"\n",
										j,
										"-P ",
										inputParsed[j]
										}));
										break;
									case "p1l5":
										_parentForm.p1l5.Text = val;
										script_status.AppendText(string.Concat(new object[]
										{
										"\n",
										j,
										"-P ",
										inputParsed[j]
										}));
										break;
									case "p1l6":
										_parentForm.p1l6.Text = val;
										script_status.AppendText(string.Concat(new object[]
										{
										"\n",
										j,
										"-P ",
										inputParsed[j]
										}));
										break;
									case "p1l7":
										_parentForm.p1l7.Text = val;
										script_status.AppendText(string.Concat(new object[]
										{
										"\n",
										j,
										"-P ",
										inputParsed[j]
										}));
										break;
									case "p1l8":
										_parentForm.p1l8.Text = val;
										script_status.AppendText(string.Concat(new object[]
										{
										"\n",
										j,
										"-P ",
										inputParsed[j]
										}));
										break;
									case "p1l9":
										_parentForm.p1l9.Text = val;
										script_status.AppendText(string.Concat(new object[]
										{
										"\n",
										j,
										"-P ",
										inputParsed[j]
										}));
										break;
									case "p1l10":
										_parentForm.p1l10.Text = val;
										script_status.AppendText(string.Concat(new object[]
										{
										"\n",
										j,
										"-P ",
										inputParsed[j]
										}));
										break;
									case "p1l11":
										_parentForm.p1l11.Text = val;
										script_status.AppendText(string.Concat(new object[]
										{
										"\n",
										j,
										"-P ",
										inputParsed[j]
										}));
										break;
									case "p1l12":
										_parentForm.p1l12.Text = val;
										script_status.AppendText(string.Concat(new object[]
										{
										"\n",
										j,
										"-P ",
										inputParsed[j]
										}));
										break;
									case "p1lOff":
										_parentForm.p1lOff.Text = val;
										script_status.AppendText(string.Concat(new object[]
										{
										"\n",
										j,
										"-P ",
										inputParsed[j]
										}));
										break;
									case "p2t1":
										_parentForm.p2t1.Text = val;
										script_status.AppendText(string.Concat(new object[]
										{
										"\n",
										j,
										"-P ",
										inputParsed[j]
										}));
										break;
									case "p2t2":
										_parentForm.p2t2.Text = val;
										script_status.AppendText(string.Concat(new object[]
										{
										"\n",
										j,
										"-P ",
										inputParsed[j]
										}));
										break;
									case "p2t3":
										_parentForm.p2t3.Text = val;
										script_status.AppendText(string.Concat(new object[]
										{
										"\n",
										j,
										"-P ",
										inputParsed[j]
										}));
										break;
									case "p2t4":
										_parentForm.p2t4.Text = val;
										script_status.AppendText(string.Concat(new object[]
										{
										"\n",
										j,
										"-P ",
										inputParsed[j]
										}));
										break;
									case "p2t5":
										_parentForm.p2t5.Text = val;
										script_status.AppendText(string.Concat(new object[]
										{
										"\n",
										j,
										"-P ",
										inputParsed[j]
										}));
										break;
									case "p2t6":
										_parentForm.p2t6.Text = val;
										script_status.AppendText(string.Concat(new object[]
										{
										"\n",
										j,
										"-P ",
										inputParsed[j]
										}));
										break;
									case "p2t7":
										_parentForm.p2t7.Text = val;
										script_status.AppendText(string.Concat(new object[]
										{
										"\n",
										j,
										"-P ",
										inputParsed[j]
										}));
										break;
									case "p2t8":
										_parentForm.p2t8.Text = val;
										script_status.AppendText(string.Concat(new object[]
										{
										"\n",
										j,
										"-P ",
										inputParsed[j]
										}));
										break;
									case "p2t9":
										_parentForm.p2t9.Text = val;
										script_status.AppendText(string.Concat(new object[]
										{
										"\n",
										j,
										"-P ",
										inputParsed[j]
										}));
										break;
									case "p2t10":
										_parentForm.p2t10.Text = val;
										script_status.AppendText(string.Concat(new object[]
										{
										"\n",
										j,
										"-P ",
										inputParsed[j]
										}));
										break;
									case "p2t11":
										_parentForm.p2t11.Text = val;
										script_status.AppendText(string.Concat(new object[]
										{
										"\n",
										j,
										"-P ",
										inputParsed[j]
										}));
										break;
									case "p2t12":
										_parentForm.p2t12.Text = val;
										script_status.AppendText(string.Concat(new object[]
										{
										"\n",
										j,
										"-P ",
										inputParsed[j]
										}));
										break;
									case "p2l1":
										_parentForm.p2l1.Text = val;
										script_status.AppendText(string.Concat(new object[]
										{
										"\n",
										j,
										"-P ",
										inputParsed[j]
										}));
										break;
									case "p2l2":
										_parentForm.p2l2.Text = val;
										script_status.AppendText(string.Concat(new object[]
										{
										"\n",
										j,
										"-P ",
										inputParsed[j]
										}));
										break;
									case "p2l3":
										_parentForm.p2l3.Text = val;
										script_status.AppendText(string.Concat(new object[] { "\n", j, "-P ", inputParsed[j] }));
										break;
									case "p2l4":
										_parentForm.p2l4.Text = val;
										script_status.AppendText(string.Concat(new object[] { "\n", j, "-P ", inputParsed[j] }));
										break;
									case "p2l5":
										_parentForm.p2l5.Text = val;
										script_status.AppendText(string.Concat(new object[] { "\n", j, "-P ", inputParsed[j] }));
										break;
									case "p2l6":
										_parentForm.p2l6.Text = val;
										script_status.AppendText(string.Concat(new object[] { "\n", j, "-P ", inputParsed[j] }));
										break;
									case "p2l7":
										_parentForm.p2l7.Text = val;
										script_status.AppendText(string.Concat(new object[]
										{
										"\n",
										j,
										"-P ",
										inputParsed[j]
										}));
										break;
									case "p2l8":
										_parentForm.p2l8.Text = val;
										script_status.AppendText(string.Concat(new object[]
										{
										"\n",
										j,
										"-P ",
										inputParsed[j]
										}));
										break;
									case "p2l9":
										_parentForm.p2l9.Text = val;
										script_status.AppendText(string.Concat(new object[]
										{
										"\n",
										j,
										"-P ",
										inputParsed[j]
										}));
										break;
									case "p2l10":
										_parentForm.p2l10.Text = val;
										script_status.AppendText(string.Concat(new object[]
										{
										"\n",
										j,
										"-P ",
										inputParsed[j]
										}));
										break;
									case "p2l11":
										_parentForm.p2l11.Text = val;
										script_status.AppendText(string.Concat(new object[]
										{
										"\n",
										j,
										"-P ",
										inputParsed[j]
										}));
										break;
									case "p2l12":
										_parentForm.p2l12.Text = val;
										script_status.AppendText(string.Concat(new object[]
										{
										"\n",
										j,
										"-P ",
										inputParsed[j]
										}));
										break;
									case "p2lOff":
										_parentForm.p2lOff.Text = val;
										script_status.AppendText(string.Concat(new object[]
										{
										"\n",
										j,
										"-P ",
										inputParsed[j]
										}));
										break;
									case "wthrinst":
										if (val == "1")
										{
											_parentForm.thrUpdateCheck.Checked = true;
										}
										else
										{
											_parentForm.thrUpdateCheck.Checked = false;
										}
										script_status.AppendText(string.Concat(new object[]
										{
										"\n",
										j,
										"-P ",
										inputParsed[j]
										}));
										break;
									case "wthrbtn":
										if (val == "1")
										{
											_parentForm.thrWriteValuesBtn_Click(null, null);
										}
										script_status.AppendText(string.Concat(new object[]
										{
										"\n",
										j,
										"-P ",
										inputParsed[j]
										}));
										break;
									case "rthrbtn":
										if (val == "1")
										{
											_parentForm.thrReadValuesBtn_Click(null, null);
										}
										script_status.AppendText(string.Concat(new object[]
										{
										"\n",
										j,
										"-P ",
										inputParsed[j]
										}));
										break;
									case "clonethrbtn":
										if (val == "1")
										{
											_parentForm.CloneWriteThrBtn_Click(null, null);
										}
										script_status.AppendText(string.Concat(new object[]
										{
										"\n",
										j,
										"-P ",
										inputParsed[j]
										}));
										break;
									case "shortthr":
										if (val == "1")
										{
											_parentForm.thrShortDBtn_Click(null, null);
										}
										script_status.AppendText(string.Concat(new object[]
										{
										"\n",
										j,
										"-P ",
										inputParsed[j]
										}));
										break;
									case "longthr":
										if (val == "1")
											_parentForm.thrLongDBtn_Click(null, null);
										script_status.AppendText(string.Concat(new object[] { "\n", j, "-P ", inputParsed[j] }));
										break;
									case "allinitlt":
										if (val == "1")
										{
											_parentForm.allL1T1Btn_Click(null, null);
										}
										script_status.AppendText(string.Concat(new object[]
										{
										"\n",
										j,
										"-P ",
										inputParsed[j]
										}));
										break;
									case "allmidthr":
										if (val == "1")
										{
											_parentForm.allMidCodeBtn_Click(null, null);
										}
										script_status.AppendText(string.Concat(new object[]
										{
										"\n",
										j,
										"-P ",
										inputParsed[j]
										}));
										break;
									case "clrthrbtn":
										if (val == "1")
											_parentForm.clearThrChartBtn_Click(null, null);
										script_status.AppendText(string.Concat(new object[] { "\n", j, "-P ", inputParsed[j] }));
										break;
									case "thrtab":
										if (val == "1")
											_parentForm.tabcontrolThr.SelectTab(0);
										else
											_parentForm.tabcontrolThr.SelectTab(1);
										script_status.AppendText(string.Concat(new object[] { "\n", j, "-P ", inputParsed[j] }));
										break;
									case "tvgt0":
										_parentForm.tvgt0.Text = val;
										script_status.AppendText(string.Concat(new object[] { "\n", j, "-P ", inputParsed[j] }));
										break;
									case "tvgg0":
										_parentForm.tvgg0.Text = val;
										script_status.AppendText(string.Concat(new object[] { "\n", j, "-P ", inputParsed[j] }));
										break;
									case "tvgt1":
										_parentForm.tvgt1.Text = val;
										script_status.AppendText(string.Concat(new object[] { "\n", j, "-P ", inputParsed[j] }));
										break;
									case "tvgg1":
										_parentForm.tvgg1.Text = val;
										script_status.AppendText(string.Concat(new object[] { "\n", j, "-P ", inputParsed[j] }));
										break;
									case "tvgt2":
										_parentForm.tvgt2.Text = val;
										script_status.AppendText(string.Concat(new object[] { "\n", j, "-P ", inputParsed[j] }));
										break;
									case "tvgg2":
										_parentForm.tvgg2.Text = val;
										script_status.AppendText(string.Concat(new object[]
										{
										"\n",
										j,
										"-P ",
										inputParsed[j]
										}));
										break;
									case "tvgt3":
										_parentForm.tvgt3.Text = val;
										script_status.AppendText(string.Concat(new object[]
										{
										"\n",
										j,
										"-P ",
										inputParsed[j]
										}));
										break;
									case "tvgg3":
										_parentForm.tvgg3.Text = val;
										script_status.AppendText(string.Concat(new object[]
										{
										"\n",
										j,
										"-P ",
										inputParsed[j]
										}));
										break;
									case "tvgt4":
										_parentForm.tvgt4.Text = val;
										script_status.AppendText(string.Concat(new object[]
										{
										"\n",
										j,
										"-P ",
										inputParsed[j]
										}));
										break;
									case "tvgg4":
										_parentForm.tvgg4.Text = val;
										script_status.AppendText(string.Concat(new object[]
										{
										"\n",
										j,
										"-P ",
										inputParsed[j]
										}));
										break;
									case "tvgt5":
										_parentForm.tvgt5.Text = val;
										script_status.AppendText(string.Concat(new object[]
										{
										"\n",
										j,
										"-P ",
										inputParsed[j]
										}));
										break;
									case "tvgg5":
										_parentForm.tvgg5.Text = val;
										script_status.AppendText(string.Concat(new object[]
										{
										"\n",
										j,
										"-P ",
										inputParsed[j]
										}));
										break;
									case "wtvginst":
										if (val == "1")
										{
											_parentForm.tvgInstantUpdateCheck.Checked = true;
										}
										else
										{
											_parentForm.tvgInstantUpdateCheck.Checked = false;
										}
										script_status.AppendText(string.Concat(new object[]
										{
										"\n",
										j,
										"-P ",
										inputParsed[j]
										}));
										break;
									case "wtvgbtn":
										if (val == "1")
										{
											_parentForm.writeTVGMemBtn_Click(null, null);
										}
										script_status.AppendText(string.Concat(new object[]
										{
										"\n",
										j,
										"-P ",
										inputParsed[j]
										}));
										break;
									case "rtvgbtn":
										if (val == "1")
										{
											_parentForm.readTVGMemBtn_Click(null, null);
										}
										script_status.AppendText(string.Concat(new object[]
										{
										"\n",
										j,
										"-P ",
										inputParsed[j]
										}));
										break;
									case "shorttvg":
										if (val == "1")
										{
											_parentForm.tvgShortDBtn_Click(null, null);
										}
										script_status.AppendText(string.Concat(new object[]
										{
										"\n",
										j,
										"-P ",
										inputParsed[j]
										}));
										break;
									case "longtvg":
										if (val == "1")
										{
											_parentForm.tvgLongDBtn_Click(null, null);
										}
										script_status.AppendText(string.Concat(new object[]
										{
										"\n",
										j,
										"-P ",
										inputParsed[j]
										}));
										break;
									case "allinitgt":
										if (val == "1")
										{
											_parentForm.allInitGainBtn_Click(null, null);
										}
										script_status.AppendText(string.Concat(new object[]
										{
										"\n",
										j,
										"-P ",
										inputParsed[j]
										}));
										break;
									case "allmidtvg":
										if (val == "1")
										{
											_parentForm.allMidCodeTVGBtn_Click(null, null);
										}
										script_status.AppendText(string.Concat(new object[]
										{
										"\n",
										j,
										"-P ",
										inputParsed[j]
										}));
										break;
									case "clrtvg":
										if (val == "1")
										{
											_parentForm.clearTVGChartBtn_Click(null, null);
										}
										script_status.AppendText(string.Concat(new object[]
										{
										"\n",
										j,
										"-P ",
										inputParsed[j]
										}));
										break;
									case "readsysdiag":
										if (val == "1")
										{
											_parentForm.readSysDiagBtn_Click(null, null);
										}
										script_status.AppendText(string.Concat(new object[]
										{
										"\n",
										j,
										"-P ",
										inputParsed[j]
										}));
										break;
									case "blsysdiag":
										if (val == "1")
										{
											_parentForm.sysdiagBLCheck.Checked = true;
										}
										else
										{
											_parentForm.sysdiagBLCheck.Checked = false;
										}
										script_status.AppendText(string.Concat(new object[]
										{
										"\n",
										j,
										"-P ",
										inputParsed[j]
										}));
										break;
									case "dlfdiag":
										if (val == "1")
										{
											_parentForm.DlXdcrFreqCheck.Checked = true;
										}
										else
										{
											_parentForm.DlXdcrFreqCheck.Checked = false;
										}
										script_status.AppendText(string.Concat(new object[]
										{
										"\n",
										j,
										"-P ",
										inputParsed[j]
										}));
										break;
									case "dlddiag":
										if (val == "1")
										{
											_parentForm.DlDecayCheck.Checked = true;
										}
										else
										{
											_parentForm.DlDecayCheck.Checked = false;
										}
										script_status.AppendText(string.Concat(new object[]
										{
										"\n",
										j,
										"-P ",
										inputParsed[j]
										}));
										break;
									case "trigtnm":
										if (val == "1")
										{
											_parentForm.trigReadTNBtn_Click(null, null);
										}
										script_status.AppendText(string.Concat(new object[]
										{
										"\n",
										j,
										"-P ",
										inputParsed[j]
										}));
										break;
									case "trigtm":
										if (val == "1")
										{
											_parentForm.tempOnlyBtn_Click(null, null);
										}
										script_status.AppendText(string.Concat(new object[]
										{
										"\n",
										j,
										"-P ",
										inputParsed[j]
										}));
										break;
									case "trignm":
										if (val == "1")
										{
											_parentForm.NoiseOnlyBtn_Click(null, null);
										}
										script_status.AppendText(string.Concat(new object[]
										{
										"\n",
										j,
										"-P ",
										inputParsed[j]
										}));
										break;
									case "dltempdiag":
										if (val == "1")
										{
											_parentForm.DlTempCheck.Checked = true;
										}
										else
										{
											_parentForm.DlTempCheck.Checked = false;
										}
										script_status.AppendText(string.Concat(new object[]
										{
										"\n",
										j,
										"-P ",
										inputParsed[j]
										}));
										break;
									case "dlnoisediag":
										if (val == "1")
										{
											_parentForm.DlNoiseCheck.Checked = true;
										}
										else
										{
											_parentForm.DlNoiseCheck.Checked = false;
										}
										script_status.AppendText(string.Concat(new object[]
										{
										"\n",
										j,
										"-P ",
										inputParsed[j]
										}));
										break;
									case "dlsdb":
										if (val == "1")
										{
											_parentForm.DlSDBCheck.Checked = true;
										}
										else
										{
											_parentForm.DlSDBCheck.Checked = false;
										}
										script_status.AppendText(string.Concat(new object[]
										{
										"\n",
										j,
										"-P ",
										inputParsed[j]
										}));
										break;
									case "tg":
										_parentForm.tempgainCombo.Text = val;
										script_status.AppendText(string.Concat(new object[]
										{
										"\n",
										j,
										"-P ",
										inputParsed[j]
										}));
										break;
									case "to":
										_parentForm.tempoffsetCombo.Text = val;
										script_status.AppendText(string.Concat(new object[]
										{
										"\n",
										j,
										"-P ",
										inputParsed[j]
										}));
										break;
									case "fdwl":
										_parentForm.freqDiagWinLengthCombo.Text = val;
										script_status.AppendText(string.Concat(new object[]
										{
										"\n",
										j,
										"-P ",
										inputParsed[j]
										}));
										break;
									case "fdst":
										_parentForm.freqDiagStartTimeCombo.Text = val;
										script_status.AppendText(string.Concat(new object[]
										{
										"\n",
										j,
										"-P ",
										inputParsed[j]
										}));
										break;
									case "fdett":
										_parentForm.freqDiagErrorTimeThrCombo.Text = val;
										script_status.AppendText(string.Concat(new object[]
										{
										"\n",
										j,
										"-P ",
										inputParsed[j]
										}));
										break;
									case "vdet":
										_parentForm.voltaDiagErrThrCombo.Text = val;
										script_status.AppendText(string.Concat(new object[]
										{
										"\n",
										j,
										"-P ",
										inputParsed[j]
										}));
										break;
									case "vdcinp":
										_parentForm.cINPBox.Text = val;
										script_status.AppendText(string.Concat(new object[]
										{
										"\n",
										j,
										"-P ",
										inputParsed[j]
										}));
										break;
									case "vdrinp":
										_parentForm.rINPBox.Text = val;
										script_status.AppendText(string.Concat(new object[]
										{
										"\n",
										j,
										"-P ",
										inputParsed[j]
										}));
										break;
									case "ddl":
										_parentForm.satDiagThrLvlCombo.Text = val;
										script_status.AppendText(string.Concat(new object[]
										{
										"\n",
										j,
										"-P ",
										inputParsed[j]
										}));
										break;
									case "lpmet":
										_parentForm.lowpowEnterTimeCombo.Text = val;
										script_status.AppendText(string.Concat(new object[]
										{
										"\n",
										j,
										"-P ",
										inputParsed[j]
										}));
										break;
									case "enlpm":
										if (val == "1")
										{
											_parentForm.lowpowEnCheck.Checked = true;
										}
										else
										{
											_parentForm.lowpowEnCheck.Checked = false;
										}
										script_status.AppendText(string.Concat(new object[]
										{
										"\n",
										j,
										"-P ",
										inputParsed[j]
										}));
										break;
									case "ovthr":
										_parentForm.ovthrCombo.Text = val;
										script_status.AppendText(string.Concat(new object[]
										{
										"\n",
										j,
										"-P ",
										inputParsed[j]
										}));
										break;
									case "diagdftbtn":
										if (val == "1")
										{
											_parentForm.defaultAllDiagRegsBtn_Click(null, null);
										}
										script_status.AppendText(string.Concat(new object[]
										{
										"\n",
										j,
										"-P ",
										inputParsed[j]
										}));
										break;
									case "eeprog":
										if (val == "1")
										{
											_parentForm.eepromPgrmBtn_Click(null, null);
										}
										script_status.AppendText(string.Concat(new object[]
										{
										"\n",
										j,
										"-P ",
										inputParsed[j]
										}));
										break;
									case "eerelo":
										if (val == "1")
										{
											_parentForm.eepromReloadBtn_Click(null, null);
										}
										script_status.AppendText(string.Concat(new object[]
										{
										"\n",
										j,
										"-P ",
										inputParsed[j]
										}));
										break;
									case "muxtest":
										_parentForm.muxOutTestCombo.Text = val;
										script_status.AppendText(string.Concat(new object[]
										{
										"\n",
										j,
										"-P ",
										inputParsed[j]
										}));
										break;
									case "muxdata":
										_parentForm.datapathMuxSelCombo.Text = val;
										script_status.AppendText(string.Concat(new object[]
										{
										"\n",
										j,
										"-P ",
										inputParsed[j]
										}));
										break;
									case "enedd":
										if (val == "1")
										{
											_parentForm.dataDumpCheck.Checked = true;
										}
										else
										{
											_parentForm.dataDumpCheck.Checked = false;
										}
										script_status.AppendText(string.Concat(new object[]
										{
										"\n",
										j,
										"-P ",
										inputParsed[j]
										}));
										break;
									case "enumr":
										if (val == "1")
										{
											_parentForm.umrCheck.Checked = true;
										}
										else
										{
											_parentForm.umrCheck.Checked = false;
										}
										script_status.AppendText(string.Concat(new object[]
										{
										"\n",
										j,
										"-P ",
										inputParsed[j]
										}));
										break;
									case "run":
										if (val == "1")
										{
											_parentForm.runBtn_Click(null, null);
										}
										script_status.AppendText(string.Concat(new object[]
										{
										"\n",
										j,
										"-P ",
										inputParsed[j]
										}));
										break;
									case "preset1":
										if (val == "1")
										{
											_parentForm.p1Radio.Checked = true;
										}
										else
										{
											_parentForm.p2Radio.Checked = true;
										}
										script_status.AppendText(string.Concat(new object[]
										{
										"\n",
										j,
										"-P ",
										inputParsed[j]
										}));
										break;
									case "preset2":
										if (val == "1")
										{
											_parentForm.p1Radio.Checked = true;
										}
										else
										{
											_parentForm.p2Radio.Checked = true;
										}
										script_status.AppendText(string.Concat(new object[]
										{
										"\n",
										j,
										"-P ",
										inputParsed[j]
										}));
										break;
									case "burstlisten":
										if (val == "1")
										{
											_parentForm.rxFalseRadio.Checked = true;
										}
										else
										{
											_parentForm.rxTrueRadio.Checked = true;
										}
										script_status.AppendText(string.Concat(new object[]
										{
										"\n",
										j,
										"-P ",
										inputParsed[j]
										}));
										break;
									case "loopbox":
										_parentForm.loopBox.Text = val;
										script_status.AppendText(string.Concat("\n", j, "-P ", inputParsed[j]));
										break;
									case "numobj":
										_parentForm.numObjToDetCombo.Text = val;
										script_status.AppendText(string.Concat("\n", j, "-P ", inputParsed[j]));
										break;
									case "loopdelay":
										_parentForm.loopDelayTextBox.Text = val;
										script_status.AppendText(string.Concat(new object[]
										{
										"\n",
										j,
										"-P ",
										inputParsed[j]
										}));
										break;
									case "startdelay":
										_parentForm.startDelayTextBox.Text = val;
										script_status.AppendText(string.Concat("\n", j, "-P ", inputParsed[j]));
										break;
									case "graphmode":
										_parentForm.graphModeCombo.Text = val;
										script_status.AppendText(string.Concat("\n", j, "-P ", inputParsed[j]));
										break;
									case "resolution":
										_parentForm.resCombo.Text = val;
										script_status.AppendText(string.Concat("\n", j, "-P ", inputParsed[j]));
										break;
									case "contclrplot":
										_parentForm.contClearCheck.Checked = (val == "1");
										script_status.AppendText(string.Concat("\n", j, "-P ", inputParsed[j]));
										break;
									case "clearplot":
										if (val == "1")
											_parentForm.clearPlotBtn_Click(null, null);
										script_status.AppendText(string.Concat("\n", j, "-P ", inputParsed[j]));
										break;
									case "simplot":
										if (val == "1")
											_parentForm.simEchoBtn_Click(null, null);
										script_status.AppendText(string.Concat("\n", j, "-P ", inputParsed[j]));
										break;
									case "exportsedd":
										if (val == "1")
											_parentForm.exportDataBtn_Click(null, null);
										script_status.AppendText(string.Concat("\n", j, "-P ", inputParsed[j]));
										break;
									case "timestamp":
										_parentForm.exportTimeStampCheck.Checked = (val == "1");
										script_status.AppendText(string.Concat("\n", j, "-P ", inputParsed[j]));
										break;
									case "tempstamp":
										_parentForm.exportAdvCheck.Checked = (val == "1");
										script_status.AppendText(string.Concat(new object[] { "\n", j, "-P ", inputParsed[j] }));
										break;
									case "exportmedd":
										_parentForm.exportDataCheck.Checked = (val == "1");
										script_status.AppendText(string.Concat(new object[] { "\n", j, "-P ", inputParsed[j] }));
										break;
									case "exportbgedd":
										_parentForm.bgExportCheck.Checked = (val == "1");
										script_status.AppendText(string.Concat(new object[] { "\n", j, "-P ", inputParsed[j] }));
										break;
									case "bgep":
										_parentForm.bgExportBox.Text = val;
										script_status.AppendText(string.Concat(new object[] { "\n", j, "-P ", inputParsed[j] }));
										break;
									case "bgeddfe":
										if (val == "1")
											_parentForm.bgExportPathBtn_Click(null, null);
										script_status.AppendText(string.Concat(new object[] { "\n", j, "-P ", inputParsed[j] }));
										break;
									case "logumr":
										if (val == "1")
											_parentForm.umrDL.Checked = true;
										else
											_parentForm.umrDL.Checked = false;
										script_status.AppendText(string.Concat(new object[] { "\n", j, "-P ", inputParsed[j] }));
										break;
									case "saveci":
										if (val == "1")
											_parentForm.saveChartImgBtn_Click(null, null);
										script_status.AppendText(string.Concat(new object[] { "\n", j, "-P ", inputParsed[j] }));
										break;
									case "uartdiag":
										if (val == "1")
											_parentForm.uartDiagUARTRadio.Checked = true;
										else
											_parentForm.uartDiagUARTRadio.Checked = false;
										script_status.AppendText(string.Concat(new object[] { "\n", j, "-P ", inputParsed[j] }));
										break;
									case "sysdiag":
										if (val == "1")
											_parentForm.uartDiagSysRadio.Checked = true;
										else
											_parentForm.uartDiagSysRadio.Checked = false;
										script_status.AppendText(string.Concat(new object[] { "\n", j, "-P ", inputParsed[j] }));
										break;
									case "enautofault":
										if (val == "1")
											_parentForm.FaultStat_AutoUpd_En_chck.Checked = true;
										else
											_parentForm.FaultStat_AutoUpd_En_chck.Checked = false;
										script_status.AppendText(string.Concat(new object[] { "\n", j, "-P ", inputParsed[j] }));
										break;
									case "autofaulttime":
										_parentForm.FaultStat_AutoUpd_Time_Box.Text = val;
										script_status.AppendText(string.Concat(new object[] { "\n", j, "-P ", inputParsed[j] }));
										break;
									case "updatefault":
										if (val == "1")
											_parentForm.Fault_Stat_Update_button_Click(null, null);
										script_status.AppendText(string.Concat(new object[] { "\n", j, "-P ", inputParsed[j] }));
										break;
									case "crlrfault":
										if (val == "1")
											_parentForm.ClearFaults_button_Click(null, null);
										script_status.AppendText(string.Concat(new object[] { "\n", j, "-P ", inputParsed[j] }));
										break;
									case "appenddl":
										_parentForm.datalogTextBox.AppendText("\n\r\n\r\n\r" + val + "\n\r");
										script_status.AppendText(string.Concat(new object[] { "\n", j, "-P ", inputParsed[j] }));
										break;
									case "savedl":
										if (val == "1")
											_parentForm.datalogSave_btn_Click(null, null);
										script_status.AppendText(string.Concat(new object[] { "\n", j, "-P ", inputParsed[j] }));
										break;
									case "clrdl":
										if (val == "1")
											_parentForm.datalogClear_btn_Click(null, null);
										script_status.AppendText(string.Concat(new object[] { "\n", j, "-P ", inputParsed[j] }));
										break;
									default:
										script_status.SelectionColor = Color.Red;
										script_status.AppendText(string.Concat(new object[] { "\n", j, "-F ", inputParsed[j] }));
										script_status.SelectionColor = Color.Black;
										break;
								}
							}
							catch
							{
								script_status.AppendText(string.Concat(new object[] { "\n", j, "-F ", inputParsed[j] }));
							}
							if (timestampint > 0)
							{
								if (j % timestampint == 0)
									script_status.AppendText("\n" + DateTime.Now.ToString("yyyy/MM/dd hh:mm:ss:ff tt"));
							}

							if (j == parseCount - 2)
							{
								abortFlag = true;
								script_status.AppendText("\nDONE! Loop=" + (i + 1));
								doneFlag = true;
							}
							if (!pauseFlag)
							{
								await Task.Delay(comIntTime);
							}
							else
							{
								while (pauseFlag)
									await Task.Delay(comIntTime);
							}
						}
					}
				}
				if (abortFlag && !doneFlag)
				{
					script_status.AppendText("\nABORT!");
					i = int.Parse(script_loop_count.Text);
				}
				abortFlag = false;
				doneFlag = false;
			}
		}

		private void script_load_btn_Click(object sender, EventArgs e)
		{
			OpenFileDialog openFileDialog = new OpenFileDialog();
			openFileDialog.Filter = "Text Files|*.txt";
			if (openFileDialog.ShowDialog() == DialogResult.OK)
				script_input.LoadFile(openFileDialog.FileName, RichTextBoxStreamType.PlainText);
		}

		private void script_saveInput_btn_Click(object sender, EventArgs e)
		{
			SaveFileDialog saveFileDialog = new SaveFileDialog();
			saveFileDialog.DefaultExt = "*.txt";
			saveFileDialog.Filter = "Text Files|*.txt";
			if (saveFileDialog.ShowDialog() == DialogResult.OK && saveFileDialog.FileName.Length > 0)
				script_input.SaveFile(saveFileDialog.FileName, RichTextBoxStreamType.PlainText);
		}

		private void script_saveStat_btn_Click(object sender, EventArgs e)
		{
			SaveFileDialog saveFileDialog = new SaveFileDialog();
			saveFileDialog.DefaultExt = "*.txt";
			saveFileDialog.Filter = "Text Files|*.txt";
			if (saveFileDialog.ShowDialog() == DialogResult.OK && saveFileDialog.FileName.Length > 0)
				script_status.SaveFile(saveFileDialog.FileName, RichTextBoxStreamType.PlainText);
		}

		private void script_clearInput_btn_Click(object sender, EventArgs e)
		{
			script_input.Clear();
		}

		private void script_clearStat_btn_Click(object sender, EventArgs e)
		{
			script_status.Clear();
		}

		private void script_help_btn_Click(object sender, EventArgs e)
		{
			script_input.AppendText("PGA460 Automated Scripting Language\n\rFormat of command structure is\nCOMMAND(INPUT); where COMMAND is a GUI available control, and INPUT is a possible control value or state. The value must be an exact match. A true state is an INPUT value of 1, and a false state is an INPUT value of 0.\n\rTo add comments, use square brackets in the format of [insert comments in between]\n\rListing of commands:\n\r\n*** GENERAL ***\n•drfreq //Driver Receive Frequency (kHz)\n•afgr //AFE Gain Range\n•gainCombo //Initial Gain Level\n•bpbw //BPF BW Frequency\n•cutoff //Cutoff Frequency\n•deglitch //Threshold Deglitch Time (us)\n•p1bp //P1 Burst Pulses\n•p1dcl //P1 Driver Current Limit (mA)\n•p1rtl //P1 Record Time Length (ms)\n•p2bp //P2 Burst Pulses\n•p2dcl //P2 Driver Current Limit (mA)\n•p2rtl //P2 Record Time Length (ms)\n•discl //Disable Current Limit\n•nlsnl //NLS Noise Level\n•nlssl //NLS Scaling Level\n•nlstop //NLS Threshold of Operation\n•p1nlsen //Enable P1 NLS\n•p2nlsen //Enable P2 NLS\n•entimedec //Enable Time Decouple\n•timedec // Time Decouple (us)\n•entempdec //Enable Temp Decouple\n•tempdec // Temp Decouple (C)\n•p1dgsr // P1 Digital Gain SR\n•p1dglr // P1 Digital Gain LR\n•p1dglrst // P1 Digital Gain LR Start Time (Thr)\n•p2dgsr // P2 Digital Gain SR\n•p2dglr // P2 Digital Gain LR\n•p2dglrst // P2 Digital Gain LR Start Time (Thr)\n•xfapdft // Default Transformer All Purpose Button\n•xfipdft // Default Transformer ISO Pole Button\n•ddapdft // Default Direct All Purpose Button (Thr)\n•ddipdft // Default Direct ISO Pole Button\n•xfbddft // Default Transformer Block Diagram Button\n•ddbddft // Default Direct Block Diagaram Button\n\n*** TIME VARYING GAIN ***\n•tvgt0 // TVG time 0\n•tvgg0 // TVG gain 0\n•tvgt1 // TVG time 1\n•tvgg1 // TVG gain 1\n•tvgt2 // TVG time 2\n•tvgg2 // TVG gain 2\n•tvgt3 // TVG time 3\n•tvgg3 // TVG gain 3\n•tvgt4 // TVG time 4\n•tvgg4 // TVG gain 4\n•tvgt5 // TVG time 5\n•tvgg5 // TVG gain 5\n•wtvginst // Write tvg instantly\n•wtvgbtn // Write tvg btn\n•rtvgbtn // Read tvg btn\n•shorttvg // Short tvg predefined btn\n•longtvg // Long tvg predefined btn\n•allinitgt // All initial tvg gain and time\n•allmidtvg // All midcode tvg btn\n•clrtvg // Clear tvg btn\n\n*** THRESHOLD ***\n•p1t1 // P1 Thr Time 1\n•p1t2 // P1 Thr Time 2\n•p1t3 // P1 Thr Time 3\n•p1t4 // P1 Thr Time 4\n•p1t5 // P1 Thr Time 5\n•p1t6 // P1 Thr Time 6\n•p1t7 // P1 Thr Time 7\n•p1t8 // P1 Thr Time 8\n•p1t9 // P1 Thr Time 9\n•p1t10 // P1 Thr Time 10\n•p1t11 // P1 Thr Time 11\n•p1t12 // P1 Thr Time 12\n•p1l1 // P1 Thr Level 1\n•p1l2 // P1 Thr Level 2\n•p1l3 // P1 Thr Level 3\n•p1l4 // P1 Thr Level 4\n•p1l5 // P1 Thr Level 5\n•p1l6 // P1 Thr Level 6\n•p1l7 // P1 Thr Level 7\n•p1l8 // P1 Thr Level 8\n•p1l9 // P1 Thr Level 9\n•p1l10 // P1 Thr Level 10\n•p1l11 // P1 Thr Level 11\n•p1l12 // P1 Thr Level 12\n•p1lOff // P1 Thr Level Offset\n•p2t1 // P2 Thr Time 1\n•p2t2 // P2 Thr Time 2\n•p2t3 // P2 Thr Time 3\n•p2t4 // P2 Thr Time 4\n•p2t5 // P2 Thr Time 5\n•p2t6 // P2 Thr Time 6\n•p2t7 // P2 Thr Time 7\n•p2t8 // P2 Thr Time 8\n•p2t9 // P2 Thr Time 9\n•p2t10 // P2 Thr Time 10\n•p2t11 // P2 Thr Time 11\n•p2t12 // P2 Thr Time 12\n•p2l1 // P2 Thr Level 1\n•p2l2 // P2 Thr Level 2\n•p2l3 // P2 Thr Level 3\n•p2l4 // P2 Thr Level 4\n•p2l5 // P2 Thr Level 5\n•p2l6 // P2 Thr Level 6\n•p2l7 // P2 Thr Level 7\n•p2l8 // P2 Thr Level 8\n•p2l9 // P2 Thr Level 9\n•p2l10 // P2 Thr Level 10\n•p2l11 // P2 Thr Level 11\n•p2l12 // P2 Thr Level 12\n•p2lOff // P2 Thr Level Offset\n•wthrinst // Write thresholds instantly\n•wthrbtn // Write thresholds btn\n•rthrbtn // Read thresholds btn\n•clonethrbtn // Clone thresholds btn\n•shortthr // Short threshold predefined btn\n•longthr // Long threshold predefined btn\n•allinitlt // All initial thr level and time\n•allmidthr // All midcode threshold btn\n•clrthrbtn // All level 1 and time 1 btn\n•thrtab // Select threshold tab for P1 or P2\n\n*** DIAGNOSTICS ***\n•readsysdiag // Read system diagnostic btn\n•blsysdiag //Burst-and-listen before pulling system diagnostic\n•dlfdiag //datalog freq diag\n•dlddiag //datalog decay diag\n•trigtnm // Trigger a noise floor and temp measurement\n•trigtm // Trigger a temp measurement\n•trignm // Trigger a noise floor measurement\n•dltempdiag // Data log temp check\n•dlnoisediag // Data log noise check\n•tg //Temperature Gain\n•to //Temperature Offset\n•fdwl //Frequency Diagnostic Window Length (Periods)\n•fdst //Frequency Diagnostic Start Time (us)\n•fdett //Frequency Diagnostic Error Time Threshold (+/- us)\n•vdet //Voltage Diagnostic Error Threshold\n•vdcinp //Voltage Diagnostic C_INP\n•vdrinp //Voltage Diagnostic R_INP\n•ddl //Decay Diagnostic Saturation Level\n•lpmet //Low Power Mode Enter Time\n•enlpm //Enable Low Power Mode\n•ovthr //Overvoltage Threshold\n•diagdftbtn //Default Diagnostics Button\n\n*** TEST ***\n•eeprog //EEPROM Program Button\n•eerelo //EEPROM PReload Button\n•muxtest //Mutliplexer output on TEST pin\n•muxdata //Datapath Mux Select\n\n*** DATA MONITOR ***\n•enedd //Enable Echo Data Dump\n•enumr //Enable Ultrasonic Measurement Results\n•run //Run button\n•preset1 //Select Preset 1\n•preset2 //Select Preset 2\n•burstlisten //Burst-and-Listen or Listen-Only\n•loopbox //Number of loops to execute\n•numobj //Number of objects to detect for UMR\n•loopdelay //Delay between each loop in milliseconds\n•startdelay //Delay before first loop in milliseconds\n•graphmode //Echo Data Dump Graph Mode\n•resolution //Echo Data Dump Resolution\n•contclrplot //Contiuously clear plot\n•clearplot // Clear echo data dump plot\n•simplot // Simulate echo data dump plot\n•exportsedd // Export single echo data dump\n•timestamp // Append time stamp to echo data dump txt\n•tempstamp // Append temp stamp to echo data dump txt\n•exportmedd // Export multiple echo data dump\n•exportbgedd // Export echo data dump background\n•bgeddfe // Export echo data dump background file explorer\n•logumr // Log ultrasonic measurement results\n•saveci // Save data monitor chart image\n\n*** RIGHT PANEL ***\n•uartdiag //UART Diagnostic radio\n•sysdiag //System Diagnostic radio\n•enautofault //Enable auto fault check\n•autofaulttime //Auto fault timeout\n•updatefault // Fault update click\n•crlrfault // Clear faults\n•appenddl //Append text to datalog\n•savedl // Save datalog text file\n•clrdl // Clear datalog\n\n*** MISC ***\n•kbenter //Keyboard Enter click\n•scripttimeout //Script timeout in milliseconds\n•commandint //Global command interval time in milliseconds\n•togglepauseresumescript //pause or resume script\n•scripttimestampint //Time stamp n-number of intervals\n•ds1led //DS1 LED state\n•fdled //Frequency Diagnostic LED state\n•vdled //Voltage Diagnostic LED state\n");
		}

		private void script_stop_btn_Click(object sender, EventArgs e)
		{
			if (script_pause_btn.Text == "RESUME Script")
				script_pause_btn_Click(null, null);
			abortFlag = true;
		}

		private void script_pause_btn_Click(object sender, EventArgs e)
		{
			if (script_pause_btn.Text == "PAUSE Script")
			{
				script_pause_btn.Text = "RESUME Script";
				pauseFlag = true;
			}
			else
			{
				script_pause_btn.Text = "PAUSE Script";
				pauseFlag = false;
			}
		}

		private void script_loop_count_TextChanged(object sender, EventArgs e)
		{
			if (!Regex.IsMatch(script_loop_count.Text, "^\\d+$") || script_loop_count.Text == "0")
				script_loop_count.Text = "1";
		}

		private void script_loop_check_CheckedChanged(object sender, EventArgs e)
		{
			if (!script_loop_check.Checked)
			{
				script_loop_count.Text = "1";
				script_loop_count.ReadOnly = true;
			}
			else
				script_loop_count.ReadOnly = false;
		}

		private MainForm _parentForm = null;
		private bool abortFlag = false;
		private bool pauseFlag = false;
		private bool doneFlag = false;
		private int comIntTime = 250;
		private int timestampint = 0;
	}
}
