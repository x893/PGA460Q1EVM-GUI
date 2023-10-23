using System;
using System.Windows.Forms;

namespace TI.eLAB.EVM
{
	internal class ProgressUpdater
	{
		public static void Update_EvalExamples(string methodIdentifier, int checkState, ToolStripStatusLabel toolStripLabelPointer, ToolStripProgressBar toolStripProgressBar)
		{
			switch (methodIdentifier)
			{
				case "MaskExciterFaults":
					if (checkState == 1)
						toolStripLabelPointer.Text = "Bits 0-4 in PULSE_P2 adress were changed : Exciter will Remain On regardless of Faults. ";
					else
						toolStripLabelPointer.Text = "Bits 0-4 in PULSE_P2 adress were changed : Exciter will turn OFF when faults.";
					return;
				case "MaskFaultPins":
					if (checkState == 1)
						toolStripLabelPointer.Text = "Bits 1-11 and 13 in PULSE_P1 adress were changed : Masked Fault Pins ";
					else
						toolStripLabelPointer.Text = "Bits 1-11 and 13 in PULSE_P1 adress were changed : Un-Masked Fault Pins";
					return;
				case "ForceORSEnabled":
					if (checkState == 1)
						toolStripLabelPointer.Text = "Bit 14 in PULSE_P1 adress was changed : ORS enable in analog ";
					else
						toolStripLabelPointer.Text = "Bit 14 in PULSE_P1 adress was changed : ORS disabled in analog s";
					return;
				case "Deviceunlock":
					toolStripLabelPointer.Text = " Device Unlocked, write acess for PULSE_P1 and PULSE_P2. ";
					return;
				case "Faults_Cleared":
					toolStripLabelPointer.Text = "All faults have been cleared";
					return;
				case "Calculate CRC":
					toolStripLabelPointer.Text = "Please click Calculate Config. CRC before clicking Apply Config.CRC";
					return;
				case "Device Locked":
					toolStripLabelPointer.Text = "Device locked. The value changed in the control will not be reflected in the device. Please press Device Unlock";
					return;
				case "Diagnostics":
					toolStripLabelPointer.Text = "The value changed in the control will not be reflected in the device. Please switch to Diagnostics mode and try again.";
					return;
			}
			toolStripLabelPointer.Text = methodIdentifier;
		}
	}
}
