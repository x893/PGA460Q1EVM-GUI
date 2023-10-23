// Remove NO_ for check version on startup
#define NO_AUTO_CHECK_VERSION

using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.Drawing;
using System.Drawing.Imaging;
using System.Globalization;
using System.IO;
using System.IO.Ports;
using System.Linq;
using System.Net;
using System.Net.NetworkInformation;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using System.Timers;
using System.Windows.Forms;
using System.Windows.Forms.DataVisualization.Charting;
using TI.eLAB.EVM.Properties;
using TIger_A;
using USBClassLibrary;

namespace TI.eLAB.EVM
{
    public partial class MainForm : Form
    {
        private const string PGA460_ToolsSoftware_Link = "http://www.ti.com/product/PGA460-Q1/toolssoftware";

        public string uartAddrComboGet
        {
            get
            {
                return uartAddrCombo.Text;
            }
        }

        public MainForm(int valOnce)
        {
            common.u2a.SuppressSplash(1);
            common.u2a.SuppressFirmwareCheck(1);
            Hide();
            splashthread = new Thread(new ThreadStart(SplashScreen.ShowSplashScreen));
            splashthread.IsBackground = true;
            splashthread.Name = "SplashThread";
            splashthread.Start();
            Directory.CreateDirectory(Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.Personal), "BOOSTXL-PGA460"));

            try
            {
                InitializeComponent();
            }
            catch (Exception ex)
            {
                Console.WriteLine("{0} Exception caught.", ex);
                MessageBox.Show("Failed to InitializeComponent(); " + ex.ToString());
            }
            graphModeCombo.Items.Clear();
            graphModeCombo.Items.AddRange(new object[]
            {
                "Data Dump",
                "ADC",
                "DSP - BP Filter",
                "DSP - Rectifier",
                "DSP - LP Filter"
            });
            VIDTextBox.Text = 8263u.ToString("X4");
            PIDTextBox.Text = 0769u.ToString("X4");
            USBPort = new USBClass();
            ListOfUSBDeviceProperties = new List<USBClass.DeviceProperties>();

            USBPort.USBDeviceAttached += USBPort_USBDeviceAttached;
            USBPort.USBDeviceRemoved += USBPort_USBDeviceRemoved;

            USBPort.RegisterForDeviceChange(true, Handle);

            MyUSBDeviceConnected = false;
            Search_Results.Columns[0].HeaderCell.Style.Font = new Font("Courier New", 9f);
            Search_Results.Columns[1].HeaderCell.Style.Font = new Font("Courier New", 9f);
            Registers_createGrids("DATA_DUMP");
            Registers_createGrids("USER");
            primaryTab.ItemSize = new Size(0, 1);
            primaryTab.SizeMode = TabSizeMode.Fixed;
            comTabControl.ItemSize = new Size(0, 1);
            comTabControl.SizeMode = TabSizeMode.Fixed;
            tciIndexTab.ItemSize = new Size(0, 1);
            tciIndexTab.SizeMode = TabSizeMode.Fixed;
            graphModeTab.ItemSize = new Size(0, 1);

            foreach (object node in leftTreeNav.Nodes)
                ((TreeNode)node).Expand();

            Application.DoEvents();
            common.u2a.GPIO_SetPort(1, 1);
            common.u2a.GPIO_WritePort(1, 1);
            try
            {
                for (int i = 0; i <= 90; i++)
                {
                    string[] regs = Tools.loadRegDefinitionFromConfigFile(Resources.REVAregisters, i, -1);
                    if (regs != null)
                    {
                        string text = regs[0].Trim();
                        string reg_name = text.Split(new char[] { '\r', '\n' }).FirstOrDefault();
                        string reg_address = "0x" + Tools.int32_Into_stringBase16(i).PadLeft(2, '0');
                        searchlist.Add(new search(reg_name, reg_address, text.ToLower()));
                    }
                }
            }
            catch
            {
                MessageBox.Show("Failed to loadRegDefinitionFromConfigFile");
            }
            debugTabControl.SelectedIndex = 1;
            ConfigRegs.Dock = DockStyle.Fill;
            DataDumpRegs.Dock = DockStyle.Fill;
            TI_EEPROM_Regs.Dock = DockStyle.Fill;
            TI_TM_Regs.Dock = DockStyle.Fill;
            graphModeCombo.SelectedIndex = 0;
            resCombo.SelectedIndex = 0;
            sampleMaxCombo.SelectedIndex = 127;
            numObjToDetCombo.SelectedIndex = 0;
            thrUpdateCheck.Checked = false;
            tvgInstantUpdateCheck.Checked = false;

            SplashScreen.UpdateLoadingText(3);

            Initialize_Comm_Interface(false);
            Initialize_PGA46x_Check();
            pga460_startup();
            Thread.Sleep(10);
            Graphics graphics = CreateGraphics();
            float dpiX = graphics.DpiX;
            float dpiY = graphics.DpiY;
            if (dpiX > 96f || dpiY > 96f)
                splitContainerHELP.SplitterDistance = splitContainerHELP.SplitterDistance * 100 / 80;

            AutoScaleDimensions = new SizeF(96f, 96f);
            AutoScaleMode = AutoScaleMode.Dpi;
            primaryTab.SelectTab("bdModeTab");
            primaryTab.SelectedTab.AutoScroll = false;
            comModeUARTBtn_Click(null, null);
            dataMonitorCheckListBox.SetItemChecked(0, true);
            dataMonitorCheckListBox.SetItemChecked(1, true);
            dataMonitorCheckListBox.SetItemChecked(2, true);
            dataMonitorCheckListBox.SetItemChecked(3, true);
            dataMonitorCheckListBox.SetItemChecked(4, true);
            dataMonitorCheckListBox.SetItemChecked(5, true);
            Text = "PGA460-Q1 EVM";
            versionDateLabel.Text = "Rev." + guiVersion + " — Build Date " + guiDate;
            if (valOnce == 0)
            {
                try
                {
                    splashthread.Abort();
                    Thread.Sleep(1);
                }
                catch
                {
                    base.Close();
                }
            }

            if (common.u2a.GPIO_ReadPort(0) == 1)
            {
                disableBPCommCheck.Checked = true;
                disableBOOSTXLPGA460CommunicationToolStripMenuItem.Text = "Enable BOOSTXL-PGA460 Communication";
            }
            else
            {
                disableBPCommCheck.Checked = false;
                disableBOOSTXLPGA460CommunicationToolStripMenuItem.Text = "Disable BOOSTXL-PGA460 Communication";
            }

            SplashScreen.UpdateLoadingText(0);
            aTimer = new System.Timers.Timer(1500.0);
            aTimer.Elapsed += OnTimedEvent;
            aTimer.Enabled = false;

            bTimer = new System.Timers.Timer(1500.0);
            bTimer.Elapsed += loadBatchEvent;
            bTimer.Enabled = false;
#if AUTO_CHECK_VERSION
            checkVersionTimer = new System.Timers.Timer(750.0);
            checkVersionTimer.Elapsed += guiUpdate;
            checkVersionTimer.Enabled = true;
#endif
            if (USB_Controller_box.Text != "USB2ANY I/F Found")
                USBTryMyDeviceConnection();
        }

        private void pga460_startup()
        {
            Tools.timeDelay(10, "MS");
            if (PGA46xStat_box.Text.Contains("Ready"))
            {
                readingRegsFlag = true;
                activateProgressBar(true);
                proj_MainCommunication(GRID_USER_MEMSPACE, false, false, false);
                activateProgressBar(false);
                readingRegsFlag = false;
                Fault_Stat_Update_button_Click(null, null);
            }
            else
            {
                REVID_Stat_TextBox.BackColor = Color.Gray;
                OPTID_Stat_TextBox.BackColor = Color.Gray;
                CMWWUERR_Stat_TextBox.BackColor = Color.Gray;
                THRCRCERR_Stat_TextBox.BackColor = Color.Gray;
                EECRCERR_Stat_TextBox.BackColor = Color.Gray;
                TRIMCRCERR_Stat_TextBox.BackColor = Color.Gray;
                TSDPROT_Stat_TextBox.BackColor = Color.Gray;
                IOREGOV_Stat_TextBox.BackColor = Color.Gray;
                IOREGUV_Stat_TextBox.BackColor = Color.Gray;
                AVDDOV_Stat_TextBox.BackColor = Color.Gray;
                AVDDUV_Stat_TextBox.BackColor = Color.Gray;
                VPWROV_Stat_TextBox.BackColor = Color.Gray;
                VPWRUV_Stat_TextBox.BackColor = Color.Gray;
                UARTDIAG0_1_Stat_TextBox.BackColor = Color.Gray;
                UARTDIAG0_2_Stat_TextBox.BackColor = Color.Gray;
                UARTDIAG0_3_Stat_TextBox.BackColor = Color.Gray;
                UARTDIAG0_4_Stat_TextBox.BackColor = Color.Gray;
                UARTDIAG0_5_Stat_TextBox.BackColor = Color.Gray;
                UARTDIAG1_1_Stat_TextBox.BackColor = Color.Gray;
                UARTDIAG1_2_Stat_TextBox.BackColor = Color.Gray;
                UARTDIAG1_3_Stat_TextBox.BackColor = Color.Gray;
                UARTDIAG1_5_Stat_TextBox.BackColor = Color.Gray;
            }
            common.u2a.GPIO_SetPort(10, 1);
            common.u2a.GPIO_WritePort(10, 1);
            common.u2a.GPIO_SetPort(11, 1);
            common.u2a.GPIO_WritePort(11, 1);
            common.u2a.GPIO_SetPort(12, 1);
            common.u2a.GPIO_WritePort(12, 1);
            initThrFlag = true;
        }

        private void activateProgressBar(bool state)
        {
            if (state)
            {
                DisableMouse();
                toolStripProgressBar1.Visible = true;
                toolStripProgressBar1.Value = 0;
            }
            else
            {
                toolStripProgressBar1.Value = 100;
                Tools.timeDelay(100, "MS");
                toolStripProgressBar1.Visible = false;
                EnableMouse();
            }
        }

        private void EnableMouse()
        {
            base.UseWaitCursor = false;
            System.Windows.Forms.Cursor.Current = Cursors.Default;
        }

        public bool PreFilterMessage(ref Message m)
        {
            return m.Msg == 513 || m.Msg == 514 || m.Msg == 515 || (m.Msg == 516 || m.Msg == 517 || m.Msg == 518);
        }

        private void DisableMouse()
        {
            base.UseWaitCursor = true;
            System.Windows.Forms.Cursor.Current = Cursors.WaitCursor;
            lastCursorPosX = Control.MousePosition.X;
            lastCursorPosY = Control.MousePosition.Y;
        }

        public void OnRegisterClickHandle()
        {
            regArray = Tools.loadRegDefinitionFromConfigFile(Resources.REVAregisters, GRID_USER_MEMSPACE.mRow, GRID_USER_MEMSPACE.mColumn);
            if (regArray != null)
                infoTextBox.Text = regArray[0];
        }

        public void OnMouseHandleGRID(object sender, MouseEventArgs e)
        {
        }

        public void OnautoupdateHandle()
        {
            proj_MainCommunication(GRID_USER_MEMSPACE, true, true, false);
        }

        private void switchedState_Check(bool state, int stateChosen)
        {
            if (!state)
            {
                sState_loopControl++;
                if (sState_loopControl <= 3)
                {
                }
                else if (sState_loopControl > 3)
                {
                    MessageBox.Show("Unable to switch state. Try again. If issue persists, Reset EVM & Restart GUI.");
                }
                else
                {
                    MessageBox.Show("Unable to switch state. Reconnect EVM & Restart GUI.");
                }
            }
            else
            {
                Console.Write("State switched");
                sState_loopControl = 0;
            }
        }

        private void abortSplashThread()
        {
            try
            {
                splashthread.Abort();
            }
            catch { }
        }

        private bool USBTryMyDeviceConnection()
        {
            uint? mi = new uint?(0u);
            if (MITextBox.Text != string.Empty)
            {
                mi = new uint?(uint.Parse(MITextBox.Text, NumberStyles.AllowHexSpecifier));
            }
            else
            {
                mi = null;
            }
            InitializeDeviceTextBoxes();
            NumberOfFoundDevicesLabel.Text = "0";
            bool result;
            if (USBClass.GetUSBDevice(uint.Parse(VIDTextBox.Text, NumberStyles.AllowHexSpecifier), uint.Parse(PIDTextBox.Text, NumberStyles.AllowHexSpecifier), ref ListOfUSBDeviceProperties, SerialPortCheckBox.Checked, mi))
            {
                NumberOfFoundDevicesLabel.Text = "Number of found devices: " + ListOfUSBDeviceProperties.Count.ToString();
                FoundDevicesComboBox.Items.Clear();
                for (int i = 0; i < ListOfUSBDeviceProperties.Count; i++)
                    FoundDevicesComboBox.Items.Add("Device " + i.ToString());

                FoundDevicesComboBox.Enabled = (ListOfUSBDeviceProperties.Count > 1);
                FoundDevicesComboBox.SelectedIndex = 0;
                if (!stillLoadingFW)
                    Connect();

                fwMessageShown = true;
                result = true;
            }
            else if (USBClass.GetUSBDevice(uint.Parse(VIDTextBox.Text, NumberStyles.AllowHexSpecifier), 19u, ref ListOfUSBDeviceProperties, SerialPortCheckBox.Checked, new uint?(2u)))
            {
                NumberOfFoundDevicesLabel.Text = "Number of found devices: " + ListOfUSBDeviceProperties.Count.ToString();
                FoundDevicesComboBox.Items.Clear();
                for (int i = 0; i < ListOfUSBDeviceProperties.Count; i++)
                    FoundDevicesComboBox.Items.Add("Device " + i.ToString());

                FoundDevicesComboBox.Enabled = (ListOfUSBDeviceProperties.Count > 1);
                FoundDevicesComboBox.SelectedIndex = 0;
                if (!fwMessageShown)
                    aTimer.Enabled = true;

                result = false;
            }
            else
                result = false;

            return result;
        }

        private void USBPort_USBDeviceAttached(object sender, USBClass.USBDeviceEventArgs e)
        {
            if (!MyUSBDeviceConnected)
            {
                activateProgressBar(true);
                if (USBTryMyDeviceConnection())
                    MyUSBDeviceConnected = true;

                activateProgressBar(false);
            }
        }

        private void USBPort_USBDeviceRemoved(object sender, USBClass.USBDeviceEventArgs e)
        {
            if (!USBClass.GetUSBDevice(8263u, 769u, ref ListOfUSBDeviceProperties, false, null))
            {
                evmUSBcount++;
                if (evmUSBcount == 4)
                {
                    MyUSBDeviceConnected = false;
                    if (!stillLoadingFW)
                        Disconnect();
                    evmUSBcount = 0;
                }
            }
        }

        protected override void WndProc(ref Message m)
        {
            bool flag = false;
            USBPort.ProcessWindowsMessage(m.Msg, m.WParam, m.LParam, ref flag);
            base.WndProc(ref m);
        }

        private void Connect()
        {
            datalogTextBox.AppendText("LP-USB Conncted!\n");
            activateProgressBar(true);
            tvgInstantUpdateCheck.Checked = false;
            thrUpdateCheck.Checked = false;
            firstTimeCheckforPGA = 0;
            Initialize_Comm_Interface(true);
            Initialize_PGA46x_Check();
            pga460_startup();
            ConnectionToolStripStatusLabel.Text = "Connected";
            activateProgressBar(false);
        }

        private void Disconnect()
        {
            datalogTextBox.AppendText("LP-USB Disconnected!\n");
            thrUpdatedAtLeastOnce = false;
            activateProgressBar(true);
            tvgInstantUpdateCheck.Checked = false;
            thrUpdateCheck.Checked = false;
            Initialize_Comm_Interface(true);
            Initialize_PGA46x_Check();
            ClearFaults_button_Click(null, null);
            base.UseWaitCursor = false;
            InitializeDeviceTextBoxes();
            fwMessageShown = false;
            activateProgressBar(false);
        }

        private void InitializeDeviceTextBoxes()
        {
            DeviceTypeTextBox.Text = string.Empty;
            FriendlyNameTextBox.Text = string.Empty;
            DeviceDescriptionTextBox.Text = string.Empty;
            DeviceManufacturerTextBox.Text = string.Empty;
            DeviceClassTextBox.Text = string.Empty;
            DeviceLocationTextBox.Text = string.Empty;
            DevicePathTextBox.Text = string.Empty;
            DevicePhysicalObjectNameTextBox.Text = string.Empty;
            SerialPortTextBox.Text = string.Empty;
            NumberOfFoundDevicesLabel.Text = "Number of found devices: 0";
            FoundDevicesComboBox.Items.Clear();
            FoundDevicesComboBox.Enabled = false;
        }

        private void VIDTextBox_Leave(object sender, EventArgs e)
        {
            uint num = 0u;
            if (!uint.TryParse(VIDTextBox.Text, NumberStyles.AllowHexSpecifier, new CultureInfo("en-US"), out num))
            {
                VIDTextBox.Focus();
                ErrorProvider.SetError(VIDTextBox, "VID is expected to be an hexadecimal number, allowed characters: 0 to 9, A to F");
            }
            else
            {
                ErrorProvider.SetError(VIDTextBox, "");
            }
        }

        private void PIDTextBox_Leave(object sender, EventArgs e)
        {
            uint num = 0u;
            if (!uint.TryParse(PIDTextBox.Text, NumberStyles.AllowHexSpecifier, new CultureInfo("en-US"), out num))
            {
                PIDTextBox.Focus();
                ErrorProvider.SetError(PIDTextBox, "PID is expected to be an hexadecimal number, allowed characters: 0 to 9, A to F");
            }
            else
            {
                ErrorProvider.SetError(PIDTextBox, "");
            }
        }

        private void MITextBox_Leave(object sender, EventArgs e)
        {
            uint num = 0u;
            ErrorProvider.SetError(MITextBox, "");
            if (MITextBox.Text != string.Empty)
            {
                if (!uint.TryParse(MITextBox.Text, NumberStyles.AllowHexSpecifier, new CultureInfo("en-US"), out num))
                {
                    MITextBox.Focus();
                    ErrorProvider.SetError(MITextBox, "MI is expected to be an hexadecimal number, allowed characters: 0 to 9, A to F");
                }
            }
        }

        private void UpdateButton_Click(object sender, EventArgs e)
        {
            USBTryMyDeviceConnection();
        }

        private void FoundDevicesComboBox_SelectedIndexChanged(object sender, EventArgs e)
        {
            int selectedIndex = ((ComboBox)sender).SelectedIndex;
            DeviceTypeTextBox.Text = ListOfUSBDeviceProperties[selectedIndex].DeviceType;
            FriendlyNameTextBox.Text = ListOfUSBDeviceProperties[selectedIndex].FriendlyName;
            DeviceDescriptionTextBox.Text = ListOfUSBDeviceProperties[selectedIndex].DeviceDescription;
            DeviceManufacturerTextBox.Text = ListOfUSBDeviceProperties[selectedIndex].DeviceManufacturer;
            DeviceClassTextBox.Text = ListOfUSBDeviceProperties[selectedIndex].DeviceClass;
            DeviceLocationTextBox.Text = ListOfUSBDeviceProperties[selectedIndex].DeviceLocation;
            DevicePathTextBox.Text = ListOfUSBDeviceProperties[selectedIndex].DevicePath;
            DevicePhysicalObjectNameTextBox.Text = ListOfUSBDeviceProperties[selectedIndex].DevicePhysicalObjectName;
            SerialPortTextBox.Text = ListOfUSBDeviceProperties[selectedIndex].COMPort;
        }

        private void Initialize_Comm_Interface(bool reset_U2A)
        {
            string serialNumber = "";
            byte[] array = new byte[64];
            if (reset_U2A)
            {
                USB_Controller_box.Text = "";
                USB_Firmware_Box.Text = "";
                USB_ConnStat_box.Text = "";
                PGA46xStat_box.Text = "";
                common.u2a.Close();
                common.u2a.Restart();
                Tools.timeDelay(1, "S");
            }
            int controllerCount = 0;
            Task task = Task.Run(delegate
            {
                controllerCount = common.u2a.FindControllers();
            });
            if (!task.Wait(TimeSpan.FromSeconds(10.0)))
            {
                DialogResult dialogResult = MessageBox.Show("Disconnect EVM USB cable, restart GUI, then reconnect USB cable.", "USB2ANY I/F Timeout", MessageBoxButtons.OK, MessageBoxIcon.Exclamation);
                if (dialogResult == DialogResult.OK)
                    Application.Exit();
            }
            if (controllerCount < 1)
            {
                USB_Controller_box.Text = "Not Detected";
                USB_Firmware_Box.Text = "";
                PGA46xStat_box.Text = "N/A or Wrong Addr";
                updateStatusBar("Hardware Not Connected");
                evmStatusText = toolStripStatusLabel1.Text;
                pictureBox1.Image = Resources.red_button;
            }
            else
            {
                if (controllerCount > 1)
                {
                    USB_Controller_box.Text = "USB2ANY I/F Found";
                    USB_ConnStat_box.Text = "Multiple Found. Using 1.";
                    evmStatusText = toolStripStatusLabel1.Text;
                }
                else
                {
                    USB_Controller_box.Text = "USB2ANY I/F Found";
                    updateStatusBar("Hardware Connected");
                    toolStripStatusLabel1.BackColor = Control.DefaultBackColor;
                    evmStatusText = toolStripStatusLabel1.Text;
                    pictureBox1.Image = Resources.red_button;
                }
                common.u2a.GetSerialNumber(0, ref serialNumber);
                if (common.u2a.Open(serialNumber) != 0)
                    USB_ConnStat_box.Text = "Error";
                else
                    USB_ConnStat_box.Text = "Connected";

                common.u2a.FirmwareVersion_Read(ref array, 64);
                USB_Firmware_Box.Text = string.Concat(
                    array[0].ToString(), ".",
                    array[1].ToString(), ".",
                    array[2].ToString(), ".",
                    array[3].ToString());
            }
        }

        public void Initialize_PGA46x_Check()
        {
            Array.Clear(uart_return_data, 0, 64);
            u2a_uart_control_master();

            if (firstTimeCheckforPGA < 2)
            {
                regAddrByte = 31;
                Color backColor = toolStripStatusLabel1.BackColor;
                for (byte b = 0; b < 8; b += 1)
                {
                    commandByte = (byte)((b << 5) + uart_cmd9);
                    MChecksumByte = calculate_UART_Checksum(new byte[] { commandByte, regAddrByte });
                    u2a_status = common.u2a.UART_Write(4, new byte[] { 85, commandByte, regAddrByte, MChecksumByte });
                    u2a_status = common.u2a.UART_Read(3, uart_return_data);
                    if (uart_return_data[0] != 0)
                    {
                        PGA46xStat_box.Text = "Ready; UART Addr = " + uartAddrCombo.Text;
                        uartAddrCombo.SelectedIndex = (int)b;
                        uartAddrOld = uartAddrCombo.Text;
                        toolStripStatusLabel1.BackColor = Control.DefaultBackColor;
                        toolStripProgressBar1.Value = 100;
                        updateStatusBar("Hardware Connected");
                        pictureBox1.Image = Resources.green_button;
                        b = 8;
                    }
                    else
                    {
                        PGA46xStat_box.Text = "N/A or Wrong Addr";
                        if (USB_ConnStat_box.Text == "Connected")
                        {
                            toolStripProgressBar1.Value = 50;
                            updateStatusBar("Hardware Not Connected");
                            evmStatusText = toolStripStatusLabel1.Text;
                        }
                        else
                            toolStripProgressBar1.Value = 0;
                        pictureBox1.Image = Resources.red_button;
                    }
                    Array.Clear(uart_return_data, 0, 64);
                }
                commandByte = (byte)((Convert.ToByte(uartAddrCombo.Text) << addrShift) + uart_cmd9);
                regAddrByte = 31;
                MChecksumByte = calculate_UART_Checksum(new byte[] { commandByte, regAddrByte });
                u2a_status = common.u2a.UART_Write(4, new byte[] { 85, commandByte, regAddrByte, MChecksumByte });
                u2a_status = common.u2a.UART_Read(3, uart_return_data);
                uartDiagB = uart_return_data[0];
                if (uart_return_data[0] != 0)
                    PGA46xStat_box.Text = "Ready; UART Addr = " + uartAddrCombo.Text;
                else
                    PGA46xStat_box.Text = "N/A or Wrong Addr";

                if (backColor == Color.Orange)
                    pga460_startup();

                Array.Clear(uart_return_data, 0, 64);
                firstTimeCheckforPGA++;
            }
            else
            {
                byte b2 = (byte)((Convert.ToByte(uartAddrCombo.Text) << addrShift) + uart_cmd9);
                regAddrByte = 31;
                MChecksumByte = calculate_UART_Checksum(new byte[] { b2, regAddrByte });
                u2a_status = common.u2a.UART_Write(4, new byte[] { 85, b2, regAddrByte, MChecksumByte });
                u2a_status = common.u2a.UART_Read(3, uart_return_data);
                uartDiagB = uart_return_data[0];
                if (uart_return_data[0] != 0)
                {
                    PGA46xStat_box.Text = "Ready; UART Addr = " + uartAddrCombo.Text;
                    pictureBox1.Image = Resources.green_button;
                    toolStripStatusLabel1.BackColor = Control.DefaultBackColor;
                    toolStripProgressBar1.Value = 100;
                }
                else
                {
                    PGA46xStat_box.Text = "N/A or Wrong Addr";
                    pictureBox1.Image = Resources.red_button;
                    if (USB_ConnStat_box.Text == "Connected")
                        toolStripProgressBar1.Value = 50;
                    else
                        toolStripProgressBar1.Value = 0;
                }
                Array.Clear(uart_return_data, 0, 64);
            }
            U2A u2a = common.u2a;
            u2a.SendCommand(40, new byte[1], 1);
        }

        public void u2a_uart_control_master()
        {
            common.u2a.UART_Control(
                (UART_BaudRate)uartBaudCombo.SelectedIndex,
                UART_Parity.None,
                UART_BitDirection.LSB_First,
                UART_CharacterLength._8_Bit,
                UART_StopBits.Two);
        }

        private int USB2ANY_send_rcv_raw(byte Command, byte[] Data, byte nBytes)
        {
            common.u2a.SendCommand(Command, Data, nBytes);
            int commandResponse = common.u2a.GetCommandResponse(Command, Data, nBytes);
            if (commandResponse < 0)
            {
                Array.Clear(Data, 0, Data.Length);
            }
            return commandResponse;
        }

        public void updateStatusBar(string msg)
        {
            ProgressUpdater.Update_EvalExamples(msg, 0, toolStripStatusLabel1, toolStripProgressBar1);
        }

        public void RegRequireDiagMode()
        {
        }

        private void Registers_createGrids(string MemSpace)
        {
            int num = 0;
            if (MemSpace == "USER")
            {
                int regSize = 8;
                int numRows = 91;
                int posX = 0;
                int posY = 3;
                int width = 450;
                int height = 1880;
                string tableRegSet = "GRID_USER_MEMSPACE";
                int num2 = 7;
                string[] optionalColHeaderLabels = new string[num2];
                bool useColors = true;
                string topLeftCellText = "Address (Register Name)";
                string colZeroText = "Value";
                bool gridIsReadOnly = false;
                Color flashColor = Tools.GoodColorsToUse(num++);
                double flashTime_ms = 5.0;
                int num3 = 91;
                string[] array = new string[num3];
                array[0] = "00 (USER_DATA1)";
                array[1] = "01 (USER_DATA2)";
                array[2] = "02 (USER_DATA3)";
                array[3] = "03 (USER_DATA4)";
                array[4] = "04 (USER_DATA5)";
                array[5] = "05 (USER_DATA6)";
                array[6] = "06 (USER_DATA7)";
                array[7] = "07 (USER_DATA8)";
                array[8] = "08 (USER_DATA9)";
                array[9] = "09 (USER_DATA10)";
                array[10] = "0A (USER_DATA11)";
                array[11] = "0B (USER_DATA12)";
                array[12] = "0C (USER_DATA13)";
                array[13] = "0D (USER_DATA14)";
                array[14] = "0E (USER_DATA15)";
                array[15] = "0F (USER_DATA16)";
                array[16] = "10 (USER_DATA17)";
                array[17] = "11 (USER_DATA18)";
                array[18] = "12 (USER_DATA19)";
                array[19] = "13 (USER_DATA20)";
                array[20] = "14 (TVGAIN0)";
                array[21] = "15 (TVGAIN1)";
                array[22] = "16 (TVGAIN2)";
                array[23] = "17 (TVGAIN3)";
                array[24] = "18 (TVGAIN4)";
                array[25] = "19 (TVGAIN5)";
                array[26] = "1A (TVGAIN6)";
                array[27] = "1B (INIT_GAIN)";
                array[28] = "1C (FREQUENCY)";
                array[29] = "1D (DEADTIME)";
                array[30] = "1E (PULSE_P1)";
                array[31] = "1F (PULSE_P2)";
                array[32] = "20 (CURR_LIM_P1)";
                array[33] = "21 (CURR_LIM_P2)";
                array[34] = "22 (REC_LENGTH)";
                array[35] = "23 (FREQ_DIAG)";
                array[36] = "24 (SAT_FDIAG_TH)";
                array[37] = "25 (FVOLT_DEC)";
                array[38] = "26 (DECPL_TEMP)";
                array[39] = "27 (DSP_SCALE)";
                array[40] = "28 (TEMP_TRIM)";
                array[41] = "29 (P1_GAIN_CTRL)";
                array[42] = "2A (P2_GAIN_CTRL)";
                array[43] = "2B (EE_CRC)";
                array[44] = "40 (EE_CNTRL)";
                array[45] = "41 (BPF_A2_MSB)";
                array[46] = "42 (BPF_A2_LSB)";
                array[47] = "43 (BPF_A3_MSB)";
                array[48] = "44 (BPF_A3_LSB)";
                array[49] = "45 (BPF_B1_MSB)";
                array[50] = "46 (BPF_B1_LSB)";
                array[51] = "47 (LPF_A2_MSB)";
                array[52] = "48 (LPF_A2_LSB)";
                array[53] = "49 (LPF_B1_MSB)";
                array[54] = "4A (LPF_B1_LSB)";
                array[55] = "4B (TEST_MUX)";
                array[56] = "4C (DEV_STAT0)";
                array[57] = "4D (DEV_STAT1)";
                array[58] = "5F (P1_THR_0)";
                array[59] = "60 (P1_THR_1)";
                array[60] = "61 (P1_THR_2)";
                array[61] = "62 (P1_THR_3)";
                array[62] = "63 (P1_THR_4)";
                array[63] = "64 (P1_THR_5)";
                array[64] = "65 (P1_THR_6)";
                array[65] = "66 (P1_THR_7)";
                array[66] = "67 (P1_THR_8)";
                array[67] = "68 (P1_THR_9)";
                array[68] = "69 (P1_THR_10)";
                array[69] = "6A (P1_THR_11)";
                array[70] = "6B (P1_THR_12)";
                array[71] = "6C (P1_THR_13)";
                array[72] = "6D (P1_THR_14)";
                array[73] = "6E (P1_THR_15)";
                array[74] = "6F (P2_THR_0)";
                array[75] = "70 (P2_THR_1)";
                array[76] = "71 (P2_THR_2)";
                array[77] = "72 (P2_THR_3)";
                array[78] = "73 (P2_THR_4)";
                array[79] = "74 (P2_THR_5)";
                array[80] = "75 (P2_THR_6)";
                array[81] = "76 (P2_THR_7)";
                array[82] = "77 (P2_THR_8)";
                array[83] = "78 (P2_THR_9)";
                array[84] = "79 (P2_THR_10)";
                array[85] = "7A (P2_THR_11)";
                array[86] = "7B (P2_THR_12)";
                array[87] = "7C (P2_THR_13)";
                array[88] = "7D (P2_THR_14)";
                array[89] = "7E (P2_THR_15)";
                array[90] = "7F (THR_CRC)";
                GRID_USER_MEMSPACE = new RegisterValueGridEditor(ConfigRegs, regSize, numRows, tableRegSet, posX, posY, height, width, optionalColHeaderLabels, array, topLeftCellText, colZeroText, useColors, gridIsReadOnly, flashColor, flashTime_ms);
                RegisterValueGridEditor grid_USER_MEMSPACE = GRID_USER_MEMSPACE;
                grid_USER_MEMSPACE.registerClickHandler = (RegisterValueGridEditor.RegisterClickedHadler)Delegate.Combine(grid_USER_MEMSPACE.registerClickHandler, new RegisterValueGridEditor.RegisterClickedHadler(OnRegisterClickHandle));
                RegisterValueGridEditor grid_USER_MEMSPACE2 = GRID_USER_MEMSPACE;
                grid_USER_MEMSPACE2.automaticUpdateHandler = (RegisterValueGridEditor.AutomatiCellUpdate)Delegate.Combine(grid_USER_MEMSPACE2.automaticUpdateHandler, new RegisterValueGridEditor.AutomatiCellUpdate(OnautoupdateHandle));
            }
            if (MemSpace == "DATA_DUMP")
            {
                int regSize = 8;
                int numRows = 128;
                int posX = 0;
                int posY = 3;
                int width = 450;
                int height = 2600;
                string tableRegSet = "GRID_DATADUMP_MEMSPACE";
                int num2 = 0;
                bool useColors = false;
                string topLeftCellText = "Address (Register Name)";
                string colZeroText = "Value";
                bool gridIsReadOnly = true;
                Color flashColor = Tools.GoodColorsToUse(num++);
                double flashTime_ms = 5.0;
                string[] optionalColHeaderLabels = new string[num2];
                string[] array = new string[128];
                array[0] = "80 (DATA_MEM_0)";
                array[1] = "81 (DATA_MEM_1)";
                array[2] = "82 (DATA_MEM_2)";
                array[3] = "83 (DATA_MEM_3)";
                array[4] = "84 (DATA_MEM_4)";
                array[5] = "85 (DATA_MEM_5)";
                array[6] = "86 (DATA_MEM_6)";
                array[7] = "87 (DATA_MEM_7)";
                array[8] = "88 (DATA_MEM_8)";
                array[9] = "89 (DATA_MEM_9)";
                array[10] = "8A (DATA_MEM_10)";
                array[11] = "8B (DATA_MEM_11)";
                array[12] = "8C (DATA_MEM_12)";
                array[13] = "8D (DATA_MEM_13)";
                array[14] = "8E (DATA_MEM_14)";
                array[15] = "8F (DATA_MEM_15)";
                array[16] = "90 (DATA_MEM_16)";
                array[17] = "91 (DATA_MEM_17)";
                array[18] = "92 (DATA_MEM_18)";
                array[19] = "93 (DATA_MEM_19)";
                array[20] = "94 (DATA_MEM_20)";
                array[21] = "95 (DATA_MEM_21)";
                array[22] = "96 (DATA_MEM_22)";
                array[23] = "97 (DATA_MEM_23)";
                array[24] = "98 (DATA_MEM_24)";
                array[25] = "99 (DATA_MEM_25)";
                array[26] = "9A (DATA_MEM_26)";
                array[27] = "9B (DATA_MEM_27)";
                array[28] = "9C (DATA_MEM_28)";
                array[29] = "9D (DATA_MEM_29)";
                array[30] = "9E (DATA_MEM_30)";
                array[31] = "9F (DATA_MEM_31)";
                array[32] = "A0 (DATA_MEM_32)";
                array[33] = "A1 (DATA_MEM_33)";
                array[34] = "A2 (DATA_MEM_34)";
                array[35] = "A3 (DATA_MEM_35)";
                array[36] = "A4 (DATA_MEM_36)";
                array[37] = "A5 (DATA_MEM_37)";
                array[38] = "A6 (DATA_MEM_38)";
                array[39] = "A7 (DATA_MEM_39)";
                array[40] = "A8 (DATA_MEM_40)";
                array[41] = "A9 (DATA_MEM_41)";
                array[42] = "AA (DATA_MEM_42)";
                array[43] = "AB (DATA_MEM_43)";
                array[44] = "AC (DATA_MEM_44)";
                array[45] = "AD (DATA_MEM_45)";
                array[46] = "AE (DATA_MEM_46)";
                array[47] = "AF (DATA_MEM_47)";
                array[48] = "B0 (DATA_MEM_48)";
                array[49] = "B1 (DATA_MEM_49)";
                array[50] = "B2 (DATA_MEM_50)";
                array[51] = "B3 (DATA_MEM_51)";
                array[52] = "B4 (DATA_MEM_52)";
                array[53] = "B5 (DATA_MEM_53)";
                array[54] = "B6 (DATA_MEM_54)";
                array[55] = "B7 (DATA_MEM_55)";
                array[56] = "B8 (DATA_MEM_56)";
                array[57] = "B9 (DATA_MEM_57)";
                array[58] = "BA (DATA_MEM_58)";
                array[59] = "BB (DATA_MEM_59)";
                array[60] = "BC (DATA_MEM_60)";
                array[61] = "BD (DATA_MEM_61)";
                array[62] = "BE (DATA_MEM_62)";
                array[63] = "BF (DATA_MEM_63)";
                array[64] = "C0 (DATA_MEM_64)";
                array[65] = "C1 (DATA_MEM_65)";
                array[66] = "C2 (DATA_MEM_66)";
                array[67] = "C3 (DATA_MEM_67)";
                array[68] = "C4 (DATA_MEM_68)";
                array[69] = "C5 (DATA_MEM_69)";
                array[70] = "C6 (DATA_MEM_70)";
                array[71] = "C7 (DATA_MEM_71)";
                array[72] = "C8 (DATA_MEM_72)";
                array[73] = "C9 (DATA_MEM_73)";
                array[74] = "CA (DATA_MEM_74)";
                array[75] = "CB (DATA_MEM_75)";
                array[76] = "CC (DATA_MEM_76)";
                array[77] = "CD (DATA_MEM_77)";
                array[78] = "CE (DATA_MEM_78)";
                array[79] = "CF (DATA_MEM_79)";
                array[80] = "D0 (DATA_MEM_80)";
                array[81] = "D1 (DATA_MEM_81)";
                array[82] = "D2 (DATA_MEM_82)";
                array[83] = "D3 (DATA_MEM_83)";
                array[84] = "D4 (DATA_MEM_84)";
                array[85] = "D5 (DATA_MEM_85)";
                array[86] = "D6 (DATA_MEM_86)";
                array[87] = "D7 (DATA_MEM_87)";
                array[88] = "D8 (DATA_MEM_88)";
                array[89] = "D9 (DATA_MEM_89)";
                array[90] = "DA (DATA_MEM_90)";
                array[91] = "DB (DATA_MEM_91)";
                array[92] = "DC (DATA_MEM_92)";
                array[93] = "DD (DATA_MEM_93)";
                array[94] = "DE (DATA_MEM_94)";
                array[95] = "DF (DATA_MEM_95)";
                array[96] = "E0 (DATA_MEM_96)";
                array[97] = "E1 (DATA_MEM_97)";
                array[98] = "E2 (DATA_MEM_98)";
                array[99] = "E3 (DATA_MEM_99)";
                array[100] = "E4 (DATA_MEM_100)";
                array[101] = "E5 (DATA_MEM_101)";
                array[102] = "E6 (DATA_MEM_102)";
                array[103] = "E7 (DATA_MEM_103)";
                array[104] = "E8 (DATA_MEM_104)";
                array[105] = "E9 (DATA_MEM_105)";
                array[106] = "EA (DATA_MEM_106)";
                array[107] = "EB (DATA_MEM_107)";
                array[108] = "EC (DATA_MEM_108)";
                array[109] = "ED (DATA_MEM_109)";
                array[110] = "EE (DATA_MEM_110)";
                array[111] = "EF (DATA_MEM_111)";
                array[112] = "F0 (DATA_MEM_112)";
                array[113] = "F1 (DATA_MEM_113)";
                array[114] = "F2 (DATA_MEM_114)";
                array[115] = "F3 (DATA_MEM_115)";
                array[116] = "F4 (DATA_MEM_116)";
                array[117] = "F5 (DATA_MEM_117)";
                array[118] = "F6 (DATA_MEM_118)";
                array[119] = "F7 (DATA_MEM_119)";
                array[120] = "F8 (DATA_MEM_120)";
                array[121] = "F9 (DATA_MEM_121)";
                array[122] = "FA (DATA_MEM_122)";
                array[123] = "FB (DATA_MEM_123)";
                array[124] = "FC (DATA_MEM_124)";
                array[125] = "FD (DATA_MEM_125)";
                array[126] = "FE (DATA_MEM_126)";
                array[127] = "FF (DATA_MEM_127)";
                GRID_DATADUMP_MEMSPACE = new RegisterValueGridEditor(DataDumpRegs, regSize, numRows, tableRegSet, posX, posY, height, width, optionalColHeaderLabels, array, topLeftCellText, colZeroText, useColors, gridIsReadOnly, flashColor, flashTime_ms);
            }
            if (MemSpace == "TI_EEPROM")
            {
                int regSize = 8;
                int numRows = 20;
                int posX = 0;
                int posY = 3;
                int width = 450;
                int height = 440;
                string tableRegSet = "GRID_TIEEPROM_MEMSPACE";
                int num2 = 0;
                int num3 = 20;
                string[] optionalColHeaderLabels = new string[num2];
                string[] array = new string[num3];
                bool useColors = true;
                string topLeftCellText = "Address (Register Name)";
                string colZeroText = "Value";
                bool gridIsReadOnly = false;
                Color flashColor = Tools.GoodColorsToUse(num++);
                double flashTime_ms = 5.0;
                array[0] = "2C (LOT_A)";
                array[1] = "2D (LOT_B)";
                array[2] = "2E (LOT_C)";
                array[3] = "2F (LAB_WAFER)";
                array[4] = "30 (COORD_X)";
                array[5] = "31 (COORD_Y)";
                array[6] = "32 (TI_TRIM1)";
                array[7] = "33 (TI_TRIM2)";
                array[8] = "34 (TI_TRIM3)";
                array[9] = "35 (TI_TRIM4)";
                array[10] = "36 (TI_TRIM5)";
                array[11] = "37 (TI_TRIM6)";
                array[12] = "38 (TI_TRIM7)";
                array[13] = "39 (TI_TRIM8)";
                array[14] = "3A (TI_TRIM9)";
                array[15] = "3B (TI_TRIM10)";
                array[16] = "3C (TI_TRIM11)";
                array[17] = "3D (TI_TRIM12)";
                array[18] = "3E (TI_TRIM13)";
                array[19] = "3F (TI_TRIM_CRC)";
                GRID_TIEEPROM_MEMSPACE = new RegisterValueGridEditor(TI_EEPROM_Regs, regSize, numRows, tableRegSet, posX, posY, height, width, optionalColHeaderLabels, array, topLeftCellText, colZeroText, useColors, gridIsReadOnly, flashColor, flashTime_ms);
            }
            if (MemSpace == "TI_TESTMODE")
            {
                GRID_TITESTMODE_MEMSPACE = new RegisterValueGridEditor(
                    TI_TM_Regs, 8, 17, "GRID_TITESTMODE_MEMSPACE",
                    0, 3, 440, 450,
                    new string[0],
                    new string[] {
                        "4E (TI_EE_CTRL)",
                        "4F (TI_EE_DIRECT)",
                        "50 (TI_EE_DD_LSB)",
                        "51 (TI_EE_DD_MSB)",
                        "52 (TI_EE_DO_LSB)",
                        "53 (TI_EE_DO_MSB)",
                        "54 (TI_EE_MARGIN)",
                        "55 (TI_TEST_MUX)",
                        "56 (TI_DECPL_MUX)",
                        "57 (TI_EE_CTRL_2)",
                        "58 (TM_CTRL_1)",
                        "59 (EE_CRC_OUT)",
                        "5A (TM_CTRL_2)",
                        "5B (TM_STAT_1)",
                        "5C (TM_CTRL_3)",
                        "5D (TM_CTRL_4)",
                        "5E (TM_CTRL_5)"},
                    "Address (Register Name)", "Value", true, false, Tools.GoodColorsToUse(num++), 5.0
                    );
            }
        }

        private void Dispose_grids()
        {
            try
            {
                GRID_TIEEPROM_MEMSPACE.Dispose();
                GRID_TITESTMODE_MEMSPACE.Dispose();
                GRID_USER_MEMSPACE.Dispose();
                GRID_USERDATA_MEMSPACE.Dispose();
                GRID_THRESHOLD_MEMSPACE.Dispose();
                GRID_DATADUMP_MEMSPACE.Dispose();
            }
            catch (NullReferenceException)
            {
            }
        }

        private void proj_MainCommunication(RegisterValueGridEditor selected_grid, bool Write_notRead, bool Single_notAll, bool pagechange = false)
        {
            activateProgressBar(true);
            byte b = 85;
            byte b2 = 0;
            byte b3 = 0;
            byte b4 = 0;

            firstReadAllPass = (!Single_notAll);

            int num;
            int num2;
            if (selected_grid == GRID_TIEEPROM_MEMSPACE)
            {
                SelectedGrid = "GRID_TIEEPROM_MEMSPACE";
                num = 44;
                num2 = 64;
            }
            else if (selected_grid == GRID_TITESTMODE_MEMSPACE)
            {
                SelectedGrid = "GRID_TITESTMODE_MEMSPACE";
                num = 78;
                num2 = 95;
            }
            else if (selected_grid == GRID_DATADUMP_MEMSPACE)
            {
                SelectedGrid = "GRID_DATADUMP_MEMSPACE";
                num = 128;
                num2 = 256;
            }
            else
            {
                SelectedGrid = "GRID_USER_MEMSPACE";
                num = 0;
                num2 = 128;
            }
            int num3 = 0;
            PrevSelectedGrid = SelectedGrid;
            string[,] array;
            if (!Single_notAll)
            {
                string[] addressGridList = selected_grid.getAddressGridList();
                selected_grid.HighlightRows(addressGridList);
                array = selected_grid.Get_HIGHLIGHTED_Grid_Values__R_D_A_O(false);
            }
            else
            {
                string[] addressGridList = selected_grid.getAddressGridList();
                array = selected_grid.Get_HIGHLIGHTED_Grid_Values__R_D_A_O(true);
            }
            int numberOfHighlighedRows = selected_grid.getNumberOfHighlighedRows();
            toolStripProgressBar1.Value = 25;
            if (numberOfHighlighedRows != 0)
            {
                int num4;
                if (Single_notAll)
                {
                    num += Convert.ToInt32(array[0, 0]);
                    if (SelectedGrid == "GRID_USER_MEMSPACE" && num > 43)
                    {
                        num += 20;
                        if (SelectedGrid == "GRID_USER_MEMSPACE" && num > 77)
                            num += 17;
                    }
                    num2 = num + 1;
                    num3 = 0;
                    string[,] array2 = new string[numberOfHighlighedRows, numberOfHighlighedRows * 5];
                    for (int i = 0; i < numberOfHighlighedRows; i++)
                    {
                        array2[i, 0] = array[i, 0];
                        array2[i, 1] = array[i, 1];
                        array2[i, 2] = array[i, 2];
                        array2[i, 3] = array[i, 3];
                        array2[i, 4] = array[i, 4];
                    }
                    num4 = array2.GetLength(0);
                }
                toolStripProgressBar1.Value = 50;
                common.u2a.UART_Read(64, uart_return_data);
                Array.Clear(uart_return_data, 0, 64);
                u2a_uart_control_master();
                num4 = 1;
                for (int j = 0; j < num4; j++)
                {
                    if (Single_notAll && j > 0)
                    {
                        num += j;
                        num2 += j;
                    }
                    for (int k = num; k < num2; k++)
                    {
                        byte b5 = Convert.ToByte(k);
                        if (Write_notRead && !Single_notAll && ((k > 75 && k < 95) || k == 64))
                        {
                            num3++;
                        }
                        else
                        {
                            if (Write_notRead)
                            {
                                b2 = (byte)(((int)Convert.ToByte(uartAddrCombo.Text) << addrShift) + (int)uart_cmd10);
                                b3 = Tools.StringBase16IntoByte(array[num3, 2].Substring(0, 2));
                                b4 = calculate_UART_Checksum(new byte[] { b2, b5, b3 });
                            }
                            if (!Write_notRead)
                            {
                                b2 = (byte)(((int)Convert.ToByte(uartAddrCombo.Text) << addrShift) + (int)uart_cmd9);
                                b4 = calculate_UART_Checksum(new byte[] { b2, b5 });
                            }
                            if (Write_notRead)
                            {
                                u2a_status = common.u2a.UART_Write(5, new byte[] { b, b2, b5, b3, b4 });
                                u2a_status = common.u2a.UART_Read(3, uart_return_data);
                                num3++;
                            }
                            if (!Write_notRead)
                            {
                                u2a_status = common.u2a.UART_Write(4, new byte[] { b, b2, b5, b4 });
                                Tools.timeDelay(1, "MS");
                                u2a_status = common.u2a.UART_Read(3, uart_return_data);
                                if (!Single_notAll)
                                {
                                    array[num3, 4] = Tools.int32_Into_stringBase16((int)uart_return_data[1], 8);
                                    num3++;
                                }
                            }
                        }
                        if (!Single_notAll)
                        {
                            if (!Write_notRead && SelectedGrid == "GRID_USER_MEMSPACE" && k == 43)
                            {
                                k = 63;
                                toolStripProgressBar1.Value = 70;
                            }
                            if (!Write_notRead && SelectedGrid == "GRID_USER_MEMSPACE" && k == 77)
                            {
                                k = 94;
                                toolStripProgressBar1.Value = 80;
                            }
                            if (Write_notRead && SelectedGrid == "GRID_USER_MEMSPACE" && k == 43)
                            {
                                k = 63;
                                toolStripProgressBar1.Value = 70;
                            }
                            if (Write_notRead && SelectedGrid == "GRID_USER_MEMSPACE" && k == 77)
                            {
                                k = 94;
                                toolStripProgressBar1.Value = 80;
                            }
                        }
                    }
                    uartDiagB = uart_return_data[0];
                    toolStripProgressBar1.Value = 90;
                    if (Write_notRead && !Single_notAll)
                    {
                        selected_grid.saveAllValues();
                    }
                    else
                    {
                        if (!Write_notRead && !Single_notAll)
                        {
                            selected_grid.setDataIntoGridDataArray(array, false);
                            selected_grid.saveAllValues();
                        }
                        if ((!Write_notRead || Write_notRead) && Single_notAll)
                        {
                            string[,] array3 = new string[1, 5];
                            array3[0, 0] = array[j, 0];
                            array3[0, 1] = array[j, 1];
                            if (!Write_notRead && Single_notAll)
                            {
                                array3[0, 4] = Tools.int32_Into_stringBase16((int)uart_return_data[1], 8);
                            }
                            updateMemoryMap(array3, SelectedGrid);
                        }
                    }

                    if (!pagechange)
                        updateHighLevelPages(array);
                    toolStripProgressBar1.Value = 100;
                }
                activateProgressBar(false);
            }
        }

        private void updateMemoryMap(string[,] RowValues_ROW_A_D_O_R, string gridSel)
        {
            if (gridSel == "GRID_USER_MEMSPACE")
            {
                GRID_USER_MEMSPACE.setDataIntoGridDataArray(RowValues_ROW_A_D_O_R, true);
                GRID_USER_MEMSPACE.saveAllValues();
            }
            if (gridSel == "GRID_DATADUMP_MEMSPACE")
            {
                GRID_DATADUMP_MEMSPACE.setDataIntoGridDataArray(RowValues_ROW_A_D_O_R, true);
                GRID_DATADUMP_MEMSPACE.saveAllValues();
            }
            if (gridSel == "GRID_THRESHOLD_MEMSPACE")
            {
                GRID_THRESHOLD_MEMSPACE.setDataIntoGridDataArray(RowValues_ROW_A_D_O_R, true);
                GRID_THRESHOLD_MEMSPACE.saveAllValues();
            }
            if (gridSel == "GRID_USERDATA_MEMSPACE")
            {
                GRID_USERDATA_MEMSPACE.setDataIntoGridDataArray(RowValues_ROW_A_D_O_R, true);
                GRID_USERDATA_MEMSPACE.saveAllValues();
            }
            if (gridSel == "GRID_TITESTMODE_MEMSPACE")
            {
                GRID_TITESTMODE_MEMSPACE.setDataIntoGridDataArray(RowValues_ROW_A_D_O_R, true);
                GRID_TITESTMODE_MEMSPACE.saveAllValues();
            }
            if (gridSel == "GRID_TIEEPROM_MEMSPACE")
            {
                GRID_TIEEPROM_MEMSPACE.setDataIntoGridDataArray(RowValues_ROW_A_D_O_R, true);
                GRID_TIEEPROM_MEMSPACE.saveAllValues();
            }
        }

        private void updateHighLevelPages(string[,] RowValues_ROW_A_D_O_R)
        {
            if (RowValues_ROW_A_D_O_R[0, 0] != null)
            {
                int i = 0;
                while (i < RowValues_ROW_A_D_O_R.GetLength(0))
                {
                    RegDefs regDefs = new RegDefs(RowValues_ROW_A_D_O_R[i, 1]);
                    int num;
                    if (RowValues_ROW_A_D_O_R[i, 4] == null)
                        num = 2;
                    else
                        num = 4;

                    byte int32_Input = Tools.StringBase16IntoByte(RowValues_ROW_A_D_O_R[i, num].Substring(0, 2));
                    string text = Tools.StringBase16_Into_StringBase2(Tools.int32_Into_stringBase16((int)int32_Input, 8), 8, true);
                    text = new string(text.ToCharArray());
                    string text2 = RowValues_ROW_A_D_O_R[i, 1];
                    switch (text2)
                    {
                        case "14 (TVGAIN0)":
                            foreach (Bit_Field bit_Field in regDefs.TVGAIN0.bit_fields)
                                bit_Field.value = text.Substring(bit_Field.start_index, bit_Field.length);

                            try
                            {
                                tvgt0.SelectedIndex = Convert.ToInt32(Tools.StringBase2_Into_StringBase16(regDefs.TVG_T0.value, 8), 16);
                            }
                            catch { }
                            try
                            {
                                tvgt1.SelectedIndex = Convert.ToInt32(Tools.StringBase2_Into_StringBase16(regDefs.TVG_T1.value, 8), 16);
                            }
                            catch { }
                            break;
                        case "15 (TVGAIN1)":
                            foreach (Bit_Field bit_Field in regDefs.TVGAIN1.bit_fields)
                                bit_Field.value = text.Substring(bit_Field.start_index, bit_Field.length);
                            try
                            {
                                tvgt2.SelectedIndex = Convert.ToInt32(Tools.StringBase2_Into_StringBase16(regDefs.TVG_T2.value, 8), 16);
                            }
                            catch { }
                            try
                            {
                                tvgt3.SelectedIndex = Convert.ToInt32(Tools.StringBase2_Into_StringBase16(regDefs.TVG_T3.value, 8), 16);
                            }
                            catch { }
                            break;
                        case "16 (TVGAIN2)":
                            foreach (Bit_Field bit_Field in regDefs.TVGAIN2.bit_fields)
                                bit_Field.value = text.Substring(bit_Field.start_index, bit_Field.length);
                            try
                            {
                                tvgt4.SelectedIndex = Convert.ToInt32(Tools.StringBase2_Into_StringBase16(regDefs.TVG_T4.value, 8), 16);
                            }
                            catch { }
                            try
                            {
                                tvgt5.SelectedIndex = Convert.ToInt32(Tools.StringBase2_Into_StringBase16(regDefs.TVG_T5.value, 8), 16);
                            }
                            catch { }
                            break;
                        case "17 (TVGAIN3)":
                            foreach (Bit_Field bit_Field in regDefs.TVGAIN3.bit_fields)
                                bit_Field.value = text.Substring(bit_Field.start_index, bit_Field.length);
                            try
                            {
                                tvgg1.SelectedIndex = Convert.ToInt32(Tools.StringBase2_Into_StringBase16(regDefs.TVG_G1.value, 8), 16);
                            }
                            catch { }

                            try
                            {
                                tvgg2_merged_pt1 = Convert.ToInt32(Tools.StringBase2_Into_StringBase16(regDefs.TVG_G2a.value, 8), 16) << 4;
                                tvgg2_merged_flag = true;
                            }
                            catch { }
                            break;
                        case "18 (TVGAIN4)":
                            foreach (Bit_Field bit_Field in regDefs.TVGAIN4.bit_fields)
                                bit_Field.value = text.Substring(bit_Field.start_index, bit_Field.length);
                            try
                            {
                                tvgg2_merged_pt2 = Convert.ToInt32(Tools.StringBase2_Into_StringBase16(regDefs.TVG_G2b.value, 8), 16);
                                tvgg2_merged_flag = true;
                            }
                            catch { }
                            try
                            {
                                tvgg3_merged_pt1 = Convert.ToInt32(Tools.StringBase2_Into_StringBase16(regDefs.TVG_G3a.value, 8), 16) << 2;
                                tvgg3_merged_flag = true;
                            }
                            catch { }
                            break;
                        case "19 (TVGAIN5)":
                            foreach (Bit_Field bit_Field in regDefs.TVGAIN5.bit_fields)
                                bit_Field.value = text.Substring(bit_Field.start_index, bit_Field.length);
                            try
                            {
                                tvgg3_merged_pt2 = Convert.ToInt32(Tools.StringBase2_Into_StringBase16(regDefs.TVG_G3b.value, 8), 16);
                                tvgg3_merged_flag = true;
                            }
                            catch { }
                            try
                            {
                                tvgg4.SelectedIndex = Convert.ToInt32(Tools.StringBase2_Into_StringBase16(regDefs.TVG_G4.value, 8), 16);
                            }
                            catch { }
                            break;
                        case "1A (TVGAIN6)":
                            foreach (Bit_Field bit_Field in regDefs.TVGAIN6.bit_fields)
                                bit_Field.value = text.Substring(bit_Field.start_index, bit_Field.length);
                            try
                            {
                                tvgg5.SelectedIndex = Convert.ToInt32(Tools.StringBase2_Into_StringBase16(regDefs.TVG_G5.value, 8), 16);
                            }
                            catch { }
                            try
                            {
                                if (Convert.ToInt32(Tools.StringBase2_Into_StringBase16(regDefs.FREQ_SHIFT.value, 16)) == 1)
                                    freqshiftCheck.Checked = true;
                                else
                                    freqshiftCheck.Checked = false;
                            }
                            catch { }
                            break;
                        case "1B (INIT_GAIN)":
                            foreach (Bit_Field bit_Field in regDefs.INIT_GAIN.bit_fields)
                                bit_Field.value = text.Substring(bit_Field.start_index, bit_Field.length);
                            try
                            {
                                gainCombo.SelectedIndex = Convert.ToInt32(Tools.StringBase2_Into_StringBase16(regDefs.GAIN_INIT.value, 8), 16);
                                tvgg0.SelectedIndex = gainCombo.SelectedIndex;
                                gainVVBoxUpdate();
                            }
                            catch { }
                            try
                            {
                                bpbwCombo.SelectedIndex = Convert.ToInt32(Tools.StringBase2_Into_StringBase16(regDefs.BPF_BW.value, 8), 16);
                            }
                            catch { }
                            break;
                        case "1C (FREQUENCY)":
                            foreach (Bit_Field bit_Field in regDefs.FREQUENCY.bit_fields)
                                bit_Field.value = text.Substring(bit_Field.start_index, bit_Field.length);
                            try
                            {
                                freqCombo.SelectedIndex = Convert.ToInt32(Tools.StringBase2_Into_StringBase16(regDefs.FREQ.value, 8), 16);
                            }
                            catch { }
                            break;
                        case "1D (DEADTIME)":
                            foreach (Bit_Field bit_Field in regDefs.DEADTIME.bit_fields)
                                bit_Field.value = text.Substring(bit_Field.start_index, bit_Field.length);
                            try
                            {
                                thrCmpDeglitchCombo.SelectedIndex = Convert.ToInt32(Tools.StringBase2_Into_StringBase16(regDefs.THR_CMP_DEGLTCH.value, 8), 16);
                            }
                            catch { }
                            try
                            {
                                deadCombo.SelectedIndex = Convert.ToInt32(Tools.StringBase2_Into_StringBase16(regDefs.PULSE_DT.value, 8), 16);
                            }
                            catch { }
                            break;
                        case "1E (PULSE_P1)":
                            foreach (Bit_Field bit_Field in regDefs.PULSE_P1.bit_fields)
                                bit_Field.value = text.Substring(bit_Field.start_index, bit_Field.length);
                            try
                            {
                                if (Convert.ToInt32(Tools.StringBase2_Into_StringBase16(regDefs.IO_IF_SEL.value, 16)) == 1)
                                    owuRadio.Checked = true;
                                else
                                    tbiRadio.Checked = true;
                            }
                            catch { }
                            try
                            {
                                if (Convert.ToInt32(Tools.StringBase2_Into_StringBase16(regDefs.UART_DIAG.value, 16)) == 1)
                                    uartDiagSysRadio.Checked = true;
                                else
                                    uartDiagUARTRadio.Checked = true;
                            }
                            catch { }
                            try
                            {
                                if (Convert.ToInt32(Tools.StringBase2_Into_StringBase16(regDefs.IO_DIS.value, 16)) == 1)
                                    ioTransEnCheck.Checked = false;
                                else
                                    ioTransEnCheck.Checked = true;
                            }
                            catch { }
                            try
                            {
                                p1PulsesCombo.SelectedIndex = Convert.ToInt32(Tools.StringBase2_Into_StringBase16(regDefs.P1_PULSE.value, 8), 16);
                            }
                            catch { }
                            break;
                        case "1F (PULSE_P2)":
                            foreach (Bit_Field bit_Field in regDefs.PULSE_P2.bit_fields)
                                bit_Field.value = text.Substring(bit_Field.start_index, bit_Field.length);
                            try
                            {
                                p2PulsesCombo.SelectedIndex = Convert.ToInt32(Tools.StringBase2_Into_StringBase16(regDefs.P2_PULSE.value, 8), 16);
                            }
                            catch { }
                            try
                            {
                                uartAddrCombo.SelectedIndex = Convert.ToInt32(Tools.StringBase2_Into_StringBase16(regDefs.UART_ADDR.value, 8), 16);
                            }
                            catch { }
                            break;
                        case "20 (CURR_LIM_P1)":
                            foreach (Bit_Field bit_Field in regDefs.CURR_LIM_P1.bit_fields)
                                bit_Field.value = text.Substring(bit_Field.start_index, bit_Field.length);
                            try
                            {
                                p1DriveCombo.SelectedIndex = Convert.ToInt32(Tools.StringBase2_Into_StringBase16(regDefs.CURR_LIM1.value, 8), 16);
                            }
                            catch { }
                            try
                            {
                                if (Convert.ToInt32(Tools.StringBase2_Into_StringBase16(regDefs.DIS_CL.value, 16)) == 1)
                                    disableCurrentLimitBox.Checked = true;
                                else
                                    disableCurrentLimitBox.Checked = false;
                            }
                            catch { }
                            try
                            {
                                if (Convert.ToInt32(Tools.StringBase2_Into_StringBase16(regDefs.IDLE_MD_DIS.value, 16)) == 1)
                                    idleMdCheck.Checked = true;
                                else
                                    idleMdCheck.Checked = false;
                            }
                            catch { }
                            break;
                        case "21 (CURR_LIM_P2)":
                            foreach (Bit_Field bit_Field in regDefs.CURR_LIM_P2.bit_fields)
                                bit_Field.value = text.Substring(bit_Field.start_index, bit_Field.length);
                            try
                            {
                                p2DriveCombo.SelectedIndex = Convert.ToInt32(Tools.StringBase2_Into_StringBase16(regDefs.CURR_LIM2.value, 8), 16);
                            }
                            catch { }
                            try
                            {
                                cutoffCombo.SelectedIndex = Convert.ToInt32(Tools.StringBase2_Into_StringBase16(regDefs.LPF_CO.value, 8), 16);
                            }
                            catch { }
                            break;
                        case "22 (REC_LENGTH)":
                            foreach (Bit_Field bit_Field in regDefs.REC_LENGTH.bit_fields)
                                bit_Field.value = text.Substring(bit_Field.start_index, bit_Field.length);
                            try
                            {
                                p1RecordCombo.SelectedIndex = Convert.ToInt32(Tools.StringBase2_Into_StringBase16(regDefs.P1_REC.value, 8), 16);
                                p1MaxDistBox.Text = Convert.ToString(Math.Truncate(Convert.ToDouble(p1RecordCombo.Text) / 1000.0 * 343.0 / 2.0 * 1000.0) / 1000.0);
                            }
                            catch { }
                            try
                            {
                                p2RecordCombo.SelectedIndex = Convert.ToInt32(Tools.StringBase2_Into_StringBase16(regDefs.P2_REC.value, 8), 16);
                                p2MaxDistBox.Text = Convert.ToString(Math.Truncate(Convert.ToDouble(p2RecordCombo.Text) / 1000.0 * 343.0 / 2.0 * 1000.0) / 1000.0);
                            }
                            catch { }
                            break;
                        case "23 (FREQ_DIAG)":
                            foreach (Bit_Field bit_Field in regDefs.FREQ_DIAG.bit_fields)
                                bit_Field.value = text.Substring(bit_Field.start_index, bit_Field.length);
                            try
                            {
                                freqDiagWinLengthCombo.SelectedIndex = Convert.ToInt32(Tools.StringBase2_Into_StringBase16(regDefs.FDIAG_LEN.value, 8), 16);
                            }
                            catch { }
                            try
                            {
                                freqDiagStartTimeCombo.SelectedIndex = Convert.ToInt32(Tools.StringBase2_Into_StringBase16(regDefs.FDIAG_START.value, 8), 16);
                            }
                            catch { }
                            break;
                        case "24 (SAT_FDIAG_TH)":
                            foreach (Bit_Field bit_Field in regDefs.SAT_FDIAG_TH.bit_fields)
                                bit_Field.value = text.Substring(bit_Field.start_index, bit_Field.length);
                            try
                            {
                                satDiagThrLvlCombo.SelectedIndex = Convert.ToInt32(Tools.StringBase2_Into_StringBase16(regDefs.SAT_TH.value, 8), 16);
                            }
                            catch { }
                            try
                            {
                                freqDiagErrorTimeThrCombo.SelectedIndex = Convert.ToInt32(Tools.StringBase2_Into_StringBase16(regDefs.FDIAG_ERR_TH.value, 8), 16);
                            }
                            catch
                            {
                                freqDiagErrorTimeThrCombo.SelectedIndex = 0;
                            }
                            try
                            {
                                if (Convert.ToInt32(Tools.StringBase2_Into_StringBase16(regDefs.P1_NLS_EN.value, 16)) == 1)
                                    p1NLSEnBox.Checked = true;
                                else
                                    p1NLSEnBox.Checked = false;
                            }
                            catch { }
                            break;
                        case "25 (FVOLT_DEC)":
                            foreach (Bit_Field bit_Field in regDefs.FVOLT_DEC.bit_fields)
                                bit_Field.value = text.Substring(bit_Field.start_index, bit_Field.length);
                            try
                            {
                                if (Convert.ToInt32(Tools.StringBase2_Into_StringBase16(regDefs.P2_NLS_EN.value, 16)) == 1)
                                    p2NLSEnBox.Checked = true;
                                else
                                    p2NLSEnBox.Checked = false;
                            }
                            catch { }
                            try
                            {
                                ovthrCombo.SelectedIndex = Convert.ToInt32(Tools.StringBase2_Into_StringBase16(regDefs.VPWR_OV_TH.value, 8), 16);
                            }
                            catch { }
                            try
                            {
                                lowpowEnterTimeCombo.SelectedIndex = Convert.ToInt32(Tools.StringBase2_Into_StringBase16(regDefs.LPM_TMR.value, 8), 16);
                            }
                            catch { }
                            try
                            {
                                voltaDiagErrThrCombo.SelectedIndex = Convert.ToInt32(Tools.StringBase2_Into_StringBase16(regDefs.FVOLT_ERR_TH.value, 8), 16);
                            }
                            catch { }
                            break;
                        case "26 (DECPL_TEMP)":
                            foreach (Bit_Field bit_Field in regDefs.DECPL_TEMP.bit_fields)
                                bit_Field.value = text.Substring(bit_Field.start_index, bit_Field.length);
                            try
                            {
                                AFEGainRngCombo.SelectedIndex = Convert.ToInt32(Tools.StringBase2_Into_StringBase16(regDefs.AFE_GAIN_RNG.value, 8), 16);
                            }
                            catch { }
                            try
                            {
                                if (Convert.ToInt32(Tools.StringBase2_Into_StringBase16(regDefs.LPM_EN.value, 16)) == 1)
                                    lowpowEnCheck.Checked = true;
                                else
                                    lowpowEnCheck.Checked = false;
                            }
                            catch { }
                            try
                            {
                                if (Convert.ToInt32(Tools.StringBase2_Into_StringBase16(regDefs.DECPL_TEMP_SEL.value, 16)) == 1)
                                    decoupletempRadio.Checked = true;
                                else
                                    decoupletimeRadio.Checked = true;
                            }
                            catch { }
                            try
                            {
                                decoupletimeBox.SelectedIndex = Convert.ToInt32(Tools.StringBase2_Into_StringBase16(regDefs.DECPL_T.value, 8), 16);
                                decoupletempBox.SelectedIndex = Convert.ToInt32(Tools.StringBase2_Into_StringBase16(regDefs.DECPL_T.value, 8), 16);
                            }
                            catch { }
                            break;
                        case "27 (DSP_SCALE)":
                            foreach (Bit_Field bit_Field in regDefs.DSP_SCALE.bit_fields)
                                bit_Field.value = text.Substring(bit_Field.start_index, bit_Field.length);
                            try
                            {
                                nlsNoiseCombo.SelectedIndex = Convert.ToInt32(Tools.StringBase2_Into_StringBase16(regDefs.NOISE_LVL.value, 8), 16);
                            }
                            catch { }
                            try
                            {
                                nlsSECombo.SelectedIndex = Convert.ToInt32(Tools.StringBase2_Into_StringBase16(regDefs.SCALE_K.value, 8), 16);
                            }
                            catch
                            {
                            }
                            try
                            {
                                nlsTOPCombo.SelectedIndex = Convert.ToInt32(Tools.StringBase2_Into_StringBase16(regDefs.SCALE_N.value, 8), 16);
                                updateTimeOffsetTextBox();
                            }
                            catch
                            {
                            }
                            break;
                        case "28 (TEMP_TRIM)":
                            foreach (Bit_Field bit_Field in regDefs.TEMP_TRIM.bit_fields)
                            {
                                bit_Field.value = text.Substring(bit_Field.start_index, bit_Field.length);
                            }
                            try
                            {
                                tempgainCombo.SelectedIndex = Convert.ToInt32(Tools.StringBase2_Into_StringBase16(regDefs.TEMP_GAIN.value, 8), 16);
                            }
                            catch
                            {
                            }
                            try
                            {
                                tempoffsetCombo.SelectedIndex = Convert.ToInt32(Tools.StringBase2_Into_StringBase16(regDefs.TEMP_OFF.value, 8), 16);
                            }
                            catch
                            {
                            }
                            break;
                        case "29 (P1_GAIN_CTRL)":
                            foreach (Bit_Field bit_Field in regDefs.P1_GAIN_CTRL.bit_fields)
                            {
                                bit_Field.value = text.Substring(bit_Field.start_index, bit_Field.length);
                            }
                            try
                            {
                                p1DigGainLrSt.SelectedIndex = Convert.ToInt32(Tools.StringBase2_Into_StringBase16(regDefs.P1_DIG_GAIN_LR_ST.value, 8), 16);
                            }
                            catch
                            {
                            }
                            try
                            {
                                p1DigGainLr.SelectedIndex = Convert.ToInt32(Tools.StringBase2_Into_StringBase16(regDefs.P1_DIG_GAIN_LR.value, 8), 16);
                            }
                            catch
                            {
                            }
                            try
                            {
                                p1DigGainSr.SelectedIndex = Convert.ToInt32(Tools.StringBase2_Into_StringBase16(regDefs.P1_DIG_GAIN_SR.value, 8), 16);
                            }
                            catch
                            {
                            }
                            break;
                        case "2A (P2_GAIN_CTRL)":
                            foreach (Bit_Field bit_Field in regDefs.P2_GAIN_CTRL.bit_fields)
                            {
                                bit_Field.value = text.Substring(bit_Field.start_index, bit_Field.length);
                            }
                            try
                            {
                                p2DigGainLrSt.SelectedIndex = Convert.ToInt32(Tools.StringBase2_Into_StringBase16(regDefs.P2_DIG_GAIN_LR_ST.value, 8), 16);
                            }
                            catch
                            {
                            }
                            try
                            {
                                p2DigGainLr.SelectedIndex = Convert.ToInt32(Tools.StringBase2_Into_StringBase16(regDefs.P2_DIG_GAIN_LR.value, 8), 16);
                            }
                            catch
                            {
                            }
                            try
                            {
                                p2DigGainSr.SelectedIndex = Convert.ToInt32(Tools.StringBase2_Into_StringBase16(regDefs.P2_DIG_GAIN_SR.value, 8), 16);
                            }
                            catch
                            {
                            }
                            break;
                        case "40 (EE_CNTRL)":
                            foreach (Bit_Field bit_Field in regDefs.EE_CNTRL.bit_fields)
                            {
                                bit_Field.value = text.Substring(bit_Field.start_index, bit_Field.length);
                            }
                            break;
                        case "4B (TEST_MUX)":
                            foreach (Bit_Field bit_Field in regDefs.TEST_MUX.bit_fields)
                            {
                                bit_Field.value = text.Substring(bit_Field.start_index, bit_Field.length);
                            }
                            try
                            {
                                datapathMuxSelCombo.SelectedIndex = Convert.ToInt32(Tools.StringBase2_Into_StringBase16(regDefs.DP_MUX.value, 8), 16);
                            }
                            catch
                            {
                                datapathMuxSelCombo.SelectedIndex = 0;
                            }
                            try
                            {
                                if (Convert.ToInt32(Tools.StringBase2_Into_StringBase16(regDefs.SAMPLE_SEL.value, 16)) == 1)
                                {
                                    sampleOut12bitRadio.Checked = true;
                                    sampleOut8bitRadio.Checked = false;
                                }
                                else
                                {
                                    sampleOut12bitRadio.Checked = false;
                                    sampleOut8bitRadio.Checked = true;
                                }
                            }
                            catch
                            {
                                releasemultiCheck.Checked = false;
                            }
                            try
                            {
                                muxOutTestCombo.SelectedIndex = Convert.ToInt32(Tools.StringBase2_Into_StringBase16(regDefs.TEST_MUX_B.value, 8), 16);
                            }
                            catch
                            {
                            }
                            break;
                        case "5F (P1_THR_0)":
                            foreach (Bit_Field bit_Field in regDefs.P1_THR_0.bit_fields)
                            {
                                bit_Field.value = text.Substring(bit_Field.start_index, bit_Field.length);
                            }
                            try
                            {
                                p1t1.SelectedIndex = Convert.ToInt32(Tools.StringBase2_Into_StringBase16(regDefs.TH_P1_T1.value, 8), 16);
                            }
                            catch
                            {
                            }
                            try
                            {
                                p1t2.SelectedIndex = Convert.ToInt32(Tools.StringBase2_Into_StringBase16(regDefs.TH_P1_T2.value, 8), 16);
                            }
                            catch
                            {
                            }
                            break;
                        case "60 (P1_THR_1)":
                            foreach (Bit_Field bit_Field in regDefs.P1_THR_1.bit_fields)
                            {
                                bit_Field.value = text.Substring(bit_Field.start_index, bit_Field.length);
                            }
                            try
                            {
                                p1t3.SelectedIndex = Convert.ToInt32(Tools.StringBase2_Into_StringBase16(regDefs.TH_P1_T3.value, 8), 16);
                            }
                            catch
                            {
                            }
                            try
                            {
                                p1t4.SelectedIndex = Convert.ToInt32(Tools.StringBase2_Into_StringBase16(regDefs.TH_P1_T4.value, 8), 16);
                            }
                            catch
                            {
                            }
                            break;
                        case "61 (P1_THR_2)":
                            foreach (Bit_Field bit_Field in regDefs.P1_THR_2.bit_fields)
                            {
                                bit_Field.value = text.Substring(bit_Field.start_index, bit_Field.length);
                            }
                            try
                            {
                                p1t5.SelectedIndex = Convert.ToInt32(Tools.StringBase2_Into_StringBase16(regDefs.TH_P1_T5.value, 8), 16);
                            }
                            catch
                            {
                            }
                            try
                            {
                                p1t6.SelectedIndex = Convert.ToInt32(Tools.StringBase2_Into_StringBase16(regDefs.TH_P1_T6.value, 8), 16);
                            }
                            catch
                            {
                            }
                            break;
                        case "62 (P1_THR_3)":
                            foreach (Bit_Field bit_Field in regDefs.P1_THR_3.bit_fields)
                            {
                                bit_Field.value = text.Substring(bit_Field.start_index, bit_Field.length);
                            }
                            try
                            {
                                p1t7.SelectedIndex = Convert.ToInt32(Tools.StringBase2_Into_StringBase16(regDefs.TH_P1_T7.value, 8), 16);
                            }
                            catch
                            {
                            }
                            try
                            {
                                p1t8.SelectedIndex = Convert.ToInt32(Tools.StringBase2_Into_StringBase16(regDefs.TH_P1_T8.value, 8), 16);
                            }
                            catch
                            {
                            }
                            break;
                        case "63 (P1_THR_4)":
                            foreach (Bit_Field bit_Field in regDefs.P1_THR_4.bit_fields)
                            {
                                bit_Field.value = text.Substring(bit_Field.start_index, bit_Field.length);
                            }
                            try
                            {
                                p1t9.SelectedIndex = Convert.ToInt32(Tools.StringBase2_Into_StringBase16(regDefs.TH_P1_T9.value, 8), 16);
                            }
                            catch
                            {
                            }
                            try
                            {
                                p1t10.SelectedIndex = Convert.ToInt32(Tools.StringBase2_Into_StringBase16(regDefs.TH_P1_T10.value, 8), 16);
                            }
                            catch
                            {
                            }
                            break;
                        case "64 (P1_THR_5)":
                            foreach (Bit_Field bit_Field in regDefs.P1_THR_5.bit_fields)
                            {
                                bit_Field.value = text.Substring(bit_Field.start_index, bit_Field.length);
                            }
                            try
                            {
                                p1t11.SelectedIndex = Convert.ToInt32(Tools.StringBase2_Into_StringBase16(regDefs.TH_P1_T11.value, 8), 16);
                            }
                            catch
                            {
                            }
                            try
                            {
                                p1t12.SelectedIndex = Convert.ToInt32(Tools.StringBase2_Into_StringBase16(regDefs.TH_P1_T12.value, 8), 16);
                            }
                            catch
                            {
                            }
                            break;
                        case "65 (P1_THR_6)":
                            foreach (Bit_Field bit_Field in regDefs.P1_THR_6.bit_fields)
                            {
                                bit_Field.value = text.Substring(bit_Field.start_index, bit_Field.length);
                            }
                            try
                            {
                                p1l1.SelectedIndex = Convert.ToInt32(Tools.StringBase2_Into_StringBase16(regDefs.TH_P1_L1.value, 8), 16);
                            }
                            catch
                            {
                            }
                            try
                            {
                                p1thrl2_merged_pt1 = Convert.ToInt32(Tools.StringBase2_Into_StringBase16(regDefs.TH_P1_L2a.value, 8), 16) << 2;
                                p1thrl2_merged_flag = true;
                            }
                            catch
                            {
                            }
                            break;
                        case "66 (P1_THR_7)":
                            foreach (Bit_Field bit_Field in regDefs.P1_THR_7.bit_fields)
                            {
                                bit_Field.value = text.Substring(bit_Field.start_index, bit_Field.length);
                            }
                            try
                            {
                                p1thrl2_merged_pt2 = Convert.ToInt32(Tools.StringBase2_Into_StringBase16(regDefs.TH_P1_L2b.value, 8), 16);
                                p1thrl2_merged_flag = true;
                            }
                            catch
                            {
                            }
                            try
                            {
                                p1l3.SelectedIndex = Convert.ToInt32(Tools.StringBase2_Into_StringBase16(regDefs.TH_P1_L3.value, 8), 16);
                            }
                            catch
                            {
                            }
                            try
                            {
                                p1thrl4_merged_pt1 = Convert.ToInt32(Tools.StringBase2_Into_StringBase16(regDefs.TH_P1_L4a.value, 8), 16) << 4;
                                p1thrl4_merged_flag = true;
                            }
                            catch
                            {
                            }
                            break;
                        case "67 (P1_THR_8)":
                            foreach (Bit_Field bit_Field in regDefs.P1_THR_8.bit_fields)
                            {
                                bit_Field.value = text.Substring(bit_Field.start_index, bit_Field.length);
                            }
                            try
                            {
                                p1thrl4_merged_pt2 = Convert.ToInt32(Tools.StringBase2_Into_StringBase16(regDefs.TH_P1_L4b.value, 8), 16);
                                p1thrl4_merged_flag = true;
                            }
                            catch
                            {
                            }
                            try
                            {
                                p1thrl5_merged_pt1 = Convert.ToInt32(Tools.StringBase2_Into_StringBase16(regDefs.TH_P1_L5a.value, 8), 16) << 1;
                                p1thrl5_merged_flag = true;
                            }
                            catch
                            {
                            }
                            break;
                        case "68 (P1_THR_9)":
                            foreach (Bit_Field bit_Field in regDefs.P1_THR_9.bit_fields)
                            {
                                bit_Field.value = text.Substring(bit_Field.start_index, bit_Field.length);
                            }
                            try
                            {
                                p1thrl5_merged_pt2 = Convert.ToInt32(Tools.StringBase2_Into_StringBase16(regDefs.TH_P1_L5b.value, 8), 16);
                                p1thrl5_merged_flag = true;
                            }
                            catch
                            {
                            }
                            try
                            {
                                p1l6.SelectedIndex = Convert.ToInt32(Tools.StringBase2_Into_StringBase16(regDefs.TH_P1_L6.value, 8), 16);
                            }
                            catch
                            {
                            }
                            try
                            {
                                p1thrl7_merged_pt1 = Convert.ToInt32(Tools.StringBase2_Into_StringBase16(regDefs.TH_P1_L7a.value, 8), 16) << 3;
                                p1thrl7_merged_flag = true;
                            }
                            catch
                            {
                            }
                            break;
                        case "69 (P1_THR_10)":
                            foreach (Bit_Field bit_Field in regDefs.P1_THR_10.bit_fields)
                            {
                                bit_Field.value = text.Substring(bit_Field.start_index, bit_Field.length);
                            }
                            try
                            {
                                p1thrl7_merged_pt2 = Convert.ToInt32(Tools.StringBase2_Into_StringBase16(regDefs.TH_P1_L7b.value, 8), 16);
                                p1thrl7_merged_flag = true;
                            }
                            catch
                            {
                            }
                            try
                            {
                                p1l8.SelectedIndex = Convert.ToInt32(Tools.StringBase2_Into_StringBase16(regDefs.TH_P1_L8.value, 8), 16);
                            }
                            catch
                            {
                            }
                            break;
                        case "6A (P1_THR_11)":
                            foreach (Bit_Field bit_Field in regDefs.P1_THR_11.bit_fields)
                            {
                                bit_Field.value = text.Substring(bit_Field.start_index, bit_Field.length);
                            }
                            try
                            {
                                p1l9.SelectedIndex = Convert.ToInt32(Tools.StringBase2_Into_StringBase16(regDefs.TH_P1_L9.value, 8), 16);
                            }
                            catch
                            {
                            }
                            break;
                        case "6B (P1_THR_12)":
                            foreach (Bit_Field bit_Field in regDefs.P1_THR_12.bit_fields)
                            {
                                bit_Field.value = text.Substring(bit_Field.start_index, bit_Field.length);
                            }
                            try
                            {
                                p1l10.SelectedIndex = Convert.ToInt32(Tools.StringBase2_Into_StringBase16(regDefs.TH_P1_L10.value, 8), 16);
                            }
                            catch
                            {
                            }
                            break;
                        case "6C (P1_THR_13)":
                            foreach (Bit_Field bit_Field in regDefs.P1_THR_13.bit_fields)
                            {
                                bit_Field.value = text.Substring(bit_Field.start_index, bit_Field.length);
                            }
                            try
                            {
                                p1l11.SelectedIndex = Convert.ToInt32(Tools.StringBase2_Into_StringBase16(regDefs.TH_P1_L11.value, 8), 16);
                            }
                            catch
                            {
                            }
                            break;
                        case "6D (P1_THR_14)":
                            foreach (Bit_Field bit_Field in regDefs.P1_THR_14.bit_fields)
                            {
                                bit_Field.value = text.Substring(bit_Field.start_index, bit_Field.length);
                            }
                            try
                            {
                                p1l12.SelectedIndex = Convert.ToInt32(Tools.StringBase2_Into_StringBase16(regDefs.TH_P1_L12.value, 8), 16);
                            }
                            catch
                            {
                            }
                            break;
                        case "6E (P1_THR_15)":
                            foreach (Bit_Field bit_Field in regDefs.P1_THR_15.bit_fields)
                            {
                                bit_Field.value = text.Substring(bit_Field.start_index, bit_Field.length);
                            }
                            try
                            {
                                p1lOff.SelectedIndex = Convert.ToInt32(Tools.StringBase2_Into_StringBase16(regDefs.TH_P1_OFF.value, 8), 16);
                            }
                            catch
                            {
                            }
                            break;
                        case "6F (P2_THR_0)":
                            foreach (Bit_Field bit_Field in regDefs.P2_THR_0.bit_fields)
                            {
                                bit_Field.value = text.Substring(bit_Field.start_index, bit_Field.length);
                            }
                            try
                            {
                                p2t1.SelectedIndex = Convert.ToInt32(Tools.StringBase2_Into_StringBase16(regDefs.TH_P2_T1.value, 8), 16);
                            }
                            catch
                            {
                            }
                            try
                            {
                                p2t2.SelectedIndex = Convert.ToInt32(Tools.StringBase2_Into_StringBase16(regDefs.TH_P2_T2.value, 8), 16);
                            }
                            catch
                            {
                            }
                            break;
                        case "70 (P2_THR_1)":
                            foreach (Bit_Field bit_Field in regDefs.P2_THR_1.bit_fields)
                            {
                                bit_Field.value = text.Substring(bit_Field.start_index, bit_Field.length);
                            }
                            try
                            {
                                p2t3.SelectedIndex = Convert.ToInt32(Tools.StringBase2_Into_StringBase16(regDefs.TH_P2_T3.value, 8), 16);
                            }
                            catch
                            {
                            }
                            try
                            {
                                p2t4.SelectedIndex = Convert.ToInt32(Tools.StringBase2_Into_StringBase16(regDefs.TH_P2_T4.value, 8), 16);
                            }
                            catch
                            {
                            }
                            break;
                        case "71 (P2_THR_2)":
                            foreach (Bit_Field bit_Field in regDefs.P2_THR_2.bit_fields)
                            {
                                bit_Field.value = text.Substring(bit_Field.start_index, bit_Field.length);
                            }
                            try
                            {
                                p2t5.SelectedIndex = Convert.ToInt32(Tools.StringBase2_Into_StringBase16(regDefs.TH_P2_T5.value, 8), 16);
                            }
                            catch
                            {
                            }
                            try
                            {
                                p2t6.SelectedIndex = Convert.ToInt32(Tools.StringBase2_Into_StringBase16(regDefs.TH_P2_T6.value, 8), 16);
                            }
                            catch
                            {
                            }
                            break;
                        case "72 (P2_THR_3)":
                            foreach (Bit_Field bit_Field in regDefs.P2_THR_3.bit_fields)
                            {
                                bit_Field.value = text.Substring(bit_Field.start_index, bit_Field.length);
                            }
                            try
                            {
                                p2t7.SelectedIndex = Convert.ToInt32(Tools.StringBase2_Into_StringBase16(regDefs.TH_P2_T7.value, 8), 16);
                            }
                            catch
                            {
                            }
                            try
                            {
                                p2t8.SelectedIndex = Convert.ToInt32(Tools.StringBase2_Into_StringBase16(regDefs.TH_P2_T8.value, 8), 16);
                            }
                            catch
                            {
                            }
                            break;
                        case "73 (P2_THR_4)":
                            foreach (Bit_Field bit_Field in regDefs.P2_THR_4.bit_fields)
                            {
                                bit_Field.value = text.Substring(bit_Field.start_index, bit_Field.length);
                            }
                            try
                            {
                                p2t9.SelectedIndex = Convert.ToInt32(Tools.StringBase2_Into_StringBase16(regDefs.TH_P2_T9.value, 8), 16);
                            }
                            catch
                            {
                            }
                            try
                            {
                                p2t10.SelectedIndex = Convert.ToInt32(Tools.StringBase2_Into_StringBase16(regDefs.TH_P2_T10.value, 8), 16);
                            }
                            catch
                            {
                            }
                            break;
                        case "74 (P2_THR_5)":
                            foreach (Bit_Field bit_Field in regDefs.P2_THR_5.bit_fields)
                            {
                                bit_Field.value = text.Substring(bit_Field.start_index, bit_Field.length);
                            }
                            try
                            {
                                p2t11.SelectedIndex = Convert.ToInt32(Tools.StringBase2_Into_StringBase16(regDefs.TH_P2_T11.value, 8), 16);
                            }
                            catch
                            {
                            }
                            try
                            {
                                p2t12.SelectedIndex = Convert.ToInt32(Tools.StringBase2_Into_StringBase16(regDefs.TH_P2_T12.value, 8), 16);
                            }
                            catch
                            {
                            }
                            break;
                        case "75 (P2_THR_6)":
                            foreach (Bit_Field bit_Field in regDefs.P2_THR_6.bit_fields)
                            {
                                bit_Field.value = text.Substring(bit_Field.start_index, bit_Field.length);
                            }
                            try
                            {
                                p2l1.SelectedIndex = Convert.ToInt32(Tools.StringBase2_Into_StringBase16(regDefs.TH_P2_L1.value, 8), 16);
                            }
                            catch
                            {
                            }
                            try
                            {
                                p2thrl2_merged_pt1 = Convert.ToInt32(Tools.StringBase2_Into_StringBase16(regDefs.TH_P2_L2a.value, 8), 16) << 2;
                                p2thrl2_merged_flag = true;
                            }
                            catch
                            {
                            }
                            break;
                        case "76 (P2_THR_7)":
                            foreach (Bit_Field bit_Field in regDefs.P2_THR_7.bit_fields)
                            {
                                bit_Field.value = text.Substring(bit_Field.start_index, bit_Field.length);
                            }
                            try
                            {
                                p2thrl2_merged_pt2 = Convert.ToInt32(Tools.StringBase2_Into_StringBase16(regDefs.TH_P2_L2b.value, 8), 16);
                                p2thrl2_merged_flag = true;
                            }
                            catch
                            {
                            }
                            try
                            {
                                p2l3.SelectedIndex = Convert.ToInt32(Tools.StringBase2_Into_StringBase16(regDefs.TH_P2_L3.value, 8), 16);
                            }
                            catch
                            {
                            }
                            try
                            {
                                p2thrl4_merged_pt1 = Convert.ToInt32(Tools.StringBase2_Into_StringBase16(regDefs.TH_P2_L4a.value, 8), 16) << 4;
                                p2thrl4_merged_flag = true;
                            }
                            catch
                            {
                            }
                            break;
                        case "77 (P2_THR_8)":
                            foreach (Bit_Field bit_Field in regDefs.P2_THR_8.bit_fields)
                            {
                                bit_Field.value = text.Substring(bit_Field.start_index, bit_Field.length);
                            }
                            try
                            {
                                p2thrl4_merged_pt2 = Convert.ToInt32(Tools.StringBase2_Into_StringBase16(regDefs.TH_P2_L4b.value, 8), 16);
                                p2thrl4_merged_flag = true;
                            }
                            catch
                            {
                            }
                            try
                            {
                                p2thrl5_merged_pt1 = Convert.ToInt32(Tools.StringBase2_Into_StringBase16(regDefs.TH_P2_L5a.value, 8), 16) << 1;
                                p2thrl5_merged_flag = true;
                            }
                            catch
                            {
                            }
                            break;
                        case "78 (P2_THR_9)":
                            foreach (Bit_Field bit_Field in regDefs.P2_THR_9.bit_fields)
                            {
                                bit_Field.value = text.Substring(bit_Field.start_index, bit_Field.length);
                            }
                            try
                            {
                                p2thrl5_merged_pt2 = Convert.ToInt32(Tools.StringBase2_Into_StringBase16(regDefs.TH_P2_L5b.value, 8), 16);
                                p2thrl5_merged_flag = true;
                            }
                            catch
                            {
                            }
                            try
                            {
                                p2l6.SelectedIndex = Convert.ToInt32(Tools.StringBase2_Into_StringBase16(regDefs.TH_P2_L6.value, 8), 16);
                            }
                            catch
                            {
                            }
                            try
                            {
                                p2thrl7_merged_pt1 = Convert.ToInt32(Tools.StringBase2_Into_StringBase16(regDefs.TH_P2_L7a.value, 8), 16) << 3;
                                p2thrl7_merged_flag = true;
                            }
                            catch
                            {
                            }
                            break;
                        case "79 (P2_THR_10)":
                            foreach (Bit_Field bit_Field in regDefs.P2_THR_10.bit_fields)
                            {
                                bit_Field.value = text.Substring(bit_Field.start_index, bit_Field.length);
                            }
                            try
                            {
                                p2thrl7_merged_pt2 = Convert.ToInt32(Tools.StringBase2_Into_StringBase16(regDefs.TH_P2_L7b.value, 8), 16);
                                p2thrl7_merged_flag = true;
                            }
                            catch
                            {
                            }
                            try
                            {
                                p2l8.SelectedIndex = Convert.ToInt32(Tools.StringBase2_Into_StringBase16(regDefs.TH_P2_L8.value, 8), 16);
                            }
                            catch
                            {
                            }
                            break;
                        case "7A (P2_THR_11)":
                            foreach (Bit_Field bit_Field in regDefs.P2_THR_11.bit_fields)
                            {
                                bit_Field.value = text.Substring(bit_Field.start_index, bit_Field.length);
                            }
                            try
                            {
                                p2l9.SelectedIndex = Convert.ToInt32(Tools.StringBase2_Into_StringBase16(regDefs.TH_P2_L9.value, 8), 16);
                            }
                            catch
                            {
                            }
                            break;
                        case "7B (P2_THR_12)":
                            foreach (Bit_Field bit_Field in regDefs.P2_THR_12.bit_fields)
                            {
                                bit_Field.value = text.Substring(bit_Field.start_index, bit_Field.length);
                            }
                            try
                            {
                                p2l10.SelectedIndex = Convert.ToInt32(Tools.StringBase2_Into_StringBase16(regDefs.TH_P2_L10.value, 8), 16);
                            }
                            catch
                            {
                            }
                            break;
                        case "7C (P2_THR_13)":
                            foreach (Bit_Field bit_Field in regDefs.P2_THR_13.bit_fields)
                            {
                                bit_Field.value = text.Substring(bit_Field.start_index, bit_Field.length);
                            }
                            try
                            {
                                p2l11.SelectedIndex = Convert.ToInt32(Tools.StringBase2_Into_StringBase16(regDefs.TH_P2_L11.value, 8), 16);
                            }
                            catch
                            {
                            }
                            break;
                        case "7D (P2_THR_14)":
                            foreach (Bit_Field bit_Field in regDefs.P2_THR_14.bit_fields)
                            {
                                bit_Field.value = text.Substring(bit_Field.start_index, bit_Field.length);
                            }
                            try
                            {
                                p2l12.SelectedIndex = Convert.ToInt32(Tools.StringBase2_Into_StringBase16(regDefs.TH_P2_L12.value, 8), 16);
                            }
                            catch
                            {
                            }
                            break;
                        case "7E (P2_THR_15)":
                            foreach (Bit_Field bit_Field in regDefs.P2_THR_15.bit_fields)
                            {
                                bit_Field.value = text.Substring(bit_Field.start_index, bit_Field.length);
                            }
                            try
                            {
                                p2lOff.SelectedIndex = Convert.ToInt32(Tools.StringBase2_Into_StringBase16(regDefs.TH_P2_OFF.value, 8), 16);
                            }
                            catch
                            {
                            }
                            break;
                    }

                    if (tvgg2_merged_flag)
                    {
                        tvgg2.SelectedIndex = tvgg2_merged_pt1 + tvgg2_merged_pt2;
                        tvgg2_merged_flag = false;
                    }
                    if (tvgg3_merged_flag)
                    {
                        tvgg3.SelectedIndex = tvgg3_merged_pt1 + tvgg3_merged_pt2;
                        tvgg3_merged_flag = false;
                    }
                    if (p1thrl2_merged_flag)
                    {
                        p1l2.SelectedIndex = p1thrl2_merged_pt1 + p1thrl2_merged_pt2;
                        p1thrl2_merged_flag = false;
                    }
                    if (p1thrl4_merged_flag)
                    {
                        p1l4.SelectedIndex = p1thrl4_merged_pt1 + p1thrl4_merged_pt2;
                        p1thrl4_merged_flag = false;
                    }
                    if (p1thrl5_merged_flag)
                    {
                        p1l5.SelectedIndex = p1thrl5_merged_pt1 + p1thrl5_merged_pt2;
                        p1thrl5_merged_flag = false;
                    }
                    if (p1thrl7_merged_flag)
                    {
                        p1l7.SelectedIndex = p1thrl7_merged_pt1 + p1thrl7_merged_pt2;
                        p1thrl7_merged_flag = false;
                    }
                    if (p2thrl2_merged_flag)
                    {
                        p2l2.SelectedIndex = p2thrl2_merged_pt1 + p2thrl2_merged_pt2;
                        p2thrl2_merged_flag = false;
                    }
                    if (p2thrl4_merged_flag)
                    {
                        p2l4.SelectedIndex = p2thrl4_merged_pt1 + p2thrl4_merged_pt2;
                        p2thrl4_merged_flag = false;
                    }
                    if (p2thrl5_merged_flag)
                    {
                        p2l5.SelectedIndex = p2thrl5_merged_pt1 + p2thrl5_merged_pt2;
                        p2thrl5_merged_flag = false;
                    }
                    if (p2thrl7_merged_flag)
                    {
                        p2l7.SelectedIndex = p2thrl7_merged_pt1 + p2thrl7_merged_pt2;
                        p2thrl7_merged_flag = false;
                    }
                    i++;
                    continue;
                }
            }
        }

        private void Populate_controls(string SelectedNode)
        {
        }

        private void text_box_status(TextBox textbox, string bin_value)
        {
            if (bin_value == "0" || bin_value == "00" || bin_value == "01" || bin_value == "10" || bin_value == "11")
                textbox.BackColor = Color.FromArgb(0, 187, 204);
            else if (bin_value == "")
                textbox.BackColor = Color.Gray;
            else
                textbox.BackColor = Color.Red;
            textbox.Text = bin_value;
        }

        private void exitToolStripMenuItem_Click(object sender, EventArgs e)
        {
            Application.Exit();
        }

        private void aboutToolStripMenuItem_Click(object sender, EventArgs e)
        {
            About form = new About();
            form.AboutTextBox = string.Concat(
                "PGA460-Q1 EVM GUI\r\nDevice Firmware: ",
                USB_Firmware_Box.Text,
                "\r\nGUI Version: ",
                guiVersion,
                "\r\n",
                guiDate,
                "\r\nPublic Release"
            );
            try
            {
                form.revHistoryTextBox.Text = "v1.0.1.2\t\r\n01/18/2018\r\n•Added frequency diagnostic error time threshold to kHz boundary equivalent.\r\n•Embedded high-frequency bandpass filter bandpass coefficient look-up table under the menu bar's File dropdown.\r\n•Enabled high-frequency parameter sweeping.\r\n•Removed temperature gain and offset controls.\r\n•TCI Index write 9 of USER_EEPROMx registers corrected such that write dropdown of '00' writes a value of 0x00, not 0xFF.\r\n•Detailed TCI echo distance and width data available for up to four objects.\r\n•Updated auto-calculated high-frequency bandpass filter bandwidth A2 coefficient.\r\n\r\nv1.0.1.1\t\r\n12/11/2017\r\n•Recommend boostxlpga460-firmware.bat update for MSP430 LaunchPad.\r\n•Updated A2 coefficient calculation for the high-frequency range to support frequencies above 400.8kHz.\r\n•Enabled Matching Daughtercard configuration to sweep damping resistor and tuning capacitor values. Need BOOSTXL-PGA460-MATCH daughtercard hardware add-on to utilize.\r\n•Appended paramater sweep and matching sweep values to background EDD export file name.\r\n•Corrected x-axis of synchronous-continuous output mode plots to time in micro-seconds by multiplying sample value by a factor of 1.45.\r\n\r\nv1.0.1.0\t\r\n11/08/2017\r\n•Recommend boostxlpga460-firmware.bat update for MSP430 LaunchPad.\r\n•Commands 0-3 no longer transmit additional post-checksum byte in UART mode when run from Data Monitor page.\r\n•Default settings uncheck frequency shift check box if already checked.\r\n•General page's Initial Gain selection sets fixed TVG gain levels when Fixed Gain Level is checked.\r\n•Parameter sweep fixed for frequency and gain level parameters.\r\n•User can specify number of decimal places for the UMR's distance value.\r\n•UMR Time-of-Flight MSB and LSB hex values data-logged when Log UMR is checked.\r\n\r\nv1.0.0.9\t\r\n10/20/2017\r\n•Time-of-flight to distance conversion corrected as listed in Rev.A of the datasheet: distance (m) = [343m/s × (MSB<<8 + LSB) ÷ 2 × 1μs] - (50μs × 343m/s)\r\n•High-frequency mode's settings retained after modifying any device settings. User no longer required to manually update coefficients.\r\n•Forced ADC and DSP Graph Modes to minimum and maximum Y-Axis values based on data, and not checksum.\r\n•Added Noise Filter button and intensity bar to eliminate communication based noise/error for synchronous-continuous output modes.\r\n•Removed trailing void-data of direct data output mode's data export.\r\n\r\nv1.0.0.8\t\r\n10/03/2017\r\n•Recommend boostxlpga460-firmware.bat update for MSP430 LaunchPad.\r\n•Updated boostxlpga460-firmware.bat file to enable greater than 15ms of 1MHz synchronous mode continuous output for ADC and DSP modes, and to support external sync/trigger modes.\r\n•UMR TOF to distance conversion precision improved at short range detection.\r\n•Data log raw synchronous 1-MSPS ADC or DSP output.\r\n•Echo Data Dump Plotter's averaged data can be exported as a text file.\r\n•Added Burst and/or Listen external sync and trigger modes\r\n•Enabled continous looping and backgorund data dump of ADC and DSP modes.\r\n•Toggle Echo Data Dump Plotter's chart display style as Line or Column.\r\n•Corrected all synchronous-continous output modes.\r\n•Echo Data Dump Plotter can import ADC or DSP synchronous-continous output mode exports.\r\n\r\nv1.0.0.7\t\r\n09/11/2017\r\n•Gain dB to V/V conversion corrected. Was previously offest by +0.5dB\r\n•Prompt OWU hardware change when configuring PGA460 device for OWU mode.\r\n•UART baud rate drop down selection fixed to operate below 115.2kBaud.\r\n•Threshold and TVG load chart feature now displayed as line rather than column on chart.\r\n•Synchronous DSP output modes correctly displayed on Data Monitor.\r\n•Enabled ADC and DSP charts to be saved as images when clicking Save Chart Image button.\r\n•Datalog can be saved as either a .TXT or .CSV file based on Export tab settings.\r\n•Ultrasonic Measurement TOF Results and Echo Data Dump Cursor match within +/-1cm.\r\n\r\nv1.0.0.6\t\r\n07/19/2017\r\n•Enabled ADC graph mode for synchronous output. Data is plotted and can be exported as TXT or CSV data. Currently works up to 2.5m (to be extended to 11m in future revision).\r\n•Multiple echo data dump loops no longer superimposed at TVG and threshold page, which caused lagging after +100 loops were executed. Displayed as line, not bars. Only displaying the last run.\r\n•Previously exported echo data dump files (.txt or .csv) can be loaded into the Threshold and TVG pages.\r\n•All single and broadcast UART commands available.\r\n•Ambient-temperature calculated in addition to die-temperature when running the temperature sensor measurement. Ambient temperature is based on VPWR voltage.\r\n•TCI Index 10 read correctly updates frequency diagnostic length and current limit enable/disable state.\r\n•TCI Index 13 bulk EEPROM read will update and populate the Memory Map page.\r\n•Button to write calculated BPF coefficients from Utilities tab to the coefficient registers has been added.\r\n\r\nv1.0.0.5\t\r\n05/24/2017\r\n•Updated Ultrasonic Measurement Result to correctly account for bursting duration when converting to distance equivalent. 50us of digital delay is present, which accounts for +1.715cm of offset at room temperature.\r\n•Added Parameter Sweep feature on Test Mode page.\r\n•Enable user to input speed of sound for Ultrasonic Measurement Result distance conversion via the Utilities tab TOF Calculator field.\r\n•Increase speed of Echo Data Dump and/or Ultrasonic Measurement command loop interval based on the UART baud rate and record time length. Fast acquisition mode allows user to control delay time between intervals.\r\n•Direct-drive defaults presets' drive current limits to 50mA.\r\n•Multi-looped run command now populates threshold and TVG page with echo data dump.\r\n•Infinite echo data dump chart update no longer ends with a line cutting across the last capture.\r\n\r\nv1.0.0.4\t\r\n04/27/2017\r\n•Prompt user to read all registers before saving grid.\r\n•Both LPM and cut power time enabled and corrected in Power Budget Calculator.\r\n•Echo data dump plotter legend correctly updates upon subsequent file loads.\r\n•Threshold page does not throw error if times and/or levels are blank, and changes are applied.\r\n•Added digital gain multiplier values to the Echo Data Dump legend.\r\n•Read status and update option of the Enable/Disable BOOSTXL-PGA460 Communication toggle under Edit.\r\n•Option to export echo data dump as .CSV, in addition to .TXT format.\r\n\r\nv1.0.0.3\t\r\n03/23/2017\r\n•ISO Pole defaulted TVG profiles automatically update.\r\n•Added scripting for GUI control automation.\r\n•Added datalog of temperature and noise level measurements.\r\n•Added Save Chart Image button to Graph tab on Data Monitor page.\r\n•Added Echo Data Dump Plotter under File Menu. Plots .TXT file echo data dump files.\r\n•Added GUI version checker. Requires a web connection.\r\n•Forced System Region Culture to 'English United States' (en-US) to correctly display time and distance equivalents.\r\n\r\nv1.0.0.2\t\r\n03/07/2017\r\n•Corrected TH8-->TH9 connection of both preset 1 and 2 on Data Monitor page.\r\n•Corrected Object 3 distance measurement result.\r\n•Data Monitor's cursor for time value corrected.\r\n•Faults updated immediately after Memory Map read and write commands.\r\n•Power budget calculator updated with the option to completely disconnect/disable cut-power to the device externally.\r\n•Power budget calculator updated with typical low power mode current, from 150uA to 300uA.\r\n•Batch file programming failure message updated with troubleshooting instructions.\r\n•Added Typical Characteristic Curve default settings option for long distance ISO-pole.\r\n•Hardware connected status corrected for non-0 UART addresses.\r\n\r\nv1.0.0.1\t\r\n02/17/2017\r\n•Accounting for R_INP in Voltage Diagnostic Equation.\r\n•Ultrasonic measurement result's distance calculation more accurate.\r\n\r\nv1.0.0.0\t\r\n02/14/2017\r\n•Initial release of PGA460-Q1 EVM GUI.\r\n\r\n";
            }
            catch { }
            form.Show();
        }

        private void AppResetButt_Click(object sender, EventArgs e)
        {
            Application.Restart();
        }
        private void leftTreeNav_AfterSelect(object sender, TreeViewEventArgs e)
        {
            string text = leftTreeNav.SelectedNode.Text;
            switch (text)
            {
                case "PGA460-Q1":
                    debugTabControl.SelectTab(statusTab);
                    MemMap_Leave();
                    runBtn.Text = "START";
                    thrUpdateCheck.Checked = false;
                    tvgInstantUpdateCheck.Checked = false;
                    break;
                case "Device Settings":
                    primaryTab.SelectTab("generalTab");
                    debugTabControl.SelectTab(statusTab);
                    leftTreeNav.SelectedNode.Expand();
                    leftTreeNav.SelectedNode = leftTreeNav.Nodes[0].Nodes[0].NextVisibleNode;
                    leftTreeNav.Focus();
                    MemMap_Leave();
                    runBtn.Text = "START";
                    thrUpdateCheck.Checked = false;
                    tvgInstantUpdateCheck.Checked = false;
                    break;
                case "General":
                    primaryTab.SelectTab("generalTab");
                    debugTabControl.SelectTab(statusTab);
                    MemMap_Leave();
                    runBtn.Text = "START";
                    thrUpdateCheck.Checked = false;
                    tvgInstantUpdateCheck.Checked = false;
                    break;
                case "Block Diagram":
                    primaryTab.SelectTab("bdModeTab");
                    primaryTab.SelectedTab.AutoScroll = false;
                    debugTabControl.SelectTab(statusTab);
                    MemMap_Leave();
                    runBtn.Text = "START";
                    thrUpdateCheck.Checked = false;
                    tvgInstantUpdateCheck.Checked = false;
                    break;
                case "Time Varying Gain":
                    primaryTab.SelectTab("tvgTab");
                    debugTabControl.SelectTab(statusTab);
                    MemMap_Leave();
                    thrUpdateCheck.Checked = false;
                    tvgInstantUpdateCheck.Checked = true;
                    updateTVGChart();
                    runBtn.Text = "START";
                    break;
                case "Test Mode":
                    primaryTab.SelectTab("testTab");
                    debugTabControl.SelectTab(statusTab);
                    MemMap_Leave();
                    runBtn.Text = "START";
                    thrUpdateCheck.Checked = false;
                    tvgInstantUpdateCheck.Checked = false;
                    break;
                case "Matching":
                    primaryTab.SelectTab("matchingTab");
                    debugTabControl.SelectTab(statusTab);
                    MemMap_Leave();
                    runBtn.Text = "START";
                    thrUpdateCheck.Checked = false;
                    tvgInstantUpdateCheck.Checked = false;
                    break;
                case "Interface Mode":
                    primaryTab.SelectTab("comSetTab");
                    debugTabControl.SelectTab(statusTab);
                    MemMap_Leave();
                    runBtn.Text = "START";
                    thrUpdateCheck.Checked = false;
                    tvgInstantUpdateCheck.Checked = false;
                    break;
                case "Threshold":
                    primaryTab.SelectTab("thrTab");
                    MemMap_Leave();
                    updateThresholdChart();
                    debugTabControl.SelectTab(statusTab);
                    runBtn.Text = "START";
                    thrUpdateCheck.Checked = true;
                    tvgInstantUpdateCheck.Checked = false;
                    break;
                case "Threshold Memory":
                    TI_EEPROM_Regs.Visible = false;
                    TI_TM_Regs.Visible = false;
                    ConfigRegs.Visible = false;
                    UserDataRegs.Visible = false;
                    ThresholdRegs.Visible = true;
                    DataDumpRegs.Visible = false;
                    primaryTab.SelectTab("MemMap");
                    debugTabControl.SelectTab(utilTab);
                    infoTextBox.Text = " Click register address for bit definitions.";
                    runBtn.Text = "START";
                    thrUpdateCheck.Checked = false;
                    tvgInstantUpdateCheck.Checked = false;
                    ReadAllRegs(true);
                    break;
                case "Memory Map":
                    desel_grid_butt_Click(null, null);
                    TI_EEPROM_Regs.Visible = false;
                    TI_TM_Regs.Visible = false;
                    ConfigRegs.Visible = true;
                    ConfigRegs.BringToFront();
                    UserDataRegs.Visible = false;
                    ThresholdRegs.Visible = false;
                    DataDumpRegs.Visible = false;
                    primaryTab.SelectTab("MemMap");
                    debugTabControl.SelectTab(regTab);
                    infoTextBox.Text = " Click register address for bit definitions.";
                    runBtn.Text = "START";
                    thrUpdateCheck.Checked = false;
                    tvgInstantUpdateCheck.Checked = false;
                    break;
                case "TI EEPROM":
                    TI_TM_Regs.Visible = false;
                    ConfigRegs.Visible = false;
                    ConfigRegs.SendToBack();
                    TI_EEPROM_Regs.Visible = true;
                    UserDataRegs.Visible = false;
                    DataDumpRegs.Visible = false;
                    ThresholdRegs.Visible = false;
                    primaryTab.SelectTab("MemMap");
                    debugTabControl.SelectTab(statusTab);
                    ts_updateCombo.Text = "Manual";
                    thrUpdateCheck.Checked = false;
                    tvgInstantUpdateCheck.Checked = false;
                    if (firstTimeMemMapTIEE)
                    {
                        ReadAllRegs(true);
                        firstTimeMemMapTIEE = false;
                    }
                    runBtn.Text = "START";
                    break;
                case "TI TESTMODE":
                    ConfigRegs.Visible = false;
                    ConfigRegs.SendToBack();
                    TI_EEPROM_Regs.Visible = false;
                    TI_TM_Regs.Visible = true;
                    UserDataRegs.Visible = false;
                    ThresholdRegs.Visible = false;
                    DataDumpRegs.Visible = false;
                    primaryTab.SelectTab("MemMap");
                    debugTabControl.SelectTab(statusTab);
                    ts_updateCombo.Text = "Manual";
                    thrUpdateCheck.Checked = false;
                    tvgInstantUpdateCheck.Checked = false;
                    if (firstTimeMemMapTITM)
                    {
                        ReadAllRegs(true);
                        firstTimeMemMapTITM = false;
                    }
                    runBtn.Text = "START";
                    break;
                case "User Data":
                    TI_EEPROM_Regs.Visible = false;
                    TI_TM_Regs.Visible = false;
                    ConfigRegs.Visible = false;
                    UserDataRegs.Visible = true;
                    ThresholdRegs.Visible = false;
                    DataDumpRegs.Visible = false;
                    primaryTab.SelectTab("MemMap");
                    debugTabControl.SelectTab(utilTab);
                    infoTextBox.Text = " Click register address for bit definitions.";
                    thrUpdateCheck.Checked = false;
                    tvgInstantUpdateCheck.Checked = false;
                    ReadAllRegs(true);
                    runBtn.Text = "START";
                    break;
                case "Dump Memory":
                    TI_EEPROM_Regs.Visible = false;
                    TI_TM_Regs.Visible = false;
                    ConfigRegs.Visible = false;
                    ConfigRegs.SendToBack();
                    UserDataRegs.Visible = false;
                    ThresholdRegs.Visible = false;
                    DataDumpRegs.Visible = true;
                    primaryTab.SelectTab("MemMap");
                    debugTabControl.SelectTab(statusTab);
                    infoTextBox.Text = " Click register address for bit definitions.";
                    thrUpdateCheck.Checked = false;
                    tvgInstantUpdateCheck.Checked = false;
                    runBtn.Text = "START";
                    break;
                case "Data Monitor":
                    primaryTab.SelectTab("dataDiagTab");
                    debugTabControl.SelectTab(statusTab);
                    MemMap_Leave();
                    thrUpdateCheck.Checked = false;
                    tvgInstantUpdateCheck.Checked = false;
                    updateThresholdChart();
                    updateTVGChart();
                    runBtn.Text = "START";
                    break;
                case "Diagnostics":
                    primaryTab.SelectTab("diagConfigTab");
                    debugTabControl.SelectTab(statusTab);
                    MemMap_Leave();
                    runBtn.Text = "START";
                    thrUpdateCheck.Checked = false;
                    tvgInstantUpdateCheck.Checked = false;
                    break;
            }
            try
            {
                if (leftTreeNav.SelectedNode != null &&
                    leftTreeNav.SelectedNode.Parent != null &&
                    leftTreeNav.SelectedNode.Parent.Text == "Device Settings")
                    Populate_controls(leftTreeNav.SelectedNode.Text);
            }
            catch (NullReferenceException)
            {
            }
        }

        public void updateThresholdChart()
        {
            if (p1t1.Text == "" || p1t2.Text == "" || p1t3.Text == "" || p1t4.Text == "" || p1t5.Text == "" || p1t6.Text == "" || p1t7.Text == "" || p1t8.Text == "" || p1t9.Text == "" || p1t10.Text == "" || p1t11.Text == "" || p1t12.Text == "" || p1l1.Text == "" || p1t2.Text == "" || p1t3.Text == "" || p1t4.Text == "" || p1t5.Text == "" || p1t6.Text == "" || p1t7.Text == "" || p1t8.Text == "" || p1t9.Text == "" || p1l10.Text == "" || p1l11.Text == "" || p1l12.Text == "")
            {
                if (PGA46xStat_box.Text.Contains("Ready") && !readingRegsFlag)
                    MessageBox.Show("Thr values cannot be blank.");
            }
            else
            {
                thrChart.Series[0].Points.Clear();
                thrChart.Series[1].Points.Clear();
                double num = Convert.ToDouble(p1t1.Text);
                double num2 = Convert.ToDouble(p1t1.Text) + Convert.ToDouble(p1t2.Text);
                double num3 = Convert.ToDouble(p1t1.Text) + Convert.ToDouble(p1t2.Text) + Convert.ToDouble(p1t3.Text);
                double num4 = Convert.ToDouble(p1t1.Text) + Convert.ToDouble(p1t2.Text) + Convert.ToDouble(p1t3.Text) + Convert.ToDouble(p1t4.Text);
                double num5 = Convert.ToDouble(p1t1.Text) + Convert.ToDouble(p1t2.Text) + Convert.ToDouble(p1t3.Text) + Convert.ToDouble(p1t4.Text) + Convert.ToDouble(p1t5.Text);
                double num6 = Convert.ToDouble(p1t1.Text) + Convert.ToDouble(p1t2.Text) + Convert.ToDouble(p1t3.Text) + Convert.ToDouble(p1t4.Text) + Convert.ToDouble(p1t5.Text) + Convert.ToDouble(p1t6.Text);
                double num7 = Convert.ToDouble(p1t1.Text) + Convert.ToDouble(p1t2.Text) + Convert.ToDouble(p1t3.Text) + Convert.ToDouble(p1t4.Text) + Convert.ToDouble(p1t5.Text) + Convert.ToDouble(p1t6.Text) + Convert.ToDouble(p1t7.Text);
                double num8 = Convert.ToDouble(p1t1.Text) + Convert.ToDouble(p1t2.Text) + Convert.ToDouble(p1t3.Text) + Convert.ToDouble(p1t4.Text) + Convert.ToDouble(p1t5.Text) + Convert.ToDouble(p1t6.Text) + Convert.ToDouble(p1t7.Text) + Convert.ToDouble(p1t8.Text);
                double num9 = Convert.ToDouble(p1t1.Text) + Convert.ToDouble(p1t2.Text) + Convert.ToDouble(p1t3.Text) + Convert.ToDouble(p1t4.Text) + Convert.ToDouble(p1t5.Text) + Convert.ToDouble(p1t6.Text) + Convert.ToDouble(p1t7.Text) + Convert.ToDouble(p1t8.Text) + Convert.ToDouble(p1t9.Text);
                double num10 = Convert.ToDouble(p1t1.Text) + Convert.ToDouble(p1t2.Text) + Convert.ToDouble(p1t3.Text) + Convert.ToDouble(p1t4.Text) + Convert.ToDouble(p1t5.Text) + Convert.ToDouble(p1t6.Text) + Convert.ToDouble(p1t7.Text) + Convert.ToDouble(p1t8.Text) + Convert.ToDouble(p1t9.Text) + Convert.ToDouble(p1t10.Text);
                double num11 = Convert.ToDouble(p1t1.Text) + Convert.ToDouble(p1t2.Text) + Convert.ToDouble(p1t3.Text) + Convert.ToDouble(p1t4.Text) + Convert.ToDouble(p1t5.Text) + Convert.ToDouble(p1t6.Text) + Convert.ToDouble(p1t7.Text) + Convert.ToDouble(p1t8.Text) + Convert.ToDouble(p1t9.Text) + Convert.ToDouble(p1t10.Text) + Convert.ToDouble(p1t11.Text);
                double num12 = Convert.ToDouble(p1t1.Text) + Convert.ToDouble(p1t2.Text) + Convert.ToDouble(p1t3.Text) + Convert.ToDouble(p1t4.Text) + Convert.ToDouble(p1t5.Text) + Convert.ToDouble(p1t6.Text) + Convert.ToDouble(p1t7.Text) + Convert.ToDouble(p1t8.Text) + Convert.ToDouble(p1t9.Text) + Convert.ToDouble(p1t10.Text) + Convert.ToDouble(p1t11.Text) + Convert.ToDouble(p1t12.Text);
                double num13 = Convert.ToDouble(p1l1.Text);
                double num14 = Convert.ToDouble(p1l2.Text);
                double num15 = Convert.ToDouble(p1l3.Text);
                double num16 = Convert.ToDouble(p1l4.Text);
                double num17 = Convert.ToDouble(p1l5.Text);
                double num18 = Convert.ToDouble(p1l6.Text);
                double num19 = Convert.ToDouble(p1l7.Text);
                double num20 = Convert.ToDouble(p1l8.Text);
                double num21 = Convert.ToDouble(p1l9.Text);
                double num22 = Convert.ToDouble(p1l10.Text);
                double num23 = Convert.ToDouble(p1l11.Text);
                double num24 = Convert.ToDouble(p1l12.Text);
                double num25 = 0.0;
                double num26 = 0.0;
                switch (p1lOff.SelectedIndex)
                {
                    case 0:
                        num25 = 0.0;
                        break;
                    case 1:
                        num25 = 1.0;
                        break;
                    case 2:
                        num25 = 2.0;
                        break;
                    case 3:
                        num25 = 3.0;
                        break;
                    case 4:
                        num25 = 4.0;
                        break;
                    case 5:
                        num25 = 5.0;
                        break;
                    case 6:
                        num25 = 6.0;
                        break;
                    case 7:
                        num25 = 7.0;
                        break;
                    case 8:
                        num25 = -8.0;
                        break;
                    case 9:
                        num25 = -7.0;
                        break;
                    case 10:
                        num25 = -6.0;
                        break;
                    case 11:
                        num25 = -5.0;
                        break;
                    case 12:
                        num25 = -4.0;
                        break;
                    case 13:
                        num25 = -3.0;
                        break;
                    case 14:
                        num25 = -2.0;
                        break;
                    case 15:
                        num25 = -1.0;
                        break;
                }
                if (thrP1PlotAddOnFlag)
                {
                    dumpChart.Series[7].Points.Clear();
                    for (double num27 = 0.0; num27 < num; num27 += 64.0)
                    {
                        dumpChart.Series[7].Points.AddXY(num27 / 1000000.0 * 343.0 / 2.0, num13 + num25);
                    }
                    for (double num27 = num; num27 < num2; num27 += 64.0)
                    {
                        dumpChart.Series[7].Points.AddXY(num27 / 1000000.0 * 343.0 / 2.0, (num14 - num13) / (num2 - num) * (num27 - num) + num13 + num25);
                    }
                    for (double num27 = num2; num27 < num3; num27 += 64.0)
                    {
                        dumpChart.Series[7].Points.AddXY(num27 / 1000000.0 * 343.0 / 2.0, (num15 - num14) / (num3 - num2) * (num27 - num2) + num14 + num25);
                    }
                    for (double num27 = num3; num27 < num4; num27 += 64.0)
                    {
                        dumpChart.Series[7].Points.AddXY(num27 / 1000000.0 * 343.0 / 2.0, (num16 - num15) / (num4 - num3) * (num27 - num3) + num15 + num25);
                    }
                    for (double num27 = num4; num27 < num5; num27 += 64.0)
                    {
                        dumpChart.Series[7].Points.AddXY(num27 / 1000000.0 * 343.0 / 2.0, (num17 - num16) / (num5 - num4) * (num27 - num4) + num16 + num25);
                    }
                    for (double num27 = num5; num27 < num6; num27 += 64.0)
                    {
                        dumpChart.Series[7].Points.AddXY(num27 / 1000000.0 * 343.0 / 2.0, (num18 - num17) / (num6 - num5) * (num27 - num5) + num17 + num25);
                    }
                    for (double num27 = num6; num27 < num7; num27 += 64.0)
                    {
                        dumpChart.Series[7].Points.AddXY(num27 / 1000000.0 * 343.0 / 2.0, (num19 - num18) / (num7 - num6) * (num27 - num6) + num18 + num25);
                    }
                    for (double num27 = num7; num27 < num8; num27 += 64.0)
                    {
                        dumpChart.Series[7].Points.AddXY(num27 / 1000000.0 * 343.0 / 2.0, (num20 - num19) / (num8 - num7) * (num27 - num7) + num19 + num25);
                    }
                    for (double num27 = num8; num27 < num9; num27 += 64.0)
                    {
                        if (num27 == num8)
                        {
                            dumpChart.Series[7].Points.AddXY(num27 / 1000000.0 * 343.0 / 2.0, (num21 - num20) / (num9 - num8) * (num27 - num8) + num20 + num25);
                        }
                    }
                    for (double num27 = num9; num27 < num10; num27 += 64.0)
                    {
                        dumpChart.Series[7].Points.AddXY(num27 / 1000000.0 * 343.0 / 2.0, (num22 - num21) / (num10 - num9) * (num27 - num9) + num21 + num26);
                    }
                    for (double num27 = num10; num27 < num11; num27 += 64.0)
                    {
                        dumpChart.Series[7].Points.AddXY(num27 / 1000000.0 * 343.0 / 2.0, (num23 - num22) / (num11 - num10) * (num27 - num10) + num22 + num26);
                    }
                    for (double num27 = num11; num27 < num12; num27 += 64.0)
                    {
                        dumpChart.Series[7].Points.AddXY(num27 / 1000000.0 * 343.0 / 2.0, (num24 - num23) / (num12 - num11) * (num27 - num11) + num23 + num26);
                    }
                    if (Convert.ToDouble(p1RecordCombo.Text) / 1000.0 * 343.0 / 2.0 > num12 / 1000000.0 * 343.0 / 2.0)
                    {
                        dumpChart.Series[7].Points.AddXY(Convert.ToDouble(p1RecordCombo.Text) / 1000.0 * 343.0 / 2.0, num24 + num25);
                    }
                    foreach (DataPoint dataPoint in dumpChart.Series[7].Points)
                    {
                        if (dataPoint.XValue == num / 1000000.0 * 343.0 / 2.0 || dataPoint.XValue == num2 / 1000000.0 * 343.0 / 2.0 || dataPoint.XValue == num3 / 1000000.0 * 343.0 / 2.0 || dataPoint.XValue == num4 / 1000000.0 * 343.0 / 2.0 || dataPoint.XValue == num5 / 1000000.0 * 343.0 / 2.0 || dataPoint.XValue == num6 / 1000000.0 * 343.0 / 2.0 || dataPoint.XValue == num7 / 1000000.0 * 343.0 / 2.0 || dataPoint.XValue == num8 / 1000000.0 * 343.0 / 2.0 || dataPoint.XValue == num9 / 1000000.0 * 343.0 / 2.0 || dataPoint.XValue == num10 / 1000000.0 * 343.0 / 2.0 || dataPoint.XValue == num11 / 1000000.0 * 343.0 / 2.0 || dataPoint.XValue == num12 / 1000000.0 * 343.0 / 2.0)
                        {
                            dataPoint.MarkerSize = 9;
                            dataPoint.MarkerStyle = MarkerStyle.Triangle;
                        }
                    }
                }
                else
                {
                    dumpChart.Series[7].Points.Clear();
                }
                for (double num27 = 0.0; num27 < num; num27 += 64.0)
                {
                    thrChart.Series[0].Points.AddXY(num27 / 1000.0, num13 + num25);
                }
                for (double num27 = num; num27 < num2; num27 += 64.0)
                {
                    thrChart.Series[0].Points.AddXY(num27 / 1000.0, (num14 - num13) / (num2 - num) * (num27 - num) + num13 + num25);
                }
                for (double num27 = num2; num27 < num3; num27 += 64.0)
                {
                    thrChart.Series[0].Points.AddXY(num27 / 1000.0, (num15 - num14) / (num3 - num2) * (num27 - num2) + num14 + num25);
                }
                for (double num27 = num3; num27 < num4; num27 += 64.0)
                {
                    thrChart.Series[0].Points.AddXY(num27 / 1000.0, (num16 - num15) / (num4 - num3) * (num27 - num3) + num15 + num25);
                }
                for (double num27 = num4; num27 < num5; num27 += 64.0)
                {
                    thrChart.Series[0].Points.AddXY(num27 / 1000.0, (num17 - num16) / (num5 - num4) * (num27 - num4) + num16 + num25);
                }
                for (double num27 = num5; num27 < num6; num27 += 64.0)
                {
                    thrChart.Series[0].Points.AddXY(num27 / 1000.0, (num18 - num17) / (num6 - num5) * (num27 - num5) + num17 + num25);
                }
                for (double num27 = num6; num27 < num7; num27 += 64.0)
                {
                    thrChart.Series[0].Points.AddXY(num27 / 1000.0, (num19 - num18) / (num7 - num6) * (num27 - num6) + num18 + num25);
                }
                for (double num27 = num7; num27 < num8; num27 += 64.0)
                {
                    thrChart.Series[0].Points.AddXY(num27 / 1000.0, (num20 - num19) / (num8 - num7) * (num27 - num7) + num19 + num25);
                }
                for (double num27 = num8; num27 < num9; num27 += 64.0)
                {
                    if (num27 == num8)
                    {
                        thrChart.Series[0].Points.AddXY(num27 / 1000.0, (num21 - num20) / (num9 - num8) * (num27 - num8) + num20 + num25);
                    }
                }
                for (double num27 = num9; num27 < num10; num27 += 64.0)
                {
                    thrChart.Series[0].Points.AddXY(num27 / 1000.0, (num22 - num21) / (num10 - num9) * (num27 - num9) + num21 + num26);
                }
                for (double num27 = num10; num27 < num11; num27 += 64.0)
                {
                    thrChart.Series[0].Points.AddXY(num27 / 1000.0, (num23 - num22) / (num11 - num10) * (num27 - num10) + num22 + num26);
                }
                for (double num27 = num11; num27 < num12; num27 += 64.0)
                {
                    thrChart.Series[0].Points.AddXY(num27 / 1000.0, (num24 - num23) / (num12 - num11) * (num27 - num11) + num23 + num26);
                }
                for (double num27 = 0.0; num27 < num12 / 1000000.0 * 343.0 / 2.0; num27 += 0.2)
                {
                    thrChart.Series[1].Points.AddXY(num27, 0.0);
                }
                foreach (DataPoint dataPoint in thrChart.Series[0].Points)
                {
                    if (dataPoint.XValue == num / 1000.0 || dataPoint.XValue == num2 / 1000.0 || dataPoint.XValue == num3 / 1000.0 || dataPoint.XValue == num4 / 1000.0 || dataPoint.XValue == num5 / 1000.0 || dataPoint.XValue == num6 / 1000.0 || dataPoint.XValue == num7 / 1000.0 || dataPoint.XValue == num8 / 1000.0 || dataPoint.XValue == num9 / 1000.0 || dataPoint.XValue == num10 / 1000.0 || dataPoint.XValue == num11 / 1000.0 || dataPoint.XValue == num12 / 1000.0)
                    {
                        dataPoint.MarkerSize = 9;
                        dataPoint.MarkerStyle = MarkerStyle.Triangle;
                    }
                }
                if (p2t1.Text == "" || p2t2.Text == "" || p2t3.Text == "" || p2t4.Text == "" ||
                    p2t5.Text == "" || p2t6.Text == "" || p2t7.Text == "" || p2t8.Text == "" ||
                    p2t9.Text == "" || p2t10.Text == "" || p2t11.Text == "" || p2t12.Text == "" ||
                    p2l1.Text == "" || p2t2.Text == "" || p2t3.Text == "" || p2t4.Text == "" ||
                    p2t5.Text == "" || p2t6.Text == "" || p2t7.Text == "" || p2t8.Text == "" ||
                    p2t9.Text == "" || p2l10.Text == "" || p2l11.Text == "" || p2l12.Text == "")
                {
                    if (PGA46xStat_box.Text.Contains("Ready") && !readingRegsFlag)
                    {
                        MessageBox.Show("Thr values cannot be blank.");
                    }
                }
                else
                {
                    thrChart.Series[2].Points.Clear();
                    thrChart.Series[3].Points.Clear();
                    num = Convert.ToDouble(p2t1.Text);
                    num2 = Convert.ToDouble(p2t1.Text) + Convert.ToDouble(p2t2.Text);
                    num3 = Convert.ToDouble(p2t1.Text) + Convert.ToDouble(p2t2.Text) + Convert.ToDouble(p2t3.Text);
                    num4 = Convert.ToDouble(p2t1.Text) + Convert.ToDouble(p2t2.Text) + Convert.ToDouble(p2t3.Text) + Convert.ToDouble(p2t4.Text);
                    num5 = Convert.ToDouble(p2t1.Text) + Convert.ToDouble(p2t2.Text) + Convert.ToDouble(p2t3.Text) + Convert.ToDouble(p2t4.Text) + Convert.ToDouble(p2t5.Text);
                    num6 = Convert.ToDouble(p2t1.Text) + Convert.ToDouble(p2t2.Text) + Convert.ToDouble(p2t3.Text) + Convert.ToDouble(p2t4.Text) + Convert.ToDouble(p2t5.Text) + Convert.ToDouble(p2t6.Text);
                    num7 = Convert.ToDouble(p2t1.Text) + Convert.ToDouble(p2t2.Text) + Convert.ToDouble(p2t3.Text) + Convert.ToDouble(p2t4.Text) + Convert.ToDouble(p2t5.Text) + Convert.ToDouble(p2t6.Text) + Convert.ToDouble(p2t7.Text);
                    num8 = Convert.ToDouble(p2t1.Text) + Convert.ToDouble(p2t2.Text) + Convert.ToDouble(p2t3.Text) + Convert.ToDouble(p2t4.Text) + Convert.ToDouble(p2t5.Text) + Convert.ToDouble(p2t6.Text) + Convert.ToDouble(p2t7.Text) + Convert.ToDouble(p2t8.Text);
                    num9 = Convert.ToDouble(p2t1.Text) + Convert.ToDouble(p2t2.Text) + Convert.ToDouble(p2t3.Text) + Convert.ToDouble(p2t4.Text) + Convert.ToDouble(p2t5.Text) + Convert.ToDouble(p2t6.Text) + Convert.ToDouble(p2t7.Text) + Convert.ToDouble(p2t8.Text) + Convert.ToDouble(p2t9.Text);
                    num10 = Convert.ToDouble(p2t1.Text) + Convert.ToDouble(p2t2.Text) + Convert.ToDouble(p2t3.Text) + Convert.ToDouble(p2t4.Text) + Convert.ToDouble(p2t5.Text) + Convert.ToDouble(p2t6.Text) + Convert.ToDouble(p2t7.Text) + Convert.ToDouble(p2t8.Text) + Convert.ToDouble(p2t9.Text) + Convert.ToDouble(p2t10.Text);
                    num11 = Convert.ToDouble(p2t1.Text) + Convert.ToDouble(p2t2.Text) + Convert.ToDouble(p2t3.Text) + Convert.ToDouble(p2t4.Text) + Convert.ToDouble(p2t5.Text) + Convert.ToDouble(p2t6.Text) + Convert.ToDouble(p2t7.Text) + Convert.ToDouble(p2t8.Text) + Convert.ToDouble(p2t9.Text) + Convert.ToDouble(p2t10.Text) + Convert.ToDouble(p2t11.Text);
                    num12 = Convert.ToDouble(p2t1.Text) + Convert.ToDouble(p2t2.Text) + Convert.ToDouble(p2t3.Text) + Convert.ToDouble(p2t4.Text) + Convert.ToDouble(p2t5.Text) + Convert.ToDouble(p2t6.Text) + Convert.ToDouble(p2t7.Text) + Convert.ToDouble(p2t8.Text) + Convert.ToDouble(p2t9.Text) + Convert.ToDouble(p2t10.Text) + Convert.ToDouble(p2t11.Text) + Convert.ToDouble(p2t12.Text);
                    num13 = Convert.ToDouble(p2l1.Text);
                    num14 = Convert.ToDouble(p2l2.Text);
                    num15 = Convert.ToDouble(p2l3.Text);
                    num16 = Convert.ToDouble(p2l4.Text);
                    num17 = Convert.ToDouble(p2l5.Text);
                    num18 = Convert.ToDouble(p2l6.Text);
                    num19 = Convert.ToDouble(p2l7.Text);
                    num20 = Convert.ToDouble(p2l8.Text);
                    num21 = Convert.ToDouble(p2l9.Text);
                    num22 = Convert.ToDouble(p2l10.Text);
                    num23 = Convert.ToDouble(p2l11.Text);
                    num24 = Convert.ToDouble(p2l12.Text);
                    num25 = 0.0;
                    num26 = 0.0;
                    switch (p2lOff.SelectedIndex)
                    {
                        case 0:
                            num25 = 0.0;
                            break;
                        case 1:
                            num25 = 1.0;
                            break;
                        case 2:
                            num25 = 2.0;
                            break;
                        case 3:
                            num25 = 3.0;
                            break;
                        case 4:
                            num25 = 4.0;
                            break;
                        case 5:
                            num25 = 5.0;
                            break;
                        case 6:
                            num25 = 6.0;
                            break;
                        case 7:
                            num25 = 7.0;
                            break;
                        case 8:
                            num25 = -8.0;
                            break;
                        case 9:
                            num25 = -7.0;
                            break;
                        case 10:
                            num25 = -6.0;
                            break;
                        case 11:
                            num25 = -5.0;
                            break;
                        case 12:
                            num25 = -4.0;
                            break;
                        case 13:
                            num25 = -3.0;
                            break;
                        case 14:
                            num25 = -2.0;
                            break;
                        case 15:
                            num25 = -1.0;
                            break;
                    }
                    if (thrP2PlotAddOnFlag)
                    {
                        dumpChart.Series[8].Points.Clear();
                        for (double num27 = 0.0; num27 < num; num27 += 64.0)
                        {
                            dumpChart.Series[8].Points.AddXY(num27 / 1000000.0 * 343.0 / 2.0, num13 + num25);
                        }
                        for (double num27 = num; num27 < num2; num27 += 64.0)
                        {
                            dumpChart.Series[8].Points.AddXY(num27 / 1000000.0 * 343.0 / 2.0, (num14 - num13) / (num2 - num) * (num27 - num) + num13 + num25);
                        }
                        for (double num27 = num2; num27 < num3; num27 += 64.0)
                        {
                            dumpChart.Series[8].Points.AddXY(num27 / 1000000.0 * 343.0 / 2.0, (num15 - num14) / (num3 - num2) * (num27 - num2) + num14 + num25);
                        }
                        for (double num27 = num3; num27 < num4; num27 += 64.0)
                        {
                            dumpChart.Series[8].Points.AddXY(num27 / 1000000.0 * 343.0 / 2.0, (num16 - num15) / (num4 - num3) * (num27 - num3) + num15 + num25);
                        }
                        for (double num27 = num4; num27 < num5; num27 += 64.0)
                        {
                            dumpChart.Series[8].Points.AddXY(num27 / 1000000.0 * 343.0 / 2.0, (num17 - num16) / (num5 - num4) * (num27 - num4) + num16 + num25);
                        }
                        for (double num27 = num5; num27 < num6; num27 += 64.0)
                        {
                            dumpChart.Series[8].Points.AddXY(num27 / 1000000.0 * 343.0 / 2.0, (num18 - num17) / (num6 - num5) * (num27 - num5) + num17 + num25);
                        }
                        for (double num27 = num6; num27 < num7; num27 += 64.0)
                        {
                            dumpChart.Series[8].Points.AddXY(num27 / 1000000.0 * 343.0 / 2.0, (num19 - num18) / (num7 - num6) * (num27 - num6) + num18 + num25);
                        }
                        for (double num27 = num7; num27 < num8; num27 += 64.0)
                        {
                            dumpChart.Series[8].Points.AddXY(num27 / 1000000.0 * 343.0 / 2.0, (num20 - num19) / (num8 - num7) * (num27 - num7) + num19 + num25);
                        }
                        for (double num27 = num8; num27 < num9; num27 += 64.0)
                        {
                            if (num27 == num8)
                            {
                                dumpChart.Series[8].Points.AddXY(num27 / 1000000.0 * 343.0 / 2.0, (num21 - num20) / (num9 - num8) * (num27 - num8) + num20 + num25);
                            }
                        }
                        for (double num27 = num9; num27 < num10; num27 += 64.0)
                        {
                            dumpChart.Series[8].Points.AddXY(num27 / 1000000.0 * 343.0 / 2.0, (num22 - num21) / (num10 - num9) * (num27 - num9) + num21 + num26);
                        }
                        for (double num27 = num10; num27 < num11; num27 += 64.0)
                        {
                            dumpChart.Series[8].Points.AddXY(num27 / 1000000.0 * 343.0 / 2.0, (num23 - num22) / (num11 - num10) * (num27 - num10) + num22 + num26);
                        }
                        for (double num27 = num11; num27 < num12; num27 += 64.0)
                        {
                            dumpChart.Series[8].Points.AddXY(num27 / 1000000.0 * 343.0 / 2.0, (num24 - num23) / (num12 - num11) * (num27 - num11) + num23 + num26);
                        }
                        if (Convert.ToDouble(p2RecordCombo.Text) / 1000.0 * 343.0 / 2.0 > num12 / 1000000.0 * 343.0 / 2.0)
                        {
                            dumpChart.Series[8].Points.AddXY(Convert.ToDouble(p2RecordCombo.Text) / 1000.0 * 343.0 / 2.0, num24 + num25);
                        }
                        foreach (DataPoint dataPoint in dumpChart.Series[8].Points)
                        {
                            if (dataPoint.XValue == num / 1000000.0 * 343.0 / 2.0 || dataPoint.XValue == num2 / 1000000.0 * 343.0 / 2.0 || dataPoint.XValue == num3 / 1000000.0 * 343.0 / 2.0 || dataPoint.XValue == num4 / 1000000.0 * 343.0 / 2.0 || dataPoint.XValue == num5 / 1000000.0 * 343.0 / 2.0 || dataPoint.XValue == num6 / 1000000.0 * 343.0 / 2.0 || dataPoint.XValue == num7 / 1000000.0 * 343.0 / 2.0 || dataPoint.XValue == num8 / 1000000.0 * 343.0 / 2.0 || dataPoint.XValue == num9 / 1000000.0 * 343.0 / 2.0 || dataPoint.XValue == num10 / 1000000.0 * 343.0 / 2.0 || dataPoint.XValue == num11 / 1000000.0 * 343.0 / 2.0 || dataPoint.XValue == num12 / 1000000.0 * 343.0 / 2.0)
                            {
                                dataPoint.MarkerSize = 9;
                                dataPoint.MarkerStyle = MarkerStyle.Triangle;
                            }
                        }
                    }
                    else
                    {
                        dumpChart.Series[8].Points.Clear();
                    }
                    for (double num27 = 0.0; num27 < num; num27 += 64.0)
                    {
                        thrChart.Series[2].Points.AddXY(num27 / 1000.0, num13 + num25);
                    }
                    for (double num27 = num; num27 < num2; num27 += 64.0)
                    {
                        thrChart.Series[2].Points.AddXY(num27 / 1000.0, (num14 - num13) / (num2 - num) * (num27 - num) + num13 + num25);
                    }
                    for (double num27 = num2; num27 < num3; num27 += 64.0)
                    {
                        thrChart.Series[2].Points.AddXY(num27 / 1000.0, (num15 - num14) / (num3 - num2) * (num27 - num2) + num14 + num25);
                    }
                    for (double num27 = num3; num27 < num4; num27 += 64.0)
                    {
                        thrChart.Series[2].Points.AddXY(num27 / 1000.0, (num16 - num15) / (num4 - num3) * (num27 - num3) + num15 + num25);
                    }
                    for (double num27 = num4; num27 < num5; num27 += 64.0)
                    {
                        thrChart.Series[2].Points.AddXY(num27 / 1000.0, (num17 - num16) / (num5 - num4) * (num27 - num4) + num16 + num25);
                    }
                    for (double num27 = num5; num27 < num6; num27 += 64.0)
                    {
                        thrChart.Series[2].Points.AddXY(num27 / 1000.0, (num18 - num17) / (num6 - num5) * (num27 - num5) + num17 + num25);
                    }
                    for (double num27 = num6; num27 < num7; num27 += 64.0)
                    {
                        thrChart.Series[2].Points.AddXY(num27 / 1000.0, (num19 - num18) / (num7 - num6) * (num27 - num6) + num18 + num25);
                    }
                    for (double num27 = num7; num27 < num8; num27 += 64.0)
                    {
                        thrChart.Series[2].Points.AddXY(num27 / 1000.0, (num20 - num19) / (num8 - num7) * (num27 - num7) + num19 + num25);
                    }
                    for (double num27 = num8; num27 < num9; num27 += 64.0)
                    {
                        if (num27 == num8)
                        {
                            thrChart.Series[2].Points.AddXY(num27 / 1000.0, (num21 - num20) / (num9 - num8) * (num27 - num8) + num20 + num25);
                        }
                    }
                    for (double num27 = num9; num27 < num10; num27 += 64.0)
                    {
                        thrChart.Series[2].Points.AddXY(num27 / 1000.0, (num22 - num21) / (num10 - num9) * (num27 - num9) + num21 + num26);
                    }
                    for (double num27 = num10; num27 < num11; num27 += 64.0)
                    {
                        thrChart.Series[2].Points.AddXY(num27 / 1000.0, (num23 - num22) / (num11 - num10) * (num27 - num10) + num22 + num26);
                    }
                    for (double num27 = num11; num27 < num12; num27 += 64.0)
                    {
                        thrChart.Series[2].Points.AddXY(num27 / 1000.0, (num24 - num23) / (num12 - num11) * (num27 - num11) + num23 + num26);
                    }
                    if (thrChart.Series[4].ChartType == SeriesChartType.Column)
                    {
                        thrChart.Series[1].Points.AddXY(0.0, 0.0);
                        thrChart.Series[1].Points.AddXY(thrChart.ChartAreas[0].AxisX.Maximum * 343.0 / 2.0 / 1000.0, 0.0);
                        thrChart.Series[3].Points.AddXY(0.0, 0.0);
                        thrChart.Series[3].Points.AddXY(thrChart.ChartAreas[0].AxisX.Maximum * 343.0 / 2.0 / 1000.0, 0.0);
                    }
                    else
                    {
                        for (double num27 = 0.0; num27 < num12 / 1000000.0 * 343.0 / 2.0; num27 += 0.2)
                        {
                            thrChart.Series[3].Points.AddXY(num27, 0.0);
                        }
                    }
                    foreach (DataPoint dataPoint in thrChart.Series[2].Points)
                    {
                        if (dataPoint.XValue == num / 1000.0 || dataPoint.XValue == num2 / 1000.0 || dataPoint.XValue == num3 / 1000.0 || dataPoint.XValue == num4 / 1000.0 || dataPoint.XValue == num5 / 1000.0 || dataPoint.XValue == num6 / 1000.0 || dataPoint.XValue == num7 / 1000.0 || dataPoint.XValue == num8 / 1000.0 || dataPoint.XValue == num9 / 1000.0 || dataPoint.XValue == num10 / 1000.0 || dataPoint.XValue == num11 / 1000.0 || dataPoint.XValue == num12 / 1000.0)
                        {
                            dataPoint.MarkerSize = 9;
                            dataPoint.MarkerStyle = MarkerStyle.Triangle;
                        }
                    }
                }
            }
        }

        public void updateTVGChart()
        {
            if (tvgt0.Text == "" || tvgt1.Text == "" || tvgt2.Text == "" || tvgt3.Text == "" || tvgt4.Text == "" || tvgt5.Text == "" || tvgg0.Text == "" || tvgg1.Text == "" || tvgg2.Text == "" || tvgg3.Text == "" || tvgg4.Text == "" || tvgg5.Text == "")
            {
                if (PGA46xStat_box.Text.Contains("Ready") && !readingRegsFlag)
                {
                    MessageBox.Show("TVG values cannot be blank.");
                }
            }
            else
            {
                tvgChart.Series[0].Points.Clear();
                tvgChart.Series[1].Points.Clear();
                double num = Convert.ToDouble(tvgt0.Text);
                double num2 = Convert.ToDouble(tvgt0.Text) + Convert.ToDouble(tvgt1.Text);
                double num3 = Convert.ToDouble(tvgt0.Text) + Convert.ToDouble(tvgt1.Text) + Convert.ToDouble(tvgt2.Text);
                double num4 = Convert.ToDouble(tvgt0.Text) + Convert.ToDouble(tvgt1.Text) + Convert.ToDouble(tvgt2.Text) + Convert.ToDouble(tvgt3.Text);
                double num5 = Convert.ToDouble(tvgt0.Text) + Convert.ToDouble(tvgt1.Text) + Convert.ToDouble(tvgt2.Text) + Convert.ToDouble(tvgt3.Text) + Convert.ToDouble(tvgt4.Text);
                double num6 = Convert.ToDouble(tvgt0.Text) + Convert.ToDouble(tvgt1.Text) + Convert.ToDouble(tvgt2.Text) + Convert.ToDouble(tvgt3.Text) + Convert.ToDouble(tvgt4.Text) + Convert.ToDouble(tvgt5.Text);
                double num7 = Convert.ToDouble(tvgg0.Text);
                double num8 = Convert.ToDouble(tvgg1.Text);
                double num9 = Convert.ToDouble(tvgg2.Text);
                double num10 = Convert.ToDouble(tvgg3.Text);
                double num11 = Convert.ToDouble(tvgg4.Text);
                double num12 = Convert.ToDouble(tvgg5.Text);
                for (double num13 = 0.0; num13 < num; num13 += 64.0)
                {
                    tvgChart.Series[0].Points.AddXY(num13 / 1000.0 / 1000.0, num7);
                }
                for (double num13 = num; num13 < num2; num13 += 64.0)
                {
                    tvgChart.Series[0].Points.AddXY(num13 / 1000.0, (num8 - num7) / (num2 - num) * (num13 - num) + num7);
                }
                for (double num13 = num2; num13 < num3; num13 += 64.0)
                {
                    tvgChart.Series[0].Points.AddXY(num13 / 1000.0, (num9 - num8) / (num3 - num2) * (num13 - num2) + num8);
                }
                for (double num13 = num3; num13 < num4; num13 += 64.0)
                {
                    tvgChart.Series[0].Points.AddXY(num13 / 1000.0, (num10 - num9) / (num4 - num3) * (num13 - num3) + num9);
                }
                for (double num13 = num4; num13 < num5; num13 += 64.0)
                {
                    tvgChart.Series[0].Points.AddXY(num13 / 1000.0, (num11 - num10) / (num5 - num4) * (num13 - num4) + num10);
                }
                for (double num13 = num5; num13 < num6; num13 += 64.0)
                {
                    tvgChart.Series[0].Points.AddXY(num13 / 1000.0, (num12 - num11) / (num6 - num5) * (num13 - num5) + num11);
                }
                if (tvgChart.Series[2].ChartType == SeriesChartType.Column)
                {
                    tvgChart.Series[1].Points.AddXY(0.0, 0.0);
                    tvgChart.Series[1].Points.AddXY(thrChart.ChartAreas[0].AxisX.Maximum * 343.0 / 2.0 / 1000.0, 0.0);
                }
                else
                {
                    for (double num13 = 0.0; num13 < num6 / 1000000.0 * 343.0 / 2.0; num13 += 0.2)
                    {
                        tvgChart.Series[1].Points.AddXY(num13, 0.0);
                    }
                }
                foreach (DataPoint dataPoint in tvgChart.Series[0].Points)
                {
                    if (dataPoint.XValue == num / 1000.0 || dataPoint.XValue == num2 / 1000.0 || dataPoint.XValue == num3 / 1000.0 || dataPoint.XValue == num4 / 1000.0 || dataPoint.XValue == num5 / 1000.0 || dataPoint.XValue == num6 / 1000.0)
                    {
                        dataPoint.MarkerSize = 9;
                        dataPoint.MarkerStyle = MarkerStyle.Triangle;
                    }
                }
                if (tvgPlotAddOnFlag)
                {
                    dumpChart.Series[9].Points.Clear();
                    for (double num13 = 0.0; num13 < num; num13 += 64.0)
                    {
                        dumpChart.Series[9].Points.AddXY(num13 / 1000000.0 * 343.0 / 2.0, num7);
                    }
                    for (double num13 = num; num13 < num2; num13 += 64.0)
                    {
                        dumpChart.Series[9].Points.AddXY(num13 / 1000000.0 * 343.0 / 2.0, (num8 - num7) / (num2 - num) * (num13 - num) + num7);
                    }
                    for (double num13 = num2; num13 < num3; num13 += 64.0)
                    {
                        dumpChart.Series[9].Points.AddXY(num13 / 1000000.0 * 343.0 / 2.0, (num9 - num8) / (num3 - num2) * (num13 - num2) + num8);
                    }
                    for (double num13 = num3; num13 < num4; num13 += 64.0)
                    {
                        dumpChart.Series[9].Points.AddXY(num13 / 1000000.0 * 343.0 / 2.0, (num10 - num9) / (num4 - num3) * (num13 - num3) + num9);
                    }
                    for (double num13 = num4; num13 < num5; num13 += 64.0)
                    {
                        dumpChart.Series[9].Points.AddXY(num13 / 1000000.0 * 343.0 / 2.0, (num11 - num10) / (num5 - num4) * (num13 - num4) + num10);
                    }
                    for (double num13 = num5; num13 < num6; num13 += 64.0)
                    {
                        dumpChart.Series[9].Points.AddXY(num13 / 1000000.0 * 343.0 / 2.0, (num12 - num11) / (num6 - num5) * (num13 - num5) + num11);
                    }
                    foreach (DataPoint dataPoint in dumpChart.Series[9].Points)
                    {
                        if (dataPoint.XValue == num / 1000000.0 * 343.0 / 2.0 || dataPoint.XValue == num2 / 1000000.0 * 343.0 / 2.0 || dataPoint.XValue == num3 / 1000000.0 * 343.0 / 2.0 || dataPoint.XValue == num4 / 1000000.0 * 343.0 / 2.0 || dataPoint.XValue == num5 / 1000000.0 * 343.0 / 2.0 || dataPoint.XValue == num6 / 1000000.0 * 343.0 / 2.0)
                        {
                            dataPoint.MarkerSize = 9;
                            dataPoint.MarkerStyle = MarkerStyle.Triangle;
                        }
                    }
                    if (p1Radio.Checked)
                    {
                        if (Convert.ToDouble(p1RecordCombo.Text) / 1000.0 * 343.0 / 2.0 > num6 / 1000000.0 * 343.0 / 2.0)
                        {
                            dumpChart.Series[9].Points.AddXY(Convert.ToDouble(p1RecordCombo.Text) / 1000.0 * 343.0 / 2.0, num12);
                        }
                    }
                    else if (Convert.ToDouble(p2RecordCombo.Text) / 1000.0 * 343.0 / 2.0 > num6 / 1000000.0 * 343.0 / 2.0)
                    {
                        dumpChart.Series[9].Points.AddXY(Convert.ToDouble(p2RecordCombo.Text) / 1000.0 * 343.0 / 2.0, num12);
                    }
                }
                else
                {
                    dumpChart.Series[9].Points.Clear();
                }
            }
        }

        private void Read_Sel_Regs_Click(object sender, EventArgs e)
        {
            activateProgressBar(true);
            readingRegsFlag = true;
            bool flag = false;
            if (thrUpdateCheck.Checked)
            {
                flag = true;
                thrUpdateCheck.Checked = false;
            }
            bool flag2 = false;
            if (tvgInstantUpdateCheck.Checked)
            {
                flag2 = true;
                tvgInstantUpdateCheck.Checked = false;
            }
            string text = leftTreeNav.SelectedNode.Text;
            switch (text)
            {
                case "Memory Map":
                    proj_MainCommunication(GRID_USER_MEMSPACE, false, true, false);
                    break;
                case "TI EEPROM":
                    proj_MainCommunication(GRID_TIEEPROM_MEMSPACE, false, true, false);
                    break;
                case "TI TESTMODE":
                    proj_MainCommunication(GRID_TITESTMODE_MEMSPACE, false, true, false);
                    break;
                case "User Data":
                    proj_MainCommunication(GRID_USERDATA_MEMSPACE, false, true, false);
                    break;
                case "Threshold Memory":
                    proj_MainCommunication(GRID_THRESHOLD_MEMSPACE, false, true, false);
                    break;
                case "Dump Memory":
                    proj_MainCommunication(GRID_DATADUMP_MEMSPACE, false, true, false);
                    break;
            }
            if (flag)
            {
                thrUpdateCheck.Checked = true;
            }
            if (flag2)
            {
                tvgInstantUpdateCheck.Checked = true;
            }
            readingRegsFlag = false;
            Fault_Stat_Update_button.PerformClick();
            activateProgressBar(false);
        }

        private void Read_all_Regs_Click(object sender, EventArgs e)
        {
            activateProgressBar(true);
            readingRegsFlag = true;
            bool flag = false;
            if (thrUpdateCheck.Checked)
            {
                flag = true;
                thrUpdateCheck.Checked = false;
            }
            bool flag2 = false;
            if (tvgInstantUpdateCheck.Checked)
            {
                flag2 = true;
                tvgInstantUpdateCheck.Checked = false;
            }
            Array.Clear(uart_return_data, 0, 64);
            string text = leftTreeNav.SelectedNode.Text;
            switch (text)
            {
                case "Memory Map":
                    proj_MainCommunication(GRID_USER_MEMSPACE, false, false, false);
                    break;
                case "TI EEPROM":
                    proj_MainCommunication(GRID_TIEEPROM_MEMSPACE, false, false, false);
                    break;
                case "TI TESTMODE":
                    proj_MainCommunication(GRID_TITESTMODE_MEMSPACE, false, false, false);
                    break;
                case "User Data":
                    proj_MainCommunication(GRID_USERDATA_MEMSPACE, false, false, false);
                    break;
                case "Threshold Memory":
                    proj_MainCommunication(GRID_THRESHOLD_MEMSPACE, false, false, false);
                    break;
                case "Dump Memory":
                    proj_MainCommunication(GRID_DATADUMP_MEMSPACE, false, false, false);
                    break;
            }
            if (flag)
            {
                thrUpdateCheck.Checked = true;
            }
            if (flag2)
            {
                tvgInstantUpdateCheck.Checked = true;
            }
            readingRegsFlag = false;
            Fault_Stat_Update_button.PerformClick();
            activateProgressBar(false);
        }

        private void ReadAllRegs(bool pagechange)
        {
            activateProgressBar(true);
            readingRegsFlag = true;
            bool flag = false;
            if (thrUpdateCheck.Checked)
            {
                flag = true;
                thrUpdateCheck.Checked = false;
            }
            bool flag2 = false;
            if (tvgInstantUpdateCheck.Checked)
            {
                flag2 = true;
                tvgInstantUpdateCheck.Checked = false;
            }
            if (!initThrFlag)
            {
            }
            if (readRegsOffPage)
            {
                proj_MainCommunication(GRID_USER_MEMSPACE, false, false, pagechange);
            }
            else
            {
                string text = leftTreeNav.SelectedNode.Text;
                switch (text)
                {
                    case "Memory Map":
                        proj_MainCommunication(GRID_USER_MEMSPACE, false, false, pagechange);
                        break;
                    case "TI EEPROM":
                        proj_MainCommunication(GRID_TIEEPROM_MEMSPACE, false, false, pagechange);
                        break;
                    case "TI TESTMODE":
                        proj_MainCommunication(GRID_TITESTMODE_MEMSPACE, false, false, pagechange);
                        break;
                    case "User Data":
                        proj_MainCommunication(GRID_USERDATA_MEMSPACE, false, false, pagechange);
                        break;
                    case "Threshold Memory":
                        proj_MainCommunication(GRID_THRESHOLD_MEMSPACE, false, false, pagechange);
                        break;
                    case "Dump Memory":
                        proj_MainCommunication(GRID_DATADUMP_MEMSPACE, false, false, pagechange);
                        break;
                }
            }
            if (flag)
            {
                thrUpdateCheck.Checked = true;
            }
            if (flag2)
            {
                tvgInstantUpdateCheck.Checked = true;
            }
            readingRegsFlag = false;
            Fault_Stat_Update_button.PerformClick();
            activateProgressBar(false);
        }

        private void Write_sel_regs_Click(object sender, EventArgs e)
        {
            activateProgressBar(true);
            bool flag = false;
            if (thrUpdateCheck.Checked)
            {
                flag = true;
                thrUpdateCheck.Checked = false;
            }
            bool flag2 = false;
            if (tvgInstantUpdateCheck.Checked)
            {
                flag2 = true;
                tvgInstantUpdateCheck.Checked = false;
            }
            string text = leftTreeNav.SelectedNode.Text;
            switch (text)
            {
                case "Memory Map":
                    proj_MainCommunication(GRID_USER_MEMSPACE, true, true, false);
                    break;
                case "TI EEPROM":
                    proj_MainCommunication(GRID_TIEEPROM_MEMSPACE, true, true, false);
                    break;
                case "TI TESTMODE":
                    proj_MainCommunication(GRID_TITESTMODE_MEMSPACE, true, true, false);
                    break;
                case "User Data":
                    proj_MainCommunication(GRID_USERDATA_MEMSPACE, true, true, false);
                    break;
                case "Threshold Memory":
                    proj_MainCommunication(GRID_THRESHOLD_MEMSPACE, true, true, false);
                    break;
                case "Dump Memory":
                    proj_MainCommunication(GRID_DATADUMP_MEMSPACE, true, true, false);
                    break;
            }
            if (flag)
            {
                thrUpdateCheck.Checked = true;
            }
            if (flag2)
            {
                tvgInstantUpdateCheck.Checked = true;
            }
            Fault_Stat_Update_button.PerformClick();
            activateProgressBar(false);
        }

        private void Write_all_regs_Click(object sender, EventArgs e)
        {
            activateProgressBar(true);
            bool flag = false;
            if (thrUpdateCheck.Checked)
            {
                flag = true;
                thrUpdateCheck.Checked = false;
            }
            bool flag2 = false;
            if (tvgInstantUpdateCheck.Checked)
            {
                flag2 = true;
                tvgInstantUpdateCheck.Checked = false;
            }
            string text = leftTreeNav.SelectedNode.Text;
            switch (text)
            {
                case "Memory Map":
                    proj_MainCommunication(GRID_USER_MEMSPACE, true, false, false);
                    break;
                case "TI EEPROM":
                    proj_MainCommunication(GRID_TIEEPROM_MEMSPACE, true, false, false);
                    break;
                case "TI TESTMODE":
                    proj_MainCommunication(GRID_TITESTMODE_MEMSPACE, true, false, false);
                    break;
                case "User Data":
                    proj_MainCommunication(GRID_USERDATA_MEMSPACE, true, false, false);
                    break;
                case "Threshold Memory":
                    proj_MainCommunication(GRID_THRESHOLD_MEMSPACE, true, false, false);
                    break;
                case "Dump Memory":
                    proj_MainCommunication(GRID_DATADUMP_MEMSPACE, true, false, false);
                    break;
            }
            if (flag)
            {
                thrUpdateCheck.Checked = true;
            }
            if (flag2)
            {
                writeTVGMemBtn_Click(null, null);
            }
            Fault_Stat_Update_button.PerformClick();
            activateProgressBar(false);
        }

        private void zero_grid_butt_Click(object sender, EventArgs e)
        {
            string text = leftTreeNav.SelectedNode.Text;
            switch (text)
            {
                case "Memory Map":
                    GRID_USER_MEMSPACE.zeroOutGridValues();
                    break;
                case "TI EEPROM":
                    GRID_TIEEPROM_MEMSPACE.zeroOutGridValues();
                    break;
                case "TI TESTMODE":
                    GRID_TITESTMODE_MEMSPACE.zeroOutGridValues();
                    break;
                case "User Data":
                    GRID_USERDATA_MEMSPACE.zeroOutGridValues();
                    break;
                case "Threshold Memory":
                    GRID_THRESHOLD_MEMSPACE.zeroOutGridValues();
                    break;
                case "Dump Memory":
                    GRID_DATADUMP_MEMSPACE.zeroOutGridValues();
                    break;
            }
        }

        private void desel_grid_butt_Click(object sender, EventArgs e)
        {
            string text = leftTreeNav.SelectedNode.Text;
            switch (text)
            {
                case "Memory Map":
                    GRID_USER_MEMSPACE.resetAllGridValues();
                    break;
                case "TI EEPROM":
                    GRID_TIEEPROM_MEMSPACE.resetAllGridValues();
                    break;
                case "TI TESTMODE":
                    GRID_TITESTMODE_MEMSPACE.resetAllGridValues();
                    break;
                case "User Data":
                    GRID_USERDATA_MEMSPACE.resetAllGridValues();
                    break;
                case "Threshold Memory":
                    GRID_THRESHOLD_MEMSPACE.resetAllGridValues();
                    break;
                case "Dump Memory":
                    GRID_DATADUMP_MEMSPACE.resetAllGridValues();
                    break;
            }
        }

        private void Save_grid_butt_Click(object sender, EventArgs e)
        {
            RegisterValueGridEditor registerValueGridEditor = null;
            string text = leftTreeNav.SelectedNode.Text;
            switch (text)
            {
                case "Memory Map":
                    registerValueGridEditor = GRID_USER_MEMSPACE;
                    break;
                case "TI EEPROM":
                    registerValueGridEditor = GRID_TIEEPROM_MEMSPACE;
                    break;
                case "TI TESTMODE":
                    registerValueGridEditor = GRID_TITESTMODE_MEMSPACE;
                    break;
                case "User Data":
                    registerValueGridEditor = GRID_USERDATA_MEMSPACE;
                    break;
                case "Threshold Memory":
                    registerValueGridEditor = GRID_THRESHOLD_MEMSPACE;
                    break;
                case "Dump Memory":
                    registerValueGridEditor = GRID_DATADUMP_MEMSPACE;
                    break;
            }
            if (registerValueGridEditor != null)
            {
                Files files = new Files();
                string gridName = registerValueGridEditor.getGridName();
                files.WriteGridToFile(registerValueGridEditor, gridName, Environment.GetFolderPath(Environment.SpecialFolder.Personal) + "\\BOOSTXL-PGA460\\", gridName + "-" + DateTime.Now.ToString("yyyy-MM-dd_HHmmss") + ".txt");
                files.Dispose();
            }
            else
            {
                MessageBox.Show("Click anywhere on an address/data grid to select it. ERROR-- No grid was selected.");
            }
        }

        private void Load_grid_butt_Click(object sender, EventArgs e)
        {
            RegisterValueGridEditor registerValueGridEditor = null;
            string text = leftTreeNav.SelectedNode.Text;
            switch (text)
            {
                case "Memory Map":
                    registerValueGridEditor = GRID_USER_MEMSPACE;
                    break;
                case "TI EEPROM":
                    registerValueGridEditor = GRID_TIEEPROM_MEMSPACE;
                    break;
                case "TI TESTMODE":
                    registerValueGridEditor = GRID_TITESTMODE_MEMSPACE;
                    break;
                case "User Data":
                    registerValueGridEditor = GRID_USERDATA_MEMSPACE;
                    break;
                case "Threshold Memory":
                    registerValueGridEditor = GRID_THRESHOLD_MEMSPACE;
                    break;
                case "Dump Memory":
                    registerValueGridEditor = GRID_DATADUMP_MEMSPACE;
                    break;
            }
            if (registerValueGridEditor != null)
            {
                Files files = new Files();
                string gridName = registerValueGridEditor.getGridName();
                readSuccessBool = files.ReadGridFromFile(registerValueGridEditor, gridName, null, gridName + ".txt");
                files.Dispose();
            }
            else
            {
                MessageBox.Show("Click anywhere on an address/data grid to select it. ERROR-- No grid was selected.");
            }
        }

        private void pictureBox1_DoubleClick(object sender, EventArgs e)
        {
        }

        private void tilogoSecret_Click(object sender, EventArgs e)
        {
            TIunlock_textBox.Visible = true;
            TIunlock_textBox.Enabled = true;
            TIunlock_textBox.Focus();
        }

        private void TIunlock_textBox_KeyPress(object sender, KeyPressEventArgs e)
        {
            if (e.KeyChar == '\r')
            {
                if (TIunlock_textBox.Text == "msa2016")
                {
                    leftTreeNav.Nodes[0].Nodes[3].Nodes.Add("TI EEPROM");
                    leftTreeNav.Nodes[0].Nodes[3].Nodes.Add("TI TESTMODE");
                    leftTreeNav.Refresh();
                    leftTreeNav.ExpandAll();
                    unlockTIEEPROM.Visible = true;
                    unlockTIEEPROM_Click(null, null);
                    pgrmIntTIEEBtn.Visible = true;
                    Registers_createGrids("TI_EEPROM");
                    Registers_createGrids("TI_TESTMODE");
                    TIunlock_textBox.Enabled = false;
                    TIunlock_textBox.Visible = false;
                    adcShiftGroup.Visible = true;
                    plotADCBtn.Visible = true;
                    fullbridgeBox.Visible = true;
                    customIndexIn.Visible = true;
                    customIndexBtn.Visible = true;
                    loadBatchBtn.Visible = true;
                    primaryTab.ItemSize = new Size(10, 10);
                    toolStripComboBox1.Visible = true;
                    graphModeCombo.Items.Clear();
                    graphModeCombo.Items.AddRange(new object[]
                    {
                        "Data Dump",
                        "ADC",
                        "DSP - BP Filter",
                        "DSP - Rectifier",
                        "DSP - LP Filter",
                        "Temp Sensor"
                    });
                    graphModeCombo.SelectedIndex = 0;
                    toggleLEDGPIO.Visible = true;
                    adcBgControls.Visible = true;
                    drvGroup.Visible = true;
                    directEEReadGroup.Visible = true;
                }
                else
                {
                    TIunlock_textBox.Text = "";
                }
            }
        }

        public void setTextInDECbox(string text)
        {
            textBox_baseConverter_Dec.Text = text;
        }

        public string getTextinDECbox()
        {
            return textBox_baseConverter_Dec.Text;
        }

        private void BaseConverter(string HEX_DEC_BIN)
        {
            if (HEX_DEC_BIN == "HEX")
            {
                try
                {
                    long value = Convert.ToInt64(textBox_baseConverter_Hex.Text, 16);
                    string text = Convert.ToString(value, 2);
                    textBox_baseConverter_Hex.Text = textBox_baseConverter_Hex.Text.ToUpper();
                    textBox_baseConverter_Binary.Text = text;
                    textBox_baseConverter_Dec.Text = value.ToString();
                }
                catch
                {
                    textBox_baseConverter_Hex.Text = "Invalid HEX value";
                    Tools.timeDelay(500, "ms");
                    textBox_baseConverter_Hex.Text = "FF";
                }
            }
            else if (HEX_DEC_BIN == "DEC")
            {
                try
                {
                    long value = Convert.ToInt64(textBox_baseConverter_Dec.Text, 10);
                    string text2 = Convert.ToString(value, 16);
                    string text = Convert.ToString(value, 2);
                    textBox_baseConverter_Hex.Text = text2.ToUpper();
                    textBox_baseConverter_Binary.Text = text;
                    textBox_baseConverter_Dec.Text = value.ToString();
                }
                catch
                {
                    textBox_baseConverter_Dec.Text = "Invalid DEC value";
                    Tools.timeDelay(500, "ms");
                    textBox_baseConverter_Dec.Text = "255";
                }
            }
            else
            {
                try
                {
                    long value = Convert.ToInt64(textBox_baseConverter_Binary.Text, 2);
                    string text2 = Convert.ToString(value, 16);
                    string text = Convert.ToString(value, 2);
                    textBox_baseConverter_Hex.Text = text2.ToUpper();
                    textBox_baseConverter_Binary.Text = text;
                    textBox_baseConverter_Dec.Text = value.ToString();
                }
                catch
                {
                    textBox_baseConverter_Binary.Text = "Invalid BIN value";
                    Tools.timeDelay(500, "ms");
                    textBox_baseConverter_Binary.Text = "1111111111111111";
                }
            }
        }

        private void baseConverterKeyPress(object sender, KeyPressEventArgs e)
        {
            if (textBox_baseConverter_Hex.ContainsFocus)
            {
                if (e.KeyChar == '\r')
                {
                    BaseConverter("HEX");
                }
            }
            if (textBox_baseConverter_Dec.ContainsFocus)
            {
                if (e.KeyChar == '\r')
                {
                    BaseConverter("DEC");
                }
            }
            if (textBox_baseConverter_Binary.ContainsFocus)
            {
                if (e.KeyChar == '\r')
                {
                    BaseConverter("BIN");
                }
            }
        }

        private int calRegValfromFields(List<Bit_Field> bitFields)
        {
            char[] array = Tools.int32_Into_stringBase2(0, 8).ToCharArray();
            foreach (Bit_Field bit_Field in bitFields)
            {
                int num = 0;
                for (int i = bit_Field.start_index; i < bit_Field.start_index + bit_Field.length; i++)
                {
                    array[i] = bit_Field.value[num++];
                }
            }
            return Convert.ToInt32(Tools.StringBase2_Into_StringBase16(new string(array)), 16);
        }

        private void Unlck_EE_button_Click(object sender, EventArgs e)
        {
        }

        private void Reload_EE_button_Click(object sender, EventArgs e)
        {
            RegDefs regDefs = new RegDefs("EE_CRC");
            regDefs.EE_CRC.ReadFromUART();
            regDefs.EE_CRC_B.value = "10100010";
            regDefs.EE_CRC.WriteToUART();
            while (regDefs.EE_CRC_B.value != "00000000")
            {
                regDefs.EE_CRC.ReadFromUART();
            }
        }

        private void Program_EE_button_Click(object sender, EventArgs e)
        {
            RegDefs regDefs = new RegDefs("EE_CRC");
            regDefs.EE_CRC.ReadFromUART();
            regDefs.EE_CRC_B.value = "10100111";
            regDefs.EE_CRC.WriteToUART();
            while (regDefs.EE_CRC_B.value != "00000000")
            {
                regDefs.EE_CRC.ReadFromUART();
            }
        }

        private void shortCutsBox_ItemCheck(object sender, ItemCheckEventArgs e)
        {
        }

        private void MemMap_Leave()
        {
        }

        private void button1_Click(object sender, EventArgs e)
        {
        }

        private void button1_MouseHover(object sender, EventArgs e)
        {
            ToolTip toolTip = new ToolTip();
            toolTip.ShowAlways = true;
        }

        private void Form1_Load(object sender, EventArgs e)
        {
            SplashScreen.UdpateStatusText("Loading Items...");
            Show();
            SplashScreen.CloseSplashScreen();
            Activate();
        }

        private void dataSheetToolStripMenuItem_Click(object sender, EventArgs e)
        {
            Process.Start("http://www.ti.com/lit/gpn/pga460-q1");
        }

        private void collateralToolStripMenuItem_Click_1(object sender, EventArgs e)
        {
            Process.Start("http://www.ti.com/product/PGA460-Q1/description");
        }

        private void Scale_range_Enter(object sender, EventArgs e)
        {
            try
            {
                tempvalue = Convert.ToInt64(((TextBox)sender).Text);
            }
            catch (FormatException)
            {
                ((TextBox)sender).Text = "0";
            }
        }

        private void Search_button_Click(object sender, EventArgs e)
        {
            Search_Results.Rows.Clear();
            IEnumerable<search> enumerable = from register in searchlist
                                             where register.description.Contains(Search_query.Text.ToLower())
                                             select register;
            foreach (search search in enumerable)
            {
                Search_Results.Rows.Add(new object[] { search.reg_name, search.reg_address });
            }
        }

        private void statusbarupdate(string diagorunlk)
        {
            if (diagorunlk == "Device Locked" && !Device_unlocked)
                MessageBox.Show("Please unlock the Device to change the values. (Press Device Unlock button)");
        }

        private void Form1_Shown(object sender, EventArgs e)
        {
            firstTime = true;
            ReadAllRegs(false);
            firstTime = false;
        }

        [DllImport("kernel32")]
        private static extern int GetPrivateProfileString(string section, string key, string def, StringBuilder retVal, int size, string filePath);

        private string Readini(string section, string key)
        {
            StringBuilder stringBuilder = new StringBuilder(255);
            int privateProfileString = GetPrivateProfileString(section, key, "", stringBuilder, 255, getfilepath());
            return stringBuilder.ToString();
        }

        private string getfilepath()
        {
            string result;
            try
            {
                result = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.Personal) + "\\BOOSTXL-PGA460\\", Assembly.GetExecutingAssembly().GetName().Name) + ".ini";
            }
            catch
            {
                result = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.Personal) + "\\BOOSTXL-PGA460\\", "\\PGA460-Q1 EVM GUI.ini");
            }
            return result;
        }

        private void updateThrBtn_Click(object sender, EventArgs e)
        {
            updateThresholdChart();
        }

        public void runBtn_Click(object sender, EventArgs e)
        {
            bool @checked = p1Radio.Checked;
            bool checked2 = rxFalseRadio.Checked;
            bool checked3 = byteLSBRadio.Checked;
            bool checked4 = bit8sampleRadio.Checked;
            byte b = 0;

            if (!Regex.IsMatch(startDelayTextBox.Text, "^\\d+$"))
                startDelayTextBox.Text = "0";

            if (startDelayTextBox.Text == "")
                startDelayTextBox.Text = "0";
            else
                Tools.timeDelay(Convert.ToDouble(startDelayTextBox.Text), "MS");

            if (@checked && checked2)
            {
                if (toolStripComboBox1.Text == "UART")
                    b = (byte)(((int)Convert.ToByte(uartAddrCombo.Text) << addrShift) + (int)uart_cmd0);
                else if (toolStripComboBox1.Text == "TCI")
                    tciCommandCombo.SelectedIndex = 0;
            }
            else if (!@checked && checked2)
            {
                if (toolStripComboBox1.Text == "UART")
                    b = (byte)(((int)Convert.ToByte(uartAddrCombo.Text) << addrShift) + (int)uart_cmd1);
                else if (toolStripComboBox1.Text == "TCI")
                    tciCommandCombo.SelectedIndex = 1;
            }
            else if (@checked && !checked2)
            {
                if (toolStripComboBox1.Text == "UART")
                    b = (byte)(((int)Convert.ToByte(uartAddrCombo.Text) << addrShift) + (int)uart_cmd2);
                else if (toolStripComboBox1.Text == "TCI")
                    tciCommandCombo.SelectedIndex = 2;
            }
            else
            {
                if (@checked || checked2)
                    return;
                if (toolStripComboBox1.Text == "UART")
                    b = (byte)(((int)Convert.ToByte(uartAddrCombo.Text) << addrShift) + (int)uart_cmd3);
                else if (toolStripComboBox1.Text == "TCI")
                    tciCommandCombo.SelectedIndex = 3;
            }
            if (freqshiftCheck.Checked && coefCalcFreq.Text != "")
            {
                writeCoef_Click(null, null);
                Tools.timeDelay(10, "MS");
                debugTabControl.SelectTab(statusTab);
            }
            if (graphModeCombo.Text == "Data Dump" && PGA46xStat_box.Text.Contains("Ready") && freqCombo.Text != "")
            {
                dumpChart.Series[6].Points.Clear();
                int selectedIndex = tciCommandCombo.SelectedIndex;
                Tools.timeDelay(10, "MS");
                if (runBtn.Text == "START")
                {
                    runBtn.Text = "STOP";
                    dataMonitorCheckListBox_SelectedIndexChanged(null, null);
                    if (toolStripComboBox1.Text == "UART")
                    {
                        if (THRCRCERR_Stat_TextBox.Text == "1")
                        {
                            if (MessageBox.Show("Thresholds must be written to at least once before running burst and/or listen command to clear the THR CRC Error.\n\nIf THR CRC Error is already cleared, ignore this message.\n\nDo you want to update thresholds with predefined mid-code threshold values?", "Threshold Status", MessageBoxButtons.YesNo, MessageBoxIcon.Exclamation) == DialogResult.Yes)
                            {
                                thrUpdateCheck.Checked = true;
                                tabcontrolThr.SelectTab(0);
                                allMidCodeBtn_Click(null, null);
                                Tools.timeDelay(10, "MS");
                                tabcontrolThr.SelectTab(1);
                                allMidCodeBtn_Click(null, null);
                                thrUpdateCheck.Checked = false;
                                Tools.timeDelay(10, "MS");
                                thrUpdatedAtLeastOnce = true;
                                Monitor_run(b);
                            }
                            else if (THRCRCERR_Stat_TextBox.Text == "1")
                                runBtn.Text = "START";
                            else
                                Monitor_run(b);
                        }
                        else
                            Monitor_run(b);
                    }
                    else if (toolStripComboBox1.Text == "TCI")
                    {
                        byte[] array = new byte[130];
                        int num = Convert.ToInt32(sampleMaxCombo.Text);
                        int num2 = 0;
                        CurrentLoopLabel.Text = "0";
                        while (runBtn.Text == "STOP")
                        {
                            CurrentLoopLabel.Text = Convert.ToString(num2);
                            if (Convert.ToDouble(loopDelayTextBox.Text) - 270.0 < 0.0)
                                Tools.timeDelay(Convert.ToDouble(loopDelayTextBox.Text), "MS");
                            else if (loopDelayTextBox.Text == "")
                            {
                                loopDelayTextBox.Text = "0";
                                Tools.timeDelay(Convert.ToDouble(loopDelayTextBox.Text), "MS");
                            }
                            else
                                Tools.timeDelay(Convert.ToDouble(loopDelayTextBox.Text) - 270.0, "MS");

                            if (Convert.ToInt16(loopBox.Text) != 0 & num2 == (int)Convert.ToInt16(loopBox.Text))
                            {
                                num2 = 0;
                                runBtn.Text = "START";
                            }
                            else
                            {
                                num2++;
                                ind11EDDEW.SelectedIndex = 1;
                                ind11PgrmEEW.SelectedIndex = 0;
                                ind11ReloadEEW.SelectedIndex = 0;
                                tciCommandCombo.SelectedIndex = 4;
                                tciIndexCombo.Enabled = true;
                                tciIndexCombo.SelectedIndex = 11;
                                writeIndexBtn_Click(null, null);

                                Tools.timeDelay(100, "MS");
                                tciCommandCombo.SelectedIndex = selectedIndex;
                                runTCIBtn_Click(null, null);
                                Tools.timeDelay(70, "MS");
                                b = (byte)(((int)Convert.ToByte(uartAddrCombo.Text) << addrShift) + (int)uart_cmd7);
                                MChecksumByte = calculate_UART_Checksum(new byte[] { b });
                                common.u2a.UART_Write(3, new byte[] { syncByte, b, MChecksumByte });
                                Tools.timeDelay(70, "MS");
                                Array.Clear(array, 0, 130);
                                common.u2a.UART_Read(130, array);
                                uartDiagB = uart_return_data[0];
                                if (contClearCheck.Checked)
                                    dumpChart.Series[11].Points.Clear();
                                else
                                    dumpChart.Series[11].Points.AddXY(-1.0, -11.0);

                                for (int i = 0; i < num; i++)
                                    dumpChart.Series[11].Points.AddXY((double)i, (double)array[i]);

                                if (!contClearCheck.Checked)
                                    dumpChart.Series[11].Points.AddXY((double)num, -11.0);

                                ind11EDDEW.SelectedIndex = 0;
                                ind11PgrmEEW.SelectedIndex = 0;
                                ind11ReloadEEW.SelectedIndex = 0;
                                tciCommandCombo.SelectedIndex = 4;
                                tciIndexCombo.Enabled = true;
                                tciIndexCombo.SelectedIndex = 11;
                                writeIndexBtn_Click(null, null);

                                if ((exportDataCheck.Checked && CurrentLoopLabel.Text != "('0'=∞)") || (exportDataCheck.Checked && loopBox.Text == "0") || bgExportCheck.Checked)
                                {
                                    int num3;
                                    if (contClearCheck.Checked)
                                        num3 = num;
                                    else
                                        num3 = num + 1;

                                    string[] array2 = new string[num3 * num2];
                                    if ((exportTimeStampCheck.Checked && !exportAdvCheck.Checked) || (!exportTimeStampCheck.Checked && exportAdvCheck.Checked))
                                        array2 = new string[num3 * num2 + 1];
                                    if (exportTimeStampCheck.Checked && exportAdvCheck.Checked)
                                        array2 = new string[num3 * num2 + 2];
                                    if (num3 > 1)
                                    {
                                        int j;
                                        if (contClearCheck.Checked)
                                        {
                                            for (j = 0; j < num3 - 1; j++)
                                                valuesToExport.Add(dumpChart.Series[11].Points[j].YValues[0].ToString());
                                        }
                                        else
                                        {
                                            for (j = 0; j < num3 - 2; j++)
                                                valuesToExport.Add(dumpChart.Series[11].Points[j + 1].YValues[0].ToString());
                                        }
                                        if ((exportTimeStampCheck.Checked && !exportAdvCheck.Checked) || (exportTimeStampCheck.Checked && exportAdvCheck.Checked))
                                        {
                                            if ((j == num3 - 1 && contClearCheck.Checked) || (j == num3 - 2 && !contClearCheck.Checked))
                                                valuesToExport.Add(DateTime.Now.ToString("MM\\/dd\\/yyyy h\\:mm\\:ss\\:ff tt"));
                                        }
                                        if (!exportTimeStampCheck.Checked && exportAdvCheck.Checked)
                                        {
                                            if ((j == num3 - 1 && contClearCheck.Checked) || (j == num3 - 2 && !contClearCheck.Checked))
                                            {
                                                tempOnlyBtn_Click(null, null);
                                                valuesToExport.Add("T=" + tempBox.Text);
                                            }
                                        }
                                        if (exportTimeStampCheck.Checked && exportAdvCheck.Checked)
                                        {
                                            tempOnlyBtn_Click(null, null);
                                            valuesToExport.Add("T=" + tempBox.Text);
                                        }
                                        if ((exportDataCheck.Checked && CurrentLoopLabel.Text != "('0'=∞)") || (exportDataCheck.Checked && loopBox.Text == "0"))
                                        {
                                            if (Convert.ToInt32(CurrentLoopLabel.Text) + 1 == Convert.ToInt32(loopBox.Text) || (loopBox.Text == "0" && runBtn.Text == "START"))
                                            {
                                                array2 = valuesToExport.ConvertAll<string>((string x) => x.ToString()).ToArray();
                                                Files files = new Files();
                                                string text = files.CreateFileName("PGA460-EDD", "", exportSaveAs, true, true, false);
                                                files.WriteArrayToFile("EDD", array2, Environment.GetFolderPath(Environment.SpecialFolder.Personal) + "\\BOOSTXL-PGA460\\", text);
                                                files.Dispose();
                                                valuesToExport.Clear();
                                            }
                                        }
                                        if (bgExportCheck.Checked)
                                        {
                                            array2 = valuesToExport.ConvertAll<string>((string x) => x.ToString()).ToArray();
                                            Files files = new Files();
                                            string text;
                                            if (!psStatusBox.Visible)
                                                text = files.CreateFileName("PGA460-EDD-BG", "", exportSaveAs, true, true, false);
                                            else
                                                text = files.CreateFileName("PGA460-EDD-BG " + psStatusBox.Text, "", exportSaveAs, true, true, false);

                                            files.WriteArrayToFile("EDD", array2, bgExportBox.Text, text);
                                            files.Dispose();
                                            valuesToExport.Clear();
                                        }
                                    }
                                    else
                                        MessageBox.Show("No Data to be Exported.");
                                }
                            }
                        }
                        runBtn.Text = "START";
                    }
                }
                else
                    runBtn.Text = "START";

                common.u2a.UART_Read(64, uart_return_data);
                common.u2a.UART_Read(64, uart_return_data);
                common.u2a.UART_Read(64, uart_return_data);
                Array.Clear(uart_return_data, 0, 64);
                common.u2a.UART_Read(64, uart_return_data);
                common.u2a.UART_Read(64, uart_return_data);
                common.u2a.UART_Read(64, uart_return_data);
                Array.Clear(uart_return_data, 0, 64);
                common.u2a.UART_Read(64, uart_return_data);
                common.u2a.UART_Read(64, uart_return_data);
                common.u2a.UART_Read(64, uart_return_data);
                Array.Clear(uart_return_data, 0, 64);
            }
            else if (graphModeCombo.Text == "ADC" || graphModeCombo.Text == "DSP - BP Filter" || graphModeCombo.Text == "DSP - Rectifier" || graphModeCombo.Text == "DSP - LP Filter")
            {
                byte b2 = 0;
                byte b3 = 0;
                if (graphModeCombo.Text == "ADC")
                    datapathMuxSelCombo.SelectedIndex = 4;
                if (graphModeCombo.Text == "DSP - BP Filter")
                    datapathMuxSelCombo.SelectedIndex = 3;
                if (graphModeCombo.Text == "DSP - Rectifier")
                    datapathMuxSelCombo.SelectedIndex = 2;
                if (graphModeCombo.Text == "DSP - LP Filter")
                    datapathMuxSelCombo.SelectedIndex = 1;

                Tools.timeDelay(10, "MS");
                if (THRCRCERR_Stat_TextBox.Text == "1")
                {
                    if (MessageBox.Show("Thresholds must be written to at least once before running burst and/or listen command to clear the THR CRC Error.\n\nIf THR CRC Error is already cleared, ignore this message. Otherwise, do you want to update thresholds with predefined mid-code threshold values?", "Threshold Status", MessageBoxButtons.YesNo, MessageBoxIcon.Exclamation) != DialogResult.Yes)
                        return;

                    thrUpdateCheck.Checked = true;
                    tabcontrolThr.SelectTab(0);
                    allMidCodeBtn_Click(null, null);
                    Tools.timeDelay(10, "MS");
                    tabcontrolThr.SelectTab(1);
                    allMidCodeBtn_Click(null, null);
                    thrUpdateCheck.Checked = false;
                    Tools.timeDelay(10, "MS");
                    thrUpdatedAtLeastOnce = true;
                }
                if (runBtn.Text == "START")
                {
                    activateProgressBar(true);
                    bool flag = true;
                    long num4 = 1L;
                    byte b4 = 0;
                    int num5;
                    if (p1Radio.Checked)
                    {
                        num5 = (p1RecordCombo.SelectedIndex + 1) * 16 - 1;
                        num4 = (long)((p1RecordCombo.SelectedIndex + 1) * 4000);
                        b4 = (byte)p1RecordCombo.SelectedIndex;
                    }
                    else
                    {
                        num5 = (p2RecordCombo.SelectedIndex + 1) * 16 - 1;
                        num4 = (long)((p2RecordCombo.SelectedIndex + 1) * 4000);
                        b4 = (byte)p2RecordCombo.SelectedIndex;
                    }
                    num5 = (int)((double)num5 * 0.72) + 1;
                    activateProgressBar(true);
                    Array.Clear(mem_return_buf_all, 0, mem_return_buf_all.Length);
                    runBtn.Text = "STOP";
                    Array.Clear(mem_return_buf, 0, mem_return_buf.Length);
                    MChecksumByte = calculate_UART_Checksum(new byte[] { b, Convert.ToByte(numObjToDetCombo.Text) });
                    common.u2a.SendCommand(42, new byte[]
                    {
                        syncByte,
                        b,
                        Convert.ToByte(numObjToDetCombo.Text),
                        MChecksumByte,
                        b4,
                        extTrigByte
                    }, 6);
                    Tools.timeDelay(100, "MS");
                    extTrigTextCheck();
                    byte[] array3 = new byte[32];
                    try
                    {
                        while (flag)
                        {
                            for (byte b5 = 0; b5 < 8; b5 += 1)
                            {
                                common.u2a.SendCommand(43, new byte[] { b5, b2, b3 }, 3);
                                Tools.timeDelay(1, "MS");
                                common.u2a.GetCommandResponse(43, mem_return_buf, 35);
                                Tools.timeDelay(1, "MS");
                                for (int j = 0; j < 32; j++)
                                    array3[j] = mem_return_buf[j + 1];
                                array3.CopyTo(mem_return_buf_all, (int)(32 * b5) + (b2 << 8));
                            }
                            b2 += 1;
                            CurrentLoopLabel.Text = Convert.ToString(b2);
                            if (b2 == num5)
                            {
                                b2 = 0;
                                flag = false;
                            }
                        }
                    }
                    catch
                    {
                        NOP(1L);
                    }
                    if (contClearCheck.Checked)
                        adcChart.Series[0].Points.Clear();
                    if (sampleOut8bitRadio.Checked)
                        adcChart.ChartAreas[0].AxisY.Maximum = 256.0;
                    else
                        adcChart.ChartAreas[0].AxisY.Maximum = 4096.0;

                    lastCorrection = false;
                    if (applyCorrection.Checked)
                    {
                        adcShiftCountBox.SelectedIndex = 7;
                        reverseADCCheck.Checked = false;
                        plotADCBtn_Click(null, null);
                        reverseADCCheck.Checked = true;
                        adcShiftCountBox.SelectedIndex = 0;
                    }
                    lastCorrection = true;
                    plotADCBtn_Click(null, null);
                    adcShiftCountBox.SelectedIndex = 0;
                    adcTrackMin.Value = 0;
                    adcTrackMax.Value = 100;
                    adcChart.ChartAreas[0].AxisX.Maximum = (double)num4;
                    adcChart.ChartAreas[0].AxisX.Minimum = 0.0;
                    if (sampleOut12bitRadio.Checked)
                    {
                        if (graphModeCombo.Text == "DSP - BP Filter")
                        {
                            adcChart.ChartAreas[0].AxisY.Maximum = 2048.0;
                            adcChart.ChartAreas[0].AxisY.Minimum = -2048.0;
                        }
                        else if (graphModeCombo.Text == "ADC")
                        {
                            adcChart.ChartAreas[0].AxisY.Maximum = 4096.0;
                            adcChart.ChartAreas[0].AxisY.Minimum = 0.0;
                        }
                        else
                        {
                            adcChart.ChartAreas[0].AxisY.Maximum = 2048.0;
                            adcChart.ChartAreas[0].AxisY.Minimum = 0.0;
                        }
                    }
                    else if (graphModeCombo.Text == "DSP - BP Filter")
                    {
                        adcChart.ChartAreas[0].AxisY.Maximum = 128.0;
                        adcChart.ChartAreas[0].AxisY.Minimum = -128.0;
                    }
                    else
                    {
                        adcChart.ChartAreas[0].AxisY.Maximum = 255.0;
                        adcChart.ChartAreas[0].AxisY.Minimum = 0.0;
                    }
                    int num6 = mem_return_buf_all.Length / (int)(16 - b4);
                    if (syncLog.Checked)
                    {
                        datalogTextBox.AppendText("\r\n" + graphModeCombo.Text + " Output\r\n");
                        for (int i = 0; i < num6; i++)
                            datalogTextBox.AppendText(mem_return_buf_all[i] + ",");
                    }
                    if (bgExportCheck.Checked && graphModeCombo.Text != "Data Dump")
                    {
                        num6 = (int)((double)adcChart.Series[0].Points.Count<DataPoint>() * 0.72);
                        string[] array4 = new string[num6 + 2];
                        array4[0] = ";" + graphModeCombo.Text;
                        for (int i = 0; i < num6; i++)
                        {
                            array4[i + 1] = string.Concat(
                                i.ToString(),
                                ",",
                                adcChart.Series[0].Points[i].XValue.ToString(),
                                ",",
                                adcChart.Series[0].Points[i].YValues[0].ToString(),
                                ","
                                );
                        }
                        array4[num6] = "";
                        array4[num6 + 1] = "EOF";
                        string text = string.Concat(
                            "PGA460-",
                            graphModeCombo.Text,
                            "-BG ",
                            DateTime.Now.ToString("yyyy-MM-dd-HHmmss"),
                            ".txt"
                        );
                        string path = bgExportBox.Text + text;
                        if (!File.Exists(path))
                            File.WriteAllLines(path, array4, Encoding.UTF8);
                    }
                    runBtn.Text = "START";
                    if (loopBox.Text == "0")
                        runBtn_Click(null, null);

                    activateProgressBar(false);
                    NOP(1L);
                }
                else
                    runBtn.Text = "START";
            }
            if (!PGA46xStat_box.Text.Contains("Ready") || freqCombo.Text == "")
            {
                if (PGA46xStat_box.Text == "Ready (Simulation)")
                    simEchoBtn_Click(null, null);
                else
                    MessageBox.Show("No PGA460-Q1 device connected and/or device settings are blank.");
            }
            else
            {
                if (graphModeCombo.Text == "ADC" || graphModeCombo.Text == "DSP - BP Filter" || graphModeCombo.Text == "DSP - Rectifier" || graphModeCombo.Text == "DSP - LP Filter")
                {
                    datapathMuxSelCombo.SelectedIndex = 0;
                    Tools.timeDelay(10, "MS");
                }
                fault_update();
            }
            uartDiagB = 0;
        }

        protected bool SaveData(string FileName, byte[] Data)
        {
            string path = Environment.GetFolderPath(Environment.SpecialFolder.Personal) + "\\BOOSTXL-PGA460\\pga460-gui-sync_" + DateTime.Now.ToString("yyyy-MM-dd_HHmmss") + ".csv";
            try
            {
                BinaryWriter binaryWriter = new BinaryWriter(File.OpenWrite(path));
                binaryWriter.Write(Data);
                binaryWriter.Flush();
                binaryWriter.Close();
            }
            catch
            {
                return false;
            }
            return true;
        }

        public static void RotateLeft(byte[] bytes)
        {
            bool flag = ShiftLeft(bytes);
            if (flag)
                bytes[bytes.Length - 1] |= 1;
        }

        public static void RotateRight(byte[] bytes)
        {
            bool flag = ShiftRight(bytes);
            if (flag)
                bytes[0] |= 0x80;
        }

        public static bool ShiftLeft(byte[] bytes)
        {
            bool result = false;
            for (int i = 0; i < bytes.Length; i++)
            {
                bool flag = (bytes[i] & 128) > 0;
                if (i > 0)
                {
                    if (flag)
                        bytes[i - 1] |= 1;
                }
                else
                    result = flag;
                bytes[i] <<= 1;
            }
            return result;
        }

        public static bool ShiftRight(byte[] bytes)
        {
            bool result = false;
            int num = bytes.Length - 1;
            for (int i = num; i >= 0; i--)
            {
                bool flag = (bytes[i] & 1) > 0;
                if (i < num)
                {
                    if (flag)
                        bytes[i + 1] |= 0x80;
                }
                else
                    result = flag;

                bytes[i] = (byte)(bytes[i] >> 1);
            }
            return result;
        }

        private void Monitor_run(byte commandBytePassed)
        {
            byte b = 85;
            byte b2 = Convert.ToByte(numObjToDetCombo.Text);
            byte[] array = new byte[130];
            int[] array2 = new int[128];
            double num = 0.0;
            eddDelay = 100.0 - Math.Pow((double)(uartBaudCombo.SelectedIndex + 5), 2.0) + Math.Pow(5 / (uartBaudCombo.SelectedIndex + 1), 3.0);
            int num2 = 0;
            if (p1Radio.Checked)
            {
                num = Convert.ToDouble(p1RecordCombo.Text);
            }
            else
            {
                num = Convert.ToDouble(p2RecordCombo.Text);
            }
            double num3 = 0.0;
            if (p2Radio.Checked)
                num3 = Convert.ToDouble(p2RecordCombo.Text);
            else
                num3 = Convert.ToDouble(p1RecordCombo.Text);

            commandByte = (byte)(((int)Convert.ToByte(uartAddrCombo.Text) << addrShift) + (int)uart_cmd10);
            regAddrByte = 64;
            regDataByte = 128;
            byte b3 = calculate_UART_Checksum(new byte[]
            {
                commandByte,
                regAddrByte,
                regDataByte
            });
            common.u2a.UART_Write(5, new byte[]
            {
                b,
                commandByte,
                regAddrByte,
                regDataByte,
                b3
            });
            Tools.timeDelay(1, "MS");
            Monitor_NumberLoops_done = 0;
            Monitor_NumberLoops_done_Temp = 0;
            thrChart.Series[3].Points.Clear();
            tvgChart.Series[2].Points.Clear();
            updateNLSChart();
            updateDGChart();
            if (commandBytePassed == (byte)(((int)Convert.ToByte(uartAddrCombo.Text) << addrShift) + (int)uart_cmd0) || commandBytePassed == (byte)(((int)Convert.ToByte(uartAddrCombo.Text) << addrShift) + (int)uart_cmd2))
            {
                for (int i = -2; i < 128; i++)
                    dumpChart.Series[6].Points.AddXY(Convert.ToDouble(p1MaxDistBox.Text) / 128.0 * (double)i, 0.0);
            }
            else
            {
                for (int i = -2; i < 128; i++)
                    dumpChart.Series[6].Points.AddXY(Convert.ToDouble(p2MaxDistBox.Text) / 128.0 * (double)i, 0.0);
            }

            if (!Regex.IsMatch(loopBox.Text, "^\\d+$"))
                loopBox.Text = "1";
            dumpChart.ChartAreas[0].AxisX.Maximum = num;

            if (p1Radio.Checked)
                dumpChart.ChartAreas[0].AxisX2.Maximum = Convert.ToDouble(p1MaxDistBox.Text);
            else
                dumpChart.ChartAreas[0].AxisX2.Maximum = Convert.ToDouble(p2MaxDistBox.Text);

            thrChart.Series[4].Points.Clear();
            tvgChart.Series[2].Points.Clear();
            if (CurrentLoopLabel.Text == "('0'=∞)")
                CurrentLoopLabel.Text = "0";

            thrChart.Series[4].ChartType = SeriesChartType.Line;
            tvgChart.Series[2].ChartType = SeriesChartType.Line;
            while (runBtn.Text == "STOP")
            {
                CurrentLoopLabel.Text = Convert.ToString(Monitor_NumberLoops_done);
                if (!fastUpdateChk.Checked)
                {
                    if (loopDelayTextBox.Text == "")
                        loopDelayTextBox.Text = "0";
                    else
                        Tools.timeDelay(Convert.ToDouble(loopDelayTextBox.Text), "MS");
                }
                if (Convert.ToInt16(loopBox.Text) != 0 & Monitor_NumberLoops_done == (int)Convert.ToInt16(loopBox.Text))
                {
                    Monitor_NumberLoops_done = 0;
                    multipleLoopCount = 1;
                    runBtn.Text = "START";
                }
                else
                {
                    Monitor_NumberLoops_done++;
                    if ((Monitor_NumberLoops_done - 1) % 6 == 0 && Monitor_NumberLoops_done > 5)
                    {
                        multipleLoopCount++;
                        Monitor_NumberLoops_done_Temp = 0;
                    }
                    Monitor_NumberLoops_done_Temp++;
                    common.u2a.UART_Read(64, uart_return_data);
                    Array.Clear(uart_return_data, 0, 64);
                    b3 = calculate_UART_Checksum(new byte[]
                    {
                        commandBytePassed,
                        b2
                    });
                    if (extTrigGPIO.Checked || syncPin.Checked)
                    {
                        common.u2a.UART_Write(5, new byte[]
                        {
                            b,
                            commandBytePassed,
                            b2,
                            b3,
                            extTrigByte
                        });
                    }
                    else
                    {
                        common.u2a.UART_Write(4, new byte[]
                        {
                            b,
                            commandBytePassed,
                            b2,
                            b3
                        });
                    }
                    if (drvEnable.Checked)
                    {
                        byte[] array3 = new byte[]
                        {
                            (byte)drvS1.Value,
                            (byte)drvS2.Value,
                            (byte)drvS3.Value,
                            (byte)drvS4.Value,
                            (byte)drvS5.Value,
                            (byte)drvFreq.SelectedIndex
                        };
                        common.u2a.SendCommand(44, array3, (byte)array3.Length);
                    }
                    extTrigTextCheck();
                    Tools.timeDelay(num3 + 0.5, "MS");
                    common.u2a.UART_Read(130, array);
                    Array.Clear(array, 0, 130);
                    if (runBtn.Text == "START")
                    {
                        break;
                    }
                    if (dataDumpCheck.Checked)
                    {
                        commandByte = (byte)(((int)Convert.ToByte(uartAddrCombo.Text) << addrShift) + (int)uart_cmd7);
                        b3 = calculate_UART_Checksum(new byte[] { commandByte });
                        common.u2a.UART_Write(3, new byte[]
                        {
                            b,
                            commandByte,
                            b3
                        });
                        if (fastUpdateChk.Checked)
                            Tools.timeDelay(eddDelay / Convert.ToDouble(fastAcqUpDown.Value), "MS");
                        else
                            Tools.timeDelay(eddDelay, "MS");

                        common.u2a.UART_Read(130, array);
                        uartDiagB = array[0];
                        if (contClearCheck.Checked & runBtn.Text == "STOP")
                        {
                            monitor_clear_plot();
                        }
                        if (!contClearCheck.Checked)
                        {
                            dumpChart.Series[(Monitor_NumberLoops_done_Temp - 1) % 6].Points.AddXY(-1.0, -11.0);
                        }
                        if (fastUpdateChk.Checked)
                        {
                            for (int j = 0; j < 127; j++)
                            {
                                sampleToDistance = num / 128.0 * (double)j;
                                if (contClearCheck.Checked)
                                    dumpChart.Series[0].Points.AddXY(sampleToDistance, (double)array[j]);
                                else
                                    dumpChart.Series[(Monitor_NumberLoops_done - 1) % 6].Points.AddXY(sampleToDistance, (double)array[j]);

                                if (runBtn.Text == "START")
                                    break;
                            }
                        }
                        else
                        {
                            thrChart.Series[4].Points.Clear();
                            tvgChart.Series[2].Points.Clear();
                            for (int j = 0; j < 127; j++)
                            {
                                if (resCombo.Text == "1/2" & j > 0)
                                    j++;
                                else if (resCombo.Text == "1/4" & j > 0)
                                    j += 3;

                                sampleToDistance = num / 128.0 * (double)j;
                                if (contClearCheck.Checked)
                                    dumpChart.Series[0].Points.AddXY(sampleToDistance, (double)array[j]);
                                else
                                    dumpChart.Series[(Monitor_NumberLoops_done - 1) % 6].Points.AddXY(sampleToDistance, (double)array[j]);

                                if (j == 127)
                                {
                                }
                                if (p1Radio.Checked)
                                {
                                    thrChart.Series[4].Points.AddXY(Convert.ToDouble(p1MaxDistBox.Text) / 128.0 * (double)j * 2.0 / 343.0 * 1000.0, (double)array[j]);
                                    tvgChart.Series[2].Points.AddXY(Convert.ToDouble(p1MaxDistBox.Text) / 128.0 * (double)j * 2.0 / 343.0 * 1000.0, (double)array[j]);
                                }
                                else
                                {
                                    thrChart.Series[4].Points.AddXY(Convert.ToDouble(p2MaxDistBox.Text) / 128.0 * (double)j * 2.0 / 343.0 * 1000.0, (double)array[j]);
                                    tvgChart.Series[2].Points.AddXY(Convert.ToDouble(p2MaxDistBox.Text) / 128.0 * (double)j * 2.0 / 343.0 * 1000.0, (double)array[j]);
                                }
                                if ((j % 36 == 0 & j > 0) || j == 127)
                                    dumpChart.Update();
                                if (runBtn.Text == "START")
                                    break;
                            }
                        }
                        if (!contClearCheck.Checked)
                            dumpChart.Series[(Monitor_NumberLoops_done_Temp - 1) % 6].Points.AddXY(num + 1.0, -11.0);

                        if ((exportDataCheck.Checked && CurrentLoopLabel.Text != "('0'=∞)") || (exportDataCheck.Checked && loopBox.Text == "0") || bgExportCheck.Checked)
                        {
                            if (contClearCheck.Checked)
                                num2 = 128;
                            else
                                num2 = 129;

                            string[] array4 = new string[num2 * Monitor_NumberLoops_done];
                            if ((exportTimeStampCheck.Checked && !exportAdvCheck.Checked) || (!exportTimeStampCheck.Checked && exportAdvCheck.Checked))
                                array4 = new string[num2 * Monitor_NumberLoops_done + 1];
                            if (exportTimeStampCheck.Checked && exportAdvCheck.Checked)
                                array4 = new string[num2 * Monitor_NumberLoops_done + 2];

                            if (num2 > 1)
                            {
                                int i = 0;
                                if (contClearCheck.Checked)
                                {
                                    for (i = 0; i < num2 - 1; i++)
                                        valuesToExport.Add(dumpChart.Series[0].Points[i].XValue.ToString() + "," + dumpChart.Series[0].Points[i].YValues[0].ToString());
                                }
                                else
                                {
                                    for (i = 0; i < num2 - 2; i++)
                                        try
                                        {
                                            valuesToExport.Add(dumpChart.Series[0].Points[i].XValue.ToString() + "," + dumpChart.Series[(Monitor_NumberLoops_done_Temp - 1) % 6].Points[i + 1].YValues[0].ToString());
                                        }
                                        catch
                                        {
                                            return;
                                        }
                                }
                                if ((exportTimeStampCheck.Checked && !exportAdvCheck.Checked) || (exportTimeStampCheck.Checked && exportAdvCheck.Checked))
                                {
                                    if ((i == num2 - 1 && contClearCheck.Checked) || (i == num2 - 2 && !contClearCheck.Checked))
                                        valuesToExport.Add(DateTime.Now.ToString("MM\\/dd\\/yyyy h\\:mm\\:ss\\:ff tt"));
                                }
                                if (!exportTimeStampCheck.Checked && exportAdvCheck.Checked)
                                {
                                    if ((i == num2 - 1 && contClearCheck.Checked) || (i == num2 - 2 && !contClearCheck.Checked))
                                    {
                                        tempOnlyBtn_Click(null, null);
                                        valuesToExport.Add("T=" + tempBox.Text);
                                    }
                                }
                                if (exportTimeStampCheck.Checked && exportAdvCheck.Checked)
                                {
                                    tempOnlyBtn_Click(null, null);
                                    valuesToExport.Add("T=" + tempBox.Text);
                                }
                                if ((exportDataCheck.Checked && CurrentLoopLabel.Text != "('0'=∞)") || (exportDataCheck.Checked && loopBox.Text == "0"))
                                {
                                    if (Convert.ToInt32(CurrentLoopLabel.Text) + 1 == Convert.ToInt32(loopBox.Text) || (loopBox.Text == "0" && runBtn.Text == "START"))
                                    {
                                        array4 = valuesToExport.ConvertAll<string>((string x) => x.ToString()).ToArray();
                                        Files files = new Files();
                                        string fileNameDOTextenstion = files.CreateFileName("PGA460-EDD", "", exportSaveAs, true, true, false);
                                        files.WriteArrayToFile("EDD", array4, Environment.GetFolderPath(Environment.SpecialFolder.Personal) + "\\BOOSTXL-PGA460\\", fileNameDOTextenstion);
                                        files.Dispose();
                                        valuesToExport.Clear();
                                    }
                                }
                                if (bgExportCheck.Checked)
                                {
                                    array4 = valuesToExport.ConvertAll<string>((string x) => x.ToString()).ToArray();
                                    Files files = new Files();
                                    string fileNameDOTextenstion;
                                    if (!psStatusBox.Visible)
                                        fileNameDOTextenstion = files.CreateFileName("PGA460-EDD-BG", "", exportSaveAs, true, true, false);
                                    else
                                        fileNameDOTextenstion = files.CreateFileName("PGA460-EDD-BG " + psStatusBox.Text, "", exportSaveAs, true, true, false);

                                    files.WriteArrayToFile("EDDBG", array4, bgExportBox.Text, fileNameDOTextenstion);
                                    files.Dispose();
                                    valuesToExport.Clear();
                                }
                            }
                            else
                            {
                                MessageBox.Show("No Data to be Exported.");
                            }
                        }
                    }
                    if (umrCheck.Checked)
                    {
                        commandByte = (byte)(((int)Convert.ToByte(uartAddrCombo.Text) << addrShift) + (int)uart_cmd10);
                        regAddrByte = 64;
                        regDataByte = 0;
                        b3 = calculate_UART_Checksum(new byte[]
                        {
                            commandByte,
                            regAddrByte,
                            regDataByte
                        });
                        common.u2a.UART_Write(5, new byte[]
                        {
                            b,
                            commandByte,
                            regAddrByte,
                            regDataByte,
                            b3
                        });
                        b3 = calculate_UART_Checksum(new byte[]
                        {
                            commandBytePassed,
                            b2
                        });
                        common.u2a.UART_Write(4, new byte[]
                        {
                            b,
                            commandBytePassed,
                            b2,
                            b3
                        });
                        if (fastUpdateChk.Checked)
                        {
                            Tools.timeDelay(eddDelay / Convert.ToDouble(fastAcqUpDown.Value), "MS");
                        }
                        else
                        {
                            Tools.timeDelay(eddDelay, "MS");
                        }
                        common.u2a.UART_Read(64, uart_return_data);
                        Array.Clear(uart_return_data, 0, 64);
                        commandByte = (byte)(((int)Convert.ToByte(uartAddrCombo.Text) << addrShift) + (int)uart_cmd5);
                        b3 = calculate_UART_Checksum(new byte[] { commandByte });
                        common.u2a.UART_Write(3, new byte[] { b, commandByte, b3 });
                        common.u2a.UART_Read((byte)(b2 * 4 + 2), uart_return_data);
                        uartDiagB = uart_return_data[0];
                        o1Dist.Text = "";
                        o1Wid.Text = "";
                        o1PA.Text = "";
                        o2Dist.Text = "";
                        o2Wid.Text = "";
                        o2PA.Text = "";
                        o3Dist.Text = "";
                        o3Wid.Text = "";
                        o3PA.Text = "";
                        o4Dist.Text = "";
                        o4Wid.Text = "";
                        o4PA.Text = "";
                        o5Dist.Text = "";
                        o5Wid.Text = "";
                        o5PA.Text = "";
                        o6Dist.Text = "";
                        o6Wid.Text = "";
                        o6PA.Text = "";
                        o7Dist.Text = "";
                        o7Wid.Text = "";
                        o7PA.Text = "";
                        o8Dist.Text = "";
                        o8Wid.Text = "";
                        o8PA.Text = "";
                        if (p2Radio.Checked)
                        {
                            double num4 = Convert.ToDouble(p2PulsesCombo.Text);
                        }
                        else
                        {
                            double num4 = Convert.ToDouble(p1PulsesCombo.Text);
                        }
                        double num5 = Convert.ToDouble(tofCalcSound.Text);
                        double num6 = 5E-05 * Convert.ToDouble(tofCalcSound.Text);
                        int numberOfDecimalPlaces = (int)distDecPlace.Value;
                        if (b2 >= 1)
                        {
                            o1Dist.Text = Tools.Double_to_string(num5 * ((double)((((int)uart_return_data[1] << 8) + (int)uart_return_data[2]) / 2) * 1E-06) - num6, numberOfDecimalPlaces);
                            o1Wid.Text = Convert.ToString((int)(uart_return_data[3] * 4));
                            o1PA.Text = Convert.ToString(uart_return_data[4]);
                            if (umrDL.Checked)
                            {
                                datalogTextBox.AppendText("\r\nUltrasonic Measurement Results: (" + DateTime.Now.ToString("yyyy-MM-dd_HHmmss") + ")");
                                datalogTextBox.AppendText("\r\nO1 Dist(m): " + o1Dist.Text);
                                datalogTextBox.AppendText("\r\nO1 TOF: " + Tools.int32_Into_stringBase16(((int)uart_return_data[1] << 8) + (int)uart_return_data[2], 16));
                                datalogTextBox.AppendText("\r\nO1 Width(us): " + o1Wid.Text);
                                datalogTextBox.AppendText("\r\nO1 Amp: " + o1PA.Text);
                            }
                        }
                        if (b2 >= 2)
                        {
                            o2Dist.Text = Tools.Double_to_string(num5 * ((double)((((int)uart_return_data[5] << 8) + (int)uart_return_data[6]) / 2) * 1E-06) - num6, numberOfDecimalPlaces);
                            o2Wid.Text = Convert.ToString((int)(uart_return_data[7] * 4));
                            o2PA.Text = Convert.ToString(uart_return_data[8]);
                            if (umrDL.Checked)
                            {
                                datalogTextBox.AppendText("\r\nO2 Dist(m): " + o2Dist.Text);
                                datalogTextBox.AppendText("\r\nO2 TOF: " + Tools.int32_Into_stringBase16(((int)uart_return_data[5] << 8) + (int)uart_return_data[6], 16));
                                datalogTextBox.AppendText("\r\nO2 Width(us): " + o2Wid.Text);
                                datalogTextBox.AppendText("\r\nO2 Amp: " + o2PA.Text);
                            }
                        }
                        if (b2 >= 3)
                        {
                            o3Dist.Text = Tools.Double_to_string(num5 * ((double)((((int)uart_return_data[9] << 8) + (int)uart_return_data[10]) / 2) * 1E-06) - num6, numberOfDecimalPlaces);
                            o3Wid.Text = Convert.ToString((int)(uart_return_data[11] * 4));
                            o3PA.Text = Convert.ToString(uart_return_data[12]);
                            if (umrDL.Checked)
                            {
                                datalogTextBox.AppendText("\r\nO3 Dist(m): " + o3Dist.Text);
                                datalogTextBox.AppendText("\r\nO3 TOF: " + Tools.int32_Into_stringBase16(((int)uart_return_data[9] << 8) + (int)uart_return_data[10], 16));
                                datalogTextBox.AppendText("\r\nO3 Width(us): " + o3Wid.Text);
                                datalogTextBox.AppendText("\r\nO3 Amp: " + o3PA.Text);
                            }
                        }
                        if (b2 >= 4)
                        {
                            o4Dist.Text = Tools.Double_to_string(num5 * ((double)((((int)uart_return_data[13] << 8) + (int)uart_return_data[14]) / 2) * 1E-06) - num6, numberOfDecimalPlaces);
                            o4Wid.Text = Convert.ToString((int)(uart_return_data[15] * 4));
                            o4PA.Text = Convert.ToString(uart_return_data[16]);
                            if (umrDL.Checked)
                            {
                                datalogTextBox.AppendText("\r\nO4 Dist(m): " + o4Dist.Text);
                                datalogTextBox.AppendText("\r\nO4 TOF: " + Tools.int32_Into_stringBase16(((int)uart_return_data[13] << 8) + (int)uart_return_data[14], 16));
                                datalogTextBox.AppendText("\r\nO4 Width(us): " + o4Wid.Text);
                                datalogTextBox.AppendText("\r\nO4 Amp: " + o4PA.Text);
                            }
                        }
                        if (b2 >= 5)
                        {
                            o5Dist.Text = Tools.Double_to_string(num5 * ((double)((((int)uart_return_data[17] << 8) + (int)uart_return_data[18]) / 2) * 1E-06) - num6, numberOfDecimalPlaces);
                            o5Wid.Text = Convert.ToString((int)(uart_return_data[19] * 4));
                            o5PA.Text = Convert.ToString(uart_return_data[20]);
                            if (umrDL.Checked)
                            {
                                datalogTextBox.AppendText("\r\nO5 Dist(m): " + o5Dist.Text);
                                datalogTextBox.AppendText("\r\nO5 TOF: " + Tools.int32_Into_stringBase16(((int)uart_return_data[17] << 8) + (int)uart_return_data[18], 16));
                                datalogTextBox.AppendText("\r\nO5 Width(us): " + o5Wid.Text);
                                datalogTextBox.AppendText("\r\nO5 Amp: " + o5PA.Text);
                            }
                        }
                        if (b2 >= 6)
                        {
                            o6Dist.Text = Tools.Double_to_string(num5 * ((double)((((int)uart_return_data[21] << 8) + (int)uart_return_data[22]) / 2) * 1E-06) - num6, numberOfDecimalPlaces);
                            o6Wid.Text = Convert.ToString((int)(uart_return_data[23] * 4));
                            o6PA.Text = Convert.ToString(uart_return_data[24]);
                            if (umrDL.Checked)
                            {
                                datalogTextBox.AppendText("\r\nO6 Dist(m): " + o6Dist.Text);
                                datalogTextBox.AppendText("\r\nO6 TOF: " + Tools.int32_Into_stringBase16(((int)uart_return_data[21] << 8) + (int)uart_return_data[22], 16));
                                datalogTextBox.AppendText("\r\nO6 Width(us): " + o6Wid.Text);
                                datalogTextBox.AppendText("\r\nO6 Amp: " + o6PA.Text);
                            }
                        }
                        if (b2 >= 7)
                        {
                            o7Dist.Text = Tools.Double_to_string(num5 * ((double)((((int)uart_return_data[25] << 8) + (int)uart_return_data[26]) / 2) * 1E-06) - num6, numberOfDecimalPlaces);
                            o7Wid.Text = Convert.ToString((int)(uart_return_data[27] * 4));
                            o7PA.Text = Convert.ToString(uart_return_data[28]);
                            if (umrDL.Checked)
                            {
                                datalogTextBox.AppendText("\r\nO7 Dist(m): " + o7Dist.Text);
                                datalogTextBox.AppendText("\r\nO7 TOF: " + Tools.int32_Into_stringBase16(((int)uart_return_data[25] << 8) + (int)uart_return_data[26], 16));
                                datalogTextBox.AppendText("\r\nO7 Width(us): " + o7Wid.Text);
                                datalogTextBox.AppendText("\r\nO7 Amp: " + o7PA.Text);
                            }
                        }
                        if (b2 >= 8)
                        {
                            o8Dist.Text = Tools.Double_to_string(num5 * ((double)((((int)uart_return_data[29] << 8) + (int)uart_return_data[30]) / 2) * 1E-06) - num6, numberOfDecimalPlaces);
                            o8Wid.Text = Convert.ToString((int)(uart_return_data[31] * 4));
                            o8PA.Text = Convert.ToString(uart_return_data[32]);
                            if (umrDL.Checked)
                            {
                                datalogTextBox.AppendText("\r\nO8 Dist(m): " + o8Dist.Text);
                                datalogTextBox.AppendText("\r\nO8 TOF: " + Tools.int32_Into_stringBase16(((int)uart_return_data[29] << 8) + (int)uart_return_data[30], 16));
                                datalogTextBox.AppendText("\r\nO8 Width(us): " + o8Wid.Text);
                                datalogTextBox.AppendText("\r\nO8 Amp: " + o8PA.Text);
                            }
                        }
                        common.u2a.UART_Read(64, uart_return_data);
                        Array.Clear(uart_return_data, 0, 64);
                        if (dataDumpCheck.Checked)
                        {
                            commandByte = (byte)(((int)Convert.ToByte(uartAddrCombo.Text) << addrShift) + (int)uart_cmd10);
                            regAddrByte = 64;
                            regDataByte = 128;
                            b3 = calculate_UART_Checksum(new byte[]
                            {
                                commandByte,
                                regAddrByte,
                                regDataByte
                            });
                            common.u2a.UART_Write(5, new byte[]
                            {
                                b,
                                commandByte,
                                regAddrByte,
                                regDataByte,
                                b3
                            });
                        }
                    }
                }
            }
            commandByte = (byte)(((int)Convert.ToByte(uartAddrCombo.Text) << addrShift) + (int)uart_cmd10);
            regAddrByte = 64;
            regDataByte = 0;
            b3 = calculate_UART_Checksum(new byte[]
            {
                commandByte,
                regAddrByte,
                regDataByte
            });
            common.u2a.UART_Write(5, new byte[]
            {
                b,
                commandByte,
                regAddrByte,
                regDataByte,
                b3
            });
            Monitor_NumberLoops_done = 0;
            Monitor_NumberLoops_done_Temp = 0;
            multipleLoopCount = 0;
        }

        private void monitor_clear_plot()
        {
            for (int i = 0; i < 6; i++)
            {
                dumpChart.Series[i].Points.Clear();
            }
            dumpChart.Series[11].Points.Clear();
        }

        private void updateTVGBtn_Click(object sender, EventArgs e)
        {
            updateTVGChart();
        }

        private void graphModeCombo_SelectedIndexChanged(object sender, EventArgs e)
        {
            string text = graphModeCombo.Text;
            if (text != null)
            {
                if (!(text == "Data Dump"))
                {
                    if (!(text == "Temp. Sensor"))
                    {
                        if (!(text == "ADC"))
                        {
                            if (!(text == "DSP - BP Filter"))
                            {
                                if (!(text == "DSP - Rectifier"))
                                {
                                    if (text == "DSP - LP Filter")
                                    {
                                        graphModeTab.SelectTab("adcGraph");
                                        datapathMuxSelCombo.SelectedIndex = 1;
                                        rawNoiseFilterBtn.Enabled = true;
                                        syncDiff.Enabled = true;
                                        syncLog.Enabled = true;
                                    }
                                }
                                else
                                {
                                    graphModeTab.SelectTab("adcGraph");
                                    datapathMuxSelCombo.SelectedIndex = 2;
                                    rawNoiseFilterBtn.Enabled = true;
                                    syncDiff.Enabled = true;
                                    syncLog.Enabled = true;
                                }
                            }
                            else
                            {
                                graphModeTab.SelectTab("adcGraph");
                                datapathMuxSelCombo.SelectedIndex = 3;
                                rawNoiseFilterBtn.Enabled = true;
                                syncDiff.Enabled = true;
                                syncLog.Enabled = true;
                            }
                        }
                        else
                        {
                            graphModeTab.SelectTab("adcGraph");
                            datapathMuxSelCombo.SelectedIndex = 4;
                            rawNoiseFilterBtn.Enabled = true;
                            syncDiff.Enabled = true;
                            syncLog.Enabled = true;
                        }
                    }
                    else
                    {
                        graphModeTab.SelectTab("tempGraph");
                        rawNoiseFilterBtn.Enabled = true;
                        syncDiff.Enabled = true;
                        syncLog.Enabled = true;
                    }
                }
                else
                {
                    graphModeTab.SelectTab("datadumpGraph");
                    datapathMuxSelCombo.SelectedIndex = 0;
                    rawNoiseFilterBtn.Enabled = false;
                    syncDiff.Enabled = false;
                    syncLog.Enabled = false;
                }
            }
        }

        private void tciCommandCombo_SelectedIndexChanged(object sender, EventArgs e)
        {
            byte[] array = new byte[1];
            byte[] array2 = array;
            byte[] array3 = new byte[]
            {
                1
            };
            byte[] array4 = new byte[]
            {
                2
            };
            byte[] array5 = new byte[]
            {
                3
            };
            array = new byte[]
            {
                4
            };
            byte[] array6 = new byte[]
            {
                5
            };
            byte[] array7 = new byte[]
            {
                6
            };
            switch (tciCommandCombo.SelectedIndex)
            {
                case 0:
                    tciIndexTab.SelectTab(0);
                    run_buffer = array2;
                    commandPeriodTextBox.Text = "328|400|472";
                    TCIpulsesBox.Text = ind3P1PR.SelectedText;
                    TCIIlimitBox.Text = ind10P1ILimR.SelectedText;
                    TCIrecBox.Text = ind4P1RR.SelectedText;
                    if (ind10P1NLSER.Text == "En")
                    {
                        TCInlsBox.Text = "Enabled";
                    }
                    else
                    {
                        TCInlsBox.Text = "Disabled";
                    }
                    disableIndexControls();
                    break;
                case 1:
                    tciIndexTab.SelectTab(0);
                    run_buffer = array3;
                    commandPeriodTextBox.Text = "920|1010|1100";
                    TCIpulsesBox.Text = ind3P2PR.SelectedText;
                    TCIIlimitBox.Text = ind10P2ILimR.SelectedText;
                    TCIrecBox.Text = ind4P2RR.SelectedText;
                    if (ind10P2NLSER.Text == "En")
                    {
                        TCInlsBox.Text = "Enabled";
                    }
                    else
                    {
                        TCInlsBox.Text = "Disabled";
                    }
                    disableIndexControls();
                    break;
                case 2:
                    tciIndexTab.SelectTab(0);
                    run_buffer = array4;
                    commandPeriodTextBox.Text = "697|780|863";
                    TCIpulsesBox.Text = ind3P1PR.SelectedText;
                    TCIIlimitBox.Text = ind10P1ILimR.SelectedText;
                    TCIrecBox.Text = ind4P1RR.SelectedText;
                    if (ind10P1NLSER.Text == "En")
                    {
                        TCInlsBox.Text = "Enabled";
                    }
                    else
                    {
                        TCInlsBox.Text = "Disabled";
                    }
                    disableIndexControls();
                    break;
                case 3:
                    tciIndexTab.SelectTab(0);
                    run_buffer = array5;
                    commandPeriodTextBox.Text = "503|580|657";
                    TCIpulsesBox.Text = ind3P2PR.SelectedText;
                    TCIIlimitBox.Text = ind10P2ILimR.SelectedText;
                    TCIrecBox.Text = ind4P2RR.SelectedText;
                    if (ind10P2NLSER.Text == "En")
                    {
                        TCInlsBox.Text = "Enabled";
                    }
                    else
                    {
                        TCInlsBox.Text = "Disabled";
                    }
                    disableIndexControls();
                    break;
                case 4:
                    tciIndexTab.SelectTab(1);
                    commandPeriodTextBox.Text = "1170|1270|1370";
                    enableIndexControls();
                    TCIpulsesBox.Text = "";
                    TCIIlimitBox.Text = "";
                    TCIrecBox.Text = "";
                    break;
                case 5:
                    tciIndexTab.SelectTab("tempNoiseTab");
                    run_buffer = array6;
                    commandPeriodTextBox.Text = "1440|1550|1660";
                    disableIndexControls();
                    TCIpulsesBox.Text = "";
                    TCIIlimitBox.Text = "";
                    TCIrecBox.Text = "";
                    break;
                case 6:
                    tciIndexTab.SelectTab("tempNoiseTab");
                    run_buffer = array7;
                    commandPeriodTextBox.Text = "2070|2200|2340";
                    disableIndexControls();
                    TCIpulsesBox.Text = "0";
                    TCIIlimitBox.Text = "N/A";
                    TCIrecBox.Text = "8.192";
                    break;
            }
        }

        private void disableIndexControls()
        {
            readIndexBtn.Enabled = false;
            writeIndexBtn.Enabled = false;
            tciIndexCombo.Enabled = false;
            tciLoopCountInd.Enabled = true;
            tciLoopCountBox.Enabled = true;
            runTCIBtn.Enabled = true;
            tciLoopInfCheck.Enabled = true;
        }

        private void enableIndexControls()
        {
            readIndexBtn.Enabled = true;
            writeIndexBtn.Enabled = true;
            tciIndexCombo.Enabled = true;
            tciIndexCombo_SelectedIndexChanged(null, null);
            tciLoopCountInd.Enabled = false;
            tciLoopCountBox.Enabled = false;
            runTCIBtn.Enabled = false;
            tciLoopInfCheck.Enabled = false;
        }

        private void uartCmdSingleCombo_SelectedIndexChanged(object sender, EventArgs e)
        {
            loadMSRich();
        }

        private void loadMSRich()
        {
            MSRichTextBox.Text = "55 00 11 22";
        }

        private void runUartBtn_Click(object sender, EventArgs e)
        {
        }

        private void freqCombo_SelectedIndexChanged(object sender, EventArgs e)
        {
            if (!firstTime)
            {
                RegDefs regDefs = new RegDefs("1C (FREQUENCY)");
                regDefs.FREQUENCY.ReadFromUART();
                switch (freqCombo.SelectedIndex)
                {
                    case 0:
                        regDefs.FREQ.value = "00000000";
                        break;
                    case 1:
                        regDefs.FREQ.value = "00000001";
                        break;
                    case 2:
                        regDefs.FREQ.value = "00000010";
                        break;
                    case 3:
                        regDefs.FREQ.value = "00000011";
                        break;
                    case 4:
                        regDefs.FREQ.value = "00000100";
                        break;
                    case 5:
                        regDefs.FREQ.value = "00000101";
                        break;
                    case 6:
                        regDefs.FREQ.value = "00000110";
                        break;
                    case 7:
                        regDefs.FREQ.value = "00000111";
                        break;
                    case 8:
                        regDefs.FREQ.value = "00001000";
                        break;
                    case 9:
                        regDefs.FREQ.value = "00001001";
                        break;
                    case 10:
                        regDefs.FREQ.value = "00001010";
                        break;
                    case 11:
                        regDefs.FREQ.value = "00001011";
                        break;
                    case 12:
                        regDefs.FREQ.value = "00001100";
                        break;
                    case 13:
                        regDefs.FREQ.value = "00001101";
                        break;
                    case 14:
                        regDefs.FREQ.value = "00001110";
                        break;
                    case 15:
                        regDefs.FREQ.value = "00001111";
                        break;
                    case 16:
                        regDefs.FREQ.value = "00010000";
                        break;
                    case 17:
                        regDefs.FREQ.value = "00010001";
                        break;
                    case 18:
                        regDefs.FREQ.value = "00010010";
                        break;
                    case 19:
                        regDefs.FREQ.value = "00010011";
                        break;
                    case 20:
                        regDefs.FREQ.value = "00010100";
                        break;
                    case 21:
                        regDefs.FREQ.value = "00010101";
                        break;
                    case 22:
                        regDefs.FREQ.value = "00010110";
                        break;
                    case 23:
                        regDefs.FREQ.value = "00010111";
                        break;
                    case 24:
                        regDefs.FREQ.value = "00011000";
                        break;
                    case 25:
                        regDefs.FREQ.value = "00011001";
                        break;
                    case 26:
                        regDefs.FREQ.value = "00011010";
                        break;
                    case 27:
                        regDefs.FREQ.value = "00011011";
                        break;
                    case 28:
                        regDefs.FREQ.value = "00011100";
                        break;
                    case 29:
                        regDefs.FREQ.value = "00011101";
                        break;
                    case 30:
                        regDefs.FREQ.value = "00011110";
                        break;
                    case 31:
                        regDefs.FREQ.value = "00011111";
                        break;
                    case 32:
                        regDefs.FREQ.value = "00100000";
                        break;
                    case 33:
                        regDefs.FREQ.value = "00100001";
                        break;
                    case 34:
                        regDefs.FREQ.value = "00100010";
                        break;
                    case 35:
                        regDefs.FREQ.value = "00100011";
                        break;
                    case 36:
                        regDefs.FREQ.value = "00100100";
                        break;
                    case 37:
                        regDefs.FREQ.value = "00100101";
                        break;
                    case 38:
                        regDefs.FREQ.value = "00100110";
                        break;
                    case 39:
                        regDefs.FREQ.value = "00100111";
                        break;
                    case 40:
                        regDefs.FREQ.value = "00101000";
                        break;
                    case 41:
                        regDefs.FREQ.value = "00101001";
                        break;
                    case 42:
                        regDefs.FREQ.value = "00101010";
                        break;
                    case 43:
                        regDefs.FREQ.value = "00101011";
                        break;
                    case 44:
                        regDefs.FREQ.value = "00101100";
                        break;
                    case 45:
                        regDefs.FREQ.value = "00101101";
                        break;
                    case 46:
                        regDefs.FREQ.value = "00101110";
                        break;
                    case 47:
                        regDefs.FREQ.value = "00101111";
                        break;
                    case 48:
                        regDefs.FREQ.value = "00110000";
                        break;
                    case 49:
                        regDefs.FREQ.value = "00110001";
                        break;
                    case 50:
                        regDefs.FREQ.value = "00110010";
                        break;
                    case 51:
                        regDefs.FREQ.value = "00110011";
                        break;
                    case 52:
                        regDefs.FREQ.value = "00110100";
                        break;
                    case 53:
                        regDefs.FREQ.value = "00110101";
                        break;
                    case 54:
                        regDefs.FREQ.value = "00110110";
                        break;
                    case 55:
                        regDefs.FREQ.value = "00110111";
                        break;
                    case 56:
                        regDefs.FREQ.value = "00111000";
                        break;
                    case 57:
                        regDefs.FREQ.value = "00111001";
                        break;
                    case 58:
                        regDefs.FREQ.value = "00111010";
                        break;
                    case 59:
                        regDefs.FREQ.value = "00111011";
                        break;
                    case 60:
                        regDefs.FREQ.value = "00111100";
                        break;
                    case 61:
                        regDefs.FREQ.value = "00111101";
                        break;
                    case 62:
                        regDefs.FREQ.value = "00111110";
                        break;
                    case 63:
                        regDefs.FREQ.value = "00111111";
                        break;
                    case 64:
                        regDefs.FREQ.value = "01000000";
                        break;
                    case 65:
                        regDefs.FREQ.value = "01000001";
                        break;
                    case 66:
                        regDefs.FREQ.value = "01000010";
                        break;
                    case 67:
                        regDefs.FREQ.value = "01000011";
                        break;
                    case 68:
                        regDefs.FREQ.value = "01000100";
                        break;
                    case 69:
                        regDefs.FREQ.value = "01000101";
                        break;
                    case 70:
                        regDefs.FREQ.value = "01000110";
                        break;
                    case 71:
                        regDefs.FREQ.value = "01000111";
                        break;
                    case 72:
                        regDefs.FREQ.value = "01001000";
                        break;
                    case 73:
                        regDefs.FREQ.value = "01001001";
                        break;
                    case 74:
                        regDefs.FREQ.value = "01001010";
                        break;
                    case 75:
                        regDefs.FREQ.value = "01001011";
                        break;
                    case 76:
                        regDefs.FREQ.value = "01001100";
                        break;
                    case 77:
                        regDefs.FREQ.value = "01001101";
                        break;
                    case 78:
                        regDefs.FREQ.value = "01001110";
                        break;
                    case 79:
                        regDefs.FREQ.value = "01001111";
                        break;
                    case 80:
                        regDefs.FREQ.value = "01010000";
                        break;
                    case 81:
                        regDefs.FREQ.value = "01010001";
                        break;
                    case 82:
                        regDefs.FREQ.value = "01010010";
                        break;
                    case 83:
                        regDefs.FREQ.value = "01010011";
                        break;
                    case 84:
                        regDefs.FREQ.value = "01010100";
                        break;
                    case 85:
                        regDefs.FREQ.value = "01010101";
                        break;
                    case 86:
                        regDefs.FREQ.value = "01010110";
                        break;
                    case 87:
                        regDefs.FREQ.value = "01010111";
                        break;
                    case 88:
                        regDefs.FREQ.value = "01011000";
                        break;
                    case 89:
                        regDefs.FREQ.value = "01011001";
                        break;
                    case 90:
                        regDefs.FREQ.value = "01011010";
                        break;
                    case 91:
                        regDefs.FREQ.value = "01011011";
                        break;
                    case 92:
                        regDefs.FREQ.value = "01011100";
                        break;
                    case 93:
                        regDefs.FREQ.value = "01011101";
                        break;
                    case 94:
                        regDefs.FREQ.value = "01011110";
                        break;
                    case 95:
                        regDefs.FREQ.value = "01011111";
                        break;
                    case 96:
                        regDefs.FREQ.value = "01100000";
                        break;
                    case 97:
                        regDefs.FREQ.value = "01100001";
                        break;
                    case 98:
                        regDefs.FREQ.value = "01100010";
                        break;
                    case 99:
                        regDefs.FREQ.value = "01100011";
                        break;
                    case 100:
                        regDefs.FREQ.value = "01100100";
                        break;
                    case 101:
                        regDefs.FREQ.value = "01100101";
                        break;
                    case 102:
                        regDefs.FREQ.value = "01100110";
                        break;
                    case 103:
                        regDefs.FREQ.value = "01100111";
                        break;
                    case 104:
                        regDefs.FREQ.value = "01101000";
                        break;
                    case 105:
                        regDefs.FREQ.value = "01101001";
                        break;
                    case 106:
                        regDefs.FREQ.value = "01101010";
                        break;
                    case 107:
                        regDefs.FREQ.value = "01101011";
                        break;
                    case 108:
                        regDefs.FREQ.value = "01101100";
                        break;
                    case 109:
                        regDefs.FREQ.value = "01101101";
                        break;
                    case 110:
                        regDefs.FREQ.value = "01101110";
                        break;
                    case 111:
                        regDefs.FREQ.value = "01101111";
                        break;
                    case 112:
                        regDefs.FREQ.value = "01110000";
                        break;
                    case 113:
                        regDefs.FREQ.value = "01110001";
                        break;
                    case 114:
                        regDefs.FREQ.value = "01110010";
                        break;
                    case 115:
                        regDefs.FREQ.value = "01110011";
                        break;
                    case 116:
                        regDefs.FREQ.value = "01110100";
                        break;
                    case 117:
                        regDefs.FREQ.value = "01110101";
                        break;
                    case 118:
                        regDefs.FREQ.value = "01110110";
                        break;
                    case 119:
                        regDefs.FREQ.value = "01110111";
                        break;
                    case 120:
                        regDefs.FREQ.value = "01111000";
                        break;
                    case 121:
                        regDefs.FREQ.value = "01111001";
                        break;
                    case 122:
                        regDefs.FREQ.value = "01111010";
                        break;
                    case 123:
                        regDefs.FREQ.value = "01111011";
                        break;
                    case 124:
                        regDefs.FREQ.value = "01111100";
                        break;
                    case 125:
                        regDefs.FREQ.value = "01111101";
                        break;
                    case 126:
                        regDefs.FREQ.value = "01111110";
                        break;
                    case 127:
                        regDefs.FREQ.value = "01111111";
                        break;
                    case 128:
                        regDefs.FREQ.value = "10000000";
                        break;
                    case 129:
                        regDefs.FREQ.value = "10000001";
                        break;
                    case 130:
                        regDefs.FREQ.value = "10000010";
                        break;
                    case 131:
                        regDefs.FREQ.value = "10000011";
                        break;
                    case 132:
                        regDefs.FREQ.value = "10000100";
                        break;
                    case 133:
                        regDefs.FREQ.value = "10000101";
                        break;
                    case 134:
                        regDefs.FREQ.value = "10000110";
                        break;
                    case 135:
                        regDefs.FREQ.value = "10000111";
                        break;
                    case 136:
                        regDefs.FREQ.value = "10001000";
                        break;
                    case 137:
                        regDefs.FREQ.value = "10001001";
                        break;
                    case 138:
                        regDefs.FREQ.value = "10001010";
                        break;
                    case 139:
                        regDefs.FREQ.value = "10001011";
                        break;
                    case 140:
                        regDefs.FREQ.value = "10001100";
                        break;
                    case 141:
                        regDefs.FREQ.value = "10001101";
                        break;
                    case 142:
                        regDefs.FREQ.value = "10001110";
                        break;
                    case 143:
                        regDefs.FREQ.value = "10001111";
                        break;
                    case 144:
                        regDefs.FREQ.value = "10010000";
                        break;
                    case 145:
                        regDefs.FREQ.value = "10010001";
                        break;
                    case 146:
                        regDefs.FREQ.value = "10010010";
                        break;
                    case 147:
                        regDefs.FREQ.value = "10010011";
                        break;
                    case 148:
                        regDefs.FREQ.value = "10010100";
                        break;
                    case 149:
                        regDefs.FREQ.value = "10010101";
                        break;
                    case 150:
                        regDefs.FREQ.value = "10010110";
                        break;
                    case 151:
                        regDefs.FREQ.value = "10010111";
                        break;
                    case 152:
                        regDefs.FREQ.value = "10011000";
                        break;
                    case 153:
                        regDefs.FREQ.value = "10011001";
                        break;
                    case 154:
                        regDefs.FREQ.value = "10011010";
                        break;
                    case 155:
                        regDefs.FREQ.value = "10011011";
                        break;
                    case 156:
                        regDefs.FREQ.value = "10011100";
                        break;
                    case 157:
                        regDefs.FREQ.value = "10011101";
                        break;
                    case 158:
                        regDefs.FREQ.value = "10011110";
                        break;
                    case 159:
                        regDefs.FREQ.value = "10011111";
                        break;
                    case 160:
                        regDefs.FREQ.value = "10100000";
                        break;
                    case 161:
                        regDefs.FREQ.value = "10100001";
                        break;
                    case 162:
                        regDefs.FREQ.value = "10100010";
                        break;
                    case 163:
                        regDefs.FREQ.value = "10100011";
                        break;
                    case 164:
                        regDefs.FREQ.value = "10100100";
                        break;
                    case 165:
                        regDefs.FREQ.value = "10100101";
                        break;
                    case 166:
                        regDefs.FREQ.value = "10100110";
                        break;
                    case 167:
                        regDefs.FREQ.value = "10100111";
                        break;
                    case 168:
                        regDefs.FREQ.value = "10101000";
                        break;
                    case 169:
                        regDefs.FREQ.value = "10101001";
                        break;
                    case 170:
                        regDefs.FREQ.value = "10101010";
                        break;
                    case 171:
                        regDefs.FREQ.value = "10101011";
                        break;
                    case 172:
                        regDefs.FREQ.value = "10101100";
                        break;
                    case 173:
                        regDefs.FREQ.value = "10101101";
                        break;
                    case 174:
                        regDefs.FREQ.value = "10101110";
                        break;
                    case 175:
                        regDefs.FREQ.value = "10101111";
                        break;
                    case 176:
                        regDefs.FREQ.value = "10110000";
                        break;
                    case 177:
                        regDefs.FREQ.value = "10110001";
                        break;
                    case 178:
                        regDefs.FREQ.value = "10110010";
                        break;
                    case 179:
                        regDefs.FREQ.value = "10110011";
                        break;
                    case 180:
                        regDefs.FREQ.value = "10110100";
                        break;
                    case 181:
                        regDefs.FREQ.value = "10110101";
                        break;
                    case 182:
                        regDefs.FREQ.value = "10110110";
                        break;
                    case 183:
                        regDefs.FREQ.value = "10110111";
                        break;
                    case 184:
                        regDefs.FREQ.value = "10111000";
                        break;
                    case 185:
                        regDefs.FREQ.value = "10111001";
                        break;
                    case 186:
                        regDefs.FREQ.value = "10111010";
                        break;
                    case 187:
                        regDefs.FREQ.value = "10111011";
                        break;
                    case 188:
                        regDefs.FREQ.value = "10111100";
                        break;
                    case 189:
                        regDefs.FREQ.value = "10111101";
                        break;
                    case 190:
                        regDefs.FREQ.value = "10111110";
                        break;
                    case 191:
                        regDefs.FREQ.value = "10111111";
                        break;
                    case 192:
                        regDefs.FREQ.value = "11000000";
                        break;
                    case 193:
                        regDefs.FREQ.value = "11000001";
                        break;
                    case 194:
                        regDefs.FREQ.value = "11000010";
                        break;
                    case 195:
                        regDefs.FREQ.value = "11000011";
                        break;
                    case 196:
                        regDefs.FREQ.value = "11000100";
                        break;
                    case 197:
                        regDefs.FREQ.value = "11000101";
                        break;
                    case 198:
                        regDefs.FREQ.value = "11000110";
                        break;
                    case 199:
                        regDefs.FREQ.value = "11000111";
                        break;
                    case 200:
                        regDefs.FREQ.value = "11001000";
                        break;
                    case 201:
                        regDefs.FREQ.value = "11001001";
                        break;
                    case 202:
                        regDefs.FREQ.value = "11001010";
                        break;
                    case 203:
                        regDefs.FREQ.value = "11001011";
                        break;
                    case 204:
                        regDefs.FREQ.value = "11001100";
                        break;
                    case 205:
                        regDefs.FREQ.value = "11001101";
                        break;
                    case 206:
                        regDefs.FREQ.value = "11001110";
                        break;
                    case 207:
                        regDefs.FREQ.value = "11001111";
                        break;
                    case 208:
                        regDefs.FREQ.value = "11010000";
                        break;
                    case 209:
                        regDefs.FREQ.value = "11010001";
                        break;
                    case 210:
                        regDefs.FREQ.value = "11010010";
                        break;
                    case 211:
                        regDefs.FREQ.value = "11010011";
                        break;
                    case 212:
                        regDefs.FREQ.value = "11010100";
                        break;
                    case 213:
                        regDefs.FREQ.value = "11010101";
                        break;
                    case 214:
                        regDefs.FREQ.value = "11010110";
                        break;
                    case 215:
                        regDefs.FREQ.value = "11010111";
                        break;
                    case 216:
                        regDefs.FREQ.value = "11011000";
                        break;
                    case 217:
                        regDefs.FREQ.value = "11011001";
                        break;
                    case 218:
                        regDefs.FREQ.value = "11011010";
                        break;
                    case 219:
                        regDefs.FREQ.value = "11011011";
                        break;
                    case 220:
                        regDefs.FREQ.value = "11011100";
                        break;
                    case 221:
                        regDefs.FREQ.value = "11011101";
                        break;
                    case 222:
                        regDefs.FREQ.value = "11011110";
                        break;
                    case 223:
                        regDefs.FREQ.value = "11011111";
                        break;
                    case 224:
                        regDefs.FREQ.value = "11100000";
                        break;
                    case 225:
                        regDefs.FREQ.value = "11100001";
                        break;
                    case 226:
                        regDefs.FREQ.value = "11100010";
                        break;
                    case 227:
                        regDefs.FREQ.value = "11100011";
                        break;
                    case 228:
                        regDefs.FREQ.value = "11100100";
                        break;
                    case 229:
                        regDefs.FREQ.value = "11100101";
                        break;
                    case 230:
                        regDefs.FREQ.value = "11100110";
                        break;
                    case 231:
                        regDefs.FREQ.value = "11100111";
                        break;
                    case 232:
                        regDefs.FREQ.value = "11101000";
                        break;
                    case 233:
                        regDefs.FREQ.value = "11101001";
                        break;
                    case 234:
                        regDefs.FREQ.value = "11101010";
                        break;
                    case 235:
                        regDefs.FREQ.value = "11101011";
                        break;
                    case 236:
                        regDefs.FREQ.value = "11101100";
                        break;
                    case 237:
                        regDefs.FREQ.value = "11101101";
                        break;
                    case 238:
                        regDefs.FREQ.value = "11101110";
                        break;
                    case 239:
                        regDefs.FREQ.value = "11101111";
                        break;
                    case 240:
                        regDefs.FREQ.value = "11110000";
                        break;
                    case 241:
                        regDefs.FREQ.value = "11110001";
                        break;
                    case 242:
                        regDefs.FREQ.value = "11110010";
                        break;
                    case 243:
                        regDefs.FREQ.value = "11110011";
                        break;
                    case 244:
                        regDefs.FREQ.value = "11110100";
                        break;
                    case 245:
                        regDefs.FREQ.value = "11110101";
                        break;
                    case 246:
                        regDefs.FREQ.value = "11110110";
                        break;
                    case 247:
                        regDefs.FREQ.value = "11110111";
                        break;
                    case 248:
                        regDefs.FREQ.value = "11111000";
                        break;
                    case 249:
                        regDefs.FREQ.value = "11111001";
                        break;
                    case 250:
                        regDefs.FREQ.value = "11111010";
                        break;
                }
                regDefs.FREQUENCY.WriteToUART();
                if (primaryTab.SelectedTab.Name != "MemMap" && !firstTime)
                {
                    string[,] array = new string[1, 5];
                    array[0, 0] = regDefs.FREQUENCY.location.ToString();
                    array[0, 1] = "1C (FREQUENCY)";
                    array[0, 4] = Tools.int32_Into_stringBase16(calRegValfromFields(regDefs.FREQUENCY.bit_fields));
                    updateMemoryMap(array, "GRID_USER_MEMSPACE");
                }
            }
            bd_freqCombo.SelectedIndex = freqCombo.SelectedIndex;
            if (freqshiftCheck.Checked)
            {
                coefCalcShift.Checked = true;
                coefCalcFreq.SelectedIndex = freqCombo.SelectedIndex;
                writeCoef_Click(null, null);
            }
            centerFreqText.Text = freqCombo.Text;
            freqErrTimeFreqEquivCalc();
        }

        private void deadCombo_SelectedIndexChanged(object sender, EventArgs e)
        {
            if (!firstTime)
            {
                RegDefs regDefs = new RegDefs("1D (DEADTIME)");
                regDefs.DEADTIME.ReadFromUART();
                switch (deadCombo.SelectedIndex)
                {
                    case 0:
                        regDefs.PULSE_DT.value = "0000";
                        break;
                    case 1:
                        regDefs.PULSE_DT.value = "0001";
                        break;
                    case 2:
                        regDefs.PULSE_DT.value = "0010";
                        break;
                    case 3:
                        regDefs.PULSE_DT.value = "0011";
                        break;
                    case 4:
                        regDefs.PULSE_DT.value = "0100";
                        break;
                    case 5:
                        regDefs.PULSE_DT.value = "0101";
                        break;
                    case 6:
                        regDefs.PULSE_DT.value = "0110";
                        break;
                    case 7:
                        regDefs.PULSE_DT.value = "0111";
                        break;
                    case 8:
                        regDefs.PULSE_DT.value = "1000";
                        break;
                    case 9:
                        regDefs.PULSE_DT.value = "1001";
                        break;
                    case 10:
                        regDefs.PULSE_DT.value = "1010";
                        break;
                    case 11:
                        regDefs.PULSE_DT.value = "1011";
                        break;
                    case 12:
                        regDefs.PULSE_DT.value = "1100";
                        break;
                    case 13:
                        regDefs.PULSE_DT.value = "1101";
                        break;
                    case 14:
                        regDefs.PULSE_DT.value = "1110";
                        break;
                    case 15:
                        regDefs.PULSE_DT.value = "1111";
                        break;
                }
                regDefs.DEADTIME.WriteToUART();
                if (primaryTab.SelectedTab.Name != "MemMap" && !firstTime)
                {
                    string[,] array = new string[1, 5];
                    array[0, 0] = regDefs.DEADTIME.location.ToString();
                    array[0, 1] = "1D (DEADTIME)";
                    array[0, 4] = Tools.int32_Into_stringBase16(calRegValfromFields(regDefs.DEADTIME.bit_fields));
                    updateMemoryMap(array, "GRID_USER_MEMSPACE");
                }
            }
        }

        private void thrCmpDeglitchCombo_SelectedIndexChanged(object sender, EventArgs e)
        {
            if (!firstTime)
            {
                RegDefs regDefs = new RegDefs("1D (DEADTIME)");
                regDefs.DEADTIME.ReadFromUART();
                switch (thrCmpDeglitchCombo.SelectedIndex)
                {
                    case 0:
                        regDefs.THR_CMP_DEGLTCH.value = "0000";
                        break;
                    case 1:
                        regDefs.THR_CMP_DEGLTCH.value = "0001";
                        break;
                    case 2:
                        regDefs.THR_CMP_DEGLTCH.value = "0010";
                        break;
                    case 3:
                        regDefs.THR_CMP_DEGLTCH.value = "0011";
                        break;
                    case 4:
                        regDefs.THR_CMP_DEGLTCH.value = "0100";
                        break;
                    case 5:
                        regDefs.THR_CMP_DEGLTCH.value = "0101";
                        break;
                    case 6:
                        regDefs.THR_CMP_DEGLTCH.value = "0110";
                        break;
                    case 7:
                        regDefs.THR_CMP_DEGLTCH.value = "0111";
                        break;
                    case 8:
                        regDefs.THR_CMP_DEGLTCH.value = "1000";
                        break;
                    case 9:
                        regDefs.THR_CMP_DEGLTCH.value = "1001";
                        break;
                    case 10:
                        regDefs.THR_CMP_DEGLTCH.value = "1010";
                        break;
                    case 11:
                        regDefs.THR_CMP_DEGLTCH.value = "1011";
                        break;
                    case 12:
                        regDefs.THR_CMP_DEGLTCH.value = "1100";
                        break;
                    case 13:
                        regDefs.THR_CMP_DEGLTCH.value = "1101";
                        break;
                    case 14:
                        regDefs.THR_CMP_DEGLTCH.value = "1110";
                        break;
                    case 15:
                        regDefs.THR_CMP_DEGLTCH.value = "1111";
                        break;
                }
                regDefs.DEADTIME.WriteToUART();
                if (primaryTab.SelectedTab.Name != "MemMap" && !firstTime)
                {
                    string[,] array = new string[1, 5];
                    array[0, 0] = regDefs.DEADTIME.location.ToString();
                    array[0, 1] = "1D (DEADTIME)";
                    array[0, 4] = Tools.int32_Into_stringBase16(calRegValfromFields(regDefs.DEADTIME.bit_fields));
                    updateMemoryMap(array, "GRID_USER_MEMSPACE");
                }
            }
        }

        private void gainCombo_SelectedIndexChanged(object sender, EventArgs e)
        {
            if (!firstTime)
            {
                RegDefs regDefs = new RegDefs("1B (INIT_GAIN)");
                regDefs.INIT_GAIN.ReadFromUART();
                switch (gainCombo.SelectedIndex)
                {
                    case 0:
                        regDefs.GAIN_INIT.value = "000000";
                        break;
                    case 1:
                        regDefs.GAIN_INIT.value = "000001";
                        break;
                    case 2:
                        regDefs.GAIN_INIT.value = "000010";
                        break;
                    case 3:
                        regDefs.GAIN_INIT.value = "000011";
                        break;
                    case 4:
                        regDefs.GAIN_INIT.value = "000100";
                        break;
                    case 5:
                        regDefs.GAIN_INIT.value = "000101";
                        break;
                    case 6:
                        regDefs.GAIN_INIT.value = "000110";
                        break;
                    case 7:
                        regDefs.GAIN_INIT.value = "000111";
                        break;
                    case 8:
                        regDefs.GAIN_INIT.value = "001000";
                        break;
                    case 9:
                        regDefs.GAIN_INIT.value = "001001";
                        break;
                    case 10:
                        regDefs.GAIN_INIT.value = "001010";
                        break;
                    case 11:
                        regDefs.GAIN_INIT.value = "001011";
                        break;
                    case 12:
                        regDefs.GAIN_INIT.value = "001100";
                        break;
                    case 13:
                        regDefs.GAIN_INIT.value = "001101";
                        break;
                    case 14:
                        regDefs.GAIN_INIT.value = "001110";
                        break;
                    case 15:
                        regDefs.GAIN_INIT.value = "001111";
                        break;
                    case 16:
                        regDefs.GAIN_INIT.value = "010000";
                        break;
                    case 17:
                        regDefs.GAIN_INIT.value = "010001";
                        break;
                    case 18:
                        regDefs.GAIN_INIT.value = "010010";
                        break;
                    case 19:
                        regDefs.GAIN_INIT.value = "010011";
                        break;
                    case 20:
                        regDefs.GAIN_INIT.value = "010100";
                        break;
                    case 21:
                        regDefs.GAIN_INIT.value = "010101";
                        break;
                    case 22:
                        regDefs.GAIN_INIT.value = "010110";
                        break;
                    case 23:
                        regDefs.GAIN_INIT.value = "010111";
                        break;
                    case 24:
                        regDefs.GAIN_INIT.value = "011000";
                        break;
                    case 25:
                        regDefs.GAIN_INIT.value = "011001";
                        break;
                    case 26:
                        regDefs.GAIN_INIT.value = "011010";
                        break;
                    case 27:
                        regDefs.GAIN_INIT.value = "011011";
                        break;
                    case 28:
                        regDefs.GAIN_INIT.value = "011100";
                        break;
                    case 29:
                        regDefs.GAIN_INIT.value = "011101";
                        break;
                    case 30:
                        regDefs.GAIN_INIT.value = "011110";
                        break;
                    case 31:
                        regDefs.GAIN_INIT.value = "011111";
                        break;
                    case 32:
                        regDefs.GAIN_INIT.value = "100000";
                        break;
                    case 33:
                        regDefs.GAIN_INIT.value = "100001";
                        break;
                    case 34:
                        regDefs.GAIN_INIT.value = "100010";
                        break;
                    case 35:
                        regDefs.GAIN_INIT.value = "100011";
                        break;
                    case 36:
                        regDefs.GAIN_INIT.value = "100100";
                        break;
                    case 37:
                        regDefs.GAIN_INIT.value = "100101";
                        break;
                    case 38:
                        regDefs.GAIN_INIT.value = "100110";
                        break;
                    case 39:
                        regDefs.GAIN_INIT.value = "100111";
                        break;
                    case 40:
                        regDefs.GAIN_INIT.value = "101000";
                        break;
                    case 41:
                        regDefs.GAIN_INIT.value = "101001";
                        break;
                    case 42:
                        regDefs.GAIN_INIT.value = "101010";
                        break;
                    case 43:
                        regDefs.GAIN_INIT.value = "101011";
                        break;
                    case 44:
                        regDefs.GAIN_INIT.value = "101100";
                        break;
                    case 45:
                        regDefs.GAIN_INIT.value = "101101";
                        break;
                    case 46:
                        regDefs.GAIN_INIT.value = "101110";
                        break;
                    case 47:
                        regDefs.GAIN_INIT.value = "101111";
                        break;
                    case 48:
                        regDefs.GAIN_INIT.value = "110000";
                        break;
                    case 49:
                        regDefs.GAIN_INIT.value = "110001";
                        break;
                    case 50:
                        regDefs.GAIN_INIT.value = "110010";
                        break;
                    case 51:
                        regDefs.GAIN_INIT.value = "110011";
                        break;
                    case 52:
                        regDefs.GAIN_INIT.value = "110100";
                        break;
                    case 53:
                        regDefs.GAIN_INIT.value = "110101";
                        break;
                    case 54:
                        regDefs.GAIN_INIT.value = "110110";
                        break;
                    case 55:
                        regDefs.GAIN_INIT.value = "110111";
                        break;
                    case 56:
                        regDefs.GAIN_INIT.value = "111000";
                        break;
                    case 57:
                        regDefs.GAIN_INIT.value = "111001";
                        break;
                    case 58:
                        regDefs.GAIN_INIT.value = "111010";
                        break;
                    case 59:
                        regDefs.GAIN_INIT.value = "111011";
                        break;
                    case 60:
                        regDefs.GAIN_INIT.value = "111100";
                        break;
                    case 61:
                        regDefs.GAIN_INIT.value = "111101";
                        break;
                    case 62:
                        regDefs.GAIN_INIT.value = "111110";
                        break;
                    case 63:
                        regDefs.GAIN_INIT.value = "111111";
                        break;
                }
                regDefs.INIT_GAIN.WriteToUART();
                if (primaryTab.SelectedTab.Name != "MemMap" && !firstTime)
                {
                    string[,] array = new string[1, 5];
                    array[0, 0] = regDefs.INIT_GAIN.location.ToString();
                    array[0, 1] = "1B (INIT_GAIN)";
                    array[0, 4] = Tools.int32_Into_stringBase16(calRegValfromFields(regDefs.INIT_GAIN.bit_fields));
                    updateMemoryMap(array, "GRID_USER_MEMSPACE");
                }
            }
            tvgg0.SelectedIndex = gainCombo.SelectedIndex;
            gainVVBoxUpdate();
            bd_afeGainCombo.SelectedIndex = gainCombo.SelectedIndex;
            if (fixedGain.Checked)
            {
                tvgInstantUpdateCheck.Checked = true;
                allInitGainBtn_Click(null, null);
                tvgInstantUpdateCheck.Checked = false;
            }
        }

        private void bpbwCombo_SelectedIndexChanged(object sender, EventArgs e)
        {
            if (!firstTime)
            {
                RegDefs regDefs = new RegDefs("1B (INIT_GAIN)");
                regDefs.INIT_GAIN.ReadFromUART();
                switch (bpbwCombo.SelectedIndex)
                {
                    case 0:
                        regDefs.BPF_BW.value = "00";
                        break;
                    case 1:
                        regDefs.BPF_BW.value = "01";
                        break;
                    case 2:
                        regDefs.BPF_BW.value = "10";
                        break;
                    case 3:
                        regDefs.BPF_BW.value = "11";
                        break;
                }
                regDefs.INIT_GAIN.WriteToUART();
                if (primaryTab.SelectedTab.Name != "MemMap" && !firstTime)
                {
                    string[,] array = new string[1, 5];
                    array[0, 0] = regDefs.INIT_GAIN.location.ToString();
                    array[0, 1] = "1B (INIT_GAIN)";
                    array[0, 4] = Tools.int32_Into_stringBase16(calRegValfromFields(regDefs.INIT_GAIN.bit_fields));
                    updateMemoryMap(array, "GRID_USER_MEMSPACE");
                }
            }
            bd_bpfbwCombo.SelectedIndex = bpbwCombo.SelectedIndex;
        }

        private void cutoffCombo_SelectedIndexChanged(object sender, EventArgs e)
        {
            if (!firstTime)
            {
                RegDefs regDefs = new RegDefs("21 (CURR_LIM_P2)");
                regDefs.CURR_LIM_P2.ReadFromUART();
                switch (cutoffCombo.SelectedIndex)
                {
                    case 0:
                        regDefs.LPF_CO.value = "00";
                        break;
                    case 1:
                        regDefs.LPF_CO.value = "01";
                        break;
                    case 2:
                        regDefs.LPF_CO.value = "10";
                        break;
                    case 3:
                        regDefs.LPF_CO.value = "11";
                        break;
                }
                regDefs.CURR_LIM_P2.WriteToUART();
                if (primaryTab.SelectedTab.Name != "MemMap" && !firstTime)
                {
                    string[,] array = new string[1, 5];
                    array[0, 0] = regDefs.CURR_LIM_P2.location.ToString();
                    array[0, 1] = "21 (CURR_LIM_P2)";
                    array[0, 4] = Tools.int32_Into_stringBase16(calRegValfromFields(regDefs.CURR_LIM_P2.bit_fields));
                    updateMemoryMap(array, "GRID_USER_MEMSPACE");
                }
            }
            bd_lpfCoCombo.SelectedIndex = cutoffCombo.SelectedIndex;
        }

        private void decoupletimeBox_SelectedIndexChanged(object sender, EventArgs e)
        {
            if (!firstTime)
            {
                RegDefs regDefs = new RegDefs("26 (DECPL_TEMP)");
                regDefs.DECPL_TEMP.ReadFromUART();
                switch (decoupletimeBox.SelectedIndex)
                {
                    case 0:
                        regDefs.DECPL_T.value = "0000";
                        break;
                    case 1:
                        regDefs.DECPL_T.value = "0001";
                        break;
                    case 2:
                        regDefs.DECPL_T.value = "0010";
                        break;
                    case 3:
                        regDefs.DECPL_T.value = "0011";
                        break;
                    case 4:
                        regDefs.DECPL_T.value = "0100";
                        break;
                    case 5:
                        regDefs.DECPL_T.value = "0101";
                        break;
                    case 6:
                        regDefs.DECPL_T.value = "0110";
                        break;
                    case 7:
                        regDefs.DECPL_T.value = "0111";
                        break;
                    case 8:
                        regDefs.DECPL_T.value = "1000";
                        break;
                    case 9:
                        regDefs.DECPL_T.value = "1001";
                        break;
                    case 10:
                        regDefs.DECPL_T.value = "1010";
                        break;
                    case 11:
                        regDefs.DECPL_T.value = "1011";
                        break;
                    case 12:
                        regDefs.DECPL_T.value = "1100";
                        break;
                    case 13:
                        regDefs.DECPL_T.value = "1101";
                        break;
                    case 14:
                        regDefs.DECPL_T.value = "1110";
                        break;
                    case 15:
                        regDefs.DECPL_T.value = "1111";
                        break;
                }
                regDefs.DECPL_TEMP.WriteToUART();
                if (primaryTab.SelectedTab.Name != "MemMap" && !firstTime)
                {
                    string[,] array = new string[1, 5];
                    array[0, 0] = regDefs.DECPL_TEMP.location.ToString();
                    array[0, 1] = "26 (DECPL_TEMP)";
                    array[0, 4] = Tools.int32_Into_stringBase16(calRegValfromFields(regDefs.DECPL_TEMP.bit_fields));
                    updateMemoryMap(array, "GRID_USER_MEMSPACE");
                }
            }
            decoupletempBox.SelectedIndex = decoupletimeBox.SelectedIndex;
        }

        private void decoupletempBox_SelectedIndexChanged(object sender, EventArgs e)
        {
            if (!firstTime)
            {
                RegDefs regDefs = new RegDefs("26 (DECPL_TEMP)");
                regDefs.DECPL_TEMP.ReadFromUART();
                switch (decoupletimeBox.SelectedIndex)
                {
                    case 0:
                        regDefs.DECPL_T.value = "0000";
                        break;
                    case 1:
                        regDefs.DECPL_T.value = "0001";
                        break;
                    case 2:
                        regDefs.DECPL_T.value = "0010";
                        break;
                    case 3:
                        regDefs.DECPL_T.value = "0011";
                        break;
                    case 4:
                        regDefs.DECPL_T.value = "0100";
                        break;
                    case 5:
                        regDefs.DECPL_T.value = "0101";
                        break;
                    case 6:
                        regDefs.DECPL_T.value = "0110";
                        break;
                    case 7:
                        regDefs.DECPL_T.value = "0111";
                        break;
                    case 8:
                        regDefs.DECPL_T.value = "1000";
                        break;
                    case 9:
                        regDefs.DECPL_T.value = "1001";
                        break;
                    case 10:
                        regDefs.DECPL_T.value = "1010";
                        break;
                    case 11:
                        regDefs.DECPL_T.value = "1011";
                        break;
                    case 12:
                        regDefs.DECPL_T.value = "1100";
                        break;
                    case 13:
                        regDefs.DECPL_T.value = "1101";
                        break;
                    case 14:
                        regDefs.DECPL_T.value = "1110";
                        break;
                    case 15:
                        regDefs.DECPL_T.value = "1111";
                        break;
                }
                regDefs.DECPL_TEMP.WriteToUART();
                if (primaryTab.SelectedTab.Name != "MemMap" && !firstTime)
                {
                    string[,] array = new string[1, 5];
                    array[0, 0] = regDefs.DECPL_TEMP.location.ToString();
                    array[0, 1] = "26 (DECPL_TEMP)";
                    array[0, 4] = Tools.int32_Into_stringBase16(calRegValfromFields(regDefs.DECPL_TEMP.bit_fields));
                    updateMemoryMap(array, "GRID_USER_MEMSPACE");
                }
            }
            decoupletimeBox.SelectedIndex = decoupletempBox.SelectedIndex;
        }

        private void tempgainCombo_SelectedIndexChanged(object sender, EventArgs e)
        {
            if (!firstTime)
            {
                RegDefs regDefs = new RegDefs("28 (TEMP_TRIM)");
                regDefs.TEMP_TRIM.ReadFromUART();
                switch (tempgainCombo.SelectedIndex)
                {
                    case 0:
                        regDefs.TEMP_GAIN.value = "0000";
                        break;
                    case 1:
                        regDefs.TEMP_GAIN.value = "0001";
                        break;
                    case 2:
                        regDefs.TEMP_GAIN.value = "0010";
                        break;
                    case 3:
                        regDefs.TEMP_GAIN.value = "0011";
                        break;
                    case 4:
                        regDefs.TEMP_GAIN.value = "0100";
                        break;
                    case 5:
                        regDefs.TEMP_GAIN.value = "0101";
                        break;
                    case 6:
                        regDefs.TEMP_GAIN.value = "0110";
                        break;
                    case 7:
                        regDefs.TEMP_GAIN.value = "0111";
                        break;
                    case 8:
                        regDefs.TEMP_GAIN.value = "1000";
                        break;
                    case 9:
                        regDefs.TEMP_GAIN.value = "1001";
                        break;
                    case 10:
                        regDefs.TEMP_GAIN.value = "1010";
                        break;
                    case 11:
                        regDefs.TEMP_GAIN.value = "1011";
                        break;
                    case 12:
                        regDefs.TEMP_GAIN.value = "1100";
                        break;
                    case 13:
                        regDefs.TEMP_GAIN.value = "1101";
                        break;
                    case 14:
                        regDefs.TEMP_GAIN.value = "1110";
                        break;
                    case 15:
                        regDefs.TEMP_GAIN.value = "1111";
                        break;
                }
                regDefs.TEMP_TRIM.WriteToUART();
                if (primaryTab.SelectedTab.Name != "MemMap" && !firstTime)
                {
                    string[,] array = new string[1, 5];
                    array[0, 0] = regDefs.TEMP_TRIM.location.ToString();
                    array[0, 1] = "28 (TEMP_TRIM)";
                    array[0, 4] = Tools.int32_Into_stringBase16(calRegValfromFields(regDefs.TEMP_TRIM.bit_fields));
                    updateMemoryMap(array, "GRID_USER_MEMSPACE");
                }
            }
        }

        private void tempoffsetCombo_SelectedIndexChanged(object sender, EventArgs e)
        {
            if (!firstTime)
            {
                RegDefs regDefs = new RegDefs("28 (TEMP_TRIM)");
                regDefs.TEMP_TRIM.ReadFromUART();
                switch (tempoffsetCombo.SelectedIndex)
                {
                    case 0:
                        regDefs.TEMP_OFF.value = "0000";
                        break;
                    case 1:
                        regDefs.TEMP_OFF.value = "0001";
                        break;
                    case 2:
                        regDefs.TEMP_OFF.value = "0010";
                        break;
                    case 3:
                        regDefs.TEMP_OFF.value = "0011";
                        break;
                    case 4:
                        regDefs.TEMP_OFF.value = "0100";
                        break;
                    case 5:
                        regDefs.TEMP_OFF.value = "0101";
                        break;
                    case 6:
                        regDefs.TEMP_OFF.value = "0110";
                        break;
                    case 7:
                        regDefs.TEMP_OFF.value = "0111";
                        break;
                    case 8:
                        regDefs.TEMP_OFF.value = "1000";
                        break;
                    case 9:
                        regDefs.TEMP_OFF.value = "1001";
                        break;
                    case 10:
                        regDefs.TEMP_OFF.value = "1010";
                        break;
                    case 11:
                        regDefs.TEMP_OFF.value = "1011";
                        break;
                    case 12:
                        regDefs.TEMP_OFF.value = "1100";
                        break;
                    case 13:
                        regDefs.TEMP_OFF.value = "1101";
                        break;
                    case 14:
                        regDefs.TEMP_OFF.value = "1110";
                        break;
                    case 15:
                        regDefs.TEMP_OFF.value = "1111";
                        break;
                }
                regDefs.TEMP_TRIM.WriteToUART();
                if (primaryTab.SelectedTab.Name != "MemMap" && !firstTime)
                {
                    string[,] array = new string[1, 5];
                    array[0, 0] = regDefs.TEMP_TRIM.location.ToString();
                    array[0, 1] = "28 (TEMP_TRIM)";
                    array[0, 4] = Tools.int32_Into_stringBase16(calRegValfromFields(regDefs.TEMP_TRIM.bit_fields));
                    updateMemoryMap(array, "GRID_USER_MEMSPACE");
                }
            }
        }

        private void ovthrCombo_SelectedIndexChanged(object sender, EventArgs e)
        {
            if (!firstTime)
            {
                RegDefs regDefs = new RegDefs("25 (FVOLT_DEC)");
                regDefs.FVOLT_DEC.ReadFromUART();
                switch (ovthrCombo.SelectedIndex)
                {
                    case 0:
                        regDefs.VPWR_OV_TH.value = "00";
                        break;
                    case 1:
                        regDefs.VPWR_OV_TH.value = "01";
                        break;
                    case 2:
                        regDefs.VPWR_OV_TH.value = "10";
                        break;
                    case 3:
                        regDefs.VPWR_OV_TH.value = "11";
                        break;
                }
                regDefs.FVOLT_DEC.WriteToUART();
                if (primaryTab.SelectedTab.Name != "MemMap" && !firstTime)
                {
                    string[,] array = new string[1, 5];
                    array[0, 0] = regDefs.FVOLT_DEC.location.ToString();
                    array[0, 1] = "25 (FVOLT_DEC)";
                    array[0, 4] = Tools.int32_Into_stringBase16(calRegValfromFields(regDefs.FVOLT_DEC.bit_fields));
                    updateMemoryMap(array, "GRID_USER_MEMSPACE");
                }
            }
        }

        private void p1PulsesCombo_SelectedIndexChanged(object sender, EventArgs e)
        {
            if (!firstTime)
            {
                RegDefs regDefs = new RegDefs("1E (PULSE_P1)");
                regDefs.PULSE_P1.ReadFromUART();
                switch (p1PulsesCombo.SelectedIndex)
                {
                    case 0:
                        regDefs.P1_PULSE.value = "00000";
                        break;
                    case 1:
                        regDefs.P1_PULSE.value = "00001";
                        break;
                    case 2:
                        regDefs.P1_PULSE.value = "00010";
                        break;
                    case 3:
                        regDefs.P1_PULSE.value = "00011";
                        break;
                    case 4:
                        regDefs.P1_PULSE.value = "00100";
                        break;
                    case 5:
                        regDefs.P1_PULSE.value = "00101";
                        break;
                    case 6:
                        regDefs.P1_PULSE.value = "00110";
                        break;
                    case 7:
                        regDefs.P1_PULSE.value = "00111";
                        break;
                    case 8:
                        regDefs.P1_PULSE.value = "01000";
                        break;
                    case 9:
                        regDefs.P1_PULSE.value = "01001";
                        break;
                    case 10:
                        regDefs.P1_PULSE.value = "01010";
                        break;
                    case 11:
                        regDefs.P1_PULSE.value = "01011";
                        break;
                    case 12:
                        regDefs.P1_PULSE.value = "01100";
                        break;
                    case 13:
                        regDefs.P1_PULSE.value = "01101";
                        break;
                    case 14:
                        regDefs.P1_PULSE.value = "01110";
                        break;
                    case 15:
                        regDefs.P1_PULSE.value = "01111";
                        break;
                    case 16:
                        regDefs.P1_PULSE.value = "10000";
                        break;
                    case 17:
                        regDefs.P1_PULSE.value = "10001";
                        break;
                    case 18:
                        regDefs.P1_PULSE.value = "10010";
                        break;
                    case 19:
                        regDefs.P1_PULSE.value = "10011";
                        break;
                    case 20:
                        regDefs.P1_PULSE.value = "10100";
                        break;
                    case 21:
                        regDefs.P1_PULSE.value = "10101";
                        break;
                    case 22:
                        regDefs.P1_PULSE.value = "10110";
                        break;
                    case 23:
                        regDefs.P1_PULSE.value = "10111";
                        break;
                    case 24:
                        regDefs.P1_PULSE.value = "11000";
                        break;
                    case 25:
                        regDefs.P1_PULSE.value = "11001";
                        break;
                    case 26:
                        regDefs.P1_PULSE.value = "11010";
                        break;
                    case 27:
                        regDefs.P1_PULSE.value = "11011";
                        break;
                    case 28:
                        regDefs.P1_PULSE.value = "11100";
                        break;
                    case 29:
                        regDefs.P1_PULSE.value = "11101";
                        break;
                    case 30:
                        regDefs.P1_PULSE.value = "11110";
                        break;
                    case 31:
                        regDefs.P1_PULSE.value = "11111";
                        break;
                }
                regDefs.PULSE_P1.WriteToUART();
                if (primaryTab.SelectedTab.Name != "MemMap" && !firstTime)
                {
                    string[,] array = new string[1, 5];
                    array[0, 0] = regDefs.PULSE_P1.location.ToString();
                    array[0, 1] = "1E (PULSE_P1)";
                    array[0, 4] = Tools.int32_Into_stringBase16(calRegValfromFields(regDefs.PULSE_P1.bit_fields));
                    updateMemoryMap(array, "GRID_USER_MEMSPACE");
                }
            }
            bd_pulseCombo.SelectedIndex = p1PulsesCombo.SelectedIndex;
        }

        private void p1RecordCombo_SelectedIndexChanged(object sender, EventArgs e)
        {
            if (!firstTime)
            {
                RegDefs regDefs = new RegDefs("22 (REC_LENGTH)");
                regDefs.REC_LENGTH.ReadFromUART();
                switch (p1RecordCombo.SelectedIndex)
                {
                    case 0:
                        regDefs.P1_REC.value = "0000";
                        break;
                    case 1:
                        regDefs.P1_REC.value = "0001";
                        break;
                    case 2:
                        regDefs.P1_REC.value = "0010";
                        break;
                    case 3:
                        regDefs.P1_REC.value = "0011";
                        break;
                    case 4:
                        regDefs.P1_REC.value = "0100";
                        break;
                    case 5:
                        regDefs.P1_REC.value = "0101";
                        break;
                    case 6:
                        regDefs.P1_REC.value = "0110";
                        break;
                    case 7:
                        regDefs.P1_REC.value = "0111";
                        break;
                    case 8:
                        regDefs.P1_REC.value = "1000";
                        break;
                    case 9:
                        regDefs.P1_REC.value = "1001";
                        break;
                    case 10:
                        regDefs.P1_REC.value = "1010";
                        break;
                    case 11:
                        regDefs.P1_REC.value = "1011";
                        break;
                    case 12:
                        regDefs.P1_REC.value = "1100";
                        break;
                    case 13:
                        regDefs.P1_REC.value = "1101";
                        break;
                    case 14:
                        regDefs.P1_REC.value = "1110";
                        break;
                    case 15:
                        regDefs.P1_REC.value = "1111";
                        break;
                }
                regDefs.REC_LENGTH.WriteToUART();
                if (primaryTab.SelectedTab.Name != "MemMap" && !firstTime)
                {
                    string[,] array = new string[1, 5];
                    array[0, 0] = regDefs.REC_LENGTH.location.ToString();
                    array[0, 1] = "22 (REC_LENGTH)";
                    array[0, 4] = Tools.int32_Into_stringBase16(calRegValfromFields(regDefs.REC_LENGTH.bit_fields));
                    updateMemoryMap(array, "GRID_USER_MEMSPACE");
                }
                p1MaxDistBox.Text = Convert.ToString(Math.Truncate(Convert.ToDouble(p1RecordCombo.Text) / 1000.0 * 343.0 / 2.0 * 1000.0) / 1000.0);
            }
            bd_recCombo.SelectedIndex = p1RecordCombo.SelectedIndex;
        }

        private void p2PulsesCombo_SelectedIndexChanged(object sender, EventArgs e)
        {
            if (!firstTime)
            {
                RegDefs regDefs = new RegDefs("1F (PULSE_P2)");
                regDefs.PULSE_P2.ReadFromUART();
                switch (p2PulsesCombo.SelectedIndex)
                {
                    case 0:
                        regDefs.P2_PULSE.value = "00000";
                        break;
                    case 1:
                        regDefs.P2_PULSE.value = "00001";
                        break;
                    case 2:
                        regDefs.P2_PULSE.value = "00010";
                        break;
                    case 3:
                        regDefs.P2_PULSE.value = "00011";
                        break;
                    case 4:
                        regDefs.P2_PULSE.value = "00100";
                        break;
                    case 5:
                        regDefs.P2_PULSE.value = "00101";
                        break;
                    case 6:
                        regDefs.P2_PULSE.value = "00110";
                        break;
                    case 7:
                        regDefs.P2_PULSE.value = "00111";
                        break;
                    case 8:
                        regDefs.P2_PULSE.value = "01000";
                        break;
                    case 9:
                        regDefs.P2_PULSE.value = "01001";
                        break;
                    case 10:
                        regDefs.P2_PULSE.value = "01010";
                        break;
                    case 11:
                        regDefs.P2_PULSE.value = "01011";
                        break;
                    case 12:
                        regDefs.P2_PULSE.value = "01100";
                        break;
                    case 13:
                        regDefs.P2_PULSE.value = "01101";
                        break;
                    case 14:
                        regDefs.P2_PULSE.value = "01110";
                        break;
                    case 15:
                        regDefs.P2_PULSE.value = "01111";
                        break;
                    case 16:
                        regDefs.P2_PULSE.value = "10000";
                        break;
                    case 17:
                        regDefs.P2_PULSE.value = "10001";
                        break;
                    case 18:
                        regDefs.P2_PULSE.value = "10010";
                        break;
                    case 19:
                        regDefs.P2_PULSE.value = "10011";
                        break;
                    case 20:
                        regDefs.P2_PULSE.value = "10100";
                        break;
                    case 21:
                        regDefs.P2_PULSE.value = "10101";
                        break;
                    case 22:
                        regDefs.P2_PULSE.value = "10110";
                        break;
                    case 23:
                        regDefs.P2_PULSE.value = "10111";
                        break;
                    case 24:
                        regDefs.P2_PULSE.value = "11000";
                        break;
                    case 25:
                        regDefs.P2_PULSE.value = "11001";
                        break;
                    case 26:
                        regDefs.P2_PULSE.value = "11010";
                        break;
                    case 27:
                        regDefs.P2_PULSE.value = "11011";
                        break;
                    case 28:
                        regDefs.P2_PULSE.value = "11100";
                        break;
                    case 29:
                        regDefs.P2_PULSE.value = "11101";
                        break;
                    case 30:
                        regDefs.P2_PULSE.value = "11110";
                        break;
                    case 31:
                        regDefs.P2_PULSE.value = "11111";
                        break;
                }
                regDefs.PULSE_P2.WriteToUART();
                if (primaryTab.SelectedTab.Name != "MemMap" && !firstTime)
                {
                    string[,] array = new string[1, 5];
                    array[0, 0] = regDefs.PULSE_P2.location.ToString();
                    array[0, 1] = "1F (PULSE_P2)";
                    array[0, 4] = Tools.int32_Into_stringBase16(calRegValfromFields(regDefs.PULSE_P2.bit_fields));
                    updateMemoryMap(array, "GRID_USER_MEMSPACE");
                }
            }
        }

        private void p2DriveCombo_SelectedIndexChanged(object sender, EventArgs e)
        {
            if (!firstTime)
            {
                RegDefs regDefs = new RegDefs("21 (CURR_LIM_P2)");
                regDefs.CURR_LIM_P2.ReadFromUART();
                switch (p2DriveCombo.SelectedIndex)
                {
                    case 0:
                        regDefs.CURR_LIM2.value = "000000";
                        break;
                    case 1:
                        regDefs.CURR_LIM2.value = "000001";
                        break;
                    case 2:
                        regDefs.CURR_LIM2.value = "000010";
                        break;
                    case 3:
                        regDefs.CURR_LIM2.value = "000011";
                        break;
                    case 4:
                        regDefs.CURR_LIM2.value = "000100";
                        break;
                    case 5:
                        regDefs.CURR_LIM2.value = "000101";
                        break;
                    case 6:
                        regDefs.CURR_LIM2.value = "000110";
                        break;
                    case 7:
                        regDefs.CURR_LIM2.value = "000111";
                        break;
                    case 8:
                        regDefs.CURR_LIM2.value = "001000";
                        break;
                    case 9:
                        regDefs.CURR_LIM2.value = "001001";
                        break;
                    case 10:
                        regDefs.CURR_LIM2.value = "001010";
                        break;
                    case 11:
                        regDefs.CURR_LIM2.value = "001011";
                        break;
                    case 12:
                        regDefs.CURR_LIM2.value = "001100";
                        break;
                    case 13:
                        regDefs.CURR_LIM2.value = "001101";
                        break;
                    case 14:
                        regDefs.CURR_LIM2.value = "001110";
                        break;
                    case 15:
                        regDefs.CURR_LIM2.value = "001111";
                        break;
                    case 16:
                        regDefs.CURR_LIM2.value = "010000";
                        break;
                    case 17:
                        regDefs.CURR_LIM2.value = "010001";
                        break;
                    case 18:
                        regDefs.CURR_LIM2.value = "010010";
                        break;
                    case 19:
                        regDefs.CURR_LIM2.value = "010011";
                        break;
                    case 20:
                        regDefs.CURR_LIM2.value = "010100";
                        break;
                    case 21:
                        regDefs.CURR_LIM2.value = "010101";
                        break;
                    case 22:
                        regDefs.CURR_LIM2.value = "010110";
                        break;
                    case 23:
                        regDefs.CURR_LIM2.value = "010111";
                        break;
                    case 24:
                        regDefs.CURR_LIM2.value = "011000";
                        break;
                    case 25:
                        regDefs.CURR_LIM2.value = "011001";
                        break;
                    case 26:
                        regDefs.CURR_LIM2.value = "011010";
                        break;
                    case 27:
                        regDefs.CURR_LIM2.value = "011011";
                        break;
                    case 28:
                        regDefs.CURR_LIM2.value = "011100";
                        break;
                    case 29:
                        regDefs.CURR_LIM2.value = "011101";
                        break;
                    case 30:
                        regDefs.CURR_LIM2.value = "011110";
                        break;
                    case 31:
                        regDefs.CURR_LIM2.value = "011111";
                        break;
                    case 32:
                        regDefs.CURR_LIM2.value = "100000";
                        break;
                    case 33:
                        regDefs.CURR_LIM2.value = "100001";
                        break;
                    case 34:
                        regDefs.CURR_LIM2.value = "100010";
                        break;
                    case 35:
                        regDefs.CURR_LIM2.value = "100011";
                        break;
                    case 36:
                        regDefs.CURR_LIM2.value = "100100";
                        break;
                    case 37:
                        regDefs.CURR_LIM2.value = "100101";
                        break;
                    case 38:
                        regDefs.CURR_LIM2.value = "100110";
                        break;
                    case 39:
                        regDefs.CURR_LIM2.value = "100111";
                        break;
                    case 40:
                        regDefs.CURR_LIM2.value = "101000";
                        break;
                    case 41:
                        regDefs.CURR_LIM2.value = "101001";
                        break;
                    case 42:
                        regDefs.CURR_LIM2.value = "101010";
                        break;
                    case 43:
                        regDefs.CURR_LIM2.value = "101011";
                        break;
                    case 44:
                        regDefs.CURR_LIM2.value = "101100";
                        break;
                    case 45:
                        regDefs.CURR_LIM2.value = "101101";
                        break;
                    case 46:
                        regDefs.CURR_LIM2.value = "101110";
                        break;
                    case 47:
                        regDefs.CURR_LIM2.value = "101111";
                        break;
                    case 48:
                        regDefs.CURR_LIM2.value = "110000";
                        break;
                    case 49:
                        regDefs.CURR_LIM2.value = "110001";
                        break;
                    case 50:
                        regDefs.CURR_LIM2.value = "110010";
                        break;
                    case 51:
                        regDefs.CURR_LIM2.value = "110011";
                        break;
                    case 52:
                        regDefs.CURR_LIM2.value = "110100";
                        break;
                    case 53:
                        regDefs.CURR_LIM2.value = "110101";
                        break;
                    case 54:
                        regDefs.CURR_LIM2.value = "110110";
                        break;
                    case 55:
                        regDefs.CURR_LIM2.value = "110111";
                        break;
                    case 56:
                        regDefs.CURR_LIM2.value = "111000";
                        break;
                    case 57:
                        regDefs.CURR_LIM2.value = "111001";
                        break;
                    case 58:
                        regDefs.CURR_LIM2.value = "111010";
                        break;
                    case 59:
                        regDefs.CURR_LIM2.value = "111011";
                        break;
                    case 60:
                        regDefs.CURR_LIM2.value = "111100";
                        break;
                    case 61:
                        regDefs.CURR_LIM2.value = "111101";
                        break;
                    case 62:
                        regDefs.CURR_LIM2.value = "111110";
                        break;
                    case 63:
                        regDefs.CURR_LIM2.value = "111111";
                        break;
                }
                regDefs.CURR_LIM_P2.WriteToUART();
                if (primaryTab.SelectedTab.Name != "MemMap" && !firstTime)
                {
                    string[,] array = new string[1, 5];
                    array[0, 0] = regDefs.CURR_LIM_P2.location.ToString();
                    array[0, 1] = "21 (CURR_LIM_P2)";
                    array[0, 4] = Tools.int32_Into_stringBase16(calRegValfromFields(regDefs.CURR_LIM_P2.bit_fields));
                    updateMemoryMap(array, "GRID_USER_MEMSPACE");
                }
            }
        }

        private void p2RecordCombo_SelectedIndexChanged(object sender, EventArgs e)
        {
            if (!firstTime)
            {
                RegDefs regDefs = new RegDefs("22 (REC_LENGTH)");
                regDefs.REC_LENGTH.ReadFromUART();
                switch (p2RecordCombo.SelectedIndex)
                {
                    case 0:
                        regDefs.P2_REC.value = "0000";
                        break;
                    case 1:
                        regDefs.P2_REC.value = "0001";
                        break;
                    case 2:
                        regDefs.P2_REC.value = "0010";
                        break;
                    case 3:
                        regDefs.P2_REC.value = "0011";
                        break;
                    case 4:
                        regDefs.P2_REC.value = "0100";
                        break;
                    case 5:
                        regDefs.P2_REC.value = "0101";
                        break;
                    case 6:
                        regDefs.P2_REC.value = "0110";
                        break;
                    case 7:
                        regDefs.P2_REC.value = "0111";
                        break;
                    case 8:
                        regDefs.P2_REC.value = "1000";
                        break;
                    case 9:
                        regDefs.P2_REC.value = "1001";
                        break;
                    case 10:
                        regDefs.P2_REC.value = "1010";
                        break;
                    case 11:
                        regDefs.P2_REC.value = "1011";
                        break;
                    case 12:
                        regDefs.P2_REC.value = "1100";
                        break;
                    case 13:
                        regDefs.P2_REC.value = "1101";
                        break;
                    case 14:
                        regDefs.P2_REC.value = "1110";
                        break;
                    case 15:
                        regDefs.P2_REC.value = "1111";
                        break;
                }
                regDefs.REC_LENGTH.WriteToUART();
                if (primaryTab.SelectedTab.Name != "MemMap" && !firstTime)
                {
                    string[,] array = new string[1, 5];
                    array[0, 0] = regDefs.REC_LENGTH.location.ToString();
                    array[0, 1] = "22 (REC_LENGTH)";
                    array[0, 4] = Tools.int32_Into_stringBase16(calRegValfromFields(regDefs.REC_LENGTH.bit_fields));
                    updateMemoryMap(array, "GRID_USER_MEMSPACE");
                }
                updateTimeOffsetTextBox();
                p2MaxDistBox.Text = Convert.ToString(Math.Truncate(Convert.ToDouble(p2RecordCombo.Text) / 1000.0 * 343.0 / 2.0 * 1000.0) / 1000.0);
            }
        }

        private void nlsNoiseCombo_SelectedIndexChanged(object sender, EventArgs e)
        {
            if (!firstTime)
            {
                RegDefs regDefs = new RegDefs("27 (DSP_SCALE)");
                regDefs.DSP_SCALE.ReadFromUART();
                switch (nlsNoiseCombo.SelectedIndex)
                {
                    case 0:
                        regDefs.NOISE_LVL.value = "00000";
                        break;
                    case 1:
                        regDefs.NOISE_LVL.value = "00001";
                        break;
                    case 2:
                        regDefs.NOISE_LVL.value = "00010";
                        break;
                    case 3:
                        regDefs.NOISE_LVL.value = "00011";
                        break;
                    case 4:
                        regDefs.NOISE_LVL.value = "00100";
                        break;
                    case 5:
                        regDefs.NOISE_LVL.value = "00101";
                        break;
                    case 6:
                        regDefs.NOISE_LVL.value = "00110";
                        break;
                    case 7:
                        regDefs.NOISE_LVL.value = "00111";
                        break;
                    case 8:
                        regDefs.NOISE_LVL.value = "01000";
                        break;
                    case 9:
                        regDefs.NOISE_LVL.value = "01001";
                        break;
                    case 10:
                        regDefs.NOISE_LVL.value = "01010";
                        break;
                    case 11:
                        regDefs.NOISE_LVL.value = "01011";
                        break;
                    case 12:
                        regDefs.NOISE_LVL.value = "01100";
                        break;
                    case 13:
                        regDefs.NOISE_LVL.value = "01101";
                        break;
                    case 14:
                        regDefs.NOISE_LVL.value = "01110";
                        break;
                    case 15:
                        regDefs.NOISE_LVL.value = "01111";
                        break;
                    case 16:
                        regDefs.NOISE_LVL.value = "10000";
                        break;
                    case 17:
                        regDefs.NOISE_LVL.value = "10001";
                        break;
                    case 18:
                        regDefs.NOISE_LVL.value = "10010";
                        break;
                    case 19:
                        regDefs.NOISE_LVL.value = "10011";
                        break;
                    case 20:
                        regDefs.NOISE_LVL.value = "10100";
                        break;
                    case 21:
                        regDefs.NOISE_LVL.value = "10101";
                        break;
                    case 22:
                        regDefs.NOISE_LVL.value = "10110";
                        break;
                    case 23:
                        regDefs.NOISE_LVL.value = "10111";
                        break;
                    case 24:
                        regDefs.NOISE_LVL.value = "11000";
                        break;
                    case 25:
                        regDefs.NOISE_LVL.value = "11001";
                        break;
                    case 26:
                        regDefs.NOISE_LVL.value = "11010";
                        break;
                    case 27:
                        regDefs.NOISE_LVL.value = "11011";
                        break;
                    case 28:
                        regDefs.NOISE_LVL.value = "11100";
                        break;
                    case 29:
                        regDefs.NOISE_LVL.value = "11101";
                        break;
                    case 30:
                        regDefs.NOISE_LVL.value = "11110";
                        break;
                    case 31:
                        regDefs.NOISE_LVL.value = "11111";
                        break;
                }
                regDefs.DSP_SCALE.WriteToUART();
                if (primaryTab.SelectedTab.Name != "MemMap" && !firstTime)
                {
                    string[,] array = new string[1, 5];
                    array[0, 0] = regDefs.DSP_SCALE.location.ToString();
                    array[0, 1] = "27 (DSP_SCALE)";
                    array[0, 4] = Tools.int32_Into_stringBase16(calRegValfromFields(regDefs.DSP_SCALE.bit_fields));
                    updateMemoryMap(array, "GRID_USER_MEMSPACE");
                }
            }
        }

        private void nlsSECombo_SelectedIndexChanged(object sender, EventArgs e)
        {
            if (!firstTime)
            {
                RegDefs regDefs = new RegDefs("27 (DSP_SCALE)");
                regDefs.DSP_SCALE.ReadFromUART();
                switch (nlsSECombo.SelectedIndex)
                {
                    case 0:
                        regDefs.SCALE_K.value = "0";
                        break;
                    case 1:
                        regDefs.SCALE_K.value = "1";
                        break;
                }
                regDefs.DSP_SCALE.WriteToUART();
                if (primaryTab.SelectedTab.Name != "MemMap" && !firstTime)
                {
                    string[,] array = new string[1, 5];
                    array[0, 0] = regDefs.DSP_SCALE.location.ToString();
                    array[0, 1] = "27 (DSP_SCALE)";
                    array[0, 4] = Tools.int32_Into_stringBase16(calRegValfromFields(regDefs.DSP_SCALE.bit_fields));
                    updateMemoryMap(array, "GRID_USER_MEMSPACE");
                }
            }
        }

        private void nlsTOPCombo_SelectedIndexChanged(object sender, EventArgs e)
        {
            if (!firstTime)
            {
                RegDefs regDefs = new RegDefs("27 (DSP_SCALE)");
                regDefs.DSP_SCALE.ReadFromUART();
                switch (nlsTOPCombo.SelectedIndex)
                {
                    case 0:
                        regDefs.SCALE_N.value = "00";
                        break;
                    case 1:
                        regDefs.SCALE_N.value = "01";
                        break;
                    case 2:
                        regDefs.SCALE_N.value = "10";
                        break;
                    case 3:
                        regDefs.SCALE_N.value = "11";
                        break;
                }
                regDefs.DSP_SCALE.WriteToUART();
                if (primaryTab.SelectedTab.Name != "MemMap" && !firstTime)
                {
                    string[,] array = new string[1, 5];
                    array[0, 0] = regDefs.DSP_SCALE.location.ToString();
                    array[0, 1] = "27 (DSP_SCALE)";
                    array[0, 4] = Tools.int32_Into_stringBase16(calRegValfromFields(regDefs.DSP_SCALE.bit_fields));
                    updateMemoryMap(array, "GRID_USER_MEMSPACE");
                }
            }
            updateTimeOffsetTextBox();
        }

        private void updateTimeOffsetTextBox()
        {
            if (p2RecordCombo.Text != "" && nlsTOPCombo.Text != "")
            {
                double num = Convert.ToDouble(p2RecordCombo.Text);
                num = double.Parse(p2RecordCombo.Text);
                nlsTOTextbox.Text = Convert.ToString((double)(nlsTOPCombo.SelectedIndex + 1) * num / 10.0);
            }
        }

        private void disableCurrentLimitBox_CheckedChanged(object sender, EventArgs e)
        {
            if (!firstTime)
            {
                RegDefs regDefs = new RegDefs("20 (CURR_LIM_P1)");
                regDefs.CURR_LIM_P1.ReadFromUART();
                if (disableCurrentLimitBox.Checked)
                {
                    regDefs.DIS_CL.value = "1";
                }
                else
                {
                    regDefs.DIS_CL.value = "0";
                }
                regDefs.CURR_LIM_P1.WriteToUART();
                if (primaryTab.SelectedTab.Name != "MemMap" && !firstTime)
                {
                    string[,] array = new string[1, 5];
                    array[0, 0] = regDefs.CURR_LIM_P1.location.ToString();
                    array[0, 1] = "20 (CURR_LIM_P1)";
                    array[0, 4] = Tools.int32_Into_stringBase16(calRegValfromFields(regDefs.CURR_LIM_P1.bit_fields));
                    updateMemoryMap(array, "GRID_USER_MEMSPACE");
                }
            }
        }

        private void attackCombo_SelectedIndexChanged(object sender, EventArgs e)
        {
        }

        private void releaseCombo_SelectedIndexChanged(object sender, EventArgs e)
        {
        }

        private void attackmultiCheck_CheckedChanged(object sender, EventArgs e)
        {
        }

        private void releasemultiCheck_CheckedChanged(object sender, EventArgs e)
        {
        }

        private void updateAttackTimeTextbox()
        {
        }

        private void updateReleaseTimeTextbox()
        {
        }

        private void freqshiftCheck_CheckedChanged(object sender, EventArgs e)
        {
            if (!firstTime)
            {
                RegDefs regDefs = new RegDefs("1A (TVGAIN6)");
                regDefs.TVGAIN6.ReadFromUART();
                if (freqshiftCheck.Checked)
                {
                    regDefs.FREQ_SHIFT.value = "1";
                }
                else
                {
                    regDefs.FREQ_SHIFT.value = "0";
                }
                regDefs.TVGAIN6.WriteToUART();
                if (primaryTab.SelectedTab.Name != "MemMap" && !firstTime)
                {
                    string[,] array = new string[1, 5];
                    array[0, 0] = regDefs.TVGAIN6.location.ToString();
                    array[0, 1] = "1A (TVGAIN6)";
                    array[0, 4] = Tools.int32_Into_stringBase16(calRegValfromFields(regDefs.TVGAIN6.bit_fields));
                    updateMemoryMap(array, "GRID_USER_MEMSPACE");
                }
            }
            int selectedIndex = freqCombo.SelectedIndex;
            if (freqshiftCheck.Checked)
            {
                freqCombo.Items.Clear();
                freqCombo.Items.AddRange(new object[]
                {
                    "180",
                    "181.2",
                    "182.4",
                    "183.6",
                    "184.8",
                    "186",
                    "187.2",
                    "188.4",
                    "189.6",
                    "190.8",
                    "192",
                    "193.2",
                    "194.4",
                    "195.6",
                    "196.8",
                    "198",
                    "199.2",
                    "200.4",
                    "201.6",
                    "202.8",
                    "204",
                    "205.2",
                    "206.4",
                    "207.6",
                    "208.8",
                    "210",
                    "211.2",
                    "212.4",
                    "213.6",
                    "214.8",
                    "216",
                    "217.2",
                    "218.4",
                    "219.6",
                    "220.8",
                    "222",
                    "223.2",
                    "224.4",
                    "225.6",
                    "226.8",
                    "228",
                    "229.2",
                    "230.4",
                    "231.6",
                    "232.8",
                    "234",
                    "235.2",
                    "236.4",
                    "237.6",
                    "238.8",
                    "240",
                    "241.2",
                    "242.4",
                    "243.6",
                    "244.8",
                    "246",
                    "247.2",
                    "248.4",
                    "249.6",
                    "250.8",
                    "252",
                    "253.2",
                    "254.4",
                    "255.6",
                    "256.8",
                    "258",
                    "259.2",
                    "260.4",
                    "261.6",
                    "262.8",
                    "264",
                    "265.2",
                    "266.4",
                    "267.6",
                    "268.8",
                    "270",
                    "271.2",
                    "272.4",
                    "273.6",
                    "274.8",
                    "276",
                    "277.2",
                    "278.4",
                    "279.6",
                    "280.8",
                    "282",
                    "283.2",
                    "284.4",
                    "285.6",
                    "286.8",
                    "288",
                    "289.2",
                    "290.4",
                    "291.6",
                    "292.8",
                    "294",
                    "295.2",
                    "296.4",
                    "297.6",
                    "298.8",
                    "300",
                    "301.2",
                    "302.4",
                    "303.6",
                    "304.8",
                    "306",
                    "307.2",
                    "308.4",
                    "309.6",
                    "310.8",
                    "312",
                    "313.2",
                    "314.4",
                    "315.6",
                    "316.8",
                    "318",
                    "319.2",
                    "320.4",
                    "321.6",
                    "322.8",
                    "324",
                    "325.2",
                    "326.4",
                    "327.6",
                    "328.8",
                    "330",
                    "331.2",
                    "332.4",
                    "333.6",
                    "334.8",
                    "336",
                    "337.2",
                    "338.4",
                    "339.6",
                    "340.8",
                    "342",
                    "343.2",
                    "344.4",
                    "345.6",
                    "346.8",
                    "348",
                    "349.2",
                    "350.4",
                    "351.6",
                    "352.8",
                    "354",
                    "355.2",
                    "356.4",
                    "357.6",
                    "358.8",
                    "360",
                    "361.2",
                    "362.4",
                    "363.6",
                    "364.8",
                    "366",
                    "367.2",
                    "368.4",
                    "369.6",
                    "370.8",
                    "372",
                    "373.2",
                    "374.4",
                    "375.6",
                    "376.8",
                    "378",
                    "379.2",
                    "380.4",
                    "381.6",
                    "382.8",
                    "384",
                    "385.2",
                    "386.4",
                    "387.6",
                    "388.8",
                    "390",
                    "391.2",
                    "392.4",
                    "393.6",
                    "394.8",
                    "396",
                    "397.2",
                    "398.4",
                    "399.6",
                    "400.8",
                    "402",
                    "403.2",
                    "404.4",
                    "405.6",
                    "406.8",
                    "408",
                    "409.2",
                    "410.4",
                    "411.6",
                    "412.8",
                    "414",
                    "415.2",
                    "416.4",
                    "417.6",
                    "418.8",
                    "420",
                    "421.2",
                    "422.4",
                    "423.6",
                    "424.8",
                    "426",
                    "427.2",
                    "428.4",
                    "429.6",
                    "430.8",
                    "432",
                    "433.2",
                    "434.4",
                    "435.6",
                    "436.8",
                    "438",
                    "439.2",
                    "440.4",
                    "441.6",
                    "442.8",
                    "444",
                    "445.2",
                    "446.4",
                    "447.6",
                    "448.8",
                    "450",
                    "451.2",
                    "452.4",
                    "453.6",
                    "454.8",
                    "456",
                    "457.2",
                    "458.4",
                    "459.6",
                    "460.8",
                    "462",
                    "463.2",
                    "464.4",
                    "465.6",
                    "466.8",
                    "468",
                    "469.2",
                    "470.4",
                    "471.6",
                    "472.8",
                    "474",
                    "475.2",
                    "476.4",
                    "477.6",
                    "478.8",
                    "480"
                });
                freqCombo.SelectedIndex = selectedIndex;
                bd_freqCombo.Items.Clear();
                bd_freqCombo.Items.AddRange(new object[]
                {
                    "180",
                    "181.2",
                    "182.4",
                    "183.6",
                    "184.8",
                    "186",
                    "187.2",
                    "188.4",
                    "189.6",
                    "190.8",
                    "192",
                    "193.2",
                    "194.4",
                    "195.6",
                    "196.8",
                    "198",
                    "199.2",
                    "200.4",
                    "201.6",
                    "202.8",
                    "204",
                    "205.2",
                    "206.4",
                    "207.6",
                    "208.8",
                    "210",
                    "211.2",
                    "212.4",
                    "213.6",
                    "214.8",
                    "216",
                    "217.2",
                    "218.4",
                    "219.6",
                    "220.8",
                    "222",
                    "223.2",
                    "224.4",
                    "225.6",
                    "226.8",
                    "228",
                    "229.2",
                    "230.4",
                    "231.6",
                    "232.8",
                    "234",
                    "235.2",
                    "236.4",
                    "237.6",
                    "238.8",
                    "240",
                    "241.2",
                    "242.4",
                    "243.6",
                    "244.8",
                    "246",
                    "247.2",
                    "248.4",
                    "249.6",
                    "250.8",
                    "252",
                    "253.2",
                    "254.4",
                    "255.6",
                    "256.8",
                    "258",
                    "259.2",
                    "260.4",
                    "261.6",
                    "262.8",
                    "264",
                    "265.2",
                    "266.4",
                    "267.6",
                    "268.8",
                    "270",
                    "271.2",
                    "272.4",
                    "273.6",
                    "274.8",
                    "276",
                    "277.2",
                    "278.4",
                    "279.6",
                    "280.8",
                    "282",
                    "283.2",
                    "284.4",
                    "285.6",
                    "286.8",
                    "288",
                    "289.2",
                    "290.4",
                    "291.6",
                    "292.8",
                    "294",
                    "295.2",
                    "296.4",
                    "297.6",
                    "298.8",
                    "300",
                    "301.2",
                    "302.4",
                    "303.6",
                    "304.8",
                    "306",
                    "307.2",
                    "308.4",
                    "309.6",
                    "310.8",
                    "312",
                    "313.2",
                    "314.4",
                    "315.6",
                    "316.8",
                    "318",
                    "319.2",
                    "320.4",
                    "321.6",
                    "322.8",
                    "324",
                    "325.2",
                    "326.4",
                    "327.6",
                    "328.8",
                    "330",
                    "331.2",
                    "332.4",
                    "333.6",
                    "334.8",
                    "336",
                    "337.2",
                    "338.4",
                    "339.6",
                    "340.8",
                    "342",
                    "343.2",
                    "344.4",
                    "345.6",
                    "346.8",
                    "348",
                    "349.2",
                    "350.4",
                    "351.6",
                    "352.8",
                    "354",
                    "355.2",
                    "356.4",
                    "357.6",
                    "358.8",
                    "360",
                    "361.2",
                    "362.4",
                    "363.6",
                    "364.8",
                    "366",
                    "367.2",
                    "368.4",
                    "369.6",
                    "370.8",
                    "372",
                    "373.2",
                    "374.4",
                    "375.6",
                    "376.8",
                    "378",
                    "379.2",
                    "380.4",
                    "381.6",
                    "382.8",
                    "384",
                    "385.2",
                    "386.4",
                    "387.6",
                    "388.8",
                    "390",
                    "391.2",
                    "392.4",
                    "393.6",
                    "394.8",
                    "396",
                    "397.2",
                    "398.4",
                    "399.6",
                    "400.8",
                    "402",
                    "403.2",
                    "404.4",
                    "405.6",
                    "406.8",
                    "408",
                    "409.2",
                    "410.4",
                    "411.6",
                    "412.8",
                    "414",
                    "415.2",
                    "416.4",
                    "417.6",
                    "418.8",
                    "420",
                    "421.2",
                    "422.4",
                    "423.6",
                    "424.8",
                    "426",
                    "427.2",
                    "428.4",
                    "429.6",
                    "430.8",
                    "432",
                    "433.2",
                    "434.4",
                    "435.6",
                    "436.8",
                    "438",
                    "439.2",
                    "440.4",
                    "441.6",
                    "442.8",
                    "444",
                    "445.2",
                    "446.4",
                    "447.6",
                    "448.8",
                    "450",
                    "451.2",
                    "452.4",
                    "453.6",
                    "454.8",
                    "456",
                    "457.2",
                    "458.4",
                    "459.6",
                    "460.8",
                    "462",
                    "463.2",
                    "464.4",
                    "465.6",
                    "466.8",
                    "468",
                    "469.2",
                    "470.4",
                    "471.6",
                    "472.8",
                    "474",
                    "475.2",
                    "476.4",
                    "477.6",
                    "478.8",
                    "480"
                });
                bd_freqCombo.SelectedIndex = selectedIndex;
                psFreqStart.Increment = 1.2m;
                psFreqStart.Minimum = 180m;
                psFreqStart.Maximum = 480m;
                psFreqStart.Value = 180m;
                psFreqEnd.Increment = 1.2m;
                psFreqEnd.Minimum = 180m;
                psFreqEnd.Maximum = 480m;
                psFreqEnd.Value = 480m;
                psFreqInc.Increment = 1.2m;
                psFreqInc.Minimum = 1.2m;
                psFreqInc.Maximum = 300m;
                psFreqInc.Value = 24m;
            }
            else
            {
                freqCombo.Items.Clear();
                freqCombo.Items.AddRange(new object[]
                {
                    "30",
                    "30.2",
                    "30.4",
                    "30.6",
                    "30.8",
                    "31",
                    "31.2",
                    "31.4",
                    "31.6",
                    "31.8",
                    "32",
                    "32.2",
                    "32.4",
                    "32.6",
                    "32.8",
                    "33",
                    "33.2",
                    "33.4",
                    "33.6",
                    "33.8",
                    "34",
                    "34.2",
                    "34.4",
                    "34.6",
                    "34.8",
                    "35",
                    "35.2",
                    "35.4",
                    "35.6",
                    "35.8",
                    "36",
                    "36.2",
                    "36.4",
                    "36.6",
                    "36.8",
                    "37",
                    "37.2",
                    "37.4",
                    "37.6",
                    "37.8",
                    "38",
                    "38.2",
                    "38.4",
                    "38.6",
                    "38.8",
                    "39",
                    "39.2",
                    "39.4",
                    "39.6",
                    "39.8",
                    "40",
                    "40.2",
                    "40.4",
                    "40.6",
                    "40.8",
                    "41",
                    "41.2",
                    "41.4",
                    "41.6",
                    "41.8",
                    "42",
                    "42.2",
                    "42.4",
                    "42.6",
                    "42.8",
                    "43",
                    "43.2",
                    "43.4",
                    "43.6",
                    "43.8",
                    "44",
                    "44.2",
                    "44.4",
                    "44.6",
                    "44.8",
                    "45",
                    "45.2",
                    "45.4",
                    "45.6",
                    "45.8",
                    "46",
                    "46.2",
                    "46.4",
                    "46.6",
                    "46.8",
                    "47",
                    "47.2",
                    "47.4",
                    "47.6",
                    "47.8",
                    "48",
                    "48.2",
                    "48.4",
                    "48.6",
                    "48.8",
                    "49",
                    "49.2",
                    "49.4",
                    "49.6",
                    "49.8",
                    "50",
                    "50.2",
                    "50.4",
                    "50.6",
                    "50.8",
                    "51",
                    "51.2",
                    "51.4",
                    "51.6",
                    "51.8",
                    "52",
                    "52.2",
                    "52.4",
                    "52.6",
                    "52.8",
                    "53",
                    "53.2",
                    "53.4",
                    "53.6",
                    "53.8",
                    "54",
                    "54.2",
                    "54.4",
                    "54.6",
                    "54.8",
                    "55",
                    "55.2",
                    "55.4",
                    "55.6",
                    "55.8",
                    "56",
                    "56.2",
                    "56.4",
                    "56.6",
                    "56.8",
                    "57",
                    "57.2",
                    "57.4",
                    "57.6",
                    "57.8",
                    "58",
                    "58.2",
                    "58.4",
                    "58.6",
                    "58.8",
                    "59",
                    "59.2",
                    "59.4",
                    "59.6",
                    "59.8",
                    "60",
                    "60.2",
                    "60.4",
                    "60.6",
                    "60.8",
                    "61",
                    "61.2",
                    "61.4",
                    "61.6",
                    "61.8",
                    "62",
                    "62.2",
                    "62.4",
                    "62.6",
                    "62.8",
                    "63",
                    "63.2",
                    "63.4",
                    "63.6",
                    "63.8",
                    "64",
                    "64.2",
                    "64.4",
                    "64.6",
                    "64.8",
                    "65",
                    "65.2",
                    "65.4",
                    "65.6",
                    "65.8",
                    "66",
                    "66.2",
                    "66.4",
                    "66.6",
                    "66.8",
                    "67",
                    "67.2",
                    "67.4",
                    "67.6",
                    "67.8",
                    "68",
                    "68.2",
                    "68.4",
                    "68.6",
                    "68.8",
                    "69",
                    "69.2",
                    "69.4",
                    "69.6",
                    "69.8",
                    "70",
                    "70.2",
                    "70.4",
                    "70.6",
                    "70.8",
                    "71",
                    "71.2",
                    "71.4",
                    "71.6",
                    "71.8",
                    "72",
                    "72.2",
                    "72.4",
                    "72.6",
                    "72.8",
                    "73",
                    "73.2",
                    "73.4",
                    "73.6",
                    "73.8",
                    "74",
                    "74.2",
                    "74.4",
                    "74.6",
                    "74.8",
                    "75",
                    "75.2",
                    "75.4",
                    "75.6",
                    "75.8",
                    "76",
                    "76.2",
                    "76.4",
                    "76.6",
                    "76.8",
                    "77",
                    "77.2",
                    "77.4",
                    "77.6",
                    "77.8",
                    "78",
                    "78.2",
                    "78.4",
                    "78.6",
                    "78.8",
                    "79",
                    "79.2",
                    "79.4",
                    "79.6",
                    "79.8",
                    "80"
                });
                freqCombo.SelectedIndex = selectedIndex;
                bd_freqCombo.Items.Clear();
                bd_freqCombo.Items.AddRange(new object[]
                {
                    "30",
                    "30.2",
                    "30.4",
                    "30.6",
                    "30.8",
                    "31",
                    "31.2",
                    "31.4",
                    "31.6",
                    "31.8",
                    "32",
                    "32.2",
                    "32.4",
                    "32.6",
                    "32.8",
                    "33",
                    "33.2",
                    "33.4",
                    "33.6",
                    "33.8",
                    "34",
                    "34.2",
                    "34.4",
                    "34.6",
                    "34.8",
                    "35",
                    "35.2",
                    "35.4",
                    "35.6",
                    "35.8",
                    "36",
                    "36.2",
                    "36.4",
                    "36.6",
                    "36.8",
                    "37",
                    "37.2",
                    "37.4",
                    "37.6",
                    "37.8",
                    "38",
                    "38.2",
                    "38.4",
                    "38.6",
                    "38.8",
                    "39",
                    "39.2",
                    "39.4",
                    "39.6",
                    "39.8",
                    "40",
                    "40.2",
                    "40.4",
                    "40.6",
                    "40.8",
                    "41",
                    "41.2",
                    "41.4",
                    "41.6",
                    "41.8",
                    "42",
                    "42.2",
                    "42.4",
                    "42.6",
                    "42.8",
                    "43",
                    "43.2",
                    "43.4",
                    "43.6",
                    "43.8",
                    "44",
                    "44.2",
                    "44.4",
                    "44.6",
                    "44.8",
                    "45",
                    "45.2",
                    "45.4",
                    "45.6",
                    "45.8",
                    "46",
                    "46.2",
                    "46.4",
                    "46.6",
                    "46.8",
                    "47",
                    "47.2",
                    "47.4",
                    "47.6",
                    "47.8",
                    "48",
                    "48.2",
                    "48.4",
                    "48.6",
                    "48.8",
                    "49",
                    "49.2",
                    "49.4",
                    "49.6",
                    "49.8",
                    "50",
                    "50.2",
                    "50.4",
                    "50.6",
                    "50.8",
                    "51",
                    "51.2",
                    "51.4",
                    "51.6",
                    "51.8",
                    "52",
                    "52.2",
                    "52.4",
                    "52.6",
                    "52.8",
                    "53",
                    "53.2",
                    "53.4",
                    "53.6",
                    "53.8",
                    "54",
                    "54.2",
                    "54.4",
                    "54.6",
                    "54.8",
                    "55",
                    "55.2",
                    "55.4",
                    "55.6",
                    "55.8",
                    "56",
                    "56.2",
                    "56.4",
                    "56.6",
                    "56.8",
                    "57",
                    "57.2",
                    "57.4",
                    "57.6",
                    "57.8",
                    "58",
                    "58.2",
                    "58.4",
                    "58.6",
                    "58.8",
                    "59",
                    "59.2",
                    "59.4",
                    "59.6",
                    "59.8",
                    "60",
                    "60.2",
                    "60.4",
                    "60.6",
                    "60.8",
                    "61",
                    "61.2",
                    "61.4",
                    "61.6",
                    "61.8",
                    "62",
                    "62.2",
                    "62.4",
                    "62.6",
                    "62.8",
                    "63",
                    "63.2",
                    "63.4",
                    "63.6",
                    "63.8",
                    "64",
                    "64.2",
                    "64.4",
                    "64.6",
                    "64.8",
                    "65",
                    "65.2",
                    "65.4",
                    "65.6",
                    "65.8",
                    "66",
                    "66.2",
                    "66.4",
                    "66.6",
                    "66.8",
                    "67",
                    "67.2",
                    "67.4",
                    "67.6",
                    "67.8",
                    "68",
                    "68.2",
                    "68.4",
                    "68.6",
                    "68.8",
                    "69",
                    "69.2",
                    "69.4",
                    "69.6",
                    "69.8",
                    "70",
                    "70.2",
                    "70.4",
                    "70.6",
                    "70.8",
                    "71",
                    "71.2",
                    "71.4",
                    "71.6",
                    "71.8",
                    "72",
                    "72.2",
                    "72.4",
                    "72.6",
                    "72.8",
                    "73",
                    "73.2",
                    "73.4",
                    "73.6",
                    "73.8",
                    "74",
                    "74.2",
                    "74.4",
                    "74.6",
                    "74.8",
                    "75",
                    "75.2",
                    "75.4",
                    "75.6",
                    "75.8",
                    "76",
                    "76.2",
                    "76.4",
                    "76.6",
                    "76.8",
                    "77",
                    "77.2",
                    "77.4",
                    "77.6",
                    "77.8",
                    "78",
                    "78.2",
                    "78.4",
                    "78.6",
                    "78.8",
                    "79",
                    "79.2",
                    "79.4",
                    "79.6",
                    "79.8",
                    "80"
                });
                bd_freqCombo.SelectedIndex = selectedIndex;
                psFreqStart.Increment = 0.2m;
                psFreqStart.Minimum = 30m;
                psFreqStart.Maximum = 80m;
                psFreqStart.Value = 30m;
                psFreqEnd.Increment = 0.2m;
                psFreqEnd.Minimum = 30m;
                psFreqEnd.Maximum = 80m;
                psFreqEnd.Value = 80m;
                psFreqInc.Increment = 0.2m;
                psFreqInc.Minimum = 0.2m;
                psFreqInc.Maximum = 50m;
                psFreqInc.Value = 10m;
            }
        }

        private void bit8sampleRadio_CheckedChanged(object sender, EventArgs e)
        {
            if (bit8sampleRadio.Checked)
            {
                sampleOut8bitRadio.Checked = true;
            }
            else
            {
                sampleOut8bitRadio.Checked = false;
            }
        }

        public void clearPlotBtn_Click(object sender, EventArgs e)
        {
            monitor_clear_plot();
        }

        public void eepromPgrmBtn_Click(object sender, EventArgs e)
        {
            Array.Clear(uart_return_data, 0, 64);
            commandByte = (byte)(((int)Convert.ToByte(uartAddrCombo.Text) << addrShift) + (int)uart_cmd10);
            regAddrByte = 64;
            RegDefs regDefs = new RegDefs("40 (EE_CNTRL)");
            byte b = (byte)Convert.ToInt32(regDefs.EE_PRGM.value, 2);
            b |= 104;
            commandByte = (byte)(((int)Convert.ToByte(uartAddrCombo.Text) << addrShift) + (int)uart_cmd10);
            MChecksumByte = calculate_UART_Checksum(new byte[]
            {
                commandByte,
                regAddrByte,
                b
            });
            common.u2a.UART_Write(5, new byte[]
            {
                syncByte,
                commandByte,
                regAddrByte,
                b,
                MChecksumByte
            });
            commandByte = (byte)(((int)Convert.ToByte(uartAddrCombo.Text) << addrShift) + (int)uart_cmd10);
            regAddrByte = 64;
            b = (byte)Convert.ToInt32(regDefs.EE_PRGM.value, 2);
            byte b2 = b;
            b |= 1;
            commandByte = (byte)(((int)Convert.ToByte(uartAddrCombo.Text) << addrShift) + (int)uart_cmd10);
            MChecksumByte = calculate_UART_Checksum(new byte[]
            {
                commandByte,
                regAddrByte,
                b
            });
            common.u2a.UART_Write(5, new byte[]
            {
                syncByte,
                commandByte,
                regAddrByte,
                b,
                MChecksumByte
            });
            eepromStatBox.Text = "Working...";
            Tools.timeDelay(1000, "MS");
            commandByte = (byte)(((int)Convert.ToByte(uartAddrCombo.Text) << addrShift) + (int)uart_cmd9);
            MChecksumByte = calculate_UART_Checksum(new byte[]
            {
                commandByte,
                regAddrByte
            });
            common.u2a.UART_Write(4, new byte[]
            {
                syncByte,
                commandByte,
                regAddrByte,
                MChecksumByte
            });
            common.u2a.UART_Read(3, uart_return_data);
            uartDiagB = uart_return_data[0];
            if ((uart_return_data[1] & 5) == 5)
            {
                eepromStatBox.Text = "Programmed Successfully";
            }
            else
            {
                eepromStatBox.Text = "Failed to Program";
            }
            Tools.timeDelay(100, "MS");
            commandByte = (byte)(((int)Convert.ToByte(uartAddrCombo.Text) << addrShift) + (int)uart_cmd10);
            MChecksumByte = calculate_UART_Checksum(new byte[]
            {
                commandByte,
                regAddrByte,
                b2
            });
            common.u2a.UART_Write(5, new byte[]
            {
                syncByte,
                commandByte,
                regAddrByte,
                b2,
                MChecksumByte
            });
        }

        public void eepromReloadBtn_Click(object sender, EventArgs e)
        {
            Array.Clear(uart_return_data, 0, 64);
            commandByte = (byte)(((int)Convert.ToByte(uartAddrCombo.Text) << addrShift) + (int)uart_cmd10);
            regAddrByte = 64;
            RegDefs regDefs = new RegDefs("40 (EE_CNTRL)");
            byte b = (byte)Convert.ToInt32(regDefs.EE_PRGM.value, 2);
            byte b2 = b;
            b |= 2;
            MChecksumByte = calculate_UART_Checksum(new byte[]
            {
                commandByte,
                regAddrByte,
                b
            });
            common.u2a.UART_Write(5, new byte[]
            {
                syncByte,
                commandByte,
                regAddrByte,
                b,
                MChecksumByte
            });
            eepromStatBox.Text = "Working...";
            Tools.timeDelay(1000, "MS");
            eepromStatBox.Text = "EEPROM Reloaded";
            commandByte = (byte)(((int)Convert.ToByte(uartAddrCombo.Text) << addrShift) + (int)uart_cmd10);
            MChecksumByte = calculate_UART_Checksum(new byte[]
            {
                commandByte,
                regAddrByte,
                b2
            });
            common.u2a.UART_Write(5, new byte[]
            {
                syncByte,
                commandByte,
                regAddrByte,
                b2,
                MChecksumByte
            });
        }

        public void thrReadValuesBtn_Click(object sender, EventArgs e)
        {
            activateProgressBar(true);
            bool flag = false;
            if (thrUpdateCheck.Checked)
            {
                flag = true;
                thrUpdateCheck.Checked = false;
            }
            initThrFlag = false;
            readRegsOffPage = true;
            ReadAllRegs(false);
            readRegsOffPage = false;
            initThrFlag = true;
            if (flag)
            {
                thrWriteValuesBtn_Click(null, null);
                thrUpdateCheck.Checked = true;
            }
            updateThrBtn_Click(null, null);
            activateProgressBar(false);
        }

        public void ClearFaults_button_Click(object sender, EventArgs e)
        {
            text_box_status(REVID_Stat_TextBox, "");
            text_box_status(OPTID_Stat_TextBox, "");
            text_box_status(CMWWUERR_Stat_TextBox, "");
            text_box_status(THRCRCERR_Stat_TextBox, "");
            text_box_status(EECRCERR_Stat_TextBox, "");
            text_box_status(TRIMCRCERR_Stat_TextBox, "");
            text_box_status(TSDPROT_Stat_TextBox, "");
            text_box_status(IOREGOV_Stat_TextBox, "");
            text_box_status(IOREGUV_Stat_TextBox, "");
            text_box_status(AVDDOV_Stat_TextBox, "");
            text_box_status(AVDDUV_Stat_TextBox, "");
            text_box_status(VPWROV_Stat_TextBox, "");
            text_box_status(VPWRUV_Stat_TextBox, "");
            text_box_status(UARTDIAG0_1_Stat_TextBox, "");
            text_box_status(UARTDIAG0_2_Stat_TextBox, "");
            text_box_status(UARTDIAG0_3_Stat_TextBox, "");
            text_box_status(UARTDIAG0_4_Stat_TextBox, "");
            text_box_status(UARTDIAG0_5_Stat_TextBox, "");
            text_box_status(UARTDIAG1_1_Stat_TextBox, "");
            text_box_status(UARTDIAG1_2_Stat_TextBox, "");
            text_box_status(UARTDIAG1_3_Stat_TextBox, "");
            text_box_status(UARTDIAG1_5_Stat_TextBox, "");
        }

        public void Fault_Stat_Update_button_Click(object sender, EventArgs e)
        {
            if (PGA46xStat_box.Text.Contains("Ready"))
            {
                fault_update();
            }
        }

        public void fault_update()
        {
            ClearFaults_button_Click(null, null);
            regAddrByte = 76;
            commandByte = (byte)(((int)Convert.ToByte(uartAddrComboText) << addrShift) + (int)uart_cmd9);
            MChecksumByte = calculate_UART_Checksum(new byte[]
            {
                commandByte,
                regAddrByte
            });
            common.u2a.UART_Write(4, new byte[]
            {
                syncByte,
                commandByte,
                regAddrByte,
                MChecksumByte
            });
            common.u2a.UART_Read(3, uart_return_data);
            string text = Tools.StringBase16_Into_StringBase2(Tools.int32_Into_stringBase16((int)uart_return_data[1], 8), false);
            string text2 = Tools.StringBase16_Into_StringBase2(Tools.int32_Into_stringBase16((int)uart_return_data[0], 8), false);
            text_box_status(REVID_Stat_TextBox, text.Substring(0, 2));
            text_box_status(OPTID_Stat_TextBox, text.Substring(2, 2));
            text_box_status(CMWWUERR_Stat_TextBox, text.Substring(4, 1));
            text_box_status(THRCRCERR_Stat_TextBox, text.Substring(5, 1));
            text_box_status(EECRCERR_Stat_TextBox, text.Substring(6, 1));
            text_box_status(TRIMCRCERR_Stat_TextBox, text.Substring(7, 1));
            if (uartDiagB != 0)
            {
                text2 = Tools.Byte_into_StringBase2(uartDiagB);
                uartDiagB = 0;
            }
            if (uartDiagUARTRadio.Checked)
            {
                text_box_status(UARTDIAG0_1_Stat_TextBox, text2.Substring(6, 1));
                text_box_status(UARTDIAG0_2_Stat_TextBox, text2.Substring(5, 1));
                text_box_status(UARTDIAG0_3_Stat_TextBox, text2.Substring(4, 1));
                text_box_status(UARTDIAG0_4_Stat_TextBox, text2.Substring(3, 1));
                text_box_status(UARTDIAG0_5_Stat_TextBox, text2.Substring(2, 1));
            }
            else
            {
                text_box_status(UARTDIAG1_1_Stat_TextBox, text2.Substring(6, 1));
                text_box_status(UARTDIAG1_2_Stat_TextBox, text2.Substring(5, 1));
                text_box_status(UARTDIAG1_3_Stat_TextBox, text2.Substring(4, 1));
                text_box_status(UARTDIAG1_5_Stat_TextBox, text2.Substring(2, 1));
            }
            text = Tools.StringBase16_Into_StringBase2(UART_Read_Write(77, 0, false), false);
            text_box_status(TSDPROT_Stat_TextBox, text.Substring(1, 1));
            text_box_status(IOREGOV_Stat_TextBox, text.Substring(2, 1));
            text_box_status(IOREGUV_Stat_TextBox, text.Substring(3, 1));
            text_box_status(AVDDOV_Stat_TextBox, text.Substring(4, 1));
            text_box_status(AVDDUV_Stat_TextBox, text.Substring(5, 1));
            text_box_status(VPWROV_Stat_TextBox, text.Substring(6, 1));
            text_box_status(VPWRUV_Stat_TextBox, text.Substring(7, 1));
            if (TSDPROT_Stat_TextBox.Text == "1" || IOREGOV_Stat_TextBox.Text == "1" || IOREGUV_Stat_TextBox.Text == "1" || AVDDOV_Stat_TextBox.Text == "1" || AVDDUV_Stat_TextBox.Text == "1" || VPWROV_Stat_TextBox.Text == "1" || VPWRUV_Stat_TextBox.Text == "1")
            {
                common.u2a.GPIO_WritePort(10, 2);
            }
            else
            {
                common.u2a.GPIO_WritePort(10, 1);
            }
            if (UARTDIAG1_2_Stat_TextBox.Text == "1")
            {
                common.u2a.GPIO_WritePort(11, 2);
            }
            else
            {
                common.u2a.GPIO_WritePort(11, 1);
            }
            if (UARTDIAG1_3_Stat_TextBox.Text == "1")
            {
                common.u2a.GPIO_WritePort(12, 2);
            }
            else
            {
                common.u2a.GPIO_WritePort(12, 1);
            }
            Initialize_PGA46x_Check();
        }

        private void FaultStat_AutoUpd_En_chck_CheckedChanged(object sender, EventArgs e)
        {
            int delayTime = (int)Convert.ToInt16(FaultStat_AutoUpd_Time_Box.Text);
            if (!FaultStat_AutoUpd_En_chck.Checked)
            {
                FaultStat_AutoUpd_Time_Box.Text = delayTime.ToString();
                FaultStat_AutoUpd_Time_Box.Enabled = false;
            }
            else
            {
                FaultStat_AutoUpd_Time_Box.Enabled = true;
                FaultStat_AutoUpd_En_chck.Refresh();
                while (FaultStat_AutoUpd_En_chck.Checked)
                {
                    if (Tools.TestForValidINT32(FaultStat_AutoUpd_Time_Box.Text, 10, 1, 100, ""))
                    {
                        delayTime = (int)Convert.ToInt16(FaultStat_AutoUpd_Time_Box.Text);
                    }
                    else if (FaultStat_AutoUpd_Time_Box.Text != "")
                    {
                        MessageBox.Show("Update Period must be between 1 and 100 seconds.");
                        FaultStat_AutoUpd_Time_Box.Text = delayTime.ToString();
                    }
                    Fault_Stat_Update_button.PerformClick();
                    Initialize_PGA46x_Check();
                    Tools.timeDelay(delayTime, "S");
                }
            }
        }

        private void comModeUARTBtn_Click(object sender, EventArgs e)
        {
            comTabControl.SelectTab(0);
            comModeUARTBtn.BackColor = Color.CadetBlue;
            comModeUARTBtn.Font = new Font(comModeUARTBtn.Font.Name, comModeUARTBtn.Font.Size, FontStyle.Bold);
            comModeUARTBtn.ForeColor = Color.White;
            comModeIOBtn.BackColor = Color.DarkGray;
            comModeIOBtn.Font = new Font(comModeIOBtn.Font.Name, comModeIOBtn.Font.Size, FontStyle.Regular);
            comModeIOBtn.ForeColor = Color.Black;
        }

        private void comModeIOBtn_Click_1(object sender, EventArgs e)
        {
            comTabControl.SelectTab(1);
            comModeIOBtn.BackColor = Color.CadetBlue;
            comModeIOBtn.Font = new Font(comModeIOBtn.Font.Name, comModeIOBtn.Font.Size, FontStyle.Bold);
            comModeIOBtn.ForeColor = Color.White;
            comModeUARTBtn.BackColor = Color.DarkGray;
            comModeUARTBtn.Font = new Font(comModeUARTBtn.Font.Name, comModeUARTBtn.Font.Size, FontStyle.Regular);
            comModeUARTBtn.ForeColor = Color.Black;
        }

        private void comModeOWUBtn_Click(object sender, EventArgs e)
        {
            comTabControl.SelectTab(2);
            comModeUARTBtn.BackColor = Color.DarkGray;
            comModeUARTBtn.Font = new Font(comModeUARTBtn.Font.Name, comModeUARTBtn.Font.Size, FontStyle.Regular);
            comModeUARTBtn.ForeColor = Color.Black;
            comModeIOBtn.BackColor = Color.DarkGray;
            comModeIOBtn.Font = new Font(comModeIOBtn.Font.Name, comModeIOBtn.Font.Size, FontStyle.Regular);
            comModeIOBtn.ForeColor = Color.Black;
            MessageBox.Show("IO One-Wire UART is not enabled on this version of the GUI.", "OWI-UART Limitation", MessageBoxButtons.OK);
        }

        private void coefOverrideCheck_CheckedChanged(object sender, EventArgs e)
        {
            if (coefOverrideCheck.Checked)
            {
                bpcob1Box.Enabled = true;
                bpcoa2Box.Enabled = true;
                bpcoa3Box.Enabled = true;
                lpcob1Box.Enabled = true;
                lpcoa2Box.Enabled = true;
            }
            else
            {
                bpcob1Box.Enabled = false;
                bpcoa2Box.Enabled = false;
                bpcoa3Box.Enabled = false;
                lpcob1Box.Enabled = false;
                lpcoa2Box.Enabled = false;
            }
        }

        private void u2aRestartBtn_Click(object sender, EventArgs e)
        {
            Initialize_Comm_Interface(true);
        }

        public void exportDataBtn_Click(object sender, EventArgs e)
        {
            if (graphModeCombo.Text == "Data Dump")
            {
                int index = 0;
                if (toolStripComboBox1.Text == "TCI")
                {
                    index = 11;
                }
                int count = dumpChart.Series[index].Points.Count;
                if (count > 1)
                {
                    Files files = new Files();
                    string[] array = new string[count];
                    if (!contClearCheck.Checked)
                    {
                        array = new string[count - 2];
                    }
                    if ((exportTimeStampCheck.Checked && !exportAdvCheck.Checked) || (!exportTimeStampCheck.Checked && exportAdvCheck.Checked))
                    {
                        if (!contClearCheck.Checked)
                        {
                            array = new string[count - 1];
                        }
                        else
                        {
                            array = new string[count + 1];
                        }
                    }
                    if (exportTimeStampCheck.Checked && exportAdvCheck.Checked)
                    {
                        if (!contClearCheck.Checked)
                        {
                            array = new string[count];
                        }
                        else
                        {
                            array = new string[count + 2];
                        }
                    }
                    int i;
                    for (i = 0; i < count; i++)
                    {
                        if (contClearCheck.Checked || i <= count - 3)
                        {
                            if (!contClearCheck.Checked)
                            {
                                array[i] = dumpChart.Series[index].Points[i].XValue.ToString() + "," + dumpChart.Series[(Monitor_NumberLoops_done_Temp - 1) % 6].Points[i + 1].YValues[0].ToString();
                            }
                            else
                            {
                                array[i] = dumpChart.Series[index].Points[i].XValue.ToString() + "," + dumpChart.Series[index].Points[i].YValues[0].ToString();
                            }
                        }
                        if ((exportTimeStampCheck.Checked && !exportAdvCheck.Checked) || (exportTimeStampCheck.Checked && exportAdvCheck.Checked))
                        {
                            if (i == count - 1)
                            {
                                if (!contClearCheck.Checked)
                                {
                                    array[i - 1] = DateTime.Now.ToString("MM\\/dd\\/yyyy h\\:mm\\:ss\\:ff tt");
                                }
                                else
                                {
                                    array[i + 1] = DateTime.Now.ToString("MM\\/dd\\/yyyy h\\:mm\\:ss\\:ff tt");
                                }
                            }
                        }
                        if (!exportTimeStampCheck.Checked && exportAdvCheck.Checked)
                        {
                            if (i == count - 1)
                            {
                                tempOnlyBtn_Click(null, null);
                                if (!contClearCheck.Checked)
                                {
                                    array[i - 1] = "T=" + tempBox.Text + "\t N=" + noiseBox.Text;
                                }
                                else
                                {
                                    array[i + 1] = "T=" + tempBox.Text + "\t N=" + noiseBox.Text;
                                }
                            }
                        }
                    }
                    if (exportTimeStampCheck.Checked && exportAdvCheck.Checked)
                    {
                        tempOnlyBtn_Click(null, null);
                        if (!contClearCheck.Checked)
                        {
                            array[i - 1] = "T=" + tempBox.Text + "\t N=" + noiseBox.Text;
                        }
                        else
                        {
                            array[i + 1] = "T=" + tempBox.Text + "\t N=" + noiseBox.Text;
                        }
                    }
                    string fileNameDOTextenstion = files.CreateFileName("EDD", "", exportSaveAs, true, true, false);
                    files.WriteArrayToFile("EDD", array, Environment.GetFolderPath(Environment.SpecialFolder.Personal) + "\\BOOSTXL-PGA460\\", fileNameDOTextenstion);
                    files.Dispose();
                }
            }
            else if (graphModeCombo.Text == "ADC" || graphModeCombo.Text == "DSP - BP Filter" || graphModeCombo.Text == "DSP - Rectifier" || graphModeCombo.Text == "DSP - LP Filter")
            {
                Files files = new Files();
                string[] array = new string[(int)((double)adcChart.Series[0].Points.Count * 0.72)];
                int i = 0;
                while ((double)i < (double)adcChart.Series[0].Points.Count * 0.72)
                {
                    array[i] = adcChart.Series[0].Points[i].XValue.ToString() + "," + adcChart.Series[0].Points[i].YValues[0].ToString();
                    i++;
                }
                string fileNameDOTextenstion = files.CreateFileName(graphModeCombo.Text, "", exportSaveAs, true, true, false);
                files.WriteArrayToFile(graphModeCombo.Text, array, Environment.GetFolderPath(Environment.SpecialFolder.Personal) + "\\BOOSTXL-PGA460\\", fileNameDOTextenstion);
                files.Dispose();
            }
            else
            {
                MessageBox.Show("No Data to be Exported.");
            }
        }

        private void checkForDevBtn_Click(object sender, EventArgs e)
        {
            firstTimeCheckforPGA = 1;
            Initialize_PGA46x_Check();
            if (PGA46xStat_box.Text.Contains("Ready"))
            {
                readRegsOffPage = true;
                ReadAllRegs(false);
                readRegsOffPage = false;
            }
        }

        private void byteLSBRadio_CheckedChanged(object sender, EventArgs e)
        {
        }

        private void byteMSBRadio_CheckedChanged(object sender, EventArgs e)
        {
            byteLSBRadio_CheckedChanged(null, null);
        }

        private void deadCombo_SelectedIndexChanged_1(object sender, EventArgs e)
        {
            if (!firstTime)
            {
                RegDefs regDefs = new RegDefs("1D (DEADTIME)");
                regDefs.DEADTIME.ReadFromUART();
                switch (deadCombo.SelectedIndex)
                {
                    case 0:
                        regDefs.PULSE_DT.value = "0000";
                        break;
                    case 1:
                        regDefs.PULSE_DT.value = "0001";
                        break;
                    case 2:
                        regDefs.PULSE_DT.value = "0010";
                        break;
                    case 3:
                        regDefs.PULSE_DT.value = "0011";
                        break;
                    case 4:
                        regDefs.PULSE_DT.value = "0100";
                        break;
                    case 5:
                        regDefs.PULSE_DT.value = "0101";
                        break;
                    case 6:
                        regDefs.PULSE_DT.value = "0110";
                        break;
                    case 7:
                        regDefs.PULSE_DT.value = "0111";
                        break;
                    case 8:
                        regDefs.PULSE_DT.value = "1000";
                        break;
                    case 9:
                        regDefs.PULSE_DT.value = "1001";
                        break;
                    case 10:
                        regDefs.PULSE_DT.value = "1010";
                        break;
                    case 11:
                        regDefs.PULSE_DT.value = "1011";
                        break;
                    case 12:
                        regDefs.PULSE_DT.value = "1100";
                        break;
                    case 13:
                        regDefs.PULSE_DT.value = "1101";
                        break;
                    case 14:
                        regDefs.PULSE_DT.value = "1110";
                        break;
                    case 15:
                        regDefs.PULSE_DT.value = "1111";
                        break;
                }
                regDefs.DEADTIME.WriteToUART();
                if (primaryTab.SelectedTab.Name != "MemMap" && !firstTime)
                {
                    string[,] array = new string[1, 5];
                    array[0, 0] = regDefs.DEADTIME.location.ToString();
                    array[0, 1] = "1D (DEADTIME)";
                    array[0, 4] = Tools.int32_Into_stringBase16(calRegValfromFields(regDefs.DEADTIME.bit_fields));
                    updateMemoryMap(array, "GRID_USER_MEMSPACE");
                }
            }
        }

        private void p1DriveCombo_SelectedIndexChanged(object sender, EventArgs e)
        {
            if (!firstTime)
            {
                RegDefs regDefs = new RegDefs("20 (CURR_LIM_P1)");
                regDefs.CURR_LIM_P1.ReadFromUART();
                switch (p1DriveCombo.SelectedIndex)
                {
                    case 0:
                        regDefs.CURR_LIM1.value = "000000";
                        break;
                    case 1:
                        regDefs.CURR_LIM1.value = "000001";
                        break;
                    case 2:
                        regDefs.CURR_LIM1.value = "000010";
                        break;
                    case 3:
                        regDefs.CURR_LIM1.value = "000011";
                        break;
                    case 4:
                        regDefs.CURR_LIM1.value = "000100";
                        break;
                    case 5:
                        regDefs.CURR_LIM1.value = "000101";
                        break;
                    case 6:
                        regDefs.CURR_LIM1.value = "000110";
                        break;
                    case 7:
                        regDefs.CURR_LIM1.value = "000111";
                        break;
                    case 8:
                        regDefs.CURR_LIM1.value = "001000";
                        break;
                    case 9:
                        regDefs.CURR_LIM1.value = "001001";
                        break;
                    case 10:
                        regDefs.CURR_LIM1.value = "001010";
                        break;
                    case 11:
                        regDefs.CURR_LIM1.value = "001011";
                        break;
                    case 12:
                        regDefs.CURR_LIM1.value = "001100";
                        break;
                    case 13:
                        regDefs.CURR_LIM1.value = "001101";
                        break;
                    case 14:
                        regDefs.CURR_LIM1.value = "001110";
                        break;
                    case 15:
                        regDefs.CURR_LIM1.value = "001111";
                        break;
                    case 16:
                        regDefs.CURR_LIM1.value = "010000";
                        break;
                    case 17:
                        regDefs.CURR_LIM1.value = "010001";
                        break;
                    case 18:
                        regDefs.CURR_LIM1.value = "010010";
                        break;
                    case 19:
                        regDefs.CURR_LIM1.value = "010011";
                        break;
                    case 20:
                        regDefs.CURR_LIM1.value = "010100";
                        break;
                    case 21:
                        regDefs.CURR_LIM1.value = "010101";
                        break;
                    case 22:
                        regDefs.CURR_LIM1.value = "010110";
                        break;
                    case 23:
                        regDefs.CURR_LIM1.value = "010111";
                        break;
                    case 24:
                        regDefs.CURR_LIM1.value = "011000";
                        break;
                    case 25:
                        regDefs.CURR_LIM1.value = "011001";
                        break;
                    case 26:
                        regDefs.CURR_LIM1.value = "011010";
                        break;
                    case 27:
                        regDefs.CURR_LIM1.value = "011011";
                        break;
                    case 28:
                        regDefs.CURR_LIM1.value = "011100";
                        break;
                    case 29:
                        regDefs.CURR_LIM1.value = "011101";
                        break;
                    case 30:
                        regDefs.CURR_LIM1.value = "011110";
                        break;
                    case 31:
                        regDefs.CURR_LIM1.value = "011111";
                        break;
                    case 32:
                        regDefs.CURR_LIM1.value = "100000";
                        break;
                    case 33:
                        regDefs.CURR_LIM1.value = "100001";
                        break;
                    case 34:
                        regDefs.CURR_LIM1.value = "100010";
                        break;
                    case 35:
                        regDefs.CURR_LIM1.value = "100011";
                        break;
                    case 36:
                        regDefs.CURR_LIM1.value = "100100";
                        break;
                    case 37:
                        regDefs.CURR_LIM1.value = "100101";
                        break;
                    case 38:
                        regDefs.CURR_LIM1.value = "100110";
                        break;
                    case 39:
                        regDefs.CURR_LIM1.value = "100111";
                        break;
                    case 40:
                        regDefs.CURR_LIM1.value = "101000";
                        break;
                    case 41:
                        regDefs.CURR_LIM1.value = "101001";
                        break;
                    case 42:
                        regDefs.CURR_LIM1.value = "101010";
                        break;
                    case 43:
                        regDefs.CURR_LIM1.value = "101011";
                        break;
                    case 44:
                        regDefs.CURR_LIM1.value = "101100";
                        break;
                    case 45:
                        regDefs.CURR_LIM1.value = "101101";
                        break;
                    case 46:
                        regDefs.CURR_LIM1.value = "101110";
                        break;
                    case 47:
                        regDefs.CURR_LIM1.value = "101111";
                        break;
                    case 48:
                        regDefs.CURR_LIM1.value = "110000";
                        break;
                    case 49:
                        regDefs.CURR_LIM1.value = "110001";
                        break;
                    case 50:
                        regDefs.CURR_LIM1.value = "110010";
                        break;
                    case 51:
                        regDefs.CURR_LIM1.value = "110011";
                        break;
                    case 52:
                        regDefs.CURR_LIM1.value = "110100";
                        break;
                    case 53:
                        regDefs.CURR_LIM1.value = "110101";
                        break;
                    case 54:
                        regDefs.CURR_LIM1.value = "110110";
                        break;
                    case 55:
                        regDefs.CURR_LIM1.value = "110111";
                        break;
                    case 56:
                        regDefs.CURR_LIM1.value = "111000";
                        break;
                    case 57:
                        regDefs.CURR_LIM1.value = "111001";
                        break;
                    case 58:
                        regDefs.CURR_LIM1.value = "111010";
                        break;
                    case 59:
                        regDefs.CURR_LIM1.value = "111011";
                        break;
                    case 60:
                        regDefs.CURR_LIM1.value = "111100";
                        break;
                    case 61:
                        regDefs.CURR_LIM1.value = "111101";
                        break;
                    case 62:
                        regDefs.CURR_LIM1.value = "111110";
                        break;
                    case 63:
                        regDefs.CURR_LIM1.value = "111111";
                        break;
                }
                regDefs.CURR_LIM_P1.WriteToUART();
                if (primaryTab.SelectedTab.Name != "MemMap" && !firstTime)
                {
                    string[,] array = new string[1, 5];
                    array[0, 0] = regDefs.CURR_LIM_P1.location.ToString();
                    array[0, 1] = "20 (CURR_LIM_P1)";
                    array[0, 4] = Tools.int32_Into_stringBase16(calRegValfromFields(regDefs.CURR_LIM_P1.bit_fields));
                    updateMemoryMap(array, "GRID_USER_MEMSPACE");
                }
            }
            bd_currentLimCombo.SelectedIndex = p1DriveCombo.SelectedIndex;
        }

        private void runTCIBtn_Click(object sender, EventArgs e)
        {
            if (PGA46xStat_box.Text.Contains("Ready") || freqCombo.Text != "")
            {
                txtToken = false;
                tciCommandCombo_SelectedIndexChanged(null, null);
                if (tciCommandCombo.SelectedIndex == 4)
                {
                    activateProgressBar(true);
                    int num = 0;
                    tciReadCount = 0;
                    while (tciReadCount < num + 1)
                    {
                        try
                        {
                            common.u2a.SendCommand(41, run_buffer, (byte)run_buffer.Length);
                            if (run_buffer[2] == 12)
                            {
                                Tools.timeDelay(400, "MS");
                            }
                            else
                            {
                                Tools.timeDelay(10, "MS");
                            }
                            if (plotTCICheck.Checked)
                            {
                                printTCI();
                            }
                            parseTCI();
                        }
                        catch
                        {
                            activateProgressBar(false);
                            return;
                        }
                        tciReadCount++;
                    }
                    if (run_buffer[2] == 12)
                    {
                        Array.Clear(tci_return_buf_all_dump, 0, tci_return_buf_all_dump.Length);
                    }
                    if (DlSysDiagTCICheck.Checked)
                    {
                        DlSysDiagTCIFlag = true;
                        readSysDiagBtn_Click(null, null);
                        DlSysDiagTCIFlag = false;
                    }
                    activateProgressBar(false);
                }
                else
                {
                    for (int i = 0; i < (int)Convert.ToInt16(tciLoopCountBox.Text); i++)
                    {
                        if (tciLoopDelayBox.Text == "")
                        {
                            tciLoopDelayBox.Text = "0";
                        }
                        Tools.timeDelay(Convert.ToDouble(tciLoopDelayBox.Text), "MS");
                        common.u2a.SendCommand(41, run_buffer, (byte)run_buffer.Length);
                        Tools.timeDelay(10, "MS");
                        tciLoopCountInd.Text = Convert.ToString(i + 1);
                        if (exportTCICheck.Checked || plotTCICheck.Checked)
                        {
                            printTCI();
                        }
                        else
                        {
                            Tools.timeDelay(70, "MS");
                        }
                        logTCIEcho(i);
                    }
                }
                tciLoopCountInd.Text = "0";
            }
            else
            {
                MessageBox.Show("No PGA460-Q1 device connected.");
            }
        }

        public void logTCIEcho(int loopCount)
        {
            int num = tciChart.Series[0].Points.Count<DataPoint>();
            int num2 = 0;
            double num3 = 0.0;
            bool flag = false;
            double num4 = 0.0;
            int num5 = 0;
            tciEchoO1D.Text = "";
            tciEchoO1W.Text = "";
            tciEchoO2D.Text = "";
            tciEchoO2W.Text = "";
            tciEchoO3D.Text = "";
            tciEchoO3W.Text = "";
            tciEchoO4D.Text = "";
            tciEchoO4W.Text = "";
            for (int i = 0; i < num; i++)
            {
                int num6 = (int)tciChart.Series[0].Points[i].YValues[0];
                if (num6 == 0 && num2 == 1)
                {
                    num4 = tciChart.Series[0].Points[i].XValue;
                    num3 = tciChart.Series[1].Points[i].XValue;
                    if (num3 > 0.0)
                    {
                        flag = true;
                    }
                }
                else if (num6 == 1 && num2 == 0)
                {
                    if (flag)
                    {
                        double xvalue = tciChart.Series[0].Points[i].XValue;
                        num3 = Math.Round(num3 + 0.0686, 3);
                        switch (num5)
                        {
                            case 0:
                                tciEchoO1D.Text = num3.ToString();
                                break;
                            case 1:
                                tciEchoO2D.Text = num3.ToString();
                                break;
                            case 2:
                                tciEchoO3D.Text = num3.ToString();
                                break;
                            case 3:
                                tciEchoO4D.Text = num3.ToString();
                                break;
                        }
                        switch (num5)
                        {
                            case 0:
                                tciEchoO1W.Text = Convert.ToString(Math.Round((xvalue - num4) * 1000.0));
                                break;
                            case 1:
                                tciEchoO2W.Text = Convert.ToString(Math.Round((xvalue - num4) * 1000.0));
                                break;
                            case 2:
                                tciEchoO3W.Text = Convert.ToString(Math.Round((xvalue - num4) * 1000.0));
                                break;
                            case 3:
                                tciEchoO4W.Text = Convert.ToString(Math.Round((xvalue - num4) * 1000.0));
                                break;
                        }
                        num5++;
                        flag = false;
                    }
                }
                num2 = num6;
            }
            if (tciEchoObjNum.Value >= 1m)
            {
                if (tciEchoO1D.Text == "")
                {
                    tciEchoO1D.Text = "NA";
                    tciEchoO1W.Text = "NA";
                }
            }
            if (tciEchoObjNum.Value >= 2m)
            {
                if (tciEchoO2D.Text == "")
                {
                    tciEchoO2D.Text = "NA";
                    tciEchoO2W.Text = "NA";
                }
            }
            if (tciEchoObjNum.Value >= 3m)
            {
                if (tciEchoO3D.Text == "")
                {
                    tciEchoO3D.Text = "NA";
                    tciEchoO3W.Text = "NA";
                }
            }
            if (tciEchoObjNum.Value >= 4m)
            {
                if (tciEchoO4D.Text == "")
                {
                    tciEchoO4D.Text = "NA";
                    tciEchoO4W.Text = "NA";
                }
            }
            if (tciEchoDLCheck.Checked)
            {
                if (loopCount == 0)
                {
                    datalogTextBox.AppendText("TCI Echo: \r\n(" + DateTime.Now.ToString("yy-MM-dd_HHmmss").ToString() + ")\r\n");
                }
                if (tciEchoObjNum.Value >= 1m)
                {
                    datalogTextBox.AppendText(string.Concat(new string[]
                    {
                        "D1(m): ",
                        tciEchoO1D.Text,
                        "; W1(us): ",
                        tciEchoO1W.Text,
                        "\r\n"
                    }));
                }
                if (tciEchoObjNum.Value >= 2m)
                {
                    datalogTextBox.AppendText(string.Concat(new string[]
                    {
                        "D2(m): ",
                        tciEchoO2D.Text,
                        "; W2(us): ",
                        tciEchoO2W.Text,
                        "\r\n"
                    }));
                }
                if (tciEchoObjNum.Value >= 3m)
                {
                    datalogTextBox.AppendText(string.Concat(new string[]
                    {
                        "D3(m): ",
                        tciEchoO3D.Text,
                        "; W3(us): ",
                        tciEchoO3W.Text,
                        "\r\n"
                    }));
                }
                if (tciEchoObjNum.Value >= 4m)
                {
                    datalogTextBox.AppendText(string.Concat(new string[]
                    {
                        "D4(m): ",
                        tciEchoO4D.Text,
                        "; W4(us): ",
                        tciEchoO4W.Text,
                        "\r\n"
                    }));
                }
            }
        }

        public void parseTCI()
        {
            int num = 0;
            int num2 = 0;
            bool flag = false;
            string[] array = new string[1080];
            string[] array2 = new string[3200];
            int num3 = 0;
            if (run_buffer[0] == 4 && run_buffer[2] == 12)
            {
                for (int i = 0; i < 3200; i++)
                {
                    for (int j = 0; j < 8; j++)
                    {
                        if (((int)tci_return_buf_all0[i] << j & 128) == 0)
                        {
                            num++;
                        }
                        else
                        {
                            num2++;
                            if (num > 0)
                            {
                                flag = true;
                            }
                        }
                        if (flag)
                        {
                            if (num > 12)
                            {
                                array2[num3] = "0";
                            }
                            else
                            {
                                array2[num3] = "1";
                            }
                            num3++;
                            num = 0;
                            num2 = 0;
                            flag = false;
                        }
                    }
                    if (num2 > 128)
                    {
                    }
                }
            }
            else
            {
                for (int i = 0; i < 3200; i++)
                {
                    for (int j = 0; j < 8; j++)
                    {
                        if (((int)tci_return_buf_all[i] << j & 128) == 0)
                        {
                            num++;
                        }
                        else
                        {
                            num2++;
                            if (num > 0)
                            {
                                flag = true;
                            }
                        }
                        if (flag)
                        {
                            if (num > 12)
                            {
                                array[num3] = "0";
                            }
                            else
                            {
                                array[num3] = "1";
                            }
                            num3++;
                            num = 0;
                            num2 = 0;
                            flag = false;
                        }
                    }
                    if (num2 > 128)
                    {
                        break;
                    }
                }
            }
            int num4 = 0;
            if (run_buffer[2] == 12)
            {
                tciReturnBox.Text = "(see chart)";
                if (contClearCheck.Checked)
                {
                    tciI12Chart.Series[0].Points.Clear();
                }
                if (contClearCheck.Checked)
                {
                    dumpChart.Series[11].Points.Clear();
                }
                tciI12Chart.Series[0].Points.AddXY(0.0, 255.0);
                dumpChart.Series[11].Points.AddXY(0.0, 255.0);
                for (int k = 0; k < (array2.Length - 8) / 8; k++)
                {
                    string text = "";
                    for (int l = 0; l < 8; l++)
                    {
                        text += array2[k * 8 + l];
                    }
                    try
                    {
                        if (graphTCIDumpCheck.Checked)
                        {
                            tciI12Chart.Series[0].Points.AddXY((double)(k + 1), (double)Tools.StringBase2_into_Byte(text));
                            dumpChart.Series[11].Points.AddXY((double)(k + 1), (double)Tools.StringBase2_into_Byte(text));
                        }
                    }
                    catch
                    {
                        num4++;
                    }
                }
                tciReturnBox.Text = "";
                tciReturnBox.Text = tciReturnBox.Text + " " + num4;
                num4 = 0;
            }
            else
            {
                tciReturnBox.Text = string.Join("", array);
                if (!tciWriteInProcess)
                {
                    try
                    {
                        switch (run_buffer[2])
                        {
                            case 0:
                                partI12StringToPGAVar(ind0TempR, 8, array, 0);
                                break;
                            case 1:
                                partI12StringToPGAVar(ind1FDiagR, 8, array, 0);
                                partI12StringToPGAVar(ind1DDiagR, 8, array, 8);
                                partI12StringToPGAVar(ind1NDiagR, 8, array, 16);
                                break;
                            case 2:
                                partI12StringToPGAVar(ind2FreqR, 8, array, 0);
                                break;
                            case 3:
                                partI12StringToPGAVar(ind3P1PR, 5, array, 0);
                                partI12StringToPGAVar(ind3P2PR, 5, array, 5);
                                partI12StringToPGAVar(ind3TCDR, 4, array, 10);
                                partI12StringToPGAVar(ind3PDeadR, 4, array, 14);
                                break;
                            case 4:
                                partI12StringToPGAVar(ind4P1RR, 4, array, 0);
                                partI12StringToPGAVar(ind4P2RR, 4, array, 4);
                                break;
                            case 5:
                                break;
                            case 6:
                                break;
                            case 7:
                                partI12StringToPGAVar(ind7BPFBR, 2, array, 0);
                                partI12StringToPGAVar(ind7InitGainR, 6, array, 2);
                                partI12StringToPGAVar(ind7LPFCOR, 2, array, 8);
                                partI12StringToPGAVar(ind7NLSNLR, 5, array, 10);
                                partI12StringToPGAVar(ind7NLSER, 1, array, 15);
                                partI12StringToPGAVar(ind7NLSOR, 2, array, 16);
                                partI12StringToPGAVar(ind7TSGR, 4, array, 18);
                                partI12StringToPGAVar(ind7TSOR, 4, array, 22);
                                partI12StringToPGAVar(ind7P1DGSTR, 2, array, 26);
                                partI12StringToPGAVar(ind7P1DGLRR, 3, array, 28);
                                partI12StringToPGAVar(ind7P1DGSRR, 3, array, 31);
                                partI12StringToPGAVar(ind7P2DGSTR, 2, array, 33);
                                partI12StringToPGAVar(ind7P2DGLRR, 3, array, 36);
                                partI12StringToPGAVar(ind7P2DGSRR, 3, array, 39);
                                break;
                            case 8:
                                break;
                            case 9:
                                partI12StringToPGAVar(ind9U1R, 8, array, 0);
                                partI12StringToPGAVar(ind9U2R, 8, array, 8);
                                partI12StringToPGAVar(ind9U3R, 8, array, 16);
                                partI12StringToPGAVar(ind9U4R, 8, array, 24);
                                partI12StringToPGAVar(ind9U5R, 8, array, 32);
                                partI12StringToPGAVar(ind9U6R, 8, array, 40);
                                partI12StringToPGAVar(ind9U7R, 8, array, 48);
                                partI12StringToPGAVar(ind9U8R, 8, array, 56);
                                partI12StringToPGAVar(ind9U9R, 8, array, 64);
                                partI12StringToPGAVar(ind9U10R, 8, array, 72);
                                partI12StringToPGAVar(ind9U11R, 8, array, 80);
                                partI12StringToPGAVar(ind9U12R, 8, array, 88);
                                partI12StringToPGAVar(ind9U13R, 8, array, 96);
                                partI12StringToPGAVar(ind9U14R, 8, array, 104);
                                partI12StringToPGAVar(ind9U15R, 8, array, 112);
                                partI12StringToPGAVar(ind9U16R, 8, array, 120);
                                partI12StringToPGAVar(ind9U17R, 8, array, 128);
                                partI12StringToPGAVar(ind9U18R, 8, array, 136);
                                partI12StringToPGAVar(ind9U19R, 8, array, 144);
                                partI12StringToPGAVar(ind9U20R, 8, array, 152);
                                break;
                            case 10:
                                partI12StringToPGAVar(ind10FDLR, 4, array, 0);
                                partI12StringToPGAVar(ind10FDSR, 4, array, 4);
                                partI12StringToPGAVar(ind10FDETR, 3, array, 8);
                                partI12StringToPGAVar(ind10STR, 4, array, 11);
                                partI12StringToPGAVar(ind10P1NLSER, 1, array, 15);
                                partI12StringToPGAVar(ind10P2NLSER, 1, array, 16);
                                partI12StringToPGAVar(ind10VOTR, 2, array, 17);
                                partI12StringToPGAVar(ind10LPMTR, 2, array, 19);
                                partI12StringToPGAVar(ind10FETR, 3, array, 21);
                                partI12StringToPGAVar(ind10AGRR, 2, array, 24);
                                partI12StringToPGAVar(ind10LPMER, 1, array, 26);
                                partI12StringToPGAVar(ind10DSR, 1, array, 27);
                                partI12StringToPGAVar(ind10DTR, 4, array, 28);
                                partI12StringToPGAVar(ind10DCLR, 1, array, 32);
                                partI12StringToPGAVar(ind10DIMR, 1, array, 33);
                                partI12StringToPGAVar(ind10P1ILimR, 6, array, 34);
                                partI12StringToPGAVar(ind10P2ILimR, 6, array, 40);
                                break;
                            case 11:
                                partI12StringToPGAVar(ind11EDDER, 1, array, 0);
                                partI12StringToPGAVar(ind11EEPPR, 4, array, 1);
                                partI12StringToPGAVar(ind11EEPSR, 1, array, 5);
                                partI12StringToPGAVar(ind11ReloadEER, 1, array, 6);
                                partI12StringToPGAVar(ind11PgrmEER, 1, array, 7);
                                break;
                            case 12:
                                break;
                            case 13:
                                {
                                    string[] addressGridList = GRID_USER_MEMSPACE.getAddressGridList();
                                    GRID_USER_MEMSPACE.HighlightRows(addressGridList);
                                    string[,] array3 = GRID_USER_MEMSPACE.Get_HIGHLIGHTED_Grid_Values__R_D_A_O(false);
                                    int numberOfHighlighedRows = GRID_USER_MEMSPACE.getNumberOfHighlighedRows();
                                    SelectedGrid = "GRID_USER_MEMSPACE";
                                    desel_grid_butt_Click(null, null);
                                    for (int i = 0; i < 43; i++)
                                    {
                                        array3[i, 2] = partI12StringToString(array3[i, 2], 8, array, i * 8);
                                        array3[i, 4] = array3[i, 2];
                                    }
                                    GRID_USER_MEMSPACE.setDataIntoGridDataArray(array3, false);
                                    GRID_USER_MEMSPACE.saveAllValues();
                                    break;
                                }
                            case 14:
                                break;
                            case 15:
                                partI12StringToPGAVar(ind15EECRCR, 8, array, 0);
                                partI12StringToPGAVar(ind15ThrCRCR, 8, array, 8);
                                break;
                            default:
                                return;
                        }
                        tciFailLoop = 0;
                    }
                    catch
                    {
                        tciReturnBox.Text = "TCI Read Fail. Retry... ";
                    }
                }
                else
                {
                    tciReturnBox.Text = "TCI Write Executed";
                    tciWriteInProcess = false;
                }
                if (tciFailLoop >= 5)
                {
                    tciFailLoop = 0;
                }
            }
        }

        private void partI12StringToPGAVar(TextBox indInX, byte varSize, string[] tciReturnFinal, int bitOffset)
        {
            indInX.HideSelection = false;
            byte b = mergeBitstoByte(tciReturnFinal, (int)varSize + bitOffset, indInX, bitOffset);
            indInX.SelectionStart = indInX.GetFirstCharIndexFromLine((int)b);
            indInX.SelectionLength = indInX.Lines[(int)b].Length;
            indInX.ScrollToCaret();
        }

        private string partI12StringToString(string preference, byte varSize, string[] tciReturnFinal, int bitOffset)
        {
            return Tools.int32_Into_stringBase16((int)mergeBitstoByteString(tciReturnFinal, (int)varSize + bitOffset, bitOffset));
        }

        public byte mergeBitstoByte(string[] tciReturnFinal, int indexVarSize, TextBox textBox, int bitOffset)
        {
            string text = "";
            if (indexVarSize - bitOffset < 8)
            {
                for (int i = 0; i < 8 - (indexVarSize - bitOffset); i++)
                {
                    text += 0;
                }
            }
            for (int j = bitOffset; j < indexVarSize; j++)
            {
                text += tciReturnFinal[j];
            }
            return Tools.StringBase2_into_Byte(text);
        }

        public byte mergeBitstoByteString(string[] tciReturnFinal, int indexVarSize, int bitOffset)
        {
            string text = "";
            if (indexVarSize - bitOffset < 8)
            {
                for (int i = 0; i < 8 - (indexVarSize - bitOffset); i++)
                {
                    text += 0;
                }
            }
            for (int j = bitOffset; j < indexVarSize; j++)
            {
                text += tciReturnFinal[j];
            }
            return Tools.StringBase2_into_Byte(text);
        }

        public void tciStatCalc(int pointCount)
        {
            if (pointCount < 9 && pointCount > 5)
            {
                TCI1_Stat_TextBox.Text = "0";
                TCI2_Stat_TextBox.Text = "0";
                TCI3_Stat_TextBox.Text = "0";
            }
            if (pointCount < 17 && pointCount > 13)
            {
                TCI1_Stat_TextBox.Text = "1";
                TCI2_Stat_TextBox.Text = "0";
                TCI3_Stat_TextBox.Text = "0";
            }
            if (pointCount < 25 && pointCount > 21)
            {
                TCI1_Stat_TextBox.Text = "0";
                TCI2_Stat_TextBox.Text = "1";
                TCI3_Stat_TextBox.Text = "0";
            }
            if (pointCount < 33 && pointCount > 29)
            {
                TCI1_Stat_TextBox.Text = "0";
                TCI2_Stat_TextBox.Text = "0";
                TCI3_Stat_TextBox.Text = "1";
            }
        }

        public void printTCI()
        {
            Array.Clear(tci_return_buf_all, 0, tci_return_buf_all.Length);
            Array.Clear(tci_return_buf, 0, tci_return_buf.Length);
            bool flag = false;
            if (run_buffer[0] == 0 || run_buffer[0] == 1 || run_buffer[0] == 2 || run_buffer[0] == 3)
            {
                int num;
                switch (run_buffer[0])
                {
                    case 0:
                        num = p1RecordCombo.SelectedIndex;
                        break;
                    case 1:
                        num = p2RecordCombo.SelectedIndex;
                        break;
                    case 2:
                        num = p1RecordCombo.SelectedIndex;
                        break;
                    case 3:
                        num = p2RecordCombo.SelectedIndex;
                        break;
                    default:
                        num = 60;
                        break;
                }
                num++;
                Array.Clear(tci_return_buf, 0, tci_return_buf.Length);
                byte b = 0;
                while (b < num)
                {
                    tci_return_buf[0] = 85;
                    tci_return_buf[1] = 85;
                    tci_return_buf[2] = 85;
                    tci_return_buf[3] = b;
                    common.u2a.SendCommand(41, tci_return_buf, 54);
                    Tools.timeDelay(66, "MS");
                    int commandResponse = common.u2a.GetCommandResponse(41, tci_return_buf, 54);
                    tci_return_buf.CopyTo(tci_return_buf_all, (int)(54 * b));
                    Array.Clear(tci_return_buf, 0, tci_return_buf.Length);
                    b += 1;
                }
                if (flag)
                {
                    if (Convert.ToInt32(tciLoopCountBox.Text) == Convert.ToInt32(tciLoopCountInd.Text))
                    {
                        if (!infLoopSet)
                        {
                        }
                        return;
                    }
                    infErrorFlag = true;
                }
                if (runBtn.Text == "START")
                {
                    tciChart.Series[0].Points.Clear();
                    tciChart.Series[1].Points.Clear();
                    double num2 = 0.0;
                    int num3 = 0;
                    bool flag2 = false;
                    double num4;
                    if (tciCommandCombo.SelectedIndex == 1 || tciCommandCombo.SelectedIndex == 3)
                    {
                        num4 = Convert.ToDouble(p2RecordCombo.Text) / 1000.0 / 0.0001;
                    }
                    else
                    {
                        num4 = Convert.ToDouble(p1RecordCombo.Text) / 1000.0 / 0.0001;
                    }
                    int i = 0;
                    while ((double)i < num4)
                    {
                        int num5 = 7;
                        for (int j = 0; j < 8; j++)
                        {
                            if (plotTCICheck.Checked)
                            {
                                tciChart.Series[0].Points.AddXY(Math.Round((double)(8 * i + j) * 0.0125, 2) - 0.4, (double)Tools.convert_byte_to_ArrayOfBits(tci_return_buf_all[i])[num5]);
                            }
                            if (exportTCICheck.Checked)
                            {
                                valuesToExport.Add(Tools.convert_byte_to_ArrayOfBits(tci_return_buf_all[i])[num5].ToString());
                            }
                            num5--;
                        }
                        if (!flag2)
                        {
                            double num6 = tciChart.Series[0].Points[i].YValues[0];
                            if (num2 == 1.0 && num6 == 1.0)
                            {
                                num3++;
                            }
                            else if (num2 == 1.0 && num6 == 0.0)
                            {
                                flag2 = true;
                                tciStatCalc(num3);
                                num3 = 0;
                            }
                            num2 = num6;
                        }
                        i++;
                    }
                    if (plotTCICheck.Checked)
                    {
                        int num7 = 0;
                        while ((double)num7 < num4 * 8.0)
                        {
                            tciChart.Series[1].Points.AddXY((double)num7 * 1.25E-05 * 343.0 / 2.0 - 0.008575 - 0.145775, -1.0);
                            num7++;
                        }
                    }
                    if (tciTimeStampCheck.Checked)
                    {
                        valuesToExport.Add(DateTime.Now.ToString("MM\\/dd\\/yyyy h\\:mm\\:ss\\:ff tt"));
                    }
                }
                Tools.timeDelay(1, "MS");
            }
            else if (run_buffer[0] == 4 && run_buffer[1] == 0)
            {
                for (int k = 0; k < 60; k++)
                {
                    tci_return_buf[0] = 85;
                    tci_return_buf[1] = run_buffer[1];
                    tci_return_buf[2] = run_buffer[2];
                    tci_return_buf[3] = (byte)k;
                    common.u2a.SendCommand(41, tci_return_buf, 54);
                    Tools.timeDelay(10, "MS");
                    int commandResponse = common.u2a.GetCommandResponse(41, tci_return_buf, 54);
                    if (run_buffer[2] == 12)
                    {
                        if (tciReadCount == 0)
                        {
                            tci_return_buf.CopyTo(tci_return_buf_all0, 54 * k);
                        }
                    }
                    else if ((k == 0 && tci_return_buf[0] == 85) || (k == 0 && tci_return_buf[1] == 85) || (k == 0 && tci_return_buf[2] == 85))
                    {
                        k = -1;
                    }
                    else
                    {
                        tci_return_buf.CopyTo(tci_return_buf_all, 54 * k);
                    }
                    Array.Clear(tci_return_buf, 0, tci_return_buf.Length);
                }
                tciChart.Series[0].Points.Clear();
                tciChart.Series[1].Points.Clear();
                int num8;
                switch (run_buffer[2])
                {
                    case 0:
                        num8 = 56;
                        break;
                    case 1:
                        num8 = 104;
                        break;
                    case 2:
                        num8 = 56;
                        break;
                    case 3:
                        num8 = 86;
                        break;
                    case 4:
                        num8 = 56;
                        break;
                    case 5:
                        num8 = 404;
                        break;
                    case 6:
                        num8 = 404;
                        break;
                    case 7:
                        num8 = 158;
                        break;
                    case 8:
                        num8 = 200;
                        break;
                    case 9:
                        num8 = 512;
                        break;
                    case 10:
                        num8 = 170;
                        break;
                    case 11:
                        num8 = 56;
                        break;
                    case 12:
                        num8 = 3104;
                        break;
                    case 13:
                        num8 = 1088;
                        break;
                    case 14:
                        num8 = 56;
                        break;
                    case 15:
                        num8 = 80;
                        break;
                    default:
                        return;
                }
                if (run_buffer[2] == 12)
                {
                    if (num8 > 3239)
                    {
                        num8 = 3239;
                    }
                    for (int i = 0; i < num8; i++)
                    {
                        int num5 = 7;
                        for (int j = 0; j < 8; j++)
                        {
                            tciChart.Series[0].Points.AddXY(Math.Round((double)(8 * i + j) * 0.0125, 2), (double)Tools.convert_byte_to_ArrayOfBits(tci_return_buf_all0[i])[num5]);
                            num5--;
                        }
                    }
                }
                else
                {
                    if (num8 > 1079)
                    {
                        num8 = 1079;
                    }
                    for (int i = 0; i < num8; i++)
                    {
                        int num5 = 7;
                        for (int j = 0; j < 8; j++)
                        {
                            tciChart.Series[0].Points.AddXY(Math.Round((double)(8 * i + j) * 0.0125, 2), (double)Tools.convert_byte_to_ArrayOfBits(tci_return_buf_all[i])[num5]);
                            num5--;
                        }
                    }
                }
                Tools.timeDelay(1, "MS");
            }
            else if (run_buffer[0] == 4 && run_buffer[1] == 1)
            {
            }
            if ((exportTCICheck.Checked && Convert.ToInt16(tciLoopCountBox.Text) == Convert.ToInt16(tciLoopCountInd.Text) && tciLoopInfCheck.Text == "Run Infinitely") || (!tciLoopInfCheck.Checked && txtToken && exportTCICheck.Checked))
            {
                string[] array = new string[valuesToExport.Count];
                array = valuesToExport.ConvertAll<string>((string x) => x.ToString()).ToArray();
                Files files = new Files();
                string fileNameDOTextenstion = files.CreateFileName("TCI", "", exportSaveAs, true, true, false);
                files.WriteArrayToFile("TCI", array, Environment.GetFolderPath(Environment.SpecialFolder.Personal) + "\\BOOSTXL-PGA460\\", fileNameDOTextenstion);
                files.Dispose();
                valuesToExport.Clear();
            }
        }

        private void dataMonitorCheckListBox_SelectedIndexChanged(object sender, EventArgs e)
        {
            if (dataMonitorCheckListBox.GetItemChecked(0))
            {
                thrP1PlotAddOnFlag = true;
                updateThresholdChart();
            }
            else
            {
                thrP1PlotAddOnFlag = false;
                updateThresholdChart();
            }
            if (dataMonitorCheckListBox.GetItemChecked(1))
            {
                thrP2PlotAddOnFlag = true;
                updateThresholdChart();
            }
            else
            {
                thrP2PlotAddOnFlag = false;
                updateThresholdChart();
            }
            if (dataMonitorCheckListBox.GetItemChecked(2))
            {
                tvgPlotAddOnFlag = true;
                updateTVGChart();
            }
            else
            {
                tvgPlotAddOnFlag = false;
                updateTVGChart();
            }
            if (dataMonitorCheckListBox.GetItemChecked(3))
            {
                nlsPlotAddOnFlag = true;
                updateNLSChart();
            }
            else
            {
                nlsPlotAddOnFlag = false;
                updateNLSChart();
            }
            if (dataMonitorCheckListBox.GetItemChecked(4))
            {
                dgPlotAddOnFlag = true;
                updateDGChart();
            }
            else
            {
                dgPlotAddOnFlag = false;
                updateDGChart();
            }
            if (dataMonitorCheckListBox.GetItemChecked(5))
            {
                dumpChart.Legends[0].Enabled = true;
            }
            else
            {
                dumpChart.Legends[0].Enabled = false;
            }
        }

        private void updateDGChart()
        {
            if (dgPlotAddOnFlag)
            {
                dumpChart.Series[12].Points.Clear();
                if (p1DigGainLr.SelectedIndex != 0 && p1DigGainLr.Text != "")
                {
                    double num = (double)(Convert.ToInt32(p1t1.Text) + Convert.ToInt32(p1t2.Text) + Convert.ToInt32(p1t3.Text) + Convert.ToInt32(p1t4.Text) + Convert.ToInt32(p1t5.Text) + Convert.ToInt32(p1t6.Text) + Convert.ToInt32(p1t7.Text) + Convert.ToInt32(p1t8.Text));
                    if (p1DigGainLrSt.SelectedIndex == 0)
                    {
                        ;
                    }
                    else if (p1DigGainLrSt.SelectedIndex == 1)
                    {
                        num += (double)Convert.ToInt32(p1t9.Text);
                    }
                    else if (p1DigGainLrSt.SelectedIndex == 1)
                    {
                        num = num + (double)Convert.ToInt32(p1t9.Text) + (double)Convert.ToInt32(p1t10.Text);
                    }
                    else
                    {
                        num = num + (double)Convert.ToInt32(p1t9.Text) + (double)Convert.ToInt32(p1t10.Text) + (double)Convert.ToInt32(p1t11.Text);
                    }
                    dumpChart.Series[12].Points.AddXY(num / 1000000.0 * 344.0 / 2.0, 0.0);
                    dumpChart.Series[12].Points.AddXY(num / 1000000.0 * 343.0 / 2.0, 256.0);
                }
                if (p2DigGainLr.SelectedIndex != 0 && p2DigGainLr.Text != "")
                {
                    double num = (double)(Convert.ToInt32(p2t1.Text) + Convert.ToInt32(p2t2.Text) + Convert.ToInt32(p2t3.Text) + Convert.ToInt32(p2t4.Text) + Convert.ToInt32(p2t5.Text) + Convert.ToInt32(p2t6.Text) + Convert.ToInt32(p2t7.Text) + Convert.ToInt32(p2t8.Text));
                    if (p2DigGainLrSt.SelectedIndex == 0)
                    {
                        ;
                    }
                    else if (p2DigGainLrSt.SelectedIndex == 1)
                    {
                        num += (double)Convert.ToInt32(p2t9.Text);
                    }
                    else if (p2DigGainLrSt.SelectedIndex == 1)
                    {
                        num = num + (double)Convert.ToInt32(p2t9.Text) + (double)Convert.ToInt32(p2t10.Text);
                    }
                    else
                    {
                        num = num + (double)Convert.ToInt32(p2t9.Text) + (double)Convert.ToInt32(p2t10.Text) + (double)Convert.ToInt32(p2t11.Text);
                    }
                    dumpChart.Series[12].Points.AddXY(num / 1000000.0 * 344.0 / 2.0, 256.0);
                    dumpChart.Series[12].Points.AddXY(num / 1000000.0 * 343.0 / 2.0, 0.0);
                }
                dumpChart.Series[12].LegendText = string.Concat(new string[]
                {
                    "DG:P1",
                    Convert.ToString(p1DigGainSr.Text),
                    Convert.ToString(p1DigGainLr.Text),
                    ";P2",
                    Convert.ToString(p2DigGainSr.Text),
                    Convert.ToString(p2DigGainLr.Text)
                });
            }
            else
            {
                dumpChart.Series[12].Points.Clear();
            }
        }

        private void eepromCRCBtn_Click(object sender, EventArgs e)
        {
            string[] addressGridList = GRID_USER_MEMSPACE.getAddressGridList();
            GRID_USER_MEMSPACE.HighlightRows(addressGridList);
            string[,] array = GRID_USER_MEMSPACE.Get_HIGHLIGHTED_Grid_Values__R_D_A_O(false);
            int numberOfHighlighedRows = GRID_USER_MEMSPACE.getNumberOfHighlighedRows();
            SelectedGrid = "GRID_USER_MEMSPACE";
            desel_grid_butt_Click(null, null);
            byte b = byte.MaxValue;
            byte[] array2 = new byte[43];
            for (int i = 0; i < 43; i++)
            {
                array2[i] = Tools.StringBase16IntoByte(array[i, 2].Substring(0, 2));
            }
            CRC8Calc crc8Calc = new CRC8Calc(CRC8_POLY.CRC8_CCITT);
            for (int i = 0; i < array2.GetLength(0); i++)
            {
                b = crc8Calc.table[(int)(array2[i] ^ b)];
            }
            string str = b.ToString("X");
            globalUserCRC = Tools.Byte_into_StringBase2(b);
            calculatedEEPROMCRCText.Text = "0x" + str;
            if (autoCRCBox.Checked)
            {
                UART_Read_Write(43, b, true);
            }
        }

        private void calcAppendChecksumBtn_Click(object sender, EventArgs e)
        {
            char[] separator = new char[]
            {
                ' ',
                ',',
                '.',
                ':',
                '\t'
            };
            string[] array = MSRichTextBox.Text.Split(separator);
            byte[] array2 = new byte[array.Length];
            for (int i = 0; i < array.Length; i++)
            {
                if (array[i] == "," || array[i] == "." || array[i] == ":" || array[i] == "\t")
                {
                    array[i] = " ";
                }
                array2[i] = Tools.StringBase16IntoByte(array[i]);
            }
            MChecksumByte = calculate_UART_Checksum(array2);
            Array.Clear(uart_send_data, 0, 64);
            uart_send_data[0] = syncByte;
            array2.CopyTo(uart_send_data, 1);
            uart_send_data[(int)((byte)(array2.Length + 1))] = MChecksumByte;
            uartSendLength = (byte)(array2.Length + 2);
            MSChecksumRich.Text = ConvertStringArrayToString(array) + Convert.ToString(Tools.StringBase2_Into_StringBase16(Tools.Byte_into_StringBase2(MChecksumByte)));
        }

        private static string ConvertStringArrayToString(string[] array)
        {
            StringBuilder stringBuilder = new StringBuilder();
            foreach (string value in array)
            {
                stringBuilder.Append(value);
                stringBuilder.Append(' ');
            }
            return stringBuilder.ToString();
        }

        private static string ConvertStringArrayToStringJoin(string[] array)
        {
            return string.Join(" ", array);
        }

        private void eepromThrCRCBtn_Click(object sender, EventArgs e)
        {
            string[] addressGridList = GRID_USER_MEMSPACE.getAddressGridList();
            GRID_USER_MEMSPACE.HighlightRows(addressGridList);
            string[,] array = GRID_USER_MEMSPACE.Get_HIGHLIGHTED_Grid_Values__R_D_A_O(false);
            int numberOfHighlighedRows = GRID_USER_MEMSPACE.getNumberOfHighlighedRows();
            SelectedGrid = "GRID_USER_MEMSPACE";
            desel_grid_butt_Click(null, null);
            byte b = byte.MaxValue;
            byte[] array2 = new byte[32];
            for (int i = 0; i < 32; i++)
            {
                array2[i] = Tools.StringBase16IntoByte(array[i + 58, 2].Substring(0, 2));
            }
            CRC8Calc crc8Calc = new CRC8Calc(CRC8_POLY.CRC8_CCITT);
            for (int i = 0; i < array2.GetLength(0); i++)
            {
                b = crc8Calc.table[(int)(array2[i] ^ b)];
            }
            string str = b.ToString("X");
            globalThrCRC = Tools.Byte_into_StringBase2(b);
            calculatedThrEEPROMCRCText.Text = "0x" + str;
            if (autoCRCBox.Checked)
            {
                UART_Read_Write(124, b, true);
            }
        }

        private void decoupletimeRadio_CheckedChanged(object sender, EventArgs e)
        {
            if (!firstTime)
            {
                RegDefs regDefs = new RegDefs("26 (DECPL_TEMP)");
                regDefs.DECPL_TEMP.ReadFromUART();
                if (!decoupletimeRadio.Checked)
                {
                    regDefs.DECPL_TEMP_SEL.value = "1";
                }
                else
                {
                    regDefs.DECPL_TEMP_SEL.value = "0";
                }
                regDefs.DECPL_TEMP.WriteToUART();
                if (primaryTab.SelectedTab.Name != "MemMap" && !firstTime)
                {
                    string[,] array = new string[1, 5];
                    array[0, 0] = regDefs.DECPL_TEMP.location.ToString();
                    array[0, 1] = "26 (DECPL_TEMP)";
                    array[0, 4] = Tools.int32_Into_stringBase16(calRegValfromFields(regDefs.DECPL_TEMP.bit_fields));
                    updateMemoryMap(array, "GRID_USER_MEMSPACE");
                }
            }
        }

        private void decoupletempRadio_CheckedChanged(object sender, EventArgs e)
        {
            decoupletimeRadio_CheckedChanged(null, null);
        }

        private void freqDiagWinLengthCombo_SelectedIndexChanged(object sender, EventArgs e)
        {
            if (!firstTime)
            {
                RegDefs regDefs = new RegDefs("23 (FREQ_DIAG)");
                regDefs.FREQ_DIAG.ReadFromUART();
                switch (freqDiagWinLengthCombo.SelectedIndex)
                {
                    case 0:
                        regDefs.FDIAG_LEN.value = "0000";
                        break;
                    case 1:
                        regDefs.FDIAG_LEN.value = "0001";
                        break;
                    case 2:
                        regDefs.FDIAG_LEN.value = "0010";
                        break;
                    case 3:
                        regDefs.FDIAG_LEN.value = "0011";
                        break;
                    case 4:
                        regDefs.FDIAG_LEN.value = "0100";
                        break;
                    case 5:
                        regDefs.FDIAG_LEN.value = "0101";
                        break;
                    case 6:
                        regDefs.FDIAG_LEN.value = "0110";
                        break;
                    case 7:
                        regDefs.FDIAG_LEN.value = "0111";
                        break;
                    case 8:
                        regDefs.FDIAG_LEN.value = "1000";
                        break;
                    case 9:
                        regDefs.FDIAG_LEN.value = "1001";
                        break;
                    case 10:
                        regDefs.FDIAG_LEN.value = "1010";
                        break;
                    case 11:
                        regDefs.FDIAG_LEN.value = "1011";
                        break;
                    case 12:
                        regDefs.FDIAG_LEN.value = "1100";
                        break;
                    case 13:
                        regDefs.FDIAG_LEN.value = "1101";
                        break;
                    case 14:
                        regDefs.FDIAG_LEN.value = "1110";
                        break;
                    case 15:
                        regDefs.FDIAG_LEN.value = "1111";
                        break;
                }
                regDefs.FREQ_DIAG.WriteToUART();
                if (primaryTab.SelectedTab.Name != "MemMap" && !firstTime)
                {
                    string[,] array = new string[1, 5];
                    array[0, 0] = regDefs.FREQ_DIAG.location.ToString();
                    array[0, 1] = "23 (FREQ_DIAG)";
                    array[0, 4] = Tools.int32_Into_stringBase16(calRegValfromFields(regDefs.FREQ_DIAG.bit_fields));
                    updateMemoryMap(array, "GRID_USER_MEMSPACE");
                }
            }
        }

        private void freqDiagStartTimeCombo_SelectedIndexChanged(object sender, EventArgs e)
        {
            if (!firstTime)
            {
                RegDefs regDefs = new RegDefs("23 (FREQ_DIAG)");
                regDefs.FREQ_DIAG.ReadFromUART();
                switch (freqDiagStartTimeCombo.SelectedIndex)
                {
                    case 0:
                        regDefs.FDIAG_START.value = "0000";
                        break;
                    case 1:
                        regDefs.FDIAG_START.value = "0001";
                        break;
                    case 2:
                        regDefs.FDIAG_START.value = "0010";
                        break;
                    case 3:
                        regDefs.FDIAG_START.value = "0011";
                        break;
                    case 4:
                        regDefs.FDIAG_START.value = "0100";
                        break;
                    case 5:
                        regDefs.FDIAG_START.value = "0101";
                        break;
                    case 6:
                        regDefs.FDIAG_START.value = "0110";
                        break;
                    case 7:
                        regDefs.FDIAG_START.value = "0111";
                        break;
                    case 8:
                        regDefs.FDIAG_START.value = "1000";
                        break;
                    case 9:
                        regDefs.FDIAG_START.value = "1001";
                        break;
                    case 10:
                        regDefs.FDIAG_START.value = "1010";
                        break;
                    case 11:
                        regDefs.FDIAG_START.value = "1011";
                        break;
                    case 12:
                        regDefs.FDIAG_START.value = "1100";
                        break;
                    case 13:
                        regDefs.FDIAG_START.value = "1101";
                        break;
                    case 14:
                        regDefs.FDIAG_START.value = "1110";
                        break;
                    case 15:
                        regDefs.FDIAG_START.value = "1111";
                        break;
                }
                regDefs.FREQ_DIAG.WriteToUART();
                if (primaryTab.SelectedTab.Name != "MemMap" && !firstTime)
                {
                    string[,] array = new string[1, 5];
                    array[0, 0] = regDefs.FREQ_DIAG.location.ToString();
                    array[0, 1] = "23 (FREQ_DIAG)";
                    array[0, 4] = Tools.int32_Into_stringBase16(calRegValfromFields(regDefs.FREQ_DIAG.bit_fields));
                    updateMemoryMap(array, "GRID_USER_MEMSPACE");
                }
            }
        }

        private void freqDiagErrorTimeThrCombo_SelectedIndexChanged(object sender, EventArgs e)
        {
            if (!firstTime)
            {
                RegDefs regDefs = new RegDefs("24 (SAT_FDIAG_TH)");
                regDefs.SAT_FDIAG_TH.ReadFromUART();
                switch (freqDiagErrorTimeThrCombo.SelectedIndex)
                {
                    case 0:
                        regDefs.FDIAG_ERR_TH.value = "000";
                        break;
                    case 1:
                        regDefs.FDIAG_ERR_TH.value = "001";
                        break;
                    case 2:
                        regDefs.FDIAG_ERR_TH.value = "010";
                        break;
                    case 3:
                        regDefs.FDIAG_ERR_TH.value = "011";
                        break;
                    case 4:
                        regDefs.FDIAG_ERR_TH.value = "100";
                        break;
                    case 5:
                        regDefs.FDIAG_ERR_TH.value = "101";
                        break;
                    case 6:
                        regDefs.FDIAG_ERR_TH.value = "110";
                        break;
                    case 7:
                        regDefs.FDIAG_ERR_TH.value = "111";
                        break;
                }
                regDefs.SAT_FDIAG_TH.WriteToUART();
                if (primaryTab.SelectedTab.Name != "MemMap" && !firstTime)
                {
                    string[,] array = new string[1, 5];
                    array[0, 0] = regDefs.SAT_FDIAG_TH.location.ToString();
                    array[0, 1] = "24 (SAT_FDIAG_TH)";
                    array[0, 4] = Tools.int32_Into_stringBase16(calRegValfromFields(regDefs.SAT_FDIAG_TH.bit_fields));
                    updateMemoryMap(array, "GRID_USER_MEMSPACE");
                }
            }
            freqErrTimeFreqEquivCalc();
        }

        public void freqErrTimeFreqEquivCalc()
        {
            try
            {
                freqDiagETTFreqText.Text = Convert.ToString(Math.Round(double.Parse(centerFreqText.Text) - 1.0 / (1.0 / double.Parse(centerFreqText.Text) + (double)(freqDiagErrorTimeThrCombo.SelectedIndex + 1) * 0.001), 2));
            }
            catch
            {
            }
        }

        private void voltaDiagErrThrCombo_SelectedIndexChanged(object sender, EventArgs e)
        {
            if (!firstTime)
            {
                RegDefs regDefs = new RegDefs("25 (FVOLT_DEC)");
                regDefs.FVOLT_DEC.ReadFromUART();
                switch (voltaDiagErrThrCombo.SelectedIndex)
                {
                    case 0:
                        regDefs.FVOLT_ERR_TH.value = "000";
                        break;
                    case 1:
                        regDefs.FVOLT_ERR_TH.value = "001";
                        break;
                    case 2:
                        regDefs.FVOLT_ERR_TH.value = "010";
                        break;
                    case 3:
                        regDefs.FVOLT_ERR_TH.value = "011";
                        break;
                    case 4:
                        regDefs.FVOLT_ERR_TH.value = "100";
                        break;
                    case 5:
                        regDefs.FVOLT_ERR_TH.value = "101";
                        break;
                    case 6:
                        regDefs.FVOLT_ERR_TH.value = "110";
                        break;
                    case 7:
                        regDefs.FVOLT_ERR_TH.value = "111";
                        break;
                }
                regDefs.FVOLT_DEC.WriteToUART();
                if (primaryTab.SelectedTab.Name != "MemMap" && !firstTime)
                {
                    string[,] array = new string[1, 5];
                    array[0, 0] = regDefs.FVOLT_DEC.location.ToString();
                    array[0, 1] = "25 (FVOLT_DEC)";
                    array[0, 4] = Tools.int32_Into_stringBase16(calRegValfromFields(regDefs.FVOLT_DEC.bit_fields));
                    updateMemoryMap(array, "GRID_USER_MEMSPACE");
                }
            }
            vdiagThBox_TextChanged(null, null);
        }

        private void forceVoltDiagONRadio_CheckedChanged(object sender, EventArgs e)
        {
        }

        private void forceVoltDiagOFFRadio_CheckedChanged(object sender, EventArgs e)
        {
        }

        private void vDiagPermDisableRadio_CheckedChanged(object sender, EventArgs e)
        {
        }

        private void lowpowEnterTimeCombo_SelectedIndexChanged(object sender, EventArgs e)
        {
            if (!firstTime && !tmrChangeFlag)
            {
                RegDefs regDefs = new RegDefs("25 (FVOLT_DEC)");
                regDefs.FVOLT_DEC.ReadFromUART();
                switch (lowpowEnterTimeCombo.SelectedIndex)
                {
                    case 0:
                        regDefs.LPM_TMR.value = "00";
                        break;
                    case 1:
                        regDefs.LPM_TMR.value = "01";
                        break;
                    case 2:
                        regDefs.LPM_TMR.value = "10";
                        break;
                    case 3:
                        regDefs.LPM_TMR.value = "11";
                        break;
                }
                regDefs.FVOLT_DEC.WriteToUART();
                if (primaryTab.SelectedTab.Name != "MemMap" && !firstTime)
                {
                    string[,] array = new string[1, 5];
                    array[0, 0] = regDefs.FVOLT_DEC.location.ToString();
                    array[0, 1] = "25 (FVOLT_DEC)";
                    array[0, 4] = Tools.int32_Into_stringBase16(calRegValfromFields(regDefs.FVOLT_DEC.bit_fields));
                    updateMemoryMap(array, "GRID_USER_MEMSPACE");
                }
            }
            tmrChangeFlag = true;
            idleMdCombo.SelectedIndex = lowpowEnterTimeCombo.SelectedIndex;
            tmrChangeFlag = false;
        }

        private void lowpowEnCheck_CheckedChanged(object sender, EventArgs e)
        {
            if (!firstTime)
            {
                RegDefs regDefs = new RegDefs("26 (DECPL_TEMP)");
                regDefs.DECPL_TEMP.ReadFromUART();
                if (lowpowEnCheck.Checked)
                {
                    regDefs.LPM_EN.value = "1";
                }
                else
                {
                    regDefs.LPM_EN.value = "0";
                }
                regDefs.DECPL_TEMP.WriteToUART();
                if (primaryTab.SelectedTab.Name != "MemMap" && !firstTime)
                {
                    string[,] array = new string[1, 5];
                    array[0, 0] = regDefs.DECPL_TEMP.location.ToString();
                    array[0, 1] = "26 (DECPL_TEMP)";
                    array[0, 4] = Tools.int32_Into_stringBase16(calRegValfromFields(regDefs.DECPL_TEMP.bit_fields));
                    updateMemoryMap(array, "GRID_USER_MEMSPACE");
                }
            }
        }

        private void ovthrCombo_SelectedIndexChanged_1(object sender, EventArgs e)
        {
            if (!firstTime)
            {
                RegDefs regDefs = new RegDefs("25 (FVOLT_DEC)");
                regDefs.FVOLT_DEC.ReadFromUART();
                switch (ovthrCombo.SelectedIndex)
                {
                    case 0:
                        regDefs.VPWR_OV_TH.value = "00";
                        break;
                    case 1:
                        regDefs.VPWR_OV_TH.value = "01";
                        break;
                    case 2:
                        regDefs.VPWR_OV_TH.value = "10";
                        break;
                    case 3:
                        regDefs.VPWR_OV_TH.value = "11";
                        break;
                }
                regDefs.FVOLT_DEC.WriteToUART();
                if (primaryTab.SelectedTab.Name != "MemMap" && !firstTime)
                {
                    string[,] array = new string[1, 5];
                    array[0, 0] = regDefs.FVOLT_DEC.location.ToString();
                    array[0, 1] = "25 (FVOLT_DEC)";
                    array[0, 4] = Tools.int32_Into_stringBase16(calRegValfromFields(regDefs.FVOLT_DEC.bit_fields));
                    updateMemoryMap(array, "GRID_USER_MEMSPACE");
                }
            }
        }

        private void satDiagThrLvlCombo_SelectedIndexChanged(object sender, EventArgs e)
        {
            if (!firstTime)
            {
                RegDefs regDefs = new RegDefs("24 (SAT_FDIAG_TH)");
                regDefs.SAT_FDIAG_TH.ReadFromUART();
                switch (satDiagThrLvlCombo.SelectedIndex)
                {
                    case 0:
                        regDefs.SAT_TH.value = "0000";
                        break;
                    case 1:
                        regDefs.SAT_TH.value = "0001";
                        break;
                    case 2:
                        regDefs.SAT_TH.value = "0010";
                        break;
                    case 3:
                        regDefs.SAT_TH.value = "0011";
                        break;
                    case 4:
                        regDefs.SAT_TH.value = "0100";
                        break;
                    case 5:
                        regDefs.SAT_TH.value = "0101";
                        break;
                    case 6:
                        regDefs.SAT_TH.value = "0110";
                        break;
                    case 7:
                        regDefs.SAT_TH.value = "0111";
                        break;
                    case 8:
                        regDefs.SAT_TH.value = "1000";
                        break;
                    case 9:
                        regDefs.SAT_TH.value = "1001";
                        break;
                    case 10:
                        regDefs.SAT_TH.value = "1010";
                        break;
                    case 11:
                        regDefs.SAT_TH.value = "1011";
                        break;
                    case 12:
                        regDefs.SAT_TH.value = "1100";
                        break;
                    case 13:
                        regDefs.SAT_TH.value = "1101";
                        break;
                    case 14:
                        regDefs.SAT_TH.value = "1110";
                        break;
                    case 15:
                        regDefs.SAT_TH.value = "1111";
                        break;
                }
                regDefs.SAT_FDIAG_TH.WriteToUART();
                if (primaryTab.SelectedTab.Name != "MemMap" && !firstTime)
                {
                    string[,] array = new string[1, 5];
                    array[0, 0] = regDefs.SAT_FDIAG_TH.location.ToString();
                    array[0, 1] = "24 (SAT_FDIAG_TH)";
                    array[0, 4] = Tools.int32_Into_stringBase16(calRegValfromFields(regDefs.SAT_FDIAG_TH.bit_fields));
                    updateMemoryMap(array, "GRID_USER_MEMSPACE");
                }
            }
        }

        private void uartDiagUARTRadio_CheckedChanged(object sender, EventArgs e)
        {
            if (!firstTime)
            {
                RegDefs regDefs = new RegDefs("1E (PULSE_P1)");
                regDefs.PULSE_P1.ReadFromUART();
                if (!uartDiagUARTRadio.Checked)
                {
                    regDefs.UART_DIAG.value = "1";
                }
                else
                {
                    regDefs.UART_DIAG.value = "0";
                }
                regDefs.PULSE_P1.WriteToUART();
                if (primaryTab.SelectedTab.Name != "MemMap" && !firstTime)
                {
                    string[,] array = new string[1, 5];
                    array[0, 0] = regDefs.PULSE_P1.location.ToString();
                    array[0, 1] = "1E (PULSE_P1)";
                    array[0, 4] = Tools.int32_Into_stringBase16(calRegValfromFields(regDefs.PULSE_P1.bit_fields));
                    updateMemoryMap(array, "GRID_USER_MEMSPACE");
                }
            }
        }

        private void uartDiagSysRadio_CheckedChanged(object sender, EventArgs e)
        {
            uartDiagUARTRadio_CheckedChanged(null, null);
        }

        private void lvlTranEn_CheckedChanged(object sender, EventArgs e)
        {
            if (lvlTranEn.Checked)
            {
                common.u2a.GPIO_SetPort(0, 1);
                common.u2a.GPIO_WritePort(0, 2);
            }
            else
            {
                common.u2a.GPIO_SetPort(0, 1);
                common.u2a.GPIO_WritePort(0, 1);
            }
        }

        private void grnLedCheck_CheckedChanged(object sender, EventArgs e)
        {
            if (grnLedCheck.Checked)
            {
                common.u2a.GPIO_SetPort(1, 1);
                common.u2a.GPIO_WritePort(1, 2);
            }
            else
            {
                common.u2a.GPIO_SetPort(1, 1);
                common.u2a.GPIO_WritePort(1, 1);
            }
        }

        private void uartAddrCombo_SelectedIndexChanged(object sender, EventArgs e)
        {
            uartAddrComboText = uartAddrCombo.Text;
            if (!firstTime)
            {
                RegDefs regDefs = new RegDefs("1F (PULSE_P2)");
                if (pgrmUARTCheck.Checked)
                {
                    changeUartAddr = true;
                }
                regDefs.PULSE_P2.ReadFromUART();
                switch (uartAddrCombo.SelectedIndex)
                {
                    case 0:
                        regDefs.UART_ADDR.value = "000";
                        break;
                    case 1:
                        regDefs.UART_ADDR.value = "001";
                        break;
                    case 2:
                        regDefs.UART_ADDR.value = "010";
                        break;
                    case 3:
                        regDefs.UART_ADDR.value = "011";
                        break;
                    case 4:
                        regDefs.UART_ADDR.value = "100";
                        break;
                    case 5:
                        regDefs.UART_ADDR.value = "101";
                        break;
                    case 6:
                        regDefs.UART_ADDR.value = "110";
                        break;
                    case 7:
                        regDefs.UART_ADDR.value = "111";
                        break;
                }
                if (pgrmUARTCheck.Checked)
                {
                    regDefs.PULSE_P2.WriteToUART();
                }
                changeUartAddr = false;
                if (primaryTab.SelectedTab.Name != "MemMap" && !firstTime)
                {
                    string[,] array = new string[1, 5];
                    array[0, 0] = regDefs.PULSE_P2.location.ToString();
                    array[0, 1] = "1F (PULSE_P2)";
                    array[0, 4] = Tools.int32_Into_stringBase16(calRegValfromFields(regDefs.PULSE_P2.bit_fields));
                    updateMemoryMap(array, "GRID_USER_MEMSPACE");
                }
                if (pgrmUARTCheck.Checked)
                {
                    uartAddrOld = uartAddrCombo.Text;
                }
            }
        }

        private void tbiRadio_CheckedChanged(object sender, EventArgs e)
        {
            if (!firstTime)
            {
                RegDefs regDefs = new RegDefs("1E (PULSE_P1)");
                regDefs.PULSE_P1.ReadFromUART();
                if (!tbiRadio.Checked)
                {
                    regDefs.IO_IF_SEL.value = "1";
                }
                else
                {
                    regDefs.IO_IF_SEL.value = "0";
                }
                regDefs.PULSE_P1.WriteToUART();
                if (primaryTab.SelectedTab.Name != "MemMap" && !firstTime)
                {
                    string[,] array = new string[1, 5];
                    array[0, 0] = regDefs.PULSE_P1.location.ToString();
                    array[0, 1] = "1E (PULSE_P1)";
                    array[0, 4] = Tools.int32_Into_stringBase16(calRegValfromFields(regDefs.PULSE_P1.bit_fields));
                    updateMemoryMap(array, "GRID_USER_MEMSPACE");
                }
            }
        }

        private void owuRadio_CheckedChanged(object sender, EventArgs e)
        {
            tbiRadio_CheckedChanged(null, null);
            if (owuRadio.Checked && comTabControl.SelectedIndex == 1)
            {
                DialogResult dialogResult = MessageBox.Show("Configure the EVM hardware for OWU mode in addition to the bit update?\r\nNote: GUI will not operate correctly in OWU hardware mode.", "OWU Hardware", MessageBoxButtons.YesNo, MessageBoxIcon.Question);
                if (dialogResult == DialogResult.Yes)
                {
                    common.u2a.GPIO_SetPort(1, 1);
                    common.u2a.GPIO_WritePort(1, 2);
                    enableOWUSynchronousHardwareToolStripMenuItem.Text = "Disable OWU && Synchronous Hardware";
                }
            }
            else
            {
                common.u2a.GPIO_SetPort(1, 1);
                common.u2a.GPIO_WritePort(1, 1);
                enableOWUSynchronousHardwareToolStripMenuItem.Text = "Enable OWU && Synchronous Hardware";
            }
        }

        private void ioTransEnCheck_CheckedChanged(object sender, EventArgs e)
        {
            if (!firstTime)
            {
                RegDefs regDefs = new RegDefs("1E (PULSE_P1)");
                regDefs.PULSE_P1.ReadFromUART();
                if (ioTransEnCheck.Checked)
                {
                    regDefs.IO_DIS.value = "0";
                }
                else
                {
                    regDefs.IO_DIS.value = "1";
                }
                regDefs.PULSE_P1.WriteToUART();
                if (primaryTab.SelectedTab.Name != "MemMap" && !firstTime)
                {
                    string[,] array = new string[1, 5];
                    array[0, 0] = regDefs.PULSE_P1.location.ToString();
                    array[0, 1] = "1E (PULSE_P1)";
                    array[0, 4] = Tools.int32_Into_stringBase16(calRegValfromFields(regDefs.PULSE_P1.bit_fields));
                    updateMemoryMap(array, "GRID_USER_MEMSPACE");
                }
            }
        }

        private void p1Radio_CheckedChanged(object sender, EventArgs e)
        {
            byteLSBRadio.Enabled = false;
            byteMSBRadio.Checked = true;
            updateTVGChart();
        }

        private void p2Radio_CheckedChanged(object sender, EventArgs e)
        {
            byteLSBRadio.Enabled = true;
            updateTVGChart();
        }

        private void p1t1_SelectedIndexChanged(object sender, EventArgs e)
        {
            pXtlXMask = (ulong)((long)p1t1.SelectedIndex);
            p1TimeMain = ((p1TimeMain & 18446480190918885375UL) | (pXtlXMask << 44 & ulong.MaxValue));
            readyThrAutoUpdate();
        }

        private void p1t2_SelectedIndexChanged(object sender, EventArgs e)
        {
            pXtlXMask = (ulong)((long)p1t2.SelectedIndex);
            p1TimeMain = ((p1TimeMain & 18446727581035134975UL) | (pXtlXMask << 40 & ulong.MaxValue));
            readyThrAutoUpdate();
        }

        private void p1t3_SelectedIndexChanged(object sender, EventArgs e)
        {
            pXtlXMask = (ulong)((long)p1t3.SelectedIndex);
            p1TimeMain = ((p1TimeMain & 18446743042917400575UL) | (pXtlXMask << 36 & ulong.MaxValue));
            readyThrAutoUpdate();
        }

        private void p1t4_SelectedIndexChanged(object sender, EventArgs e)
        {
            pXtlXMask = (ulong)((long)p1t4.SelectedIndex);
            p1TimeMain = ((p1TimeMain & 18446744009285042175UL) | (pXtlXMask << 32 & ulong.MaxValue));
            readyThrAutoUpdate();
        }

        private void p1t5_SelectedIndexChanged(object sender, EventArgs e)
        {
            pXtlXMask = (ulong)((long)p1t5.SelectedIndex);
            p1TimeMain = ((p1TimeMain & 18446744069683019775UL) | (pXtlXMask << 28 & ulong.MaxValue));
            readyThrAutoUpdate();
        }

        private void p1t6_SelectedIndexChanged(object sender, EventArgs e)
        {
            pXtlXMask = (ulong)((long)p1t6.SelectedIndex);
            p1TimeMain = ((p1TimeMain & 18446744073457893375UL) | (pXtlXMask << 24 & ulong.MaxValue));
            readyThrAutoUpdate();
        }

        private void p1t7_SelectedIndexChanged(object sender, EventArgs e)
        {
            pXtlXMask = (ulong)((long)p1t7.SelectedIndex);
            p1TimeMain = ((p1TimeMain & 18446744073693822975UL) | (pXtlXMask << 20 & ulong.MaxValue));
            readyThrAutoUpdate();
        }

        private void p1t8_SelectedIndexChanged(object sender, EventArgs e)
        {
            pXtlXMask = (ulong)((long)p1t8.SelectedIndex);
            p1TimeMain = ((p1TimeMain & 18446744073708568575UL) | (pXtlXMask << 16 & ulong.MaxValue));
            readyThrAutoUpdate();
        }

        private void p1t9_SelectedIndexChanged(object sender, EventArgs e)
        {
            pXtlXMask = (ulong)((long)p1t9.SelectedIndex);
            p1TimeMain = ((p1TimeMain & 18446744073709490175UL) | (pXtlXMask << 12 & ulong.MaxValue));
            readyThrAutoUpdate();
        }

        private void p1t10_SelectedIndexChanged(object sender, EventArgs e)
        {
            pXtlXMask = (ulong)((long)p1t10.SelectedIndex);
            p1TimeMain = ((p1TimeMain & 18446744073709547775UL) | (pXtlXMask << 8 & ulong.MaxValue));
            readyThrAutoUpdate();
        }

        private void p1t11_SelectedIndexChanged(object sender, EventArgs e)
        {
            pXtlXMask = (ulong)((long)p1t11.SelectedIndex);
            p1TimeMain = ((p1TimeMain & 18446744073709551375UL) | (pXtlXMask << 4 & ulong.MaxValue));
            readyThrAutoUpdate();
        }

        private void p1t12_SelectedIndexChanged(object sender, EventArgs e)
        {
            pXtlXMask = (ulong)((long)p1t12.SelectedIndex);
            p1TimeMain = ((p1TimeMain & 18446744073709551600UL) | (pXtlXMask & ulong.MaxValue));
            readyThrAutoUpdate();
        }

        private void p1l1_SelectedIndexChanged(object sender, EventArgs e)
        {
            pXtlXMask = (ulong)((long)p1l1.SelectedIndex);
            p1LevelMain = ((p1LevelMain & 576460752303423487UL) | (pXtlXMask << 59 & ulong.MaxValue));
            readyThrAutoUpdate();
        }

        private void p1l2_SelectedIndexChanged(object sender, EventArgs e)
        {
            pXtlXMask = (ulong)((long)p1l2.SelectedIndex);
            p1LevelMain = ((p1LevelMain & 17888297719915610111UL) | (pXtlXMask << 54 & ulong.MaxValue));
            readyThrAutoUpdate();
        }

        private void p1l3_SelectedIndexChanged(object sender, EventArgs e)
        {
            pXtlXMask = (ulong)((long)p1l3.SelectedIndex);
            p1LevelMain = ((p1LevelMain & 18429292625153490943UL) | (pXtlXMask << 49 & ulong.MaxValue));
            readyThrAutoUpdate();
        }

        private void p1l4_SelectedIndexChanged(object sender, EventArgs e)
        {
            pXtlXMask = (ulong)((long)p1l4.SelectedIndex);
            p1LevelMain = ((p1LevelMain & 18446198715942174719UL) | (pXtlXMask << 44 & ulong.MaxValue));
            readyThrAutoUpdate();
        }

        private void p1l5_SelectedIndexChanged(object sender, EventArgs e)
        {
            pXtlXMask = (ulong)((long)p1l5.SelectedIndex);
            p1LevelMain = ((p1LevelMain & 18446727031279321087UL) | (pXtlXMask << 39 & ulong.MaxValue));
            readyThrAutoUpdate();
        }

        private void p1l6_SelectedIndexChanged(object sender, EventArgs e)
        {
            pXtlXMask = (ulong)((long)p1l6.SelectedIndex);
            p1LevelMain = ((p1LevelMain & 18446743541133606911UL) | (pXtlXMask << 34 & ulong.MaxValue));
            readyThrAutoUpdate();
        }

        private void p1l7_SelectedIndexChanged(object sender, EventArgs e)
        {
            pXtlXMask = (ulong)((long)p1l7.SelectedIndex);
            p1LevelMain = ((p1LevelMain & 18446744057066553343UL) | (pXtlXMask << 29 & ulong.MaxValue));
            readyThrAutoUpdate();
        }

        private void p1l8_SelectedIndexChanged(object sender, EventArgs e)
        {
            pXtlXMask = (ulong)((long)p1l8.SelectedIndex);
            p1LevelMain = ((p1LevelMain & 18446744073189457919UL) | (pXtlXMask << 24 & ulong.MaxValue));
            readyThrAutoUpdate();
        }

        private void p1l9_SelectedIndexChanged(object sender, EventArgs e)
        {
            readyThrAutoUpdate();
        }

        private void p1l10_SelectedIndexChanged(object sender, EventArgs e)
        {
            readyThrAutoUpdate();
        }

        private void p1l11_SelectedIndexChanged(object sender, EventArgs e)
        {
            readyThrAutoUpdate();
        }

        private void p1l12_SelectedIndexChanged(object sender, EventArgs e)
        {
            readyThrAutoUpdate();
        }

        private void p1lOff_SelectedIndexChanged(object sender, EventArgs e)
        {
            pXtlXMask = (ulong)((long)p1lOff.SelectedIndex);
            p1LevelMain = ((p1LevelMain & 18446744073709551584UL) | (pXtlXMask & ulong.MaxValue));
            dumpChart.ChartAreas[0].AxisY.Minimum = 0.0;
            readyThrAutoUpdate();
        }

        private void p2t1_SelectedIndexChanged(object sender, EventArgs e)
        {
            pXtlXMask = (ulong)((long)p2t1.SelectedIndex);
            p2TimeMain = ((p2TimeMain & 18446480190918885375UL) | (pXtlXMask << 44 & ulong.MaxValue));
            readyThrAutoUpdate();
        }

        private void p2t2_SelectedIndexChanged(object sender, EventArgs e)
        {
            pXtlXMask = (ulong)((long)p2t2.SelectedIndex);
            p2TimeMain = ((p2TimeMain & 18446727581035134975UL) | (pXtlXMask << 40 & ulong.MaxValue));
            readyThrAutoUpdate();
        }

        private void p2t3_SelectedIndexChanged(object sender, EventArgs e)
        {
            pXtlXMask = (ulong)((long)p2t3.SelectedIndex);
            p2TimeMain = ((p2TimeMain & 18446743042917400575UL) | (pXtlXMask << 36 & ulong.MaxValue));
            readyThrAutoUpdate();
        }

        private void p2t4_SelectedIndexChanged(object sender, EventArgs e)
        {
            pXtlXMask = (ulong)((long)p2t4.SelectedIndex);
            p2TimeMain = ((p2TimeMain & 18446744009285042175UL) | (pXtlXMask << 32 & ulong.MaxValue));
            readyThrAutoUpdate();
        }

        private void p2t5_SelectedIndexChanged(object sender, EventArgs e)
        {
            pXtlXMask = (ulong)((long)p2t5.SelectedIndex);
            p2TimeMain = ((p2TimeMain & 18446744069683019775UL) | (pXtlXMask << 28 & ulong.MaxValue));
            readyThrAutoUpdate();
        }

        private void p2t6_SelectedIndexChanged(object sender, EventArgs e)
        {
            pXtlXMask = (ulong)((long)p2t6.SelectedIndex);
            p2TimeMain = ((p2TimeMain & 18446744073457893375UL) | (pXtlXMask << 24 & ulong.MaxValue));
            readyThrAutoUpdate();
        }

        private void p2t7_SelectedIndexChanged(object sender, EventArgs e)
        {
            pXtlXMask = (ulong)((long)p2t7.SelectedIndex);
            p2TimeMain = ((p2TimeMain & 18446744073693822975UL) | (pXtlXMask << 20 & ulong.MaxValue));
            readyThrAutoUpdate();
        }

        private void p2t8_SelectedIndexChanged(object sender, EventArgs e)
        {
            pXtlXMask = (ulong)((long)p2t8.SelectedIndex);
            p2TimeMain = ((p2TimeMain & 18446744073708568575UL) | (pXtlXMask << 16 & ulong.MaxValue));
            readyThrAutoUpdate();
        }

        private void p2t9_SelectedIndexChanged(object sender, EventArgs e)
        {
            pXtlXMask = (ulong)((long)p2t9.SelectedIndex);
            p2TimeMain = ((p2TimeMain & 18446744073709490175UL) | (pXtlXMask << 12 & ulong.MaxValue));
            readyThrAutoUpdate();
        }

        private void p2t10_SelectedIndexChanged(object sender, EventArgs e)
        {
            pXtlXMask = (ulong)((long)p2t10.SelectedIndex);
            p2TimeMain = ((p2TimeMain & 18446744073709547775UL) | (pXtlXMask << 8 & ulong.MaxValue));
            readyThrAutoUpdate();
        }

        private void p2t11_SelectedIndexChanged(object sender, EventArgs e)
        {
            pXtlXMask = (ulong)((long)p2t11.SelectedIndex);
            p2TimeMain = ((p2TimeMain & 18446744073709551375UL) | (pXtlXMask << 4 & ulong.MaxValue));
            readyThrAutoUpdate();
        }

        private void p2t12_SelectedIndexChanged(object sender, EventArgs e)
        {
            pXtlXMask = (ulong)((long)p2t12.SelectedIndex);
            p2TimeMain = ((p2TimeMain & 18446744073709551600UL) | (pXtlXMask & ulong.MaxValue));
            readyThrAutoUpdate();
        }

        private void p2l1_SelectedIndexChanged(object sender, EventArgs e)
        {
            pXtlXMask = (ulong)((long)p2l1.SelectedIndex);
            p2LevelMain = ((p2LevelMain & 576460752303423487UL) | (pXtlXMask << 59 & ulong.MaxValue));
            readyThrAutoUpdate();
        }

        private void p2l2_SelectedIndexChanged(object sender, EventArgs e)
        {
            pXtlXMask = (ulong)((long)p2l2.SelectedIndex);
            p2LevelMain = ((p2LevelMain & 17888297719915610111UL) | (pXtlXMask << 54 & ulong.MaxValue));
            readyThrAutoUpdate();
        }

        private void p2l3_SelectedIndexChanged(object sender, EventArgs e)
        {
            pXtlXMask = (ulong)((long)p2l3.SelectedIndex);
            p2LevelMain = ((p2LevelMain & 18429292625153490943UL) | (pXtlXMask << 49 & ulong.MaxValue));
            readyThrAutoUpdate();
        }

        private void p2l4_SelectedIndexChanged(object sender, EventArgs e)
        {
            pXtlXMask = (ulong)((long)p2l4.SelectedIndex);
            p2LevelMain = ((p2LevelMain & 18446198715942174719UL) | (pXtlXMask << 44 & ulong.MaxValue));
            readyThrAutoUpdate();
        }

        private void p2l5_SelectedIndexChanged(object sender, EventArgs e)
        {
            pXtlXMask = (ulong)((long)p2l5.SelectedIndex);
            p2LevelMain = ((p2LevelMain & 18446727031279321087UL) | (pXtlXMask << 39 & ulong.MaxValue));
            readyThrAutoUpdate();
        }

        private void p2l6_SelectedIndexChanged(object sender, EventArgs e)
        {
            pXtlXMask = (ulong)((long)p2l6.SelectedIndex);
            p2LevelMain = ((p2LevelMain & 18446743541133606911UL) | (pXtlXMask << 34 & ulong.MaxValue));
            readyThrAutoUpdate();
        }

        private void p2l7_SelectedIndexChanged(object sender, EventArgs e)
        {
            pXtlXMask = (ulong)((long)p2l7.SelectedIndex);
            p2LevelMain = ((p2LevelMain & 18446744057066553343UL) | (pXtlXMask << 29 & ulong.MaxValue));
            readyThrAutoUpdate();
        }

        private void p2l8_SelectedIndexChanged(object sender, EventArgs e)
        {
            pXtlXMask = (ulong)((long)p2l8.SelectedIndex);
            p2LevelMain = ((p2LevelMain & 18446744073189457919UL) | (pXtlXMask << 24 & ulong.MaxValue));
            readyThrAutoUpdate();
        }

        private void p2l9_SelectedIndexChanged(object sender, EventArgs e)
        {
            readyThrAutoUpdate();
        }

        private void p2l10_SelectedIndexChanged(object sender, EventArgs e)
        {
            readyThrAutoUpdate();
        }

        private void p2l11_SelectedIndexChanged(object sender, EventArgs e)
        {
            readyThrAutoUpdate();
        }

        private void p2l12_SelectedIndexChanged(object sender, EventArgs e)
        {
            readyThrAutoUpdate();
        }

        private void p2lOff_SelectedIndexChanged(object sender, EventArgs e)
        {
            pXtlXMask = (ulong)((long)p2lOff.SelectedIndex);
            p2LevelMain = ((p2LevelMain & 18446744073709551584UL) | (pXtlXMask & ulong.MaxValue));
            dumpChart.ChartAreas[0].AxisY.Minimum = 0.0;
            readyThrAutoUpdate();
        }

        private void readyThrAutoUpdate()
        {
            thrReady = true;
        }

        public void thrWriteValuesBtn_Click(object sender, EventArgs e)
        {
            ulong num;
            ulong num2;
            int num3;
            if (tabcontrolThr.SelectedTab.Text == "Preset 1")
            {
                if (p1t1.Text == "" || p1t2.Text == "" || p1t3.Text == "" || p1t4.Text == "" || p1t5.Text == "" || p1t6.Text == "" || p1t7.Text == "" || p1t8.Text == "" || p1t9.Text == "" || p1t10.Text == "" || p1t11.Text == "" || p1t12.Text == "" || p1l1.Text == "" || p1t2.Text == "" || p1t3.Text == "" || p1t4.Text == "" || p1t5.Text == "" || p1t6.Text == "" || p1t7.Text == "" || p1t8.Text == "" || p1t9.Text == "" || p1l10.Text == "" || p1l11.Text == "" || p1l12.Text == "")
                {
                    if (PGA46xStat_box.Text.Contains("Ready"))
                    {
                        toolStripStatusLabelGUIError.Visible = true;
                        toolStripStatusLabelGUIError.Text = "Thr values cannot be blank";
                    }
                    return;
                }
                toolStripStatusLabelGUIError.Visible = false;
                activateProgressBar(true);
                num = p1TimeMain;
                num2 = p1LevelMain;
                num3 = 110;
                UART_Read_Write(Convert.ToByte(num3), Convert.ToByte(p1lOff.SelectedIndex), true);
                UART_Read_Write(Convert.ToByte(num3 - 1), Convert.ToByte(p1l12.SelectedIndex), true);
                UART_Read_Write(Convert.ToByte(num3 - 2), Convert.ToByte(p1l11.SelectedIndex), true);
                UART_Read_Write(Convert.ToByte(num3 - 3), Convert.ToByte(p1l10.SelectedIndex), true);
                UART_Read_Write(Convert.ToByte(num3 - 4), Convert.ToByte(p1l9.SelectedIndex), true);
                if (!thrUpdatedAtLeastOnce)
                {
                    thrUpdatedAtLeastOnce = true;
                }
            }
            else
            {
                if (!(tabcontrolThr.SelectedTab.Text == "Preset 2"))
                {
                    return;
                }
                if (p2t1.Text == "" || p2t2.Text == "" || p2t3.Text == "" || p2t4.Text == "" || p2t5.Text == "" || p2t6.Text == "" || p2t7.Text == "" || p2t8.Text == "" || p2t9.Text == "" || p2t10.Text == "" || p2t11.Text == "" || p2t12.Text == "" || p2l1.Text == "" || p2t2.Text == "" || p2t3.Text == "" || p2t4.Text == "" || p2t5.Text == "" || p2t6.Text == "" || p2t7.Text == "" || p2t8.Text == "" || p2t9.Text == "" || p2l10.Text == "" || p2l11.Text == "" || p2l12.Text == "")
                {
                    if (PGA46xStat_box.Text.Contains("Ready"))
                    {
                        toolStripStatusLabelGUIError.Visible = true;
                        toolStripStatusLabelGUIError.Text = "Thr values cannot be blank";
                    }
                    return;
                }
                toolStripStatusLabelGUIError.Visible = false;
                activateProgressBar(true);
                num = p2TimeMain;
                num2 = p2LevelMain;
                num3 = 126;
                UART_Read_Write(Convert.ToByte(num3), Convert.ToByte(p2lOff.SelectedIndex), true);
                UART_Read_Write(Convert.ToByte(num3 - 1), Convert.ToByte(p2l12.SelectedIndex), true);
                UART_Read_Write(Convert.ToByte(num3 - 2), Convert.ToByte(p2l11.SelectedIndex), true);
                UART_Read_Write(Convert.ToByte(num3 - 3), Convert.ToByte(p2l10.SelectedIndex), true);
                UART_Read_Write(Convert.ToByte(num3 - 4), Convert.ToByte(p2l9.SelectedIndex), true);
                if (!thrUpdatedAtLeastOnce)
                {
                    thrUpdatedAtLeastOnce = true;
                }
            }
            for (int i = 3; i < 14; i++)
            {
                byte data_H;
                if (i < 8)
                    data_H = Convert.ToByte(num2 >> i * 8 & 255UL);
                else
                    data_H = Convert.ToByte(num >> (i - 8) * 8 & 255UL);
                int value = num3 - i - 2;
                Tools.timeDelay(1, "MS");
                UART_Read_Write(Convert.ToByte(value), data_H, true);
            }
            Fault_Stat_Update_button_Click(null, null);
            activateProgressBar(false);
        }

        public void thrLongDBtn_Click(object sender, EventArgs e)
        {
            bool flag = false;
            if (thrUpdateCheck.Checked)
            {
                flag = true;
                thrUpdateCheck.Checked = false;
            }
            if (tabcontrolThr.SelectedTab.Text == "Preset 1")
            {
                p1l1.SelectedIndex = 19;
                p1l2.SelectedIndex = 19;
                p1l3.SelectedIndex = 8;
                p1l4.SelectedIndex = 7;
                p1l5.SelectedIndex = 4;
                p1l6.SelectedIndex = 4;
                p1l7.SelectedIndex = 3;
                p1l8.SelectedIndex = 3;
                p1l9.SelectedIndex = 40;
                p1l10.SelectedIndex = 48;
                p1l11.SelectedIndex = 52;
                p1l12.SelectedIndex = 60;
                p1lOff.SelectedIndex = 0;
                p1t1.SelectedIndex = 7;
                p1t2.SelectedIndex = 7;
                p1t3.SelectedIndex = 7;
                p1t4.SelectedIndex = 7;
                p1t5.SelectedIndex = 7;
                p1t6.SelectedIndex = 7;
                p1t7.SelectedIndex = 7;
                p1t8.SelectedIndex = 7;
                p1t9.SelectedIndex = 7;
                p1t10.SelectedIndex = 8;
                p1t11.SelectedIndex = 8;
                p1t12.SelectedIndex = 8;
            }
            if (tabcontrolThr.SelectedTab.Text == "Preset 2")
            {
                p2l1.SelectedIndex = 19;
                p2l2.SelectedIndex = 19;
                p2l3.SelectedIndex = 8;
                p2l4.SelectedIndex = 7;
                p2l5.SelectedIndex = 4;
                p2l6.SelectedIndex = 4;
                p2l7.SelectedIndex = 3;
                p2l8.SelectedIndex = 3;
                p2l9.SelectedIndex = 40;
                p2l10.SelectedIndex = 48;
                p2l11.SelectedIndex = 52;
                p2l12.SelectedIndex = 60;
                p2lOff.SelectedIndex = 0;
                p2t1.SelectedIndex = 7;
                p2t2.SelectedIndex = 7;
                p2t3.SelectedIndex = 7;
                p2t4.SelectedIndex = 7;
                p2t5.SelectedIndex = 7;
                p2t6.SelectedIndex = 7;
                p2t7.SelectedIndex = 7;
                p2t8.SelectedIndex = 7;
                p2t9.SelectedIndex = 7;
                p2t10.SelectedIndex = 8;
                p2t11.SelectedIndex = 8;
                p2t12.SelectedIndex = 8;
            }
            if (flag)
            {
                thrWriteValuesBtn_Click(null, null);
                thrUpdateCheck.Checked = true;
            }
            updateThrBtn_Click(null, null);
        }

        public void thrShortDBtn_Click(object sender, EventArgs e)
        {
            bool flag = false;
            if (thrUpdateCheck.Checked)
            {
                flag = true;
                thrUpdateCheck.Checked = false;
            }
            if (tabcontrolThr.SelectedTab.Text == "Preset 1")
            {
                p1l1.SelectedIndex = 19;
                p1l2.SelectedIndex = 19;
                p1l3.SelectedIndex = 8;
                p1l4.SelectedIndex = 7;
                p1l5.SelectedIndex = 4;
                p1l6.SelectedIndex = 4;
                p1l7.SelectedIndex = 3;
                p1l8.SelectedIndex = 3;
                p1l9.SelectedIndex = 40;
                p1l10.SelectedIndex = 48;
                p1l11.SelectedIndex = 52;
                p1l12.SelectedIndex = 60;
                p1lOff.SelectedIndex = 0;
                p1t1.SelectedIndex = 4;
                p1t2.SelectedIndex = 4;
                p1t3.SelectedIndex = 4;
                p1t4.SelectedIndex = 4;
                p1t5.SelectedIndex = 4;
                p1t6.SelectedIndex = 4;
                p1t7.SelectedIndex = 4;
                p1t8.SelectedIndex = 4;
                p1t9.SelectedIndex = 5;
                p1t10.SelectedIndex = 5;
                p1t11.SelectedIndex = 5;
                p1t12.SelectedIndex = 5;
            }
            if (tabcontrolThr.SelectedTab.Text == "Preset 2")
            {
                p2l1.SelectedIndex = 19;
                p2l2.SelectedIndex = 19;
                p2l3.SelectedIndex = 8;
                p2l4.SelectedIndex = 7;
                p2l5.SelectedIndex = 4;
                p2l6.SelectedIndex = 4;
                p2l7.SelectedIndex = 3;
                p2l8.SelectedIndex = 3;
                p2l9.SelectedIndex = 40;
                p2l10.SelectedIndex = 48;
                p2l11.SelectedIndex = 52;
                p2l12.SelectedIndex = 60;
                p2lOff.SelectedIndex = 0;
                p2t1.SelectedIndex = 4;
                p2t2.SelectedIndex = 4;
                p2t3.SelectedIndex = 4;
                p2t4.SelectedIndex = 4;
                p2t5.SelectedIndex = 4;
                p2t6.SelectedIndex = 4;
                p2t7.SelectedIndex = 4;
                p2t8.SelectedIndex = 4;
                p2t9.SelectedIndex = 5;
                p2t10.SelectedIndex = 5;
                p2t11.SelectedIndex = 5;
                p2t12.SelectedIndex = 5;
            }
            if (flag)
            {
                thrWriteValuesBtn_Click(null, null);
                thrUpdateCheck.Checked = true;
            }
            updateThrBtn_Click(null, null);
        }

        public void writeTVGMemBtn_Click(object sender, EventArgs e)
        {
            if (tvgt0.Text == "" || tvgt1.Text == "" || tvgt2.Text == "" || tvgt3.Text == "" || tvgt4.Text == "" || tvgt5.Text == "" || tvgg0.Text == "" || tvgg1.Text == "" || tvgg2.Text == "" || tvgg3.Text == "" || tvgg4.Text == "" || tvgg5.Text == "")
            {
                if (PGA46xStat_box.Text.Contains("Ready"))
                {
                    toolStripStatusLabelGUIError.Visible = true;
                    toolStripStatusLabelGUIError.Text = "TVG values cannot be blank";
                }
            }
            else
            {
                toolStripStatusLabelGUIError.Visible = false;
                activateProgressBar(true);
                ulong num = tvgTimeMain;
                ulong num2 = tvgLevelMain;
                int num3 = 26;
                for (int i = 0; i < 7; i++)
                {
                    byte data_H;
                    if (i < 4)
                        data_H = Convert.ToByte(num2 >> i * 8 & 255UL);
                    else
                        data_H = Convert.ToByte(num >> (i - 4) * 8 & 255UL);

                    UART_Read_Write(Convert.ToByte(num3 - i), data_H, true);
                }
                if (!initThrFlag)
                {
                    readRegsOffPage = true;
                    ReadAllRegs(false);
                    readRegsOffPage = false;
                }
                tvgReady = false;
                updateTVGBtn_Click(null, null);
                activateProgressBar(false);
            }
        }

        public void readTVGMemBtn_Click(object sender, EventArgs e)
        {
            activateProgressBar(true);
            bool flag = false;
            if (tvgInstantUpdateCheck.Checked)
            {
                flag = true;
                tvgInstantUpdateCheck.Checked = false;
            }
            initThrFlag = false;
            readRegsOffPage = true;
            ReadAllRegs(false);
            readRegsOffPage = false;
            initThrFlag = true;
            if (flag)
            {
                writeTVGMemBtn_Click(null, null);
                tvgInstantUpdateCheck.Checked = true;
            }
            updateTVGBtn_Click(null, null);
            activateProgressBar(false);
        }

        public void tvgShortDBtn_Click(object sender, EventArgs e)
        {
            bool flag = false;
            if (tvgInstantUpdateCheck.Checked)
            {
                flag = true;
                tvgInstantUpdateCheck.Checked = false;
            }
            tvgg0.SelectedIndex = 0;
            tvgg1.SelectedIndex = 2;
            tvgg2.SelectedIndex = 6;
            tvgg3.SelectedIndex = 18;
            tvgg4.SelectedIndex = 26;
            tvgg5.SelectedIndex = 38;
            tvgt0.SelectedIndex = 4;
            tvgt1.SelectedIndex = 4;
            tvgt2.SelectedIndex = 4;
            tvgt3.SelectedIndex = 4;
            tvgt4.SelectedIndex = 4;
            tvgt5.SelectedIndex = 4;
            if (flag)
            {
                writeTVGMemBtn_Click(null, null);
                tvgInstantUpdateCheck.Checked = true;
            }
            updateTVGBtn_Click(null, null);
        }

        public void tvgLongDBtn_Click(object sender, EventArgs e)
        {
            bool flag = false;
            if (tvgInstantUpdateCheck.Checked)
            {
                flag = true;
                tvgInstantUpdateCheck.Checked = false;
            }
            tvgg0.SelectedIndex = 0;
            tvgg1.SelectedIndex = 4;
            tvgg2.SelectedIndex = 10;
            tvgg3.SelectedIndex = 20;
            tvgg4.SelectedIndex = 32;
            tvgg5.SelectedIndex = 48;
            tvgt0.SelectedIndex = 8;
            tvgt1.SelectedIndex = 13;
            tvgt2.SelectedIndex = 14;
            tvgt3.SelectedIndex = 14;
            tvgt4.SelectedIndex = 14;
            tvgt5.SelectedIndex = 15;
            if (flag)
            {
                writeTVGMemBtn_Click(null, null);
                tvgInstantUpdateCheck.Checked = true;
            }
            updateTVGBtn_Click(null, null);
        }

        private void tvgt0_SelectedIndexChanged(object sender, EventArgs e)
        {
            pXtlXMask = (ulong)tvgt0.SelectedIndex;
            tvgTimeMain = ((tvgTimeMain & 18446744073693822975UL) | (pXtlXMask << 20 & ulong.MaxValue));
            tvgReadyFunction();
        }

        private void tvgt1_SelectedIndexChanged(object sender, EventArgs e)
        {
            pXtlXMask = (ulong)tvgt1.SelectedIndex;
            tvgTimeMain = ((tvgTimeMain & 18446744073708568575UL) | (pXtlXMask << 16 & ulong.MaxValue));
            tvgReadyFunction();
        }

        private void tvgt2_SelectedIndexChanged(object sender, EventArgs e)
        {
            pXtlXMask = (ulong)tvgt2.SelectedIndex;
            tvgTimeMain = ((tvgTimeMain & 18446744073709490175UL) | (pXtlXMask << 12 & ulong.MaxValue));
            tvgReadyFunction();
        }

        private void tvgt3_SelectedIndexChanged(object sender, EventArgs e)
        {
            pXtlXMask = (ulong)tvgt3.SelectedIndex;
            tvgTimeMain = ((tvgTimeMain & 18446744073709547775UL) | (pXtlXMask << 8 & ulong.MaxValue));
            tvgReadyFunction();
        }

        private void tvgt4_SelectedIndexChanged(object sender, EventArgs e)
        {
            pXtlXMask = (ulong)tvgt4.SelectedIndex;
            tvgTimeMain = ((tvgTimeMain & 18446744073709551375UL) | (pXtlXMask << 4 & ulong.MaxValue));
            tvgReadyFunction();
        }

        private void tvgt5_SelectedIndexChanged(object sender, EventArgs e)
        {
            pXtlXMask = (ulong)tvgt5.SelectedIndex;
            tvgTimeMain = ((tvgTimeMain & 18446744073709551600UL) | (pXtlXMask & ulong.MaxValue));
            tvgReadyFunction();
        }

        private void tvgg0_SelectedIndexChanged(object sender, EventArgs e)
        {
            gainCombo.SelectedIndex = tvgg0.SelectedIndex;
            tvgReadyFunction();
            if (tvgg1.Text != "" || tvgg2.Text != "" || tvgg3.Text != "" || tvgg4.Text != "" || tvgg5.Text != "")
            {
                updateTVGChart();
            }
        }

        private void tvgg1_SelectedIndexChanged(object sender, EventArgs e)
        {
            pXtlXMask = (ulong)tvgg1.SelectedIndex;
            tvgLevelMain = ((tvgLevelMain & 18446744069481693183UL) | (pXtlXMask << 26 & ulong.MaxValue));
            tvgReadyFunction();
        }

        private void tvgg2_SelectedIndexChanged(object sender, EventArgs e)
        {
            pXtlXMask = (ulong)tvgg2.SelectedIndex;
            tvgLevelMain = ((tvgLevelMain & 18446744073643491327UL) | (pXtlXMask << 20 & ulong.MaxValue));
            tvgReadyFunction();
        }

        private void tvgg3_SelectedIndexChanged(object sender, EventArgs e)
        {
            pXtlXMask = (ulong)tvgg3.SelectedIndex;
            tvgLevelMain = ((tvgLevelMain & 18446744073708519423UL) | (pXtlXMask << 14 & ulong.MaxValue));
            tvgReadyFunction();
        }

        private void tvgg4_SelectedIndexChanged(object sender, EventArgs e)
        {
            pXtlXMask = (ulong)tvgg4.SelectedIndex;
            tvgLevelMain = ((tvgLevelMain & 18446744073709535487UL) | (pXtlXMask << 8 & ulong.MaxValue));
            tvgReadyFunction();
        }

        private void tvgg5_SelectedIndexChanged(object sender, EventArgs e)
        {
            ulong num = freqshiftCheck.Checked ? 1UL : 0UL;
            pXtlXMask = (ulong)tvgg5.SelectedIndex;
            tvgLevelMain = (
                (tvgLevelMain & 18446744073709551360UL) |
                (pXtlXMask << 2 & ulong.MaxValue) |
                (num & ulong.MaxValue));
            tvgReadyFunction();
        }

        private void tvgReadyFunction()
        {
            tvgReady = true;
        }

        public void simEchoBtn_Click(object sender, EventArgs e)
        {
            byte[] array = new byte[]
            {
                0,
                64,
                byte.MaxValue,
                byte.MaxValue,
                byte.MaxValue,
                byte.MaxValue,
                byte.MaxValue,
                byte.MaxValue,
                117,
                31,
                15,
                13,
                6,
                15,
                17,
                14,
                9,
                3,
                3,
                4,
                3,
                4,
                2,
                3,
                4,
                3,
                4,
                2,
                4,
                3,
                3,
                4,
                2,
                5,
                141,
                158,
                107,
                45,
                19,
                9,
                7,
                5,
                4,
                7,
                3,
                4,
                3,
                2,
                6,
                3,
                4,
                4,
                5,
                7,
                5,
                4,
                7,
                11,
                5,
                3,
                6,
                12,
                10,
                6,
                8,
                7,
                19,
                20,
                13,
                7,
                7,
                3,
                3,
                3,
                4,
                4,
                4,
                3,
                2,
                4,
                5,
                7,
                4,
                3,
                3,
                3,
                4,
                3,
                2,
                5,
                2,
                4,
                3,
                4,
                5,
                3,
                5,
                2,
                10,
                10,
                6,
                5,
                3,
                5,
                2,
                2,
                3,
                3,
                4,
                3,
                2,
                4,
                3,
                6,
                2,
                3,
                2,
                2,
                4,
                2,
                4,
                3,
                3,
                4,
                2,
                5,
                5,
                3
            };
            monitor_clear_plot();
            for (int i = 0; i < Convert.ToInt32(sampleMaxCombo.Text); i++)
                dumpChart.Series[0].Points.AddXY((double)i, (double)array[i]);
            for (int j = 0; j < Convert.ToInt32(sampleMaxCombo.Text); j++)
                dumpChart.Series[6].Points.AddXY(Convert.ToDouble(5) / 128.0 * (double)j, 0.0);
        }

        private void autoCRCBox_CheckedChanged(object sender, EventArgs e)
        {
            if (autoCRCBox.Checked)
            {
                Fault_Stat_Update_button_Click(null, null);
                if ((THRCRCERR_Stat_TextBox.Text == "1" || EECRCERR_Stat_TextBox.Text == "1") && !autoCRCFirst)
                {
                    activateProgressBar(true);
                    readRegsOffPage = true;
                    ReadAllRegs(false);
                    readRegsOffPage = false;
                    if (THRCRCERR_Stat_TextBox.Text == "1")
                    {
                        eepromThrCRCBtn_Click(null, null);
                    }
                    Fault_Stat_Update_button_Click(null, null);
                    activateProgressBar(false);
                }
                else if (autoCRCFirst)
                {
                    eepromThrCRCBtn_Click(null, null);
                }
                autoCRCBox.Checked = false;
            }
        }

        public void clearThrChartBtn_Click(object sender, EventArgs e)
        {
            for (int i = 0; i < 5; i++)
                thrChart.Series[i].Points.Clear();
            thrChart.Series[4].ChartType = SeriesChartType.Line;
        }

        public void clearTVGChartBtn_Click(object sender, EventArgs e)
        {
            for (int i = 0; i < 3; i++)
                tvgChart.Series[i].Points.Clear();
            tvgChart.Series[2].ChartType = SeriesChartType.Line;
        }

        public void allInitGainBtn_Click(object sender, EventArgs e)
        {
            bool flag = false;
            if (tvgInstantUpdateCheck.Checked)
            {
                flag = true;
                tvgInstantUpdateCheck.Checked = false;
            }

            if (tvgg0.Text == "")
                tvgg0.SelectedIndex = 32;
            
            tvgg1.SelectedIndex = tvgg0.SelectedIndex;
            tvgg2.SelectedIndex = tvgg0.SelectedIndex;
            tvgg3.SelectedIndex = tvgg0.SelectedIndex;
            tvgg4.SelectedIndex = tvgg0.SelectedIndex;
            tvgg5.SelectedIndex = tvgg0.SelectedIndex;
            if (tvgt0.Text == "")
                tvgt0.SelectedIndex = 8;
            
            tvgt1.SelectedIndex = tvgt0.SelectedIndex;
            tvgt2.SelectedIndex = tvgt0.SelectedIndex;
            tvgt3.SelectedIndex = tvgt0.SelectedIndex;
            tvgt4.SelectedIndex = tvgt0.SelectedIndex;
            tvgt5.SelectedIndex = tvgt0.SelectedIndex;
            if (flag)
            {
                writeTVGMemBtn_Click(null, null);
                tvgInstantUpdateCheck.Checked = true;
            }
            updateTVGBtn_Click(null, null);
        }

        private void muxOutTestCombo_SelectedIndexChanged(object sender, EventArgs e)
        {
            if (!firstTime)
            {
                RegDefs regDefs = new RegDefs("4B (TEST_MUX)");
                regDefs.TEST_MUX.ReadFromUART();
                switch (muxOutTestCombo.SelectedIndex)
                {
                    case 0:
                        regDefs.TEST_MUX_B.value = "000";
                        break;
                    case 1:
                        regDefs.TEST_MUX_B.value = "001";
                        break;
                    case 2:
                        regDefs.TEST_MUX_B.value = "010";
                        break;
                    case 3:
                        regDefs.TEST_MUX_B.value = "011";
                        break;
                    case 4:
                        regDefs.TEST_MUX_B.value = "100";
                        break;
                    case 5:
                        regDefs.TEST_MUX_B.value = "101";
                        break;
                    case 6:
                        regDefs.TEST_MUX_B.value = "110";
                        break;
                    case 7:
                        regDefs.TEST_MUX_B.value = "111";
                        break;
                }
                regDefs.TEST_MUX.WriteToUART();
                if (primaryTab.SelectedTab.Name != "MemMap" && !firstTime)
                {
                    string[,] array = new string[1, 5];
                    array[0, 0] = regDefs.TEST_MUX.location.ToString();
                    array[0, 1] = "4B (TEST_MUX)";
                    array[0, 4] = Tools.int32_Into_stringBase16(calRegValfromFields(regDefs.TEST_MUX.bit_fields));
                    updateMemoryMap(array, "GRID_USER_MEMSPACE");
                }
            }
        }

        private void datapathMuxSelCombo_SelectedIndexChanged(object sender, EventArgs e)
        {
            if (!firstTime)
            {
                RegDefs regDefs = new RegDefs("4B (TEST_MUX)");
                regDefs.TEST_MUX.ReadFromUART();
                switch (datapathMuxSelCombo.SelectedIndex)
                {
                    case 0:
                        regDefs.DP_MUX.value = "000";
                        break;
                    case 1:
                        regDefs.DP_MUX.value = "001";
                        break;
                    case 2:
                        regDefs.DP_MUX.value = "010";
                        break;
                    case 3:
                        regDefs.DP_MUX.value = "011";
                        break;
                    case 4:
                        regDefs.DP_MUX.value = "100";
                        break;
                    case 5:
                        regDefs.DP_MUX.value = "101";
                        break;
                    case 6:
                        regDefs.DP_MUX.value = "110";
                        break;
                    case 7:
                        regDefs.DP_MUX.value = "111";
                        break;
                }
                regDefs.TEST_MUX.WriteToUART();
                if (primaryTab.SelectedTab.Name != "MemMap" && !firstTime)
                {
                    string[,] array = new string[1, 5];
                    array[0, 0] = regDefs.TEST_MUX.location.ToString();
                    array[0, 1] = "4B (TEST_MUX)";
                    array[0, 4] = Tools.int32_Into_stringBase16(calRegValfromFields(regDefs.TEST_MUX.bit_fields));
                    updateMemoryMap(array, "GRID_USER_MEMSPACE");
                }
            }
        }

        private void sampleOut8bitRadio_CheckedChanged(object sender, EventArgs e)
        {
            if (!firstTime)
            {
                RegDefs regDefs = new RegDefs("4B (TEST_MUX)");
                regDefs.TEST_MUX.ReadFromUART();
                if (sampleOut8bitRadio.Checked && !sampleOut12bitRadio.Checked)
                {
                    regDefs.SAMPLE_SEL.value = "0";
                }
                else
                {
                    regDefs.SAMPLE_SEL.value = "1";
                }
                regDefs.TEST_MUX.WriteToUART();
                if (primaryTab.SelectedTab.Name != "MemMap" && !firstTime)
                {
                    string[,] array = new string[1, 5];
                    array[0, 0] = regDefs.TEST_MUX.location.ToString();
                    array[0, 1] = "4B (TEST_MUX)";
                    array[0, 4] = Tools.int32_Into_stringBase16(calRegValfromFields(regDefs.TEST_MUX.bit_fields));
                    updateMemoryMap(array, "GRID_USER_MEMSPACE");
                }
            }
        }

        private void sampleOut12bitRadio_CheckedChanged(object sender, EventArgs e)
        {
            sampleOut8bitRadio_CheckedChanged(null, null);
        }

        private void cloneThrBtn_Click(object sender, EventArgs e)
        {
            if (tabcontrolThr.SelectedTab.Text == "Preset 1")
            {
                p2t1.SelectedIndex = p1t1.SelectedIndex;
                p2t2.SelectedIndex = p1t2.SelectedIndex;
                p2t3.SelectedIndex = p1t3.SelectedIndex;
                p2t4.SelectedIndex = p1t4.SelectedIndex;
                p2t5.SelectedIndex = p1t5.SelectedIndex;
                p2t6.SelectedIndex = p1t6.SelectedIndex;
                p2t7.SelectedIndex = p1t7.SelectedIndex;
                p2t8.SelectedIndex = p1t8.SelectedIndex;
                p2t9.SelectedIndex = p1t9.SelectedIndex;
                p2t10.SelectedIndex = p1t10.SelectedIndex;
                p2t11.SelectedIndex = p1t11.SelectedIndex;
                p2t12.SelectedIndex = p1t12.SelectedIndex;
                p2l1.SelectedIndex = p1l1.SelectedIndex;
                p2l2.SelectedIndex = p1l2.SelectedIndex;
                p2l3.SelectedIndex = p1l3.SelectedIndex;
                p2l4.SelectedIndex = p1l4.SelectedIndex;
                p2l5.SelectedIndex = p1l5.SelectedIndex;
                p2l6.SelectedIndex = p1l6.SelectedIndex;
                p2l7.SelectedIndex = p1l7.SelectedIndex;
                p2l8.SelectedIndex = p1l8.SelectedIndex;
                p2l9.SelectedIndex = p1l9.SelectedIndex;
                p2l10.SelectedIndex = p1l10.SelectedIndex;
                p2l11.SelectedIndex = p1l11.SelectedIndex;
                p2l12.SelectedIndex = p1l12.SelectedIndex;
                p2lOff.SelectedIndex = p1lOff.SelectedIndex;
            }
            if (tabcontrolThr.SelectedTab.Text == "Preset 2")
            {
                p1t1.SelectedIndex = p2t1.SelectedIndex;
                p1t2.SelectedIndex = p2t2.SelectedIndex;
                p1t3.SelectedIndex = p2t3.SelectedIndex;
                p1t4.SelectedIndex = p2t4.SelectedIndex;
                p1t5.SelectedIndex = p2t5.SelectedIndex;
                p1t6.SelectedIndex = p2t6.SelectedIndex;
                p1t7.SelectedIndex = p2t7.SelectedIndex;
                p1t8.SelectedIndex = p2t8.SelectedIndex;
                p1t9.SelectedIndex = p2t9.SelectedIndex;
                p1t10.SelectedIndex = p2t10.SelectedIndex;
                p1t11.SelectedIndex = p2t11.SelectedIndex;
                p1t12.SelectedIndex = p2t12.SelectedIndex;
                p1l1.SelectedIndex = p2l1.SelectedIndex;
                p1l2.SelectedIndex = p2l2.SelectedIndex;
                p1l3.SelectedIndex = p2l3.SelectedIndex;
                p1l4.SelectedIndex = p2l4.SelectedIndex;
                p1l5.SelectedIndex = p2l5.SelectedIndex;
                p1l6.SelectedIndex = p2l6.SelectedIndex;
                p1l7.SelectedIndex = p2l7.SelectedIndex;
                p1l8.SelectedIndex = p2l8.SelectedIndex;
                p1l9.SelectedIndex = p2l9.SelectedIndex;
                p1l10.SelectedIndex = p2l10.SelectedIndex;
                p1l11.SelectedIndex = p2l11.SelectedIndex;
                p1l12.SelectedIndex = p2l12.SelectedIndex;
                p1lOff.SelectedIndex = p2lOff.SelectedIndex;
            }
        }

        private void gainVVBoxUpdate()
        {
            if (AFEGainRngCombo.SelectedIndex == 0)
            {
                switch (gainCombo.SelectedIndex)
                {
                    case 0:
                        gainVVBox.Text = "794.3";
                        break;
                    case 1:
                        gainVVBox.Text = "841.4";
                        break;
                    case 2:
                        gainVVBox.Text = "891.3";
                        break;
                    case 3:
                        gainVVBox.Text = "944.1";
                        break;
                    case 4:
                        gainVVBox.Text = "1000";
                        break;
                    case 5:
                        gainVVBox.Text = "1059.3";
                        break;
                    case 6:
                        gainVVBox.Text = "1122";
                        break;
                    case 7:
                        gainVVBox.Text = "1188.5";
                        break;
                    case 8:
                        gainVVBox.Text = "1258.9";
                        break;
                    case 9:
                        gainVVBox.Text = "1333.5";
                        break;
                    case 10:
                        gainVVBox.Text = "1412.5";
                        break;
                    case 11:
                        gainVVBox.Text = "1496.2";
                        break;
                    case 12:
                        gainVVBox.Text = "1584.9";
                        break;
                    case 13:
                        gainVVBox.Text = "1678.8";
                        break;
                    case 14:
                        gainVVBox.Text = "1778.3";
                        break;
                    case 15:
                        gainVVBox.Text = "1883.6";
                        break;
                    case 16:
                        gainVVBox.Text = "1995.3";
                        break;
                    case 17:
                        gainVVBox.Text = "2113.5";
                        break;
                    case 18:
                        gainVVBox.Text = "2238.7";
                        break;
                    case 19:
                        gainVVBox.Text = "2371.4";
                        break;
                    case 20:
                        gainVVBox.Text = "2511.9";
                        break;
                    case 21:
                        gainVVBox.Text = "2660.7";
                        break;
                    case 22:
                        gainVVBox.Text = "2818.4";
                        break;
                    case 23:
                        gainVVBox.Text = "2985.4";
                        break;
                    case 24:
                        gainVVBox.Text = "3162.3";
                        break;
                    case 25:
                        gainVVBox.Text = "3349.7";
                        break;
                    case 26:
                        gainVVBox.Text = "3548.1";
                        break;
                    case 27:
                        gainVVBox.Text = "3758.4";
                        break;
                    case 28:
                        gainVVBox.Text = "3981.1";
                        break;
                    case 29:
                        gainVVBox.Text = "4217";
                        break;
                    case 30:
                        gainVVBox.Text = "4466.8";
                        break;
                    case 31:
                        gainVVBox.Text = "4731.5";
                        break;
                    case 32:
                        gainVVBox.Text = "5011.9";
                        break;
                    case 33:
                        gainVVBox.Text = "5308.8";
                        break;
                    case 34:
                        gainVVBox.Text = "5623.4";
                        break;
                    case 35:
                        gainVVBox.Text = "5956.6";
                        break;
                    case 36:
                        gainVVBox.Text = "6309.6";
                        break;
                    case 37:
                        gainVVBox.Text = "6683.4";
                        break;
                    case 38:
                        gainVVBox.Text = "7079.5";
                        break;
                    case 39:
                        gainVVBox.Text = "7498.9";
                        break;
                    case 40:
                        gainVVBox.Text = "7943.3";
                        break;
                    case 41:
                        gainVVBox.Text = "8414";
                        break;
                    case 42:
                        gainVVBox.Text = "8912.5";
                        break;
                    case 43:
                        gainVVBox.Text = "9440.6";
                        break;
                    case 44:
                        gainVVBox.Text = "10000";
                        break;
                    case 45:
                        gainVVBox.Text = "10592.5";
                        break;
                    case 46:
                        gainVVBox.Text = "11220.2";
                        break;
                    case 47:
                        gainVVBox.Text = "11885";
                        break;
                    case 48:
                        gainVVBox.Text = "12589.3";
                        break;
                    case 49:
                        gainVVBox.Text = "13335.2";
                        break;
                    case 50:
                        gainVVBox.Text = "14125.4";
                        break;
                    case 51:
                        gainVVBox.Text = "14962.4";
                        break;
                    case 52:
                        gainVVBox.Text = "15848.9";
                        break;
                    case 53:
                        gainVVBox.Text = "16788";
                        break;
                    case 54:
                        gainVVBox.Text = "17782.8";
                        break;
                    case 55:
                        gainVVBox.Text = "18836.5";
                        break;
                    case 56:
                        gainVVBox.Text = "19952.6";
                        break;
                    case 57:
                        gainVVBox.Text = "21134.9";
                        break;
                    case 58:
                        gainVVBox.Text = "22387.2";
                        break;
                    case 59:
                        gainVVBox.Text = "23713.7";
                        break;
                    case 60:
                        gainVVBox.Text = "25118.9";
                        break;
                    case 61:
                        gainVVBox.Text = "26607.3";
                        break;
                    case 62:
                        gainVVBox.Text = "28183.8";
                        break;
                    case 63:
                        gainVVBox.Text = "29853.8";
                        break;
                }
            }
            else if (AFEGainRngCombo.SelectedIndex == 1)
            {
                switch (gainCombo.SelectedIndex)
                {
                    case 0:
                        gainVVBox.Text = "398.1";
                        break;
                    case 1:
                        gainVVBox.Text = "421.7";
                        break;
                    case 2:
                        gainVVBox.Text = "446.7";
                        break;
                    case 3:
                        gainVVBox.Text = "473.2";
                        break;
                    case 4:
                        gainVVBox.Text = "501.2";
                        break;
                    case 5:
                        gainVVBox.Text = "530.9";
                        break;
                    case 6:
                        gainVVBox.Text = "562.3";
                        break;
                    case 7:
                        gainVVBox.Text = "595.7";
                        break;
                    case 8:
                        gainVVBox.Text = "631";
                        break;
                    case 9:
                        gainVVBox.Text = "668.3";
                        break;
                    case 10:
                        gainVVBox.Text = "707.9";
                        break;
                    case 11:
                        gainVVBox.Text = "749.9";
                        break;
                    case 12:
                        gainVVBox.Text = "794.3";
                        break;
                    case 13:
                        gainVVBox.Text = "841.4";
                        break;
                    case 14:
                        gainVVBox.Text = "891.3";
                        break;
                    case 15:
                        gainVVBox.Text = "944.1";
                        break;
                    case 16:
                        gainVVBox.Text = "1000";
                        break;
                    case 17:
                        gainVVBox.Text = "1059.3";
                        break;
                    case 18:
                        gainVVBox.Text = "1122";
                        break;
                    case 19:
                        gainVVBox.Text = "1188.5";
                        break;
                    case 20:
                        gainVVBox.Text = "1258.9";
                        break;
                    case 21:
                        gainVVBox.Text = "1333.5";
                        break;
                    case 22:
                        gainVVBox.Text = "1412.5";
                        break;
                    case 23:
                        gainVVBox.Text = "1496.2";
                        break;
                    case 24:
                        gainVVBox.Text = "1584.9";
                        break;
                    case 25:
                        gainVVBox.Text = "1678.8";
                        break;
                    case 26:
                        gainVVBox.Text = "1778.3";
                        break;
                    case 27:
                        gainVVBox.Text = "1883.6";
                        break;
                    case 28:
                        gainVVBox.Text = "1995.3";
                        break;
                    case 29:
                        gainVVBox.Text = "2113.5";
                        break;
                    case 30:
                        gainVVBox.Text = "2238.7";
                        break;
                    case 31:
                        gainVVBox.Text = "2371.4";
                        break;
                    case 32:
                        gainVVBox.Text = "2511.9";
                        break;
                    case 33:
                        gainVVBox.Text = "2660.7";
                        break;
                    case 34:
                        gainVVBox.Text = "2818.4";
                        break;
                    case 35:
                        gainVVBox.Text = "2985.4";
                        break;
                    case 36:
                        gainVVBox.Text = "3162.3";
                        break;
                    case 37:
                        gainVVBox.Text = "3349.7";
                        break;
                    case 38:
                        gainVVBox.Text = "3548.1";
                        break;
                    case 39:
                        gainVVBox.Text = "3758.4";
                        break;
                    case 40:
                        gainVVBox.Text = "3981.1";
                        break;
                    case 41:
                        gainVVBox.Text = "4217";
                        break;
                    case 42:
                        gainVVBox.Text = "4466.8";
                        break;
                    case 43:
                        gainVVBox.Text = "4731.5";
                        break;
                    case 44:
                        gainVVBox.Text = "5011.9";
                        break;
                    case 45:
                        gainVVBox.Text = "5308.8";
                        break;
                    case 46:
                        gainVVBox.Text = "5623.4";
                        break;
                    case 47:
                        gainVVBox.Text = "5956.6";
                        break;
                    case 48:
                        gainVVBox.Text = "6309.6";
                        break;
                    case 49:
                        gainVVBox.Text = "6683.4";
                        break;
                    case 50:
                        gainVVBox.Text = "7079.5";
                        break;
                    case 51:
                        gainVVBox.Text = "7498.9";
                        break;
                    case 52:
                        gainVVBox.Text = "7943.3";
                        break;
                    case 53:
                        gainVVBox.Text = "8414";
                        break;
                    case 54:
                        gainVVBox.Text = "8912.5";
                        break;
                    case 55:
                        gainVVBox.Text = "9440.6";
                        break;
                    case 56:
                        gainVVBox.Text = "10000";
                        break;
                    case 57:
                        gainVVBox.Text = "10592.5";
                        break;
                    case 58:
                        gainVVBox.Text = "11220.2";
                        break;
                    case 59:
                        gainVVBox.Text = "11885";
                        break;
                    case 60:
                        gainVVBox.Text = "12589.3";
                        break;
                    case 61:
                        gainVVBox.Text = "13335.2";
                        break;
                    case 62:
                        gainVVBox.Text = "14125.4";
                        break;
                    case 63:
                        gainVVBox.Text = "14962.4";
                        break;
                }
            }
            else if (AFEGainRngCombo.SelectedIndex == 2)
            {
                switch (gainCombo.SelectedIndex)
                {
                    case 0:
                        gainVVBox.Text = "199.5";
                        break;
                    case 1:
                        gainVVBox.Text = "211.3";
                        break;
                    case 2:
                        gainVVBox.Text = "223.9";
                        break;
                    case 3:
                        gainVVBox.Text = "237.1";
                        break;
                    case 4:
                        gainVVBox.Text = "251.2";
                        break;
                    case 5:
                        gainVVBox.Text = "266.1";
                        break;
                    case 6:
                        gainVVBox.Text = "281.8";
                        break;
                    case 7:
                        gainVVBox.Text = "298.5";
                        break;
                    case 8:
                        gainVVBox.Text = "316.2";
                        break;
                    case 9:
                        gainVVBox.Text = "335";
                        break;
                    case 10:
                        gainVVBox.Text = "354.8";
                        break;
                    case 11:
                        gainVVBox.Text = "375.8";
                        break;
                    case 12:
                        gainVVBox.Text = "398.1";
                        break;
                    case 13:
                        gainVVBox.Text = "421.7";
                        break;
                    case 14:
                        gainVVBox.Text = "446.7";
                        break;
                    case 15:
                        gainVVBox.Text = "473.2";
                        break;
                    case 16:
                        gainVVBox.Text = "501.2";
                        break;
                    case 17:
                        gainVVBox.Text = "530.9";
                        break;
                    case 18:
                        gainVVBox.Text = "562.3";
                        break;
                    case 19:
                        gainVVBox.Text = "595.7";
                        break;
                    case 20:
                        gainVVBox.Text = "631";
                        break;
                    case 21:
                        gainVVBox.Text = "668.3";
                        break;
                    case 22:
                        gainVVBox.Text = "707.9";
                        break;
                    case 23:
                        gainVVBox.Text = "749.9";
                        break;
                    case 24:
                        gainVVBox.Text = "794.3";
                        break;
                    case 25:
                        gainVVBox.Text = "841.4";
                        break;
                    case 26:
                        gainVVBox.Text = "891.3";
                        break;
                    case 27:
                        gainVVBox.Text = "944.1";
                        break;
                    case 28:
                        gainVVBox.Text = "1000";
                        break;
                    case 29:
                        gainVVBox.Text = "1059.3";
                        break;
                    case 30:
                        gainVVBox.Text = "1122";
                        break;
                    case 31:
                        gainVVBox.Text = "1188.5";
                        break;
                    case 32:
                        gainVVBox.Text = "1258.9";
                        break;
                    case 33:
                        gainVVBox.Text = "1333.5";
                        break;
                    case 34:
                        gainVVBox.Text = "1412.5";
                        break;
                    case 35:
                        gainVVBox.Text = "1496.2";
                        break;
                    case 36:
                        gainVVBox.Text = "1584.9";
                        break;
                    case 37:
                        gainVVBox.Text = "1678.8";
                        break;
                    case 38:
                        gainVVBox.Text = "1778.3";
                        break;
                    case 39:
                        gainVVBox.Text = "1883.6";
                        break;
                    case 40:
                        gainVVBox.Text = "1995.3";
                        break;
                    case 41:
                        gainVVBox.Text = "2113.5";
                        break;
                    case 42:
                        gainVVBox.Text = "2238.7";
                        break;
                    case 43:
                        gainVVBox.Text = "2371.4";
                        break;
                    case 44:
                        gainVVBox.Text = "2511.9";
                        break;
                    case 45:
                        gainVVBox.Text = "2660.7";
                        break;
                    case 46:
                        gainVVBox.Text = "2818.4";
                        break;
                    case 47:
                        gainVVBox.Text = "2985.4";
                        break;
                    case 48:
                        gainVVBox.Text = "3162.3";
                        break;
                    case 49:
                        gainVVBox.Text = "3349.7";
                        break;
                    case 50:
                        gainVVBox.Text = "3548.1";
                        break;
                    case 51:
                        gainVVBox.Text = "3758.4";
                        break;
                    case 52:
                        gainVVBox.Text = "3981.1";
                        break;
                    case 53:
                        gainVVBox.Text = "4217";
                        break;
                    case 54:
                        gainVVBox.Text = "4466.8";
                        break;
                    case 55:
                        gainVVBox.Text = "4731.5";
                        break;
                    case 56:
                        gainVVBox.Text = "5011.9";
                        break;
                    case 57:
                        gainVVBox.Text = "5308.8";
                        break;
                    case 58:
                        gainVVBox.Text = "5623.4";
                        break;
                    case 59:
                        gainVVBox.Text = "5956.6";
                        break;
                    case 60:
                        gainVVBox.Text = "6309.6";
                        break;
                    case 61:
                        gainVVBox.Text = "6683.4";
                        break;
                    case 62:
                        gainVVBox.Text = "7079.5";
                        break;
                    case 63:
                        gainVVBox.Text = "7498.9";
                        break;
                }
            }
            else if (AFEGainRngCombo.SelectedIndex == 3)
            {
                switch (gainCombo.SelectedIndex)
                {
                    case 0:
                        gainVVBox.Text = "39.8";
                        break;
                    case 1:
                        gainVVBox.Text = "42.2";
                        break;
                    case 2:
                        gainVVBox.Text = "44.7";
                        break;
                    case 3:
                        gainVVBox.Text = "47.3";
                        break;
                    case 4:
                        gainVVBox.Text = "50.1";
                        break;
                    case 5:
                        gainVVBox.Text = "53.1";
                        break;
                    case 6:
                        gainVVBox.Text = "56.2";
                        break;
                    case 7:
                        gainVVBox.Text = "59.6";
                        break;
                    case 8:
                        gainVVBox.Text = "63.1";
                        break;
                    case 9:
                        gainVVBox.Text = "66.8";
                        break;
                    case 10:
                        gainVVBox.Text = "70.8";
                        break;
                    case 11:
                        gainVVBox.Text = "75";
                        break;
                    case 12:
                        gainVVBox.Text = "79.4";
                        break;
                    case 13:
                        gainVVBox.Text = "84.1";
                        break;
                    case 14:
                        gainVVBox.Text = "89.1";
                        break;
                    case 15:
                        gainVVBox.Text = "94.4";
                        break;
                    case 16:
                        gainVVBox.Text = "100";
                        break;
                    case 17:
                        gainVVBox.Text = "105.9";
                        break;
                    case 18:
                        gainVVBox.Text = "112.2";
                        break;
                    case 19:
                        gainVVBox.Text = "118.9";
                        break;
                    case 20:
                        gainVVBox.Text = "125.9";
                        break;
                    case 21:
                        gainVVBox.Text = "133.4";
                        break;
                    case 22:
                        gainVVBox.Text = "141.3";
                        break;
                    case 23:
                        gainVVBox.Text = "149.6";
                        break;
                    case 24:
                        gainVVBox.Text = "158.5";
                        break;
                    case 25:
                        gainVVBox.Text = "167.9";
                        break;
                    case 26:
                        gainVVBox.Text = "177.8";
                        break;
                    case 27:
                        gainVVBox.Text = "188.4";
                        break;
                    case 28:
                        gainVVBox.Text = "199.5";
                        break;
                    case 29:
                        gainVVBox.Text = "211.3";
                        break;
                    case 30:
                        gainVVBox.Text = "223.9";
                        break;
                    case 31:
                        gainVVBox.Text = "237.1";
                        break;
                    case 32:
                        gainVVBox.Text = "251.2";
                        break;
                    case 33:
                        gainVVBox.Text = "266.1";
                        break;
                    case 34:
                        gainVVBox.Text = "281.8";
                        break;
                    case 35:
                        gainVVBox.Text = "298.5";
                        break;
                    case 36:
                        gainVVBox.Text = "316.2";
                        break;
                    case 37:
                        gainVVBox.Text = "335";
                        break;
                    case 38:
                        gainVVBox.Text = "354.8";
                        break;
                    case 39:
                        gainVVBox.Text = "375.8";
                        break;
                    case 40:
                        gainVVBox.Text = "398.1";
                        break;
                    case 41:
                        gainVVBox.Text = "421.7";
                        break;
                    case 42:
                        gainVVBox.Text = "446.7";
                        break;
                    case 43:
                        gainVVBox.Text = "473.2";
                        break;
                    case 44:
                        gainVVBox.Text = "501.2";
                        break;
                    case 45:
                        gainVVBox.Text = "530.9";
                        break;
                    case 46:
                        gainVVBox.Text = "562.3";
                        break;
                    case 47:
                        gainVVBox.Text = "595.7";
                        break;
                    case 48:
                        gainVVBox.Text = "631";
                        break;
                    case 49:
                        gainVVBox.Text = "668.3";
                        break;
                    case 50:
                        gainVVBox.Text = "707.9";
                        break;
                    case 51:
                        gainVVBox.Text = "749.9";
                        break;
                    case 52:
                        gainVVBox.Text = "794.3";
                        break;
                    case 53:
                        gainVVBox.Text = "841.4";
                        break;
                    case 54:
                        gainVVBox.Text = "891.3";
                        break;
                    case 55:
                        gainVVBox.Text = "944.1";
                        break;
                    case 56:
                        gainVVBox.Text = "1000";
                        break;
                    case 57:
                        gainVVBox.Text = "1059.3";
                        break;
                    case 58:
                        gainVVBox.Text = "1122";
                        break;
                    case 59:
                        gainVVBox.Text = "1188.5";
                        break;
                    case 60:
                        gainVVBox.Text = "1258.9";
                        break;
                    case 61:
                        gainVVBox.Text = "1333.5";
                        break;
                    case 62:
                        gainVVBox.Text = "1412.5";
                        break;
                    case 63:
                        gainVVBox.Text = "1496.2";
                        break;
                }
            }
        }

        private void loadWriteAllBtn_Click(object sender, EventArgs e)
        {
            Load_grid_butt_Click(null, null);
            if (readSuccessBool)
            {
                Write_all_regs_Click(null, null);
            }
        }

        private void updateNLSChart()
        {
            if (nlsPlotAddOnFlag)
            {
                dumpChart.Series[10].Points.Clear();
                if (p1NLSEnBox.Checked)
                {
                    double num = (double)(Convert.ToInt32(p1t1.Text) + Convert.ToInt32(p1t2.Text) + Convert.ToInt32(p1t3.Text) + Convert.ToInt32(p1t4.Text) + Convert.ToInt32(p1t5.Text) + Convert.ToInt32(p1t6.Text) + Convert.ToInt32(p1t7.Text) + Convert.ToInt32(p1t8.Text));
                    if (!p2NLSEnBox.Checked)
                    {
                        dumpChart.Series[10].Points.Clear();
                    }
                    if (nlsTOPCombo.SelectedIndex == 0)
                    {
                        num += (double)Convert.ToInt32(p1t9.Text);
                    }
                    else if (nlsTOPCombo.SelectedIndex == 1)
                    {
                        num = num + (double)Convert.ToInt32(p1t9.Text) + (double)Convert.ToInt32(p1t10.Text);
                    }
                    else if (nlsTOPCombo.SelectedIndex == 1)
                    {
                        num = num + (double)Convert.ToInt32(p1t9.Text) + (double)Convert.ToInt32(p1t10.Text) + (double)Convert.ToInt32(p1t11.Text);
                    }
                    else
                    {
                        num = num + (double)Convert.ToInt32(p1t9.Text) + (double)Convert.ToInt32(p1t10.Text) + (double)Convert.ToInt32(p1t11.Text) + (double)Convert.ToInt32(p1t12.Text);
                    }
                    dumpChart.Series[10].Points.AddXY(num / 1000000.0 * 344.0 / 2.0, 0.0);
                    dumpChart.Series[10].Points.AddXY(num / 1000000.0 * 343.0 / 2.0, 300.0);
                }
                if (p2NLSEnBox.Checked)
                {
                    double num = (double)(Convert.ToInt32(p2t1.Text) + Convert.ToInt32(p2t2.Text) + Convert.ToInt32(p2t3.Text) + Convert.ToInt32(p2t4.Text) + Convert.ToInt32(p2t5.Text) + Convert.ToInt32(p2t6.Text) + Convert.ToInt32(p2t7.Text) + Convert.ToInt32(p2t8.Text));
                    if (!p1NLSEnBox.Checked)
                    {
                        dumpChart.Series[10].Points.Clear();
                    }
                    if (nlsTOPCombo.SelectedIndex == 0)
                    {
                        num += (double)Convert.ToInt32(p2t9.Text);
                    }
                    else if (nlsTOPCombo.SelectedIndex == 1)
                    {
                        num = num + (double)Convert.ToInt32(p2t9.Text) + (double)Convert.ToInt32(p2t10.Text);
                    }
                    else if (nlsTOPCombo.SelectedIndex == 1)
                    {
                        num = num + (double)Convert.ToInt32(p2t9.Text) + (double)Convert.ToInt32(p2t10.Text) + (double)Convert.ToInt32(p2t11.Text);
                    }
                    else
                    {
                        num = num + (double)Convert.ToInt32(p2t9.Text) + (double)Convert.ToInt32(p2t10.Text) + (double)Convert.ToInt32(p2t11.Text) + (double)Convert.ToInt32(p2t12.Text);
                    }
                    dumpChart.Series[10].Points.AddXY(num / 1000000.0 * 344.0 / 2.0, 300.0);
                    dumpChart.Series[10].Points.AddXY(num / 1000000.0 * 343.0 / 2.0, 0.0);
                }
            }
            else
            {
                dumpChart.Series[10].Points.Clear();
            }
        }

        private void tofCalcRTT_TextChanged(object sender, EventArgs e)
        {
            TOFConverter("RTT");
        }

        private void tofCalcDist_TextChanged(object sender, EventArgs e)
        {
            TOFConverter("DIST");
        }

        private void tofCalcSound_TextChanged(object sender, EventArgs e)
        {
            TOFConverter("SOS");
        }

        private void TOFConverter(string input)
        {
            if (input == "RTT")
            {
                try
                {
                    double num = Convert.ToDouble(tofCalcRTT.Text);
                    double num2 = Convert.ToDouble(tofCalcDist.Text);
                    double num3 = Convert.ToDouble(tofCalcSound.Text);
                    num2 = num / 1000.0 * num3 / 2.0;
                    tofCalcDist.Text = Tools.Double_to_string(num2, 3);
                }
                catch
                {
                    tofCalcRTT.Text = "Invalid Time";
                    Tools.timeDelay(500, "ms");
                    tofCalcRTT.Text = "5.83";
                }
            }
            else if (input == "DIST")
            {
                try
                {
                    double num = Convert.ToDouble(tofCalcRTT.Text);
                    double num2 = Convert.ToDouble(tofCalcDist.Text);
                    double num3 = Convert.ToDouble(tofCalcSound.Text);
                    num = num2 / num3 * 2.0 * 1000.0;
                    tofCalcRTT.Text = Tools.Double_to_string(num, 3);
                }
                catch
                {
                    tofCalcDist.Text = "Invalid Distance";
                    Tools.timeDelay(500, "ms");
                    tofCalcDist.Text = "1";
                }
            }
            else
            {
                try
                {
                    double num = Convert.ToDouble(tofCalcRTT.Text);
                    double num2 = Convert.ToDouble(tofCalcDist.Text);
                    double num3 = Convert.ToDouble(tofCalcSound.Text);
                    num2 = num / 1000.0 * num3 / 2.0;
                    tofCalcDist.Text = Tools.Double_to_string(num2, 4);
                }
                catch
                {
                    tofCalcSound.Text = "Invalid Speed";
                    Tools.timeDelay(500, "ms");
                    tofCalcSound.Text = "343";
                }
            }
        }

        private void tableLayoutPanel13_Paint(object sender, PaintEventArgs e)
        {
        }

        private void cursorClear_Click(object sender, EventArgs e)
        {
            dumpChart.ChartAreas[0].CursorX.SetCursorPosition(-1.0);
            dumpChart.ChartAreas[0].CursorY.SetCursorPosition(-1.0);
            dumpCursorTime.Text = "";
            dumpCursorData.Text = "";
            dumpCursorDist.Text = "";
            dumpCursorGain.Text = "";
        }

        private void dumpChart_Click(object sender, EventArgs e)
        {
            double num = 5E-05 * Convert.ToDouble(tofCalcSound.Text);
            double num3 = Convert.ToDouble(p2RecordCombo.Text);
            num -= num3 / 128.0;
            dumpCursorDist.Text = Tools.Double_to_string(dumpChart.ChartAreas[0].CursorX.Position, 3);
            dumpCursorData.Text = Tools.Double_to_string(dumpChart.ChartAreas[0].CursorY.Position, 0);
            dumpCursorTime.Text = Tools.Double_to_string(dumpChart.ChartAreas[0].CursorX.Position / 343.0 * 1000.0 * 2.0 - num, 2);
        }

        private void tciIndexCombo_SelectedIndexChanged(object sender, EventArgs e)
        {
            switch (tciIndexCombo.SelectedIndex)
            {
                case 0:
                    tciIndexTab.SelectTab(CS0tempTab);
                    disableIndexW();
                    break;
                case 1:
                    tciIndexTab.SelectTab(CS1diagTab);
                    disableIndexW();
                    break;
                case 2:
                    tciIndexTab.SelectTab(CS2freqTab);
                    enableIndexW();
                    break;
                case 3:
                    tciIndexTab.SelectTab(CS3pulsesTab);
                    enableIndexW();
                    break;
                case 4:
                    tciIndexTab.SelectTab(CS4recTab);
                    enableIndexW();
                    break;
                case 5:
                    tciIndexTab.SelectTab(CS5thrp1Tab);
                    enableIndexW();
                    break;
                case 6:
                    tciIndexTab.SelectTab(CS6thrp2Tab);
                    enableIndexW();
                    break;
                case 7:
                    tciIndexTab.SelectTab(CS7ddpTab);
                    enableIndexW();
                    break;
                case 8:
                    tciIndexTab.SelectTab(CS8tvgTab);
                    enableIndexW();
                    break;
                case 9:
                    tciIndexTab.SelectTab(CS9userTab);
                    enableIndexW();
                    break;
                case 10:
                    tciIndexTab.SelectTab(CS10miscTab);
                    enableIndexW();
                    break;
                case 11:
                    tciIndexTab.SelectTab(CS11eeTab);
                    enableIndexW();
                    break;
                case 12:
                    tciIndexTab.SelectTab(CS12dumpTab);
                    disableIndexW();
                    break;
                case 13:
                    tciIndexTab.SelectTab(CS13bulkTab);
                    enableIndexW();
                    break;
                case 14:
                    tciIndexTab.SelectTab(CS14resTab);
                    disableIndexW();
                    break;
                case 15:
                    tciIndexTab.SelectTab(CS15crcTab);
                    disableIndexW();
                    break;
                default:
                    disableIndexW();
                    break;
            }
        }

        private void disableIndexW()
        {
            writeIndexBtn.Enabled = false;
        }

        private void enableIndexW()
        {
            writeIndexBtn.Enabled = true;
        }

        public void readSysDiagBtn_Click(object sender, EventArgs e)
        {
            byte b = 85;
            byte[] array = new byte[64];
            sysDiagVoltBox.Text = "";
            sysDiagFreqBox.Text = "";
            sysDiagDecayBox.Text = "";
            if (sysdiagBLCheck.Checked)
            {
                if (!DlSysDiagTCIFlag)
                {
                    string text = loopBox.Text;
                    loopBox.Text = "1";
                    runBtn_Click(null, null);
                    loopBox.Text = text;
                    if (sysdiagBLCheck.Checked)
                    {
                    }
                }
                else
                {
                    if (p1Radio.Checked)
                    {
                        tciCommandCombo.SelectedIndex = 0;
                    }
                    else
                    {
                        tciCommandCombo.SelectedIndex = 0;
                    }
                    runTCIBtn_Click(null, null);
                    tciCommandCombo.SelectedIndex = 4;
                }
            }
            Array.Clear(array, 0, 64);
            if (!DlSysDiagTCIFlag)
            {
                byte b2 = (byte)(((int)Convert.ToByte(uartAddrCombo.Text) << addrShift) + (int)uart_cmd8);
                byte b3 = calculate_UART_Checksum(new byte[]
                {
                    b2
                });
                common.u2a.UART_Write(3, new byte[]
                {
                    b,
                    b2,
                    b3
                });
                common.u2a.UART_Read(5, array);
            }
            try
            {
                sysDiagFreqBox.Text = Tools.Double_to_string(1.0 / ((double)array[1] * 5E-07) / 1000.0, 1);
                if (DlXdcrFreqCheck.Checked)
                {
                    if (DlSysDiagTCICheck.Checked)
                    {
                        datalogTextBox.AppendText("\r\nSysDiag XDCR Frequency (kHz): " + ind1VDiagR.GetFirstCharIndexOfCurrentLine());
                    }
                    else
                    {
                        datalogTextBox.AppendText("\r\nSysDiag XDCR Frequency (kHz): " + sysDiagFreqBox.Text);
                    }
                }
            }
            catch
            {
                sysDiagFreqBox.Text = "Fail";
            }
            try
            {
                sysDiagDecayBox.Text = Tools.Double_to_string((double)(array[2] * 16), 2);
                if (DlDecayCheck.Checked)
                {
                    if (DlSysDiagTCICheck.Checked)
                    {
                        datalogTextBox.AppendText("\r\nSysDiag XDCR Decay (us): " + ind1VDiagR.GetFirstCharIndexOfCurrentLine());
                    }
                    else
                    {
                        datalogTextBox.AppendText("\r\nSysDiag XDCR Decay (us): " + sysDiagDecayBox.Text);
                    }
                }
            }
            catch
            {
                sysDiagDecayBox.Text = "Fail";
            }
            try
            {
                if (DlSDBCheck.Checked)
                {
                    datalogTextBox.AppendText("\r\nSysDiag Freq Bit: " + UARTDIAG1_2_Stat_TextBox.Text);
                    datalogTextBox.AppendText("\r\nSysDiag Volt Bit: " + UARTDIAG1_3_Stat_TextBox.Text);
                }
            }
            catch
            {
                sysDiagDecayBox.Text = "Fail";
            }
        }

        public void trigReadTNBtn_Click(object sender, EventArgs e)
        {
            byte b = 85;
            byte[] array = new byte[64];
            if (tempOnlyFlag || !noiseOnlyFlag)
            {
                Array.Clear(array, 0, 64);
                byte b2 = (byte)(((int)Convert.ToByte(uartAddrCombo.Text) << addrShift) + (int)uart_cmd4);
                byte b3 = 0;
                byte b4 = calculate_UART_Checksum(new byte[]
                {
                    b2,
                    b3
                });
                common.u2a.UART_Write(4, new byte[]
                {
                    b,
                    b2,
                    b3,
                    b4
                });
                b2 = (byte)(((int)Convert.ToByte(uartAddrCombo.Text) << addrShift) + (int)uart_cmd6);
                b4 = calculate_UART_Checksum(new byte[]
                {
                    b2
                });
                common.u2a.UART_Write(3, new byte[]
                {
                    b,
                    b2,
                    b4
                });
                common.u2a.UART_Read(4, array);
                try
                {
                    tempBox.Text = Tools.Double_to_string((double)(((int)array[1] - tempoffsetCombo.SelectedIndex) / (1 + tempgainCombo.SelectedIndex / 128) - 64) / 1.5, 1);
                    if (ambientVPWR.Text == "")
                    {
                        ambientVPWR.SelectedIndex = 1;
                    }
                    ambientTemp.Text = Tools.Double_to_string(double.Parse(tempBox.Text) - 96.1 * (double.Parse(ambientVPWR.Text) * 0.012), 1);
                    if (DlTempCheck.Checked)
                    {
                        datalogTextBox.AppendText("\r\nDie Temperature (C): " + tempBox.Text);
                    }
                    if (DlAmbTempCheck.Checked)
                    {
                        datalogTextBox.AppendText("\r\nAmbient (C): " + ambientTemp.Text);
                    }
                }
                catch
                {
                    tempBox.Text = "Fail";
                }
            }
            if (!tempOnlyFlag || noiseOnlyFlag)
            {
                Array.Clear(array, 0, 64);
                byte b2 = (byte)(((int)Convert.ToByte(uartAddrCombo.Text) << addrShift) + (int)uart_cmd4);
                byte b3 = 1;
                byte b4 = calculate_UART_Checksum(new byte[]
                {
                    b2,
                    b3
                });
                common.u2a.UART_Write(4, new byte[]
                {
                    b,
                    b2,
                    b3,
                    b4
                });
                b2 = (byte)(((int)Convert.ToByte(uartAddrCombo.Text) << addrShift) + (int)uart_cmd6);
                b4 = calculate_UART_Checksum(new byte[]
                {
                    b2
                });
                common.u2a.UART_Write(3, new byte[]
                {
                    b,
                    b2,
                    b4
                });
                common.u2a.UART_Read(4, array);
                if (firstTimeNoise)
                {
                    Tools.timeDelay(100, "MS");
                    common.u2a.UART_Write(3, new byte[]
                    {
                        b,
                        b2,
                        b4
                    });
                    common.u2a.UART_Read(4, array);
                    firstTimeNoise = false;
                }
                try
                {
                    noiseBox.Text = Tools.Double_to_string((double)array[2], 0);
                    if (DlNoiseCheck.Checked)
                    {
                        datalogTextBox.AppendText("\r\nNoise Level : " + noiseBox.Text);
                    }
                }
                catch
                {
                    noiseBox.Text = "Fail";
                }
            }
        }

        private void readIndexBtn_Click(object sender, EventArgs e)
        {
            byte[] array = new byte[]
            {
                4
            };
            switch (tciIndexCombo.SelectedIndex)
            {
                case 0:
                    {
                        byte[] array2 = new byte[3];
                        array2[0] = 4;
                        array = array2;
                        break;
                    }
                case 1:
                    array = new byte[]
                    {
                    4,
                    0,
                    1
                    };
                    break;
                case 2:
                    array = new byte[]
                    {
                    4,
                    0,
                    2
                    };
                    break;
                case 3:
                    array = new byte[]
                    {
                    4,
                    0,
                    3
                    };
                    break;
                case 4:
                    array = new byte[]
                    {
                    4,
                    0,
                    4
                    };
                    break;
                case 5:
                    array = new byte[]
                    {
                    4,
                    0,
                    5
                    };
                    break;
                case 6:
                    array = new byte[]
                    {
                    4,
                    0,
                    6
                    };
                    break;
                case 7:
                    array = new byte[]
                    {
                    4,
                    0,
                    7
                    };
                    break;
                case 8:
                    array = new byte[]
                    {
                    4,
                    0,
                    8
                    };
                    break;
                case 9:
                    array = new byte[]
                    {
                    4,
                    0,
                    9
                    };
                    break;
                case 10:
                    array = new byte[]
                    {
                    4,
                    0,
                    10
                    };
                    break;
                case 11:
                    array = new byte[]
                    {
                    4,
                    0,
                    11
                    };
                    break;
                case 12:
                    array = new byte[]
                    {
                    4,
                    0,
                    12
                    };
                    break;
                case 13:
                    array = new byte[]
                    {
                    4,
                    0,
                    13
                    };
                    break;
                case 14:
                    array = new byte[]
                    {
                    4,
                    0,
                    14
                    };
                    break;
                case 15:
                    array = new byte[]
                    {
                    4,
                    0,
                    15
                    };
                    break;
            }
            run_buffer = array;
            runTCIBtn_Click(null, null);
            if (tciReturnBox.Text == "TCI Read Fail. Retry... ")
            {
                tciFailLoop += 1;
                if (tciFailLoop < 3)
                {
                    readIndexBtn_Click(null, null);
                }
                else
                {
                    tciFailLoop = 0;
                }
            }
            else
            {
                tciFailLoop = 0;
            }
        }

        private void writeIndexBtn_Click(object sender, EventArgs e)
        {
            string text = "";
            tciWriteInProcess = true;
            byte[] first = new byte[]
            {
                4
            };
            switch (tciIndexCombo.SelectedIndex)
            {
                case 0:
                    {
                        byte[] array = new byte[3];
                        array[0] = 4;
                        array[1] = 1;
                        first = array;
                        break;
                    }
                case 1:
                    first = new byte[]
                    {
                    4,
                    1,
                    1
                    };
                    break;
                case 2:
                    first = new byte[]
                    {
                    4,
                    1,
                    2
                    };
                    text = Tools.StringBase10_Into_StringBase2(Convert.ToString(ind2FreqW.SelectedIndex), 8, true);
                    break;
                case 3:
                    first = new byte[]
                    {
                    4,
                    1,
                    3
                    };
                    text = Tools.StringBase10_Into_StringBase2(Convert.ToString(ind3P1PW.SelectedIndex), 5, true) + Tools.StringBase10_Into_StringBase2(Convert.ToString(ind3P2PW.SelectedIndex), 5, true) + Tools.StringBase10_Into_StringBase2(Convert.ToString(ind3TCDW.SelectedIndex), 4, true) + Tools.StringBase10_Into_StringBase2(Convert.ToString(ind3PDeadW.SelectedIndex), 4, true);
                    break;
                case 4:
                    first = new byte[]
                    {
                    4,
                    1,
                    4
                    };
                    text = Tools.StringBase10_Into_StringBase2(Convert.ToString(ind4P1RW.SelectedIndex), 4, true) + Tools.StringBase10_Into_StringBase2(Convert.ToString(ind4P2RW.SelectedIndex), 4, true);
                    break;
                case 5:
                    first = new byte[]
                    {
                    4,
                    1,
                    5
                    };
                    text = string.Concat(new string[]
                    {
                    Tools.StringBase10_Into_StringBase2(Convert.ToString(p1t1.SelectedIndex), 4, true),
                    Tools.StringBase10_Into_StringBase2(Convert.ToString(p1t2.SelectedIndex), 4, true),
                    Tools.StringBase10_Into_StringBase2(Convert.ToString(p1t3.SelectedIndex), 4, true),
                    Tools.StringBase10_Into_StringBase2(Convert.ToString(p1t4.SelectedIndex), 4, true),
                    Tools.StringBase10_Into_StringBase2(Convert.ToString(p1t5.SelectedIndex), 4, true),
                    Tools.StringBase10_Into_StringBase2(Convert.ToString(p1t6.SelectedIndex), 4, true),
                    Tools.StringBase10_Into_StringBase2(Convert.ToString(p1t7.SelectedIndex), 4, true),
                    Tools.StringBase10_Into_StringBase2(Convert.ToString(p1t8.SelectedIndex), 4, true),
                    Tools.StringBase10_Into_StringBase2(Convert.ToString(p1t9.SelectedIndex), 4, true),
                    Tools.StringBase10_Into_StringBase2(Convert.ToString(p1t10.SelectedIndex), 4, true),
                    Tools.StringBase10_Into_StringBase2(Convert.ToString(p1t11.SelectedIndex), 4, true),
                    Tools.StringBase10_Into_StringBase2(Convert.ToString(p1t12.SelectedIndex), 4, true),
                    Tools.StringBase10_Into_StringBase2(Convert.ToString(p1l1.SelectedIndex), 5, true),
                    Tools.StringBase10_Into_StringBase2(Convert.ToString(p1l2.SelectedIndex), 5, true),
                    Tools.StringBase10_Into_StringBase2(Convert.ToString(p1l3.SelectedIndex), 5, true),
                    Tools.StringBase10_Into_StringBase2(Convert.ToString(p1l4.SelectedIndex), 5, true),
                    Tools.StringBase10_Into_StringBase2(Convert.ToString(p1l5.SelectedIndex), 5, true),
                    Tools.StringBase10_Into_StringBase2(Convert.ToString(p1l6.SelectedIndex), 5, true),
                    Tools.StringBase10_Into_StringBase2(Convert.ToString(p1l7.SelectedIndex), 5, true),
                    Tools.StringBase10_Into_StringBase2(Convert.ToString(p1l8.SelectedIndex), 5, true),
                    Tools.StringBase10_Into_StringBase2(Convert.ToString(p1l9.SelectedIndex), 8, true),
                    Tools.StringBase10_Into_StringBase2(Convert.ToString(p1l10.SelectedIndex), 8, true),
                    Tools.StringBase10_Into_StringBase2(Convert.ToString(p1l11.SelectedIndex), 8, true),
                    Tools.StringBase10_Into_StringBase2(Convert.ToString(p1l12.SelectedIndex), 8, true),
                    Tools.StringBase10_Into_StringBase2(Convert.ToString(p1lOff.SelectedIndex), 4, true)
                    });
                    break;
                case 6:
                    first = new byte[]
                    {
                    4,
                    1,
                    6
                    };
                    eepromThrCRCBtn_Click(null, null);
                    text = string.Concat(new string[]
                    {
                    Tools.StringBase10_Into_StringBase2(Convert.ToString(p2t1.SelectedIndex), 4, true),
                    Tools.StringBase10_Into_StringBase2(Convert.ToString(p2t2.SelectedIndex), 4, true),
                    Tools.StringBase10_Into_StringBase2(Convert.ToString(p2t3.SelectedIndex), 4, true),
                    Tools.StringBase10_Into_StringBase2(Convert.ToString(p2t4.SelectedIndex), 4, true),
                    Tools.StringBase10_Into_StringBase2(Convert.ToString(p2t5.SelectedIndex), 4, true),
                    Tools.StringBase10_Into_StringBase2(Convert.ToString(p2t6.SelectedIndex), 4, true),
                    Tools.StringBase10_Into_StringBase2(Convert.ToString(p2t7.SelectedIndex), 4, true),
                    Tools.StringBase10_Into_StringBase2(Convert.ToString(p2t8.SelectedIndex), 4, true),
                    Tools.StringBase10_Into_StringBase2(Convert.ToString(p2t9.SelectedIndex), 4, true),
                    Tools.StringBase10_Into_StringBase2(Convert.ToString(p2t10.SelectedIndex), 4, true),
                    Tools.StringBase10_Into_StringBase2(Convert.ToString(p2t11.SelectedIndex), 4, true),
                    Tools.StringBase10_Into_StringBase2(Convert.ToString(p2t12.SelectedIndex), 4, true),
                    Tools.StringBase10_Into_StringBase2(Convert.ToString(p2l1.SelectedIndex), 5, true),
                    Tools.StringBase10_Into_StringBase2(Convert.ToString(p2l2.SelectedIndex), 5, true),
                    Tools.StringBase10_Into_StringBase2(Convert.ToString(p2l3.SelectedIndex), 5, true),
                    Tools.StringBase10_Into_StringBase2(Convert.ToString(p2l4.SelectedIndex), 5, true),
                    Tools.StringBase10_Into_StringBase2(Convert.ToString(p2l5.SelectedIndex), 5, true),
                    Tools.StringBase10_Into_StringBase2(Convert.ToString(p2l6.SelectedIndex), 5, true),
                    Tools.StringBase10_Into_StringBase2(Convert.ToString(p2l7.SelectedIndex), 5, true),
                    Tools.StringBase10_Into_StringBase2(Convert.ToString(p2l8.SelectedIndex), 5, true),
                    Tools.StringBase10_Into_StringBase2(Convert.ToString(p2l9.SelectedIndex), 8, true),
                    Tools.StringBase10_Into_StringBase2(Convert.ToString(p2l10.SelectedIndex), 8, true),
                    Tools.StringBase10_Into_StringBase2(Convert.ToString(p2l11.SelectedIndex), 8, true),
                    Tools.StringBase10_Into_StringBase2(Convert.ToString(p2l12.SelectedIndex), 8, true),
                    Tools.StringBase10_Into_StringBase2(Convert.ToString(p2lOff.SelectedIndex), 4, true)
                    });
                    break;
                case 7:
                    first = new byte[]
                    {
                    4,
                    1,
                    7
                    };
                    text = string.Concat(new string[]
                    {
                    Tools.StringBase10_Into_StringBase2(Convert.ToString(ind7BPFBW.SelectedIndex), 2, true),
                    Tools.StringBase10_Into_StringBase2(Convert.ToString(ind7InitGainW.SelectedIndex), 6, true),
                    Tools.StringBase10_Into_StringBase2(Convert.ToString(ind7LPFCOW.SelectedIndex), 2, true),
                    Tools.StringBase10_Into_StringBase2(Convert.ToString(ind7NLSNLW.SelectedIndex), 5, true),
                    Tools.StringBase10_Into_StringBase2(Convert.ToString(ind7NLSEW.SelectedIndex), 1, true),
                    Tools.StringBase10_Into_StringBase2(Convert.ToString(ind7NLSOW.SelectedIndex), 2, true),
                    Tools.StringBase10_Into_StringBase2(Convert.ToString(ind7TSGW.SelectedIndex), 4, true),
                    Tools.StringBase10_Into_StringBase2(Convert.ToString(ind7TSOW.SelectedIndex), 4, true),
                    Tools.StringBase10_Into_StringBase2(Convert.ToString(ind7P1DGSTW.SelectedIndex), 2, true),
                    Tools.StringBase10_Into_StringBase2(Convert.ToString(ind7P1DGLRW.SelectedIndex), 3, true),
                    Tools.StringBase10_Into_StringBase2(Convert.ToString(ind7P1DGSRW.SelectedIndex), 3, true),
                    Tools.StringBase10_Into_StringBase2(Convert.ToString(ind7P2DGSTW.SelectedIndex), 2, true),
                    Tools.StringBase10_Into_StringBase2(Convert.ToString(ind7P2DGLRW.SelectedIndex), 3, true),
                    Tools.StringBase10_Into_StringBase2(Convert.ToString(ind7P2DGSRW.SelectedIndex), 3, true)
                    });
                    break;
                case 8:
                    first = new byte[]
                    {
                    4,
                    1,
                    8
                    };
                    text = string.Concat(new string[]
                    {
                    Tools.StringBase10_Into_StringBase2(Convert.ToString(tvgt0.SelectedIndex), 4, true),
                    Tools.StringBase10_Into_StringBase2(Convert.ToString(tvgt1.SelectedIndex), 4, true),
                    Tools.StringBase10_Into_StringBase2(Convert.ToString(tvgt2.SelectedIndex), 4, true),
                    Tools.StringBase10_Into_StringBase2(Convert.ToString(tvgt3.SelectedIndex), 4, true),
                    Tools.StringBase10_Into_StringBase2(Convert.ToString(tvgt4.SelectedIndex), 4, true),
                    Tools.StringBase10_Into_StringBase2(Convert.ToString(tvgt5.SelectedIndex), 4, true),
                    Tools.StringBase10_Into_StringBase2(Convert.ToString(tvgg1.SelectedIndex), 6, true),
                    Tools.StringBase10_Into_StringBase2(Convert.ToString(tvgg2.SelectedIndex), 6, true),
                    Tools.StringBase10_Into_StringBase2(Convert.ToString(tvgg3.SelectedIndex), 6, true),
                    Tools.StringBase10_Into_StringBase2(Convert.ToString(tvgg4.SelectedIndex), 6, true),
                    Tools.StringBase10_Into_StringBase2(Convert.ToString(tvgg5.SelectedIndex), 6, true),
                    "00"
                    });
                    break;
                case 9:
                    first = new byte[]
                    {
                    4,
                    1,
                    9
                    };
                    text = string.Concat(new string[]
                    {
                    Tools.StringBase10_Into_StringBase2(Convert.ToString(ind9U1W.SelectedIndex), 8, true),
                    Tools.StringBase10_Into_StringBase2(Convert.ToString(ind9U2W.SelectedIndex), 8, true),
                    Tools.StringBase10_Into_StringBase2(Convert.ToString(ind9U3W.SelectedIndex), 8, true),
                    Tools.StringBase10_Into_StringBase2(Convert.ToString(ind9U4W.SelectedIndex), 8, true),
                    Tools.StringBase10_Into_StringBase2(Convert.ToString(ind9U5W.SelectedIndex), 8, true),
                    Tools.StringBase10_Into_StringBase2(Convert.ToString(ind9U6W.SelectedIndex), 8, true),
                    Tools.StringBase10_Into_StringBase2(Convert.ToString(ind9U7W.SelectedIndex), 8, true),
                    Tools.StringBase10_Into_StringBase2(Convert.ToString(ind9U8W.SelectedIndex), 8, true),
                    Tools.StringBase10_Into_StringBase2(Convert.ToString(ind9U9W.SelectedIndex), 8, true),
                    Tools.StringBase10_Into_StringBase2(Convert.ToString(ind9U10W.SelectedIndex), 8, true),
                    Tools.StringBase10_Into_StringBase2(Convert.ToString(ind9U11W.SelectedIndex), 8, true),
                    Tools.StringBase10_Into_StringBase2(Convert.ToString(ind9U12W.SelectedIndex), 8, true),
                    Tools.StringBase10_Into_StringBase2(Convert.ToString(ind9U13W.SelectedIndex), 8, true),
                    Tools.StringBase10_Into_StringBase2(Convert.ToString(ind9U14W.SelectedIndex), 8, true),
                    Tools.StringBase10_Into_StringBase2(Convert.ToString(ind9U15W.SelectedIndex), 8, true),
                    Tools.StringBase10_Into_StringBase2(Convert.ToString(ind9U16W.SelectedIndex), 8, true),
                    Tools.StringBase10_Into_StringBase2(Convert.ToString(ind9U17W.SelectedIndex), 8, true),
                    Tools.StringBase10_Into_StringBase2(Convert.ToString(ind9U18W.SelectedIndex), 8, true),
                    Tools.StringBase10_Into_StringBase2(Convert.ToString(ind9U19W.SelectedIndex), 8, true),
                    Tools.StringBase10_Into_StringBase2(Convert.ToString(ind9U20W.SelectedIndex), 8, true)
                    });
                    break;
                case 10:
                    first = new byte[]
                    {
                    4,
                    1,
                    10
                    };
                    text = string.Concat(new string[]
                    {
                    Tools.StringBase10_Into_StringBase2(Convert.ToString(ind10FDLW.SelectedIndex), 4, true),
                    Tools.StringBase10_Into_StringBase2(Convert.ToString(ind10FDSW.SelectedIndex), 4, true),
                    Tools.StringBase10_Into_StringBase2(Convert.ToString(ind10FDETW.SelectedIndex), 3, true),
                    Tools.StringBase10_Into_StringBase2(Convert.ToString(ind10STW.SelectedIndex), 4, true),
                    Tools.StringBase10_Into_StringBase2(Convert.ToString(ind10P1NLSEW.SelectedIndex), 1, true),
                    Tools.StringBase10_Into_StringBase2(Convert.ToString(ind10P2NLSEW.SelectedIndex), 1, true),
                    Tools.StringBase10_Into_StringBase2(Convert.ToString(ind10VOTW.SelectedIndex), 2, true),
                    Tools.StringBase10_Into_StringBase2(Convert.ToString(ind10LPMTW.SelectedIndex), 2, true),
                    Tools.StringBase10_Into_StringBase2(Convert.ToString(ind10FETW.SelectedIndex), 3, true),
                    Tools.StringBase10_Into_StringBase2(Convert.ToString(ind10AGRW.SelectedIndex), 2, true),
                    Tools.StringBase10_Into_StringBase2(Convert.ToString(ind10LPMEW.SelectedIndex), 1, true),
                    Tools.StringBase10_Into_StringBase2(Convert.ToString(ind10DSW.SelectedIndex), 1, true),
                    Tools.StringBase10_Into_StringBase2(Convert.ToString(ind10DTW.SelectedIndex), 4, true),
                    Tools.StringBase10_Into_StringBase2(Convert.ToString(ind10DCLW.SelectedIndex), 1, true),
                    Tools.StringBase10_Into_StringBase2(Convert.ToString(ind10DIMW.SelectedIndex), 1, true),
                    Tools.StringBase10_Into_StringBase2(Convert.ToString(ind10P1ILimW.SelectedIndex), 6, true),
                    Tools.StringBase10_Into_StringBase2(Convert.ToString(ind10P2ILimW.SelectedIndex), 6, true)
                    });
                    break;
                case 11:
                    first = new byte[]
                    {
                    4,
                    1,
                    11
                    };
                    text = string.Concat(new string[]
                    {
                    Tools.StringBase10_Into_StringBase2(Convert.ToString(ind11EDDEW.SelectedIndex), 1, true),
                    Tools.StringBase10_Into_StringBase2(Convert.ToString(ind11EEPPW.SelectedIndex), 4, true),
                    "0",
                    Tools.StringBase10_Into_StringBase2(Convert.ToString(ind11ReloadEEW.SelectedIndex), 1, true),
                    Tools.StringBase10_Into_StringBase2(Convert.ToString(ind11PgrmEEW.SelectedIndex), 1, true)
                    });
                    break;
                case 12:
                    first = new byte[]
                    {
                    4,
                    1,
                    12
                    };
                    break;
                case 13:
                    {
                        first = new byte[]
                        {
                    4,
                    1,
                    13
                        };
                        string[] addressGridList = GRID_USER_MEMSPACE.getAddressGridList();
                        GRID_USER_MEMSPACE.HighlightRows(addressGridList);
                        string[,] array2 = GRID_USER_MEMSPACE.Get_HIGHLIGHTED_Grid_Values__R_D_A_O(false);
                        int numberOfHighlighedRows = GRID_USER_MEMSPACE.getNumberOfHighlighedRows();
                        SelectedGrid = "GRID_USER_MEMSPACE";
                        desel_grid_butt_Click(null, null);
                        for (int i = 0; i < 44; i++)
                        {
                            text += Tools.StringBase16_Into_StringBase2(array2[i, 2], 8, true);
                        }
                        int length = text.Length;
                        break;
                    }
                case 14:
                    first = new byte[]
                    {
                    4,
                    1,
                    14
                    };
                    break;
                case 15:
                    first = new byte[]
                    {
                    4,
                    1,
                    15
                    };
                    break;
            }
            run_buffer = first.Concat(tciWriteChecksum(tciIndexCombo.SelectedIndex, text)).ToArray<byte>();
            runTCIBtn_Click(null, null);
        }

        private byte[] tciWriteChecksum(int index, string data)
        {
            data = "1" + Tools.StringBase10_Into_StringBase2(Convert.ToString(index), 4, true) + data;
            while (data.Length % 8 != 0)
            {
                data += "0";
            }
            byte[] array = new byte[data.Length / 8];
            byte[] array2 = new byte[data.Length / 8 + 1];
            int i;
            for (i = 0; i < data.Length / 8; i++)
            {
                array[i] = Convert.ToByte(data.Substring(8 * i, 8), 2);
                array2[i] = array[i];
            }
            array2[i] = calculate_UART_Checksum(array);
            return array2;
        }

        private void tciLoopInfCheck_CheckedChanged(object sender, EventArgs e)
        {
            int num = 0;
            if (PGA46xStat_box.Text.Contains("Ready") || freqCombo.Text != "")
            {
                infLoopSet = true;
                infErrorFlag = false;
                tciLoopCountBox.Text = "1";
                tciLoopInfCheck.Text = "Stop Loops";
                tciCommandCombo_SelectedIndexChanged(null, null);
                while (tciLoopInfCheck.Checked)
                {
                    tciLoopInfCountReal.Visible = true;
                    tciLoopInfCountReal.Text = Convert.ToString(num);
                    for (int i = 0; i < (int)Convert.ToInt16(tciLoopCountBox.Text); i++)
                    {
                        num++;
                        tciLoopInfCountReal.Text = Convert.ToString(num);
                        if (tciLoopDelayBox.Text == "")
                        {
                            tciLoopDelayBox.Text = "0";
                        }
                        Tools.timeDelay(Convert.ToDouble(tciLoopDelayBox.Text), "MS");
                        common.u2a.SendCommand(41, run_buffer, 1);
                        Tools.timeDelay(10, "MS");
                        tciLoopCountInd.Text = Convert.ToString(i + 1);
                        if (exportTCICheck.Checked || plotTCICheck.Checked)
                        {
                            printTCI();
                        }
                        else
                        {
                            Tools.timeDelay(70, "MS");
                        }
                    }
                }
                if (infErrorFlag && tciLoopCountBox.Text == "1")
                {
                }
                tciLoopInfCheck.Text = "Run Infinitely";
                tciLoopCountInd.Text = "0";
                infLoopSet = false;
                txtToken = true;
                tciLoopInfCountReal.Visible = false;
                tciLoopCountInd.Text = tciLoopInfCountReal.Text;
            }
            else
            {
                MessageBox.Show("No PGA460-Q1 device connected.");
            }
        }

        public void tempOnlyBtn_Click(object sender, EventArgs e)
        {
            tempOnlyFlag = true;
            trigReadTNBtn_Click(null, null);
            tempOnlyFlag = false;
        }

        public void NoiseOnlyBtn_Click(object sender, EventArgs e)
        {
            noiseOnlyFlag = true;
            trigReadTNBtn_Click(null, null);
            noiseOnlyFlag = false;
        }

        private void exportTCICheck_CheckedChanged(object sender, EventArgs e)
        {
            if (exportTCICheck.Checked)
            {
                plotTCICheck.Checked = true;
                plotTCICheck.Enabled = false;
            }
            else
            {
                plotTCICheck.Enabled = true;
            }
        }

        private void NOP(long durationTicks)
        {
            Stopwatch stopwatch = Stopwatch.StartNew();
            while (stopwatch.ElapsedTicks < durationTicks)
            {
            }
        }

        public static string UART_Read_Write(byte register_index, byte data_H, bool Write_notRead)
        {
            byte b = 85;
            byte b2 = 0;
            byte b3 = 0;
            byte b4 = 0;
            byte b5 = 9;
            byte b6 = 10;
            int num = 5;
            byte[] array = new byte[64];
            Array.Clear(array, 0, 64);
            if (Write_notRead)
            {
                if (changeUartAddr)
                {
                    b2 = (byte)(((int)Convert.ToByte(uartAddrOld) << num) + (int)b6);
                }
                else
                {
                    b2 = (byte)(((int)Convert.ToByte(uartAddrComboText) << num) + (int)b6);
                }
                b3 = data_H;
                b4 = calculate_UART_Checksum(new byte[]
                {
                    b2,
                    register_index,
                    b3
                });
            }
            if (!Write_notRead)
            {
                if (changeUartAddr)
                {
                    b2 = (byte)(((int)Convert.ToByte(uartAddrOld) << num) + (int)b5);
                }
                else
                {
                    b2 = (byte)(((int)Convert.ToByte(uartAddrComboText) << num) + (int)b5);
                }
                b4 = calculate_UART_Checksum(new byte[]
                {
                    b2,
                    register_index
                });
            }
            if (Write_notRead)
            {
                common.u2a.UART_Write(5, new byte[]
                {
                    b,
                    b2,
                    register_index,
                    b3,
                    b4
                });
                common.u2a.UART_Read(3, array);
            }
            string result;
            if (!Write_notRead)
            {
                common.u2a.UART_Write(4, new byte[]
                {
                    b,
                    b2,
                    register_index,
                    b4
                });
                common.u2a.UART_Read(3, array);
                result = Tools.int32_Into_stringBase16((int)array[1], 8);
            }
            else
            {
                result = Tools.int32_Into_stringBase16((int)b3, 8);
            }
            return result;
        }

        public static void singleRegImmediateUpdate()
        {
            singleRegImmediateUpdateFlag = true;
        }

        public static byte calculate_UART_Checksum(byte[] ChecksumInput)
        {
            uint num = 0u;
            for (int i = 0; i < ChecksumInput.Length; i++)
            {
                if ((uint)ChecksumInput[i] + num < num)
                {
                    num = num + (uint)ChecksumInput[i] + 1u;
                }
                else
                {
                    num += (uint)ChecksumInput[i];
                }
                if (num > 255u)
                {
                    num -= 255u;
                }
            }
            num = (~num & 255u);
            return Convert.ToByte(num);
        }

        private void forceReadyBtn_Click(object sender, EventArgs e)
        {
            defaultAllGeneralBtn_Click(null, null);
            defaultAllDiagRegsBtn_Click(null, null);
            allMidCodeTVGBtn_Click(null, null);
            allMidCodeBtn_Click(null, null);
            PGA46xStat_box.Text = "Ready (Simulation)";
        }

        public void datalogClear_btn_Click(object sender, EventArgs e)
        {
            datalogTextBox.Clear();
        }

        public void datalogSave_btn_Click(object sender, EventArgs e)
        {
            File.WriteAllText(string.Concat(new string[]
            {
                Environment.GetFolderPath(Environment.SpecialFolder.Personal),
                "\\BOOSTXL-PGA460\\pga460-gui-datalog_",
                DateTime.Now.ToString("yyyy-MM-dd_HHmmss"),
                ".",
                exportSaveAs
            }), datalogTextBox.Text);
            datalogTextBox.AppendText("\n\rDatalog saved to 'My Documents/BOOSTXL-PGA460'!\n\r");
        }

        private void getResultsBtn_Click(object sender, EventArgs e)
        {
            byte b = Convert.ToByte(numObjToDetCombo.Text);
            Tools.timeDelay(100, "MS");
            common.u2a.UART_Read(64, uart_return_data);
            Array.Clear(uart_return_data, 0, 64);
            commandByte = (byte)(((int)Convert.ToByte(uartAddrCombo.Text) << addrShift) + (int)uart_cmd5);
            MChecksumByte = calculate_UART_Checksum(new byte[]
            {
                commandByte
            });
            common.u2a.UART_Write(3, new byte[]
            {
                syncByte,
                commandByte,
                MChecksumByte
            });
            common.u2a.UART_Read((byte)(b * 4 + 1), uart_return_data);
            uartDiagB = uart_return_data[0];
        }

        private void customIndexBtn_Click(object sender, EventArgs e)
        {
            common.u2a.SendCommand(41, new byte[]
            {
                Tools.StringBase10_Into_Byte(customIndexIn.Text)
            }, 1);
        }

        private void AFEGainRngCombo_SelectedIndexChanged(object sender, EventArgs e)
        {
            if (!firstTime)
            {
                RegDefs regDefs = new RegDefs("26 (DECPL_TEMP)");
                regDefs.DECPL_TEMP.ReadFromUART();
                switch (AFEGainRngCombo.SelectedIndex)
                {
                    case 0:
                        regDefs.AFE_GAIN_RNG.value = "00";
                        break;
                    case 1:
                        regDefs.AFE_GAIN_RNG.value = "01";
                        break;
                    case 2:
                        regDefs.AFE_GAIN_RNG.value = "10";
                        break;
                    case 3:
                        regDefs.AFE_GAIN_RNG.value = "11";
                        break;
                }
                regDefs.DECPL_TEMP.WriteToUART();
                if (primaryTab.SelectedTab.Name != "MemMap" && !firstTime)
                {
                    string[,] array = new string[1, 5];
                    array[0, 0] = regDefs.DECPL_TEMP.location.ToString();
                    array[0, 1] = "26 (DECPL_TEMP)";
                    array[0, 4] = Tools.int32_Into_stringBase16(calRegValfromFields(regDefs.DECPL_TEMP.bit_fields));
                    updateMemoryMap(array, "GRID_USER_MEMSPACE");
                }
            }
            int selectedIndex = tvgg0.SelectedIndex;
            int selectedIndex2 = tvgg1.SelectedIndex;
            int selectedIndex3 = tvgg2.SelectedIndex;
            int selectedIndex4 = tvgg3.SelectedIndex;
            int selectedIndex5 = tvgg4.SelectedIndex;
            int selectedIndex6 = tvgg5.SelectedIndex;
            if (AFEGainRngCombo.SelectedIndex == 0)
            {
                gainCombo.Items.Clear();
                bd_afeGainCombo.Items.Clear();
                gainCombo.Items.AddRange(new object[]
                {
                    "58.5",
                    "59",
                    "59.5",
                    "60",
                    "60.5",
                    "61",
                    "61.5",
                    "62",
                    "62.5",
                    "63",
                    "63.5",
                    "64",
                    "64.5",
                    "65",
                    "65.5",
                    "66",
                    "66.5",
                    "67",
                    "67.5",
                    "68",
                    "68.5",
                    "69",
                    "69.5",
                    "70",
                    "70.5",
                    "71",
                    "71.5",
                    "72",
                    "72.5",
                    "73",
                    "73.5",
                    "74",
                    "74.5",
                    "75",
                    "75.5",
                    "76",
                    "76.5",
                    "77",
                    "77.5",
                    "78",
                    "78.5",
                    "79",
                    "79.5",
                    "80",
                    "80.5",
                    "81",
                    "81.5",
                    "82",
                    "82.5",
                    "83",
                    "83.5",
                    "84",
                    "84.5",
                    "85",
                    "85.5",
                    "86",
                    "86.5",
                    "87",
                    "87.5",
                    "88",
                    "88.5",
                    "89",
                    "89.5",
                    "90"
                });
                bd_afeGainCombo.Items.AddRange(new object[]
                {
                    "58.5",
                    "59",
                    "59.5",
                    "60",
                    "60.5",
                    "61",
                    "61.5",
                    "62",
                    "62.5",
                    "63",
                    "63.5",
                    "64",
                    "64.5",
                    "65",
                    "65.5",
                    "66",
                    "66.5",
                    "67",
                    "67.5",
                    "68",
                    "68.5",
                    "69",
                    "69.5",
                    "70",
                    "70.5",
                    "71",
                    "71.5",
                    "72",
                    "72.5",
                    "73",
                    "73.5",
                    "74",
                    "74.5",
                    "75",
                    "75.5",
                    "76",
                    "76.5",
                    "77",
                    "77.5",
                    "78",
                    "78.5",
                    "79",
                    "79.5",
                    "80",
                    "80.5",
                    "81",
                    "81.5",
                    "82",
                    "82.5",
                    "83",
                    "83.5",
                    "84",
                    "84.5",
                    "85",
                    "85.5",
                    "86",
                    "86.5",
                    "87",
                    "87.5",
                    "88",
                    "88.5",
                    "89",
                    "89.5",
                    "90"
                });
                tvgg0.Items.Clear();
                tvgg1.Items.Clear();
                tvgg2.Items.Clear();
                tvgg3.Items.Clear();
                tvgg4.Items.Clear();
                tvgg5.Items.Clear();
                for (int i = 0; i < 64; i++)
                {
                    tvgg0.Items.Add(gainCombo.Items[i]);
                    tvgg1.Items.Add(gainCombo.Items[i]);
                    tvgg2.Items.Add(gainCombo.Items[i]);
                    tvgg3.Items.Add(gainCombo.Items[i]);
                    tvgg4.Items.Add(gainCombo.Items[i]);
                    tvgg5.Items.Add(gainCombo.Items[i]);
                }
                gainCombo.Text = "58.5";
                tvgg0.SelectedIndex = selectedIndex;
                tvgg1.SelectedIndex = selectedIndex2;
                tvgg2.SelectedIndex = selectedIndex3;
                tvgg3.SelectedIndex = selectedIndex4;
                tvgg4.SelectedIndex = selectedIndex5;
                tvgg5.SelectedIndex = selectedIndex6;
                dumpChart.ChartAreas[0].AxisY2.Minimum = 50.0;
                dumpChart.ChartAreas[0].AxisY2.Maximum = 90.0;
            }
            else if (AFEGainRngCombo.SelectedIndex == 1)
            {
                gainCombo.Items.Clear();
                bd_afeGainCombo.Items.Clear();
                tvgg0.Items.Clear();
                tvgg1.Items.Clear();
                tvgg2.Items.Clear();
                tvgg3.Items.Clear();
                tvgg4.Items.Clear();
                tvgg5.Items.Clear();
                gainCombo.Items.AddRange(new object[]
                {
                    "52.5",
                    "53",
                    "53.5",
                    "54",
                    "54.5",
                    "55",
                    "55.5",
                    "56",
                    "56.5",
                    "57",
                    "57.5",
                    "58",
                    "58.5",
                    "59",
                    "59.5",
                    "60",
                    "60.5",
                    "61",
                    "61.5",
                    "62",
                    "62.5",
                    "63",
                    "63.5",
                    "64",
                    "64.5",
                    "65",
                    "65.5",
                    "66",
                    "66.5",
                    "67",
                    "67.5",
                    "68",
                    "68.5",
                    "69",
                    "69.5",
                    "70",
                    "70.5",
                    "71",
                    "71.5",
                    "72",
                    "72.5",
                    "73",
                    "73.5",
                    "74",
                    "74.5",
                    "75",
                    "75.5",
                    "76",
                    "76.5",
                    "77",
                    "77.5",
                    "78",
                    "78.5",
                    "79",
                    "79.5",
                    "80",
                    "80.5",
                    "81",
                    "81.5",
                    "82",
                    "82.5",
                    "83",
                    "83.5",
                    "84"
                });
                bd_afeGainCombo.Items.AddRange(new object[]
                {
                    "52.5",
                    "53",
                    "53.5",
                    "54",
                    "54.5",
                    "55",
                    "55.5",
                    "56",
                    "56.5",
                    "57",
                    "57.5",
                    "58",
                    "58.5",
                    "59",
                    "59.5",
                    "60",
                    "60.5",
                    "61",
                    "61.5",
                    "62",
                    "62.5",
                    "63",
                    "63.5",
                    "64",
                    "64.5",
                    "65",
                    "65.5",
                    "66",
                    "66.5",
                    "67",
                    "67.5",
                    "68",
                    "68.5",
                    "69",
                    "69.5",
                    "70",
                    "70.5",
                    "71",
                    "71.5",
                    "72",
                    "72.5",
                    "73",
                    "73.5",
                    "74",
                    "74.5",
                    "75",
                    "75.5",
                    "76",
                    "76.5",
                    "77",
                    "77.5",
                    "78",
                    "78.5",
                    "79",
                    "79.5",
                    "80",
                    "80.5",
                    "81",
                    "81.5",
                    "82",
                    "82.5",
                    "83",
                    "83.5",
                    "84"
                });
                for (int i = 0; i < 64; i++)
                {
                    tvgg0.Items.Add(gainCombo.Items[i]);
                    tvgg1.Items.Add(gainCombo.Items[i]);
                    tvgg2.Items.Add(gainCombo.Items[i]);
                    tvgg3.Items.Add(gainCombo.Items[i]);
                    tvgg4.Items.Add(gainCombo.Items[i]);
                    tvgg5.Items.Add(gainCombo.Items[i]);
                }
                gainCombo.Text = "52.5";
                tvgg0.SelectedIndex = selectedIndex;
                tvgg1.SelectedIndex = selectedIndex2;
                tvgg2.SelectedIndex = selectedIndex3;
                tvgg3.SelectedIndex = selectedIndex4;
                tvgg4.SelectedIndex = selectedIndex5;
                tvgg5.SelectedIndex = selectedIndex6;
                dumpChart.ChartAreas[0].AxisY2.Minimum = 50.0;
                dumpChart.ChartAreas[0].AxisY2.Maximum = 90.0;
            }
            else if (AFEGainRngCombo.SelectedIndex == 2)
            {
                gainCombo.Items.Clear();
                bd_afeGainCombo.Items.Clear();
                tvgg0.Items.Clear();
                tvgg1.Items.Clear();
                tvgg2.Items.Clear();
                tvgg3.Items.Clear();
                tvgg4.Items.Clear();
                tvgg5.Items.Clear();
                gainCombo.Items.AddRange(new object[]
                {
                    "46.5",
                    "47",
                    "47.5",
                    "48",
                    "48.5",
                    "49",
                    "49.5",
                    "50",
                    "50.5",
                    "51",
                    "51.5",
                    "52",
                    "52.5",
                    "53",
                    "53.5",
                    "54",
                    "54.5",
                    "55",
                    "55.5",
                    "56",
                    "56.5",
                    "57",
                    "57.5",
                    "58",
                    "58.5",
                    "59",
                    "59.5",
                    "60",
                    "60.5",
                    "61",
                    "61.5",
                    "62",
                    "62.5",
                    "63",
                    "63.5",
                    "64",
                    "64.5",
                    "65",
                    "65.5",
                    "66",
                    "66.5",
                    "67",
                    "67.5",
                    "68",
                    "68.5",
                    "69",
                    "69.5",
                    "70",
                    "70.5",
                    "71",
                    "71.5",
                    "72",
                    "72.5",
                    "73",
                    "73.5",
                    "74",
                    "74.5",
                    "75",
                    "75.5",
                    "76",
                    "76.5",
                    "77",
                    "77.5",
                    "78"
                });
                bd_afeGainCombo.Items.AddRange(new object[]
                {
                    "46.5",
                    "47",
                    "47.5",
                    "48",
                    "48.5",
                    "49",
                    "49.5",
                    "50",
                    "50.5",
                    "51",
                    "51.5",
                    "52",
                    "52.5",
                    "53",
                    "53.5",
                    "54",
                    "54.5",
                    "55",
                    "55.5",
                    "56",
                    "56.5",
                    "57",
                    "57.5",
                    "58",
                    "58.5",
                    "59",
                    "59.5",
                    "60",
                    "60.5",
                    "61",
                    "61.5",
                    "62",
                    "62.5",
                    "63",
                    "63.5",
                    "64",
                    "64.5",
                    "65",
                    "65.5",
                    "66",
                    "66.5",
                    "67",
                    "67.5",
                    "68",
                    "68.5",
                    "69",
                    "69.5",
                    "70",
                    "70.5",
                    "71",
                    "71.5",
                    "72",
                    "72.5",
                    "73",
                    "73.5",
                    "74",
                    "74.5",
                    "75",
                    "75.5",
                    "76",
                    "76.5",
                    "77",
                    "77.5",
                    "78"
                });
                for (int i = 0; i < 64; i++)
                {
                    tvgg0.Items.Add(gainCombo.Items[i]);
                    tvgg1.Items.Add(gainCombo.Items[i]);
                    tvgg2.Items.Add(gainCombo.Items[i]);
                    tvgg3.Items.Add(gainCombo.Items[i]);
                    tvgg4.Items.Add(gainCombo.Items[i]);
                    tvgg5.Items.Add(gainCombo.Items[i]);
                }
                gainCombo.Text = "46.5";
                tvgg0.SelectedIndex = selectedIndex;
                tvgg1.SelectedIndex = selectedIndex2;
                tvgg2.SelectedIndex = selectedIndex3;
                tvgg3.SelectedIndex = selectedIndex4;
                tvgg4.SelectedIndex = selectedIndex5;
                tvgg5.SelectedIndex = selectedIndex6;
                dumpChart.ChartAreas[0].AxisY2.Minimum = 40.0;
                dumpChart.ChartAreas[0].AxisY2.Maximum = 80.0;
            }
            else if (AFEGainRngCombo.SelectedIndex == 3)
            {
                gainCombo.Items.Clear();
                bd_afeGainCombo.Items.Clear();
                tvgg0.Items.Clear();
                tvgg1.Items.Clear();
                tvgg2.Items.Clear();
                tvgg3.Items.Clear();
                tvgg4.Items.Clear();
                tvgg5.Items.Clear();
                gainCombo.Items.AddRange(new object[]
                {
                    "32.5",
                    "33",
                    "33.5",
                    "34",
                    "34.5",
                    "35",
                    "35.5",
                    "36",
                    "36.5",
                    "37",
                    "37.5",
                    "38",
                    "38.5",
                    "39",
                    "39.5",
                    "40",
                    "40.5",
                    "41",
                    "41.5",
                    "42",
                    "42.5",
                    "43",
                    "43.5",
                    "44",
                    "44.5",
                    "45",
                    "45.5",
                    "46",
                    "46.5",
                    "47",
                    "47.5",
                    "48",
                    "48.5",
                    "49",
                    "49.5",
                    "50",
                    "50.5",
                    "51",
                    "51.5",
                    "52",
                    "52.5",
                    "53",
                    "53.5",
                    "54",
                    "54.5",
                    "55",
                    "55.5",
                    "56",
                    "56.5",
                    "57",
                    "57.5",
                    "58",
                    "58.5",
                    "59",
                    "59.5",
                    "60",
                    "60.5",
                    "61",
                    "61.5",
                    "62",
                    "62.5",
                    "63",
                    "63.5",
                    "64"
                });
                bd_afeGainCombo.Items.AddRange(new object[]
                {
                    "32.5",
                    "33",
                    "33.5",
                    "34",
                    "34.5",
                    "35",
                    "35.5",
                    "36",
                    "36.5",
                    "37",
                    "37.5",
                    "38",
                    "38.5",
                    "39",
                    "39.5",
                    "40",
                    "40.5",
                    "41",
                    "41.5",
                    "42",
                    "42.5",
                    "43",
                    "43.5",
                    "44",
                    "44.5",
                    "45",
                    "45.5",
                    "46",
                    "46.5",
                    "47",
                    "47.5",
                    "48",
                    "48.5",
                    "49",
                    "49.5",
                    "50",
                    "50.5",
                    "51",
                    "51.5",
                    "52",
                    "52.5",
                    "53",
                    "53.5",
                    "54",
                    "54.5",
                    "55",
                    "55.5",
                    "56",
                    "56.5",
                    "57",
                    "57.5",
                    "58",
                    "58.5",
                    "59",
                    "59.5",
                    "60",
                    "60.5",
                    "61",
                    "61.5",
                    "62",
                    "62.5",
                    "63",
                    "63.5",
                    "64"
                });
                for (int i = 0; i < 64; i++)
                {
                    tvgg0.Items.Add(gainCombo.Items[i]);
                    tvgg1.Items.Add(gainCombo.Items[i]);
                    tvgg2.Items.Add(gainCombo.Items[i]);
                    tvgg3.Items.Add(gainCombo.Items[i]);
                    tvgg4.Items.Add(gainCombo.Items[i]);
                    tvgg5.Items.Add(gainCombo.Items[i]);
                }
                gainCombo.Text = "32.5";
                tvgg0.SelectedIndex = selectedIndex;
                tvgg1.SelectedIndex = selectedIndex2;
                tvgg2.SelectedIndex = selectedIndex3;
                tvgg3.SelectedIndex = selectedIndex4;
                tvgg4.SelectedIndex = selectedIndex5;
                tvgg5.SelectedIndex = selectedIndex6;
                dumpChart.ChartAreas[0].AxisY2.Minimum = 30.0;
                dumpChart.ChartAreas[0].AxisY2.Maximum = 70.0;
            }
        }

        private void p1DefaultBtn_Click(object sender, EventArgs e)
        {
            p1PulsesCombo.SelectedIndex = 4;
            Tools.timeDelay(10, "MS");
            p1DriveCombo.SelectedIndex = 7;
            Tools.timeDelay(10, "MS");
            p1RecordCombo.SelectedIndex = 1;
            Tools.timeDelay(10, "MS");
        }

        private void p2DefaultBtn_Click(object sender, EventArgs e)
        {
            p2PulsesCombo.SelectedIndex = 16;
            Tools.timeDelay(10, "MS");
            p2DriveCombo.SelectedIndex = 49;
            Tools.timeDelay(10, "MS");
            p2RecordCombo.SelectedIndex = 9;
            Tools.timeDelay(10, "MS");
        }

        private void p1NLSEnBox_CheckedChanged(object sender, EventArgs e)
        {
            if (!firstTime)
            {
                RegDefs regDefs = new RegDefs("24 (SAT_FDIAG_TH)");
                regDefs.SAT_FDIAG_TH.ReadFromUART();
                if (p1NLSEnBox.Checked)
                {
                    regDefs.P1_NLS_EN.value = "1";
                }
                else
                {
                    regDefs.P1_NLS_EN.value = "0";
                }
                regDefs.SAT_FDIAG_TH.WriteToUART();
                if (primaryTab.SelectedTab.Name != "MemMap" && !firstTime)
                {
                    string[,] array = new string[1, 5];
                    array[0, 0] = regDefs.SAT_FDIAG_TH.location.ToString();
                    array[0, 1] = "24 (SAT_FDIAG_TH)";
                    array[0, 4] = Tools.int32_Into_stringBase16(calRegValfromFields(regDefs.SAT_FDIAG_TH.bit_fields));
                    updateMemoryMap(array, "GRID_USER_MEMSPACE");
                }
            }
        }

        private void p2NLSEnBox_CheckedChanged(object sender, EventArgs e)
        {
            if (!firstTime)
            {
                RegDefs regDefs = new RegDefs("25 (FVOLT_DEC)");
                regDefs.FVOLT_DEC.ReadFromUART();
                if (p2NLSEnBox.Checked)
                {
                    regDefs.P2_NLS_EN.value = "1";
                }
                else
                {
                    regDefs.P2_NLS_EN.value = "0";
                }
                regDefs.FVOLT_DEC.WriteToUART();
                if (primaryTab.SelectedTab.Name != "MemMap" && !firstTime)
                {
                    string[,] array = new string[1, 5];
                    array[0, 0] = regDefs.FVOLT_DEC.location.ToString();
                    array[0, 1] = "25 (FVOLT_DEC)";
                    array[0, 4] = Tools.int32_Into_stringBase16(calRegValfromFields(regDefs.FVOLT_DEC.bit_fields));
                    updateMemoryMap(array, "GRID_USER_MEMSPACE");
                }
            }
        }

        private void p1DigGainLrSt_SelectedIndexChanged(object sender, EventArgs e)
        {
            if (!firstTime)
            {
                RegDefs regDefs = new RegDefs("29 (P1_GAIN_CTRL)");
                regDefs.P1_GAIN_CTRL.ReadFromUART();
                switch (p1DigGainLrSt.SelectedIndex)
                {
                    case 0:
                        regDefs.P1_DIG_GAIN_LR_ST.value = "00";
                        break;
                    case 1:
                        regDefs.P1_DIG_GAIN_LR_ST.value = "01";
                        break;
                    case 2:
                        regDefs.P1_DIG_GAIN_LR_ST.value = "10";
                        break;
                    case 3:
                        regDefs.P1_DIG_GAIN_LR_ST.value = "11";
                        break;
                }
                regDefs.P1_GAIN_CTRL.WriteToUART();
                if (primaryTab.SelectedTab.Name != "MemMap" && !firstTime)
                {
                    string[,] array = new string[1, 5];
                    array[0, 0] = regDefs.P1_GAIN_CTRL.location.ToString();
                    array[0, 1] = "29 (P1_GAIN_CTRL)";
                    array[0, 4] = Tools.int32_Into_stringBase16(calRegValfromFields(regDefs.P1_GAIN_CTRL.bit_fields));
                    updateMemoryMap(array, "GRID_USER_MEMSPACE");
                }
            }
        }

        private void p1DigGainLr_SelectedIndexChanged(object sender, EventArgs e)
        {
            if (!firstTime)
            {
                RegDefs regDefs = new RegDefs("29 (P1_GAIN_CTRL)");
                regDefs.P1_GAIN_CTRL.ReadFromUART();
                switch (p1DigGainLr.SelectedIndex)
                {
                    case 0:
                        regDefs.P1_DIG_GAIN_LR.value = "000";
                        break;
                    case 1:
                        regDefs.P1_DIG_GAIN_LR.value = "001";
                        break;
                    case 2:
                        regDefs.P1_DIG_GAIN_LR.value = "010";
                        break;
                    case 3:
                        regDefs.P1_DIG_GAIN_LR.value = "011";
                        break;
                    case 4:
                        regDefs.P1_DIG_GAIN_LR.value = "100";
                        break;
                    case 5:
                        regDefs.P1_DIG_GAIN_LR.value = "101";
                        break;
                    case 6:
                        regDefs.P1_DIG_GAIN_LR.value = "110";
                        break;
                    case 7:
                        regDefs.P1_DIG_GAIN_LR.value = "111";
                        break;
                }
                regDefs.P1_GAIN_CTRL.WriteToUART();
                if (primaryTab.SelectedTab.Name != "MemMap" && !firstTime)
                {
                    string[,] array = new string[1, 5];
                    array[0, 0] = regDefs.P1_GAIN_CTRL.location.ToString();
                    array[0, 1] = "29 (P1_GAIN_CTRL)";
                    array[0, 4] = Tools.int32_Into_stringBase16(calRegValfromFields(regDefs.P1_GAIN_CTRL.bit_fields));
                    updateMemoryMap(array, "GRID_USER_MEMSPACE");
                }
            }
            bd_dgCombo.SelectedIndex = p1DigGainLr.SelectedIndex;
        }

        private void idleMdCheck_CheckedChanged(object sender, EventArgs e)
        {
            if (!firstTime)
            {
                RegDefs regDefs = new RegDefs("20 (CURR_LIM_P1)");
                regDefs.CURR_LIM_P1.ReadFromUART();
                if (idleMdCheck.Checked)
                {
                    regDefs.IDLE_MD_DIS.value = "0";
                }
                else
                {
                    regDefs.IDLE_MD_DIS.value = "1";
                }
                regDefs.CURR_LIM_P1.WriteToUART();
                if (primaryTab.SelectedTab.Name != "MemMap" && !firstTime)
                {
                    string[,] array = new string[1, 5];
                    array[0, 0] = regDefs.CURR_LIM_P1.location.ToString();
                    array[0, 1] = "20 (CURR_LIM_P1)";
                    array[0, 4] = Tools.int32_Into_stringBase16(calRegValfromFields(regDefs.CURR_LIM_P1.bit_fields));
                    updateMemoryMap(array, "GRID_USER_MEMSPACE");
                }
            }
        }

        private void idleMdCombo_SelectedIndexChanged(object sender, EventArgs e)
        {
            if (!firstTime && !tmrChangeFlag)
            {
                RegDefs regDefs = new RegDefs("25 (FVOLT_DEC)");
                regDefs.FVOLT_DEC.ReadFromUART();
                switch (idleMdCombo.SelectedIndex)
                {
                    case 0:
                        regDefs.LPM_TMR.value = "00";
                        break;
                    case 1:
                        regDefs.LPM_TMR.value = "01";
                        break;
                    case 2:
                        regDefs.LPM_TMR.value = "10";
                        break;
                    case 3:
                        regDefs.LPM_TMR.value = "11";
                        break;
                }
                regDefs.FVOLT_DEC.WriteToUART();
                if (primaryTab.SelectedTab.Name != "MemMap" && !firstTime)
                {
                    string[,] array = new string[1, 5];
                    array[0, 0] = regDefs.FVOLT_DEC.location.ToString();
                    array[0, 1] = "25 (FVOLT_DEC)";
                    array[0, 4] = Tools.int32_Into_stringBase16(calRegValfromFields(regDefs.FVOLT_DEC.bit_fields));
                    updateMemoryMap(array, "GRID_USER_MEMSPACE");
                }
            }
            tmrChangeFlag = true;
            lowpowEnterTimeCombo.SelectedIndex = idleMdCombo.SelectedIndex;
            tmrChangeFlag = false;
        }

        private void p1DigGainSr_SelectedIndexChanged(object sender, EventArgs e)
        {
            if (!firstTime)
            {
                RegDefs regDefs = new RegDefs("29 (P1_GAIN_CTRL)");
                regDefs.P1_GAIN_CTRL.ReadFromUART();
                switch (p1DigGainSr.SelectedIndex)
                {
                    case 0:
                        regDefs.P1_DIG_GAIN_SR.value = "000";
                        break;
                    case 1:
                        regDefs.P1_DIG_GAIN_SR.value = "001";
                        break;
                    case 2:
                        regDefs.P1_DIG_GAIN_SR.value = "010";
                        break;
                    case 3:
                        regDefs.P1_DIG_GAIN_SR.value = "011";
                        break;
                    case 4:
                        regDefs.P1_DIG_GAIN_SR.value = "100";
                        break;
                    case 5:
                        regDefs.P1_DIG_GAIN_SR.value = "101";
                        break;
                    case 6:
                        regDefs.P1_DIG_GAIN_SR.value = "110";
                        break;
                    case 7:
                        regDefs.P1_DIG_GAIN_SR.value = "111";
                        break;
                }
                regDefs.P1_GAIN_CTRL.WriteToUART();
                if (primaryTab.SelectedTab.Name != "MemMap" && !firstTime)
                {
                    string[,] array = new string[1, 5];
                    array[0, 0] = regDefs.P1_GAIN_CTRL.location.ToString();
                    array[0, 1] = "29 (P1_GAIN_CTRL)";
                    array[0, 4] = Tools.int32_Into_stringBase16(calRegValfromFields(regDefs.P1_GAIN_CTRL.bit_fields));
                    updateMemoryMap(array, "GRID_USER_MEMSPACE");
                }
            }
        }

        private void p2DigGainSr_SelectedIndexChanged(object sender, EventArgs e)
        {
            if (!firstTime)
            {
                RegDefs regDefs = new RegDefs("2A (P2_GAIN_CTRL)");
                regDefs.P2_GAIN_CTRL.ReadFromUART();
                switch (p2DigGainSr.SelectedIndex)
                {
                    case 0:
                        regDefs.P2_DIG_GAIN_SR.value = "000";
                        break;
                    case 1:
                        regDefs.P2_DIG_GAIN_SR.value = "001";
                        break;
                    case 2:
                        regDefs.P2_DIG_GAIN_SR.value = "010";
                        break;
                    case 3:
                        regDefs.P2_DIG_GAIN_SR.value = "011";
                        break;
                    case 4:
                        regDefs.P2_DIG_GAIN_SR.value = "100";
                        break;
                    case 5:
                        regDefs.P2_DIG_GAIN_SR.value = "101";
                        break;
                    case 6:
                        regDefs.P2_DIG_GAIN_SR.value = "110";
                        break;
                    case 7:
                        regDefs.P2_DIG_GAIN_SR.value = "111";
                        break;
                }
                regDefs.P2_GAIN_CTRL.WriteToUART();
                if (primaryTab.SelectedTab.Name != "MemMap" && !firstTime)
                {
                    string[,] array = new string[1, 5];
                    array[0, 0] = regDefs.P2_GAIN_CTRL.location.ToString();
                    array[0, 1] = "2A (P2_GAIN_CTRL)";
                    array[0, 4] = Tools.int32_Into_stringBase16(calRegValfromFields(regDefs.P2_GAIN_CTRL.bit_fields));
                    updateMemoryMap(array, "GRID_USER_MEMSPACE");
                }
            }
        }

        private void p2DigGainLr_SelectedIndexChanged(object sender, EventArgs e)
        {
            if (!firstTime)
            {
                RegDefs regDefs = new RegDefs("2A (P2_GAIN_CTRL)");
                regDefs.P2_GAIN_CTRL.ReadFromUART();
                switch (p2DigGainLr.SelectedIndex)
                {
                    case 0:
                        regDefs.P2_DIG_GAIN_LR.value = "000";
                        break;
                    case 1:
                        regDefs.P2_DIG_GAIN_LR.value = "001";
                        break;
                    case 2:
                        regDefs.P2_DIG_GAIN_LR.value = "010";
                        break;
                    case 3:
                        regDefs.P2_DIG_GAIN_LR.value = "011";
                        break;
                    case 4:
                        regDefs.P2_DIG_GAIN_LR.value = "100";
                        break;
                    case 5:
                        regDefs.P2_DIG_GAIN_LR.value = "101";
                        break;
                    case 6:
                        regDefs.P2_DIG_GAIN_LR.value = "110";
                        break;
                    case 7:
                        regDefs.P2_DIG_GAIN_LR.value = "111";
                        break;
                }
                regDefs.P2_GAIN_CTRL.WriteToUART();
                if (primaryTab.SelectedTab.Name != "MemMap" && !firstTime)
                {
                    string[,] array = new string[1, 5];
                    array[0, 0] = regDefs.P2_GAIN_CTRL.location.ToString();
                    array[0, 1] = "2A (P2_GAIN_CTRL)";
                    array[0, 4] = Tools.int32_Into_stringBase16(calRegValfromFields(regDefs.P2_GAIN_CTRL.bit_fields));
                    updateMemoryMap(array, "GRID_USER_MEMSPACE");
                }
            }
        }

        private void p2DigGainLrSt_SelectedIndexChanged(object sender, EventArgs e)
        {
            if (!firstTime)
            {
                RegDefs regDefs = new RegDefs("2A (P2_GAIN_CTRL)");
                regDefs.P2_GAIN_CTRL.ReadFromUART();
                switch (p2DigGainLrSt.SelectedIndex)
                {
                    case 0:
                        regDefs.P2_DIG_GAIN_LR_ST.value = "00";
                        break;
                    case 1:
                        regDefs.P2_DIG_GAIN_LR_ST.value = "01";
                        break;
                    case 2:
                        regDefs.P2_DIG_GAIN_LR_ST.value = "10";
                        break;
                    case 3:
                        regDefs.P2_DIG_GAIN_LR_ST.value = "11";
                        break;
                }
                regDefs.P2_GAIN_CTRL.WriteToUART();
                if (primaryTab.SelectedTab.Name != "MemMap" && !firstTime)
                {
                    string[,] array = new string[1, 5];
                    array[0, 0] = regDefs.P2_GAIN_CTRL.location.ToString();
                    array[0, 1] = "2A (P2_GAIN_CTRL)";
                    array[0, 4] = Tools.int32_Into_stringBase16(calRegValfromFields(regDefs.P2_GAIN_CTRL.bit_fields));
                    updateMemoryMap(array, "GRID_USER_MEMSPACE");
                }
            }
        }

        private void pgrmTIEEPROMBtn_Click(object sender, EventArgs e)
        {
            Array.Clear(uart_return_data, 0, 64);
            regAddrByte = 78;
            commandByte = (byte)(((int)Convert.ToByte(uartAddrCombo.Text) << addrShift) + (int)uart_cmd9);
            MChecksumByte = calculate_UART_Checksum(new byte[]
            {
                commandByte,
                regAddrByte
            });
            common.u2a.UART_Write(4, new byte[]
            {
                syncByte,
                commandByte,
                regAddrByte,
                MChecksumByte
            });
            common.u2a.UART_Read(3, uart_return_data);
            byte b = uart_return_data[1];
            UART_Read_Write(78, 6, true);
            Tools.timeDelay(10, "MS");
            regAddrByte = 89;
            commandByte = (byte)(((int)Convert.ToByte(uartAddrCombo.Text) << addrShift) + (int)uart_cmd9);
            MChecksumByte = calculate_UART_Checksum(new byte[]
            {
                commandByte,
                regAddrByte
            });
            common.u2a.UART_Write(4, new byte[]
            {
                syncByte,
                commandByte,
                regAddrByte,
                MChecksumByte
            });
            common.u2a.UART_Read(3, uart_return_data);
            Tools.timeDelay(10, "MS");
            UART_Read_Write(63, uart_return_data[1], true);
            Tools.timeDelay(10, "MS");
            UART_Read_Write(78, 6, true);
            Tools.timeDelay(10, "MS");
            UART_Read_Write(78, 6, true);
            Tools.timeDelay(10, "MS");
            UART_Read_Write(78, 2, true);
            Tools.timeDelay(10, "MS");
            eepromStatBox.Text = "Working...";
            Tools.timeDelay(1000, "MS");
            regAddrByte = 78;
            commandByte = (byte)(((int)Convert.ToByte(uartAddrCombo.Text) << addrShift) + (int)uart_cmd9);
            MChecksumByte = calculate_UART_Checksum(new byte[]
            {
                commandByte,
                regAddrByte
            });
            common.u2a.UART_Write(4, new byte[]
            {
                syncByte,
                commandByte,
                regAddrByte,
                MChecksumByte
            });
            common.u2a.UART_Read(3, uart_return_data);
            uartDiagB = uart_return_data[0];
            if ((uart_return_data[1] & 8) == 8)
            {
                eepromStatBox.Text = "Internal Programmed Successfully";
            }
            else
            {
                eepromStatBox.Text = "Failed to Program";
            }
            Tools.timeDelay(100, "MS");
            commandByte = (byte)(((int)Convert.ToByte(uartAddrCombo.Text) << addrShift) + (int)uart_cmd10);
            MChecksumByte = calculate_UART_Checksum(new byte[]
            {
                commandByte,
                regAddrByte,
                b
            });
            common.u2a.UART_Write(5, new byte[]
            {
                syncByte,
                commandByte,
                regAddrByte,
                b,
                MChecksumByte
            });
        }

        private void dataDumpCheck_CheckedChanged(object sender, EventArgs e)
        {
            if (dataDumpCheck.Checked && !umrCheck.Checked)
            {
                o1Dist.Enabled = false;
                o1Wid.Enabled = false;
                o1PA.Enabled = false;
                o2Dist.Enabled = false;
                o2Wid.Enabled = false;
                o2PA.Enabled = false;
                o3Dist.Enabled = false;
                o3Wid.Enabled = false;
                o3PA.Enabled = false;
                o4Dist.Enabled = false;
                o4Wid.Enabled = false;
                o4PA.Enabled = false;
                o5Dist.Enabled = false;
                o5Wid.Enabled = false;
                o5PA.Enabled = false;
                o6Dist.Enabled = false;
                o6Wid.Enabled = false;
                o6PA.Enabled = false;
                o7Dist.Enabled = false;
                o7Wid.Enabled = false;
                o7PA.Enabled = false;
                o8Dist.Enabled = false;
                o8Wid.Enabled = false;
                o8PA.Enabled = false;
            }
            else if (!dataDumpCheck.Checked && umrCheck.Checked)
            {
                o1Dist.Enabled = true;
                o1Wid.Enabled = true;
                o1PA.Enabled = true;
                o2Dist.Enabled = true;
                o2Wid.Enabled = true;
                o2PA.Enabled = true;
                o3Dist.Enabled = true;
                o3Wid.Enabled = true;
                o3PA.Enabled = true;
                o4Dist.Enabled = true;
                o4Wid.Enabled = true;
                o4PA.Enabled = true;
                o5Dist.Enabled = true;
                o5Wid.Enabled = true;
                o5PA.Enabled = true;
                o6Dist.Enabled = true;
                o6Wid.Enabled = true;
                o6PA.Enabled = true;
                o7Dist.Enabled = true;
                o7Wid.Enabled = true;
                o7PA.Enabled = true;
                o8Dist.Enabled = true;
                o8Wid.Enabled = true;
                o8PA.Enabled = true;
            }
            else if (dataDumpCheck.Checked && umrCheck.Checked)
            {
                o1Dist.Enabled = true;
                o1Wid.Enabled = true;
                o1PA.Enabled = true;
                o2Dist.Enabled = true;
                o2Wid.Enabled = true;
                o2PA.Enabled = true;
                o3Dist.Enabled = true;
                o3Wid.Enabled = true;
                o3PA.Enabled = true;
                o4Dist.Enabled = true;
                o4Wid.Enabled = true;
                o4PA.Enabled = true;
                o5Dist.Enabled = true;
                o5Wid.Enabled = true;
                o5PA.Enabled = true;
                o6Dist.Enabled = true;
                o6Wid.Enabled = true;
                o6PA.Enabled = true;
                o7Dist.Enabled = true;
                o7Wid.Enabled = true;
                o7PA.Enabled = true;
                o8Dist.Enabled = true;
                o8Wid.Enabled = true;
                o8PA.Enabled = true;
            }
            else
            {
                o1Dist.Enabled = false;
                o1Wid.Enabled = false;
                o1PA.Enabled = false;
                o2Dist.Enabled = false;
                o2Wid.Enabled = false;
                o2PA.Enabled = false;
                o3Dist.Enabled = false;
                o3Wid.Enabled = false;
                o3PA.Enabled = false;
                o4Dist.Enabled = false;
                o4Wid.Enabled = false;
                o4PA.Enabled = false;
                o5Dist.Enabled = false;
                o5Wid.Enabled = false;
                o5PA.Enabled = false;
                o6Dist.Enabled = false;
                o6Wid.Enabled = false;
                o6PA.Enabled = false;
                o7Dist.Enabled = false;
                o7Wid.Enabled = false;
                o7PA.Enabled = false;
                o8Dist.Enabled = false;
                o8Wid.Enabled = false;
                o8PA.Enabled = false;
            }
        }

        private void umrCheck_CheckedChanged(object sender, EventArgs e)
        {
            dataDumpCheck_CheckedChanged(null, null);
        }

        private void graphTCIDumpCheck_CheckedChanged(object sender, EventArgs e)
        {
        }

        private static void ExecuteCommand(string command)
        {
            Process process = Process.Start(new ProcessStartInfo("cmd.exe", "/c " + command)
            {
                CreateNoWindow = true,
                UseShellExecute = false,
                RedirectStandardError = true,
                RedirectStandardOutput = true
            });
            process.WaitForExit();
            string text = process.StandardOutput.ReadToEnd();
            string text2 = process.StandardError.ReadToEnd();
            int exitCode = process.ExitCode;
            Console.WriteLine("output>>" + (string.IsNullOrEmpty(text) ? "(none)" : text));
            Console.WriteLine("error>>" + (string.IsNullOrEmpty(text2) ? "(none)" : text2));
            Console.WriteLine("ExitCode: " + exitCode.ToString(), "ExecuteCommand");
            process.Close();
        }

        public void ExecuteBatFile()
        {
            int num = 1;
            cmdOutput = "";
            activateProgressBar(true);
            aTimer.Enabled = false;
            bTimer.Enabled = false;
            WaitForm myWaitForm = new WaitForm();
            myWaitForm.StartPosition = FormStartPosition.CenterScreen;
            myWaitForm.Show();
            Tools.timeDelay(1, "S");
            string workingDirectory = string.Format(batPathDir, new object[0]);
            proc.StartInfo.UseShellExecute = false;
            proc.StartInfo.RedirectStandardOutput = true;
            proc.StartInfo.WorkingDirectory = workingDirectory;
            proc.StartInfo.FileName = batPathTextBox.Text;
            proc.StartInfo.CreateNoWindow = true;
            stillLoadingFW = true;
            toolStripProgressBar1.Value = 50;
            proc.OutputDataReceived += delegate (object s, DataReceivedEventArgs e)
            {
                try
                {
                    cmdOutput += e.Data;
                }
                catch
                {
                }
            };
            try
            {
                proc.Start();
                proc.BeginOutputReadLine();
                while (!proc.HasExited)
                {
                    datalogTextBox.Text = cmdOutput;
                }
                proc.WaitForExit();
                toolStripProgressBar1.Value = 75;
                num = proc.ExitCode;
                myWaitForm.Close();
            }
            catch (Win32Exception ex)
            {
                myWaitForm.Close();
                Console.WriteLine(ex.Message);
            }
            bTimer.Enabled = false;
            myWaitForm.Close();
            proc.Close();
            if (num == 0 && cmdOutput.Contains("Success"))
            {
                activateProgressBar(false);
                if (MessageBox.Show("Success! Batch file executed. Proceed with the following steps:\r\n\r\n1) Reset EVM hardware by disconnecting-reconnecting the USB cable.\r\n2) Restart GUI? (Recommended)", "Confirm", MessageBoxButtons.YesNo, MessageBoxIcon.Exclamation) == DialogResult.Yes)
                {
                    common.u2a.Restart();
                    AppResetButt_Click(null, null);
                }
                else
                {
                    stillLoadingFW = false;
                    aTimer.Enabled = true;
                    bTimer.Enabled = true;
                }
            }
            else
            {
                stillLoadingFW = false;
                aTimer.Enabled = true;
                bTimer.Enabled = true;
                activateProgressBar(false);
                MessageBox.Show("Failure! Batch file did not execute or load to MSP-EXP430F5529LP properly. Confirm the following:\n\n\r 1) The MSP430-CCS required drivers are installed by the one_time_setup.bat\n\n\r 2) The boostxlpga460-firmware batch file is selected from the same file directory containing the ccs_base and user_files folders", "Batch Programming Failure", MessageBoxButtons.OK, MessageBoxIcon.Exclamation);
            }
        }

        private static void TestBatchExecuteCommand(string command)
        {
            string workingDirectory = string.Format("C:\\Users\\a0221619\\Documents\\BOOSTXL-PGA460", new object[0]);
            Process process = new Process();
            process.StartInfo.WorkingDirectory = workingDirectory;
            process.StartInfo.FileName = "boostxlpga460-firmware.bat";
            process.StartInfo.CreateNoWindow = false;
            process.Start();
            process.WaitForExit();
            MessageBox.Show("Bat file executed!");
        }

        private void fwBackground()
        {
            while (stillLoadingFW)
            {
                Tools.timeDelay(1, "S");
                if (toolStripProgressBar1.Value <= 98)
                {
                    if (toolStripProgressBar1.GetCurrentParent().InvokeRequired)
                    {
                        toolStripProgressBar1.GetCurrentParent().Invoke(new MethodInvoker(delegate
                        {
                            toolStripProgressBar1.Value = toolStripProgressBar1.Value++;
                        }));
                    }
                }
            }
        }

        private void loadBatchBtn_Click(object sender, EventArgs e)
        {
            if (batPathTextBox.Text == "<select-bat-file-path>")
            {
                MessageBox.Show("Select a .bat file!");
            }
            else
            {
                common.u2a.Close();
                ExecuteBatFile();
            }
            batPathTextBox.Text = "<select-bat-file-path>";
        }

        public void batExplorer_Click(object sender, EventArgs e)
        {
            openFileDialog1.Filter = "Batch files (*.bat)|*.bat|All Files (*.*)|*.*";
            openFileDialog1.FileName = "boostxlpga460-firmware.bat";
            openFileDialog1.InitialDirectory = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.Personal), "BOOSTXL-PGA460");
            Tools.timeDelay(1, "MS");
            DialogResult dialogResult = openFileDialog1.ShowDialog();
            if (dialogResult == DialogResult.OK)
            {
                if (openFileDialog1.FileName.Contains(".bat"))
                {
                    batPathTextBox.Text = openFileDialog1.FileName;
                    batPathDir = Path.GetDirectoryName(openFileDialog1.FileName);
                    batPathFile = openFileDialog1.SafeFileName;
                    batPathTextBox.Text = openFileDialog1.FileName;
                    batGood = true;
                }
                else
                {
                    MessageBox.Show("Select a .bat file");
                    batGood = false;
                }
            }
        }

        private void vdiagThBox_TextChanged(object sender, EventArgs e)
        {
            if (!(freqCombo.Text == ""))
            {
                vdiagThBox.Text = Convert.ToString(Math.Round(0.00325 * Convert.ToDouble(voltaDiagErrThrCombo.Text) * (Convert.ToDouble(rINPBox.Text) + 1.0 / (6.28 * (Convert.ToDouble(freqCombo.Text) * 1000.0) * (Math.Pow(10.0, -12.0) * Convert.ToDouble(cINPBox.Text)))), 3));
            }
        }

        public void CloneWriteThrBtn_Click(object sender, EventArgs e)
        {
            cloneThrBtn_Click(null, null);
            if (tabcontrolThr.SelectedTab.Text == "Preset 2")
            {
                tabcontrolThr.SelectTab(0);
            }
            else if (tabcontrolThr.SelectedTab.Text == "Preset 1")
            {
                tabcontrolThr.SelectTab(1);
            }
            thrWriteValuesBtn_Click(null, null);
            updateThrBtn_Click(null, null);
        }

        public void bgExportPathBtn_Click(object sender, EventArgs e)
        {
            DialogResult dialogResult = folderBrowserDialog1.ShowDialog();
            if (dialogResult == DialogResult.OK)
            {
                bgExportBox.Text = folderBrowserDialog1.SelectedPath;
                bgExportBox.Text = bgExportBox.Text + "\\";
            }
        }

        private void bgExportCheck_CheckedChanged(object sender, EventArgs e)
        {
            if (bgExportBox.Text == "<bg-export-path>")
            {
                if (bgExportCheck.Checked)
                {
                    MessageBox.Show("Select a background storage path!");
                    bgExportCheck.Checked = false;
                }
            }
            else if (bgExportCheck.Checked)
            {
                contClearCheck.Checked = true;
                contClearCheck.Enabled = false;
                exportDataCheck.Checked = false;
            }
            if (!bgExportCheck.Checked)
            {
                contClearCheck.Enabled = true;
            }
        }

        private void exportDataCheck_CheckedChanged(object sender, EventArgs e)
        {
            if (exportDataCheck.Checked)
            {
                bgExportCheck.Checked = false;
            }
        }

        private void idleInternal_Click(object sender, EventArgs e)
        {
            if (idleInternal.Text == "ENABLE Idle Mode [Internal Trim]")
            {
                if (!(eepromStatBox.Text == "TI EEPROM Unlocked"))
                {
                    byte[] array = new byte[]
                    {
                        65,
                        83,
                        67
                    };
                    commandByte = (byte)(((int)Convert.ToByte(uartAddrCombo.Text) << addrShift) + (int)uart_cmd31);
                    Tools.timeDelay(10, "MS");
                    Array.Clear(uart_return_data, 0, 64);
                    MChecksumByte = calculate_UART_Checksum(new byte[]
                    {
                        commandByte,
                        array[0],
                        array[1],
                        array[2]
                    });
                    common.u2a.UART_Write(6, new byte[]
                    {
                        syncByte,
                        commandByte,
                        array[0],
                        array[1],
                        array[2],
                        MChecksumByte
                    });
                    eepromStatBox.Text = "TI EEPROM Unlocked";
                    Tools.timeDelay(10, "MS");
                }
                UART_Read_Write(61, 71, true);
                idleInternal.Text = "DISABLE Idle Mode [Internal Trim]";
                Tools.timeDelay(10, "MS");
                idleMdCheck.Checked = false;
                Tools.timeDelay(10, "MS");
                idleMdCheck.Checked = true;
            }
            else if (idleInternal.Text == "DISABLE Idle Mode [Internal Trim]")
            {
                UART_Read_Write(61, 119, true);
                idleInternal.Text = "ENABLE Idle Mode [Internal Trim]";
                Tools.timeDelay(10, "MS");
                idleMdCheck.Checked = true;
                Tools.timeDelay(10, "MS");
                idleMdCheck.Checked = false;
            }
        }

        private void biasInternal_Click(object sender, EventArgs e)
        {
            if (biasInternal.Text == "REDUCE Bias Current [Internal Trim]")
            {
                if (!(eepromStatBox.Text == "TI EEPROM Unlocked"))
                {
                    byte[] array = new byte[]
                    {
                        65,
                        83,
                        67
                    };
                    commandByte = (byte)(((int)Convert.ToByte(uartAddrCombo.Text) << addrShift) + (int)uart_cmd31);
                    Tools.timeDelay(10, "MS");
                    Array.Clear(uart_return_data, 0, 64);
                    MChecksumByte = calculate_UART_Checksum(new byte[]
                    {
                        commandByte,
                        array[0],
                        array[1],
                        array[2]
                    });
                    common.u2a.UART_Write(6, new byte[]
                    {
                        syncByte,
                        commandByte,
                        array[0],
                        array[1],
                        array[2],
                        MChecksumByte
                    });
                    eepromStatBox.Text = "TI EEPROM Unlocked";
                    Tools.timeDelay(10, "MS");
                }
                UART_Read_Write(55, 13, true);
                biasInternal.Text = "DEFAULT Bias Current [Internal Trim]";
            }
            else if (biasInternal.Text == "DEFAULT Bias Current [Internal Trim]")
            {
                UART_Read_Write(55, 9, true);
                biasInternal.Text = "REDUCE Bias Current [Internal Trim]";
            }
        }

        private void collapsePanelBtn_Click(object sender, EventArgs e)
        {
        }

        private void tableLayoutPanelHELP_Paint(object sender, PaintEventArgs e)
        {
        }

        private void tableLayoutPanelMAIN_Paint(object sender, PaintEventArgs e)
        {
        }

        private void groupBox1_Enter(object sender, EventArgs e)
        {
        }

        private void tableLayoutPanel8_Paint(object sender, PaintEventArgs e)
        {
        }

        private void infoTextBox_TextChanged(object sender, EventArgs e)
        {
        }

        private void tabpage_Paint(object sender, PaintEventArgs e)
        {
            SolidBrush brush = new SolidBrush(Color.White);
            e.Graphics.FillRectangle(brush, e.ClipRectangle);
        }

        private void toolStripButton1_Click(object sender, EventArgs e)
        {
        }

        private void ts_saveGrid_Click(object sender, EventArgs e)
        {
            DialogResult dialogResult = MessageBox.Show("Read all registers before saving grid? This is to ensure the latest settings are captured.", "Read All then Save Grid", MessageBoxButtons.YesNoCancel, MessageBoxIcon.Question);
            if (dialogResult == DialogResult.Yes)
            {
                Read_all_Regs_Click(null, null);
            }
            else if (dialogResult == DialogResult.Cancel)
            {
                return;
            }
            Save_grid_butt_Click(null, null);
        }

        private void ts_openGrid_Click(object sender, EventArgs e)
        {
            Load_grid_butt_Click(null, null);
            if (readSuccessBool && ts_updateCombo.Text == "Immediate")
            {
                ts_writeAll_Click(null, null);
            }
        }

        private void ts_readAll_Click(object sender, EventArgs e)
        {
            Read_all_Regs_Click(null, null);
            fault_update();
        }

        private void ts_readSingle_Click(object sender, EventArgs e)
        {
            Read_Sel_Regs_Click(null, null);
            fault_update();
        }

        private void ts_writeAll_Click(object sender, EventArgs e)
        {
            Write_all_regs_Click(null, null);
            fault_update();
        }

        private void ts_writeSingle_Click(object sender, EventArgs e)
        {
            Write_sel_regs_Click(null, null);
            fault_update();
        }

        private void ts_zeroGrid_Click(object sender, EventArgs e)
        {
            zero_grid_butt_Click(null, null);
            if (ts_updateCombo.Text == "Immediate")
            {
                ts_writeAll_Click(null, null);
            }
        }

        private void ts_deselectGrid_Click(object sender, EventArgs e)
        {
            desel_grid_butt_Click(null, null);
        }

        private void splitContainer1_SplitterMoved(object sender, SplitterEventArgs e)
        {
        }

        private void splitter1_SplitterMoved(object sender, SplitterEventArgs e)
        {
        }

        private void tofConverterKeyPress(object sender, KeyPressEventArgs e)
        {
            if (tofCalcRTT.ContainsFocus)
            {
                if (e.KeyChar == '\r')
                {
                    TOFConverter("RTT");
                }
            }
            if (tofCalcDist.ContainsFocus)
            {
                if (e.KeyChar == '\r')
                {
                    TOFConverter("DIST");
                }
            }
            if (tofCalcSound.ContainsFocus)
            {
                if (e.KeyChar == '\r')
                {
                    TOFConverter("SOS");
                }
            }
        }

        private void splitContainerHELP_SplitterMoved(object sender, SplitterEventArgs e)
        {
        }

        private void leftPaneToolStripMenuItem_Click(object sender, EventArgs e)
        {
        }

        private void visibleToolStripMenuItem_Click(object sender, EventArgs e)
        {
            splitContainerHELP.Panel2Collapsed = false;
            visibleToolStripMenuItem.Text = "✓Visible";
            hideToolStripMenuItem.Text = "Hidden";
        }

        private void hideToolStripMenuItem_Click(object sender, EventArgs e)
        {
            splitContainerHELP.Panel2Collapsed = true;
            visibleToolStripMenuItem.Text = "Visible";
            hideToolStripMenuItem.Text = "✓Hidden";
        }

        private void regToMemMapToggle(object sender, EventArgs e)
        {
            if (debugTabControl.SelectedIndex == 2)
            {
                desel_grid_butt_Click(null, null);
                TI_EEPROM_Regs.Visible = false;
                TI_TM_Regs.Visible = false;
                ConfigRegs.Visible = true;
                UserDataRegs.Visible = false;
                ThresholdRegs.Visible = false;
                DataDumpRegs.Visible = false;
                primaryTab.SelectTab("MemMap");
                debugTabControl.SelectTab(regTab);
                infoTextBox.Text = " Click register address for bit definitions.";
            }
        }

        private void thresholdInstantUpdate(object sender, EventArgs e)
        {
            p1t1_SelectedIndexChanged(null, null);
            p1t2_SelectedIndexChanged(null, null);
            p1t3_SelectedIndexChanged(null, null);
            p1t4_SelectedIndexChanged(null, null);
            p1t5_SelectedIndexChanged(null, null);
            p1t6_SelectedIndexChanged(null, null);
            p1t7_SelectedIndexChanged(null, null);
            p1t8_SelectedIndexChanged(null, null);
            p1t9_SelectedIndexChanged(null, null);
            p1t10_SelectedIndexChanged(null, null);
            p1t11_SelectedIndexChanged(null, null);
            p1t12_SelectedIndexChanged(null, null);
            p1l1_SelectedIndexChanged(null, null);
            p1l2_SelectedIndexChanged(null, null);
            p1l3_SelectedIndexChanged(null, null);
            p1l4_SelectedIndexChanged(null, null);
            p1l5_SelectedIndexChanged(null, null);
            p1l6_SelectedIndexChanged(null, null);
            p1l7_SelectedIndexChanged(null, null);
            p1l8_SelectedIndexChanged(null, null);
            p1l9_SelectedIndexChanged(null, null);
            p1l10_SelectedIndexChanged(null, null);
            p1l11_SelectedIndexChanged(null, null);
            p1l12_SelectedIndexChanged(null, null);
            p1lOff_SelectedIndexChanged(null, null);
            p2t1_SelectedIndexChanged(null, null);
            p2t2_SelectedIndexChanged(null, null);
            p2t3_SelectedIndexChanged(null, null);
            p2t4_SelectedIndexChanged(null, null);
            p2t5_SelectedIndexChanged(null, null);
            p2t6_SelectedIndexChanged(null, null);
            p2t7_SelectedIndexChanged(null, null);
            p2t8_SelectedIndexChanged(null, null);
            p2t9_SelectedIndexChanged(null, null);
            p2t10_SelectedIndexChanged(null, null);
            p2t11_SelectedIndexChanged(null, null);
            p2t12_SelectedIndexChanged(null, null);
            p2l1_SelectedIndexChanged(null, null);
            p2l2_SelectedIndexChanged(null, null);
            p2l3_SelectedIndexChanged(null, null);
            p2l4_SelectedIndexChanged(null, null);
            p2l5_SelectedIndexChanged(null, null);
            p2l6_SelectedIndexChanged(null, null);
            p2l7_SelectedIndexChanged(null, null);
            p2l8_SelectedIndexChanged(null, null);
            p2l9_SelectedIndexChanged(null, null);
            p2l10_SelectedIndexChanged(null, null);
            p2l11_SelectedIndexChanged(null, null);
            p2l12_SelectedIndexChanged(null, null);
            p2lOff_SelectedIndexChanged(null, null);
            if (initThrFlag && thrReady)
            {
                updateThrBtn_Click(null, null);
                if (thrUpdateCheck.Checked && PGA46xStat_box.Text.Contains("Ready"))
                {
                    Tools.timeDelay(50, "MS");
                    thrWriteValuesBtn_Click(null, null);
                    Tools.timeDelay(50, "MS");
                    thrReady = false;
                }
            }
        }

        public void allL1T1Btn_Click(object sender, EventArgs e)
        {
            bool flag = false;
            if (thrUpdateCheck.Checked)
            {
                flag = true;
                thrUpdateCheck.Checked = false;
            }
            if (tabcontrolThr.SelectedTab.Text == "Preset 1")
            {
                if (p1l1.Text == "")
                {
                    p1l1.SelectedIndex = 16;
                }
                p1l2.SelectedIndex = p1l1.SelectedIndex;
                p1l3.SelectedIndex = p1l1.SelectedIndex;
                p1l4.SelectedIndex = p1l1.SelectedIndex;
                p1l5.SelectedIndex = p1l1.SelectedIndex;
                p1l6.SelectedIndex = p1l1.SelectedIndex;
                p1l7.SelectedIndex = p1l1.SelectedIndex;
                p1l8.SelectedIndex = p1l1.SelectedIndex;
                p1l9.SelectedIndex = (int)Convert.ToInt16(p1l1.Text);
                p1l10.SelectedIndex = (int)Convert.ToInt16(p1l1.Text);
                p1l11.SelectedIndex = (int)Convert.ToInt16(p1l1.Text);
                p1l12.SelectedIndex = (int)Convert.ToInt16(p1l1.Text);
                p1lOff.SelectedIndex = 0;
                if (p1t1.Text == "")
                {
                    p1t1.SelectedIndex = 8;
                }
                p1t2.SelectedIndex = p1t1.SelectedIndex;
                p1t3.SelectedIndex = p1t1.SelectedIndex;
                p1t4.SelectedIndex = p1t1.SelectedIndex;
                p1t5.SelectedIndex = p1t1.SelectedIndex;
                p1t6.SelectedIndex = p1t1.SelectedIndex;
                p1t7.SelectedIndex = p1t1.SelectedIndex;
                p1t8.SelectedIndex = p1t1.SelectedIndex;
                p1t9.SelectedIndex = p1t1.SelectedIndex;
                p1t10.SelectedIndex = p1t1.SelectedIndex;
                p1t11.SelectedIndex = p1t1.SelectedIndex;
                p1t12.SelectedIndex = p1t1.SelectedIndex;
            }
            if (tabcontrolThr.SelectedTab.Text == "Preset 2")
            {
                if (p2l1.Text == "")
                {
                    p2l1.SelectedIndex = 16;
                }
                p2l2.SelectedIndex = p2l1.SelectedIndex;
                p2l3.SelectedIndex = p2l1.SelectedIndex;
                p2l4.SelectedIndex = p2l1.SelectedIndex;
                p2l5.SelectedIndex = p2l1.SelectedIndex;
                p2l6.SelectedIndex = p2l1.SelectedIndex;
                p2l7.SelectedIndex = p2l1.SelectedIndex;
                p2l8.SelectedIndex = p2l1.SelectedIndex;
                p2l9.SelectedIndex = (int)Convert.ToInt16(p2l1.Text);
                p2l10.SelectedIndex = (int)Convert.ToInt16(p2l1.Text);
                p2l11.SelectedIndex = (int)Convert.ToInt16(p2l1.Text);
                p2l12.SelectedIndex = (int)Convert.ToInt16(p2l1.Text);
                p2lOff.SelectedIndex = 0;
                if (p2t1.Text == "")
                {
                    p2t1.SelectedIndex = 8;
                }
                p2t2.SelectedIndex = p2t1.SelectedIndex;
                p2t3.SelectedIndex = p2t1.SelectedIndex;
                p2t4.SelectedIndex = p2t1.SelectedIndex;
                p2t5.SelectedIndex = p2t1.SelectedIndex;
                p2t6.SelectedIndex = p2t1.SelectedIndex;
                p2t7.SelectedIndex = p2t1.SelectedIndex;
                p2t8.SelectedIndex = p2t1.SelectedIndex;
                p2t9.SelectedIndex = p2t1.SelectedIndex;
                p2t10.SelectedIndex = p2t1.SelectedIndex;
                p2t11.SelectedIndex = p2t1.SelectedIndex;
                p2t12.SelectedIndex = p2t1.SelectedIndex;
            }
            if (flag)
            {
                thrWriteValuesBtn_Click(null, null);
                thrUpdateCheck.Checked = true;
            }
            updateThrBtn_Click(null, null);
        }

        public void allMidCodeBtn_Click(object sender, EventArgs e)
        {
            activateProgressBar(true);
            bool flag = false;
            if (thrUpdateCheck.Checked)
            {
                flag = true;
                thrUpdateCheck.Checked = false;
            }
            if (tabcontrolThr.SelectedTab.Text == "Preset 1")
            {
                p1l1.SelectedIndex = 16;
                p1l2.SelectedIndex = 16;
                p1l3.SelectedIndex = 16;
                p1l4.SelectedIndex = 16;
                p1l5.SelectedIndex = 16;
                p1l6.SelectedIndex = 16;
                p1l7.SelectedIndex = 16;
                p1l8.SelectedIndex = 16;
                p1l9.SelectedIndex = 128;
                p1l10.SelectedIndex = 128;
                p1l11.SelectedIndex = 128;
                p1l12.SelectedIndex = 128;
                p1lOff.SelectedIndex = 0;
                p1t1.SelectedIndex = 8;
                p1t2.SelectedIndex = 8;
                p1t3.SelectedIndex = 8;
                p1t4.SelectedIndex = 8;
                p1t5.SelectedIndex = 8;
                p1t6.SelectedIndex = 8;
                p1t7.SelectedIndex = 8;
                p1t8.SelectedIndex = 8;
                p1t9.SelectedIndex = 8;
                p1t10.SelectedIndex = 8;
                p1t11.SelectedIndex = 8;
                p1t12.SelectedIndex = 8;
            }
            if (tabcontrolThr.SelectedTab.Text == "Preset 2")
            {
                p2l1.SelectedIndex = 16;
                p2l2.SelectedIndex = 16;
                p2l3.SelectedIndex = 16;
                p2l4.SelectedIndex = 16;
                p2l5.SelectedIndex = 16;
                p2l6.SelectedIndex = 16;
                p2l7.SelectedIndex = 16;
                p2l8.SelectedIndex = 16;
                p2l9.SelectedIndex = 128;
                p2l10.SelectedIndex = 128;
                p2l11.SelectedIndex = 128;
                p2l12.SelectedIndex = 128;
                p2lOff.SelectedIndex = 0;
                p2t1.SelectedIndex = 8;
                p2t2.SelectedIndex = 8;
                p2t3.SelectedIndex = 8;
                p2t4.SelectedIndex = 8;
                p2t5.SelectedIndex = 8;
                p2t6.SelectedIndex = 8;
                p2t7.SelectedIndex = 8;
                p2t8.SelectedIndex = 8;
                p2t9.SelectedIndex = 8;
                p2t10.SelectedIndex = 8;
                p2t11.SelectedIndex = 8;
                p2t12.SelectedIndex = 8;
            }
            if (flag)
            {
                thrWriteValuesBtn_Click(null, null);
                thrUpdateCheck.Checked = true;
            }
            updateThrBtn_Click(null, null);
            activateProgressBar(false);
        }

        private void tvgInstantUpdate(object sender, EventArgs e)
        {
            tvgg0_SelectedIndexChanged(null, null);
            tvgg1_SelectedIndexChanged(null, null);
            tvgg2_SelectedIndexChanged(null, null);
            tvgg3_SelectedIndexChanged(null, null);
            tvgg4_SelectedIndexChanged(null, null);
            tvgg5_SelectedIndexChanged(null, null);
            tvgt0_SelectedIndexChanged(null, null);
            tvgt1_SelectedIndexChanged(null, null);
            tvgt2_SelectedIndexChanged(null, null);
            tvgt3_SelectedIndexChanged(null, null);
            tvgt4_SelectedIndexChanged(null, null);
            tvgt5_SelectedIndexChanged(null, null);
            if (initThrFlag && tvgReady)
            {
                updateTVGBtn_Click(null, null);
                if (tvgInstantUpdateCheck.Checked && PGA46xStat_box.Text.Contains("Ready"))
                {
                    Tools.timeDelay(50, "MS");
                    writeTVGMemBtn_Click(null, null);
                    Tools.timeDelay(50, "MS");
                    tvgReady = false;
                }
            }
        }

        public void allMidCodeTVGBtn_Click(object sender, EventArgs e)
        {
            activateProgressBar(true);
            bool flag = false;
            if (tvgInstantUpdateCheck.Checked)
            {
                flag = true;
                tvgInstantUpdateCheck.Checked = false;
            }
            tvgg0.SelectedIndex = 32;
            tvgg1.SelectedIndex = tvgg0.SelectedIndex;
            tvgg2.SelectedIndex = tvgg0.SelectedIndex;
            tvgg3.SelectedIndex = tvgg0.SelectedIndex;
            tvgg4.SelectedIndex = tvgg0.SelectedIndex;
            tvgg5.SelectedIndex = tvgg0.SelectedIndex;
            tvgt0.SelectedIndex = 8;
            tvgt1.SelectedIndex = tvgt0.SelectedIndex;
            tvgt2.SelectedIndex = tvgt0.SelectedIndex;
            tvgt3.SelectedIndex = tvgt0.SelectedIndex;
            tvgt4.SelectedIndex = tvgt0.SelectedIndex;
            tvgt5.SelectedIndex = tvgt0.SelectedIndex;
            if (flag)
            {
                writeTVGMemBtn_Click(null, null);
                tvgInstantUpdateCheck.Checked = true;
            }
            updateTVGBtn_Click(null, null);
            activateProgressBar(false);
        }

        private void sampleMaxCombo_SelectedIndexChanged(object sender, EventArgs e)
        {
        }

        private void button51_Click(object sender, EventArgs e)
        {
        }

        private void clearPlotCheck_CheckedChanged(object sender, EventArgs e)
        {
        }

        public void defaultAllGeneralBtn_Click(object sender, EventArgs e)
        {
            activateProgressBar(true);
            defaultingFlag = true;
            freqCombo.SelectedIndex = 143;
            freqshiftCheck.Checked = false;
            deadCombo.SelectedIndex = 0;
            AFEGainRngCombo.SelectedIndex = 1;
            gainCombo.SelectedIndex = 24;
            bd_afeGainCombo.SelectedIndex = 24;
            bpbwCombo.SelectedIndex = 1;
            cutoffCombo.SelectedIndex = 1;
            thrCmpDeglitchCombo.SelectedIndex = 8;
            decoupletimeRadio.Checked = true;
            decoupletimeBox.SelectedIndex = 15;
            tempgainCombo.SelectedIndex = 0;
            tempoffsetCombo.SelectedIndex = 0;
            p1DefaultBtn_Click(null, null);
            p2DefaultBtn_Click(null, null);
            nlsNoiseCombo.SelectedIndex = 0;
            nlsSECombo.SelectedIndex = 0;
            nlsTOPCombo.SelectedIndex = 0;
            p1DigGainSr.SelectedIndex = 0;
            p1DigGainLr.SelectedIndex = 3;
            p1DigGainLrSt.SelectedIndex = 0;
            p2DigGainSr.SelectedIndex = 0;
            p2DigGainLr.SelectedIndex = 3;
            p2DigGainLrSt.SelectedIndex = 0;
            defaultingFlag = false;
            activateProgressBar(false);
        }

        public void defaultAllDiagRegsBtn_Click(object sender, EventArgs e)
        {
            activateProgressBar(true);
            freqDiagWinLengthCombo.SelectedIndex = 1;
            freqDiagStartTimeCombo.SelectedIndex = 1;
            freqDiagErrorTimeThrCombo.SelectedIndex = 1;
            voltaDiagErrThrCombo.SelectedIndex = 1;
            lowpowEnterTimeCombo.SelectedIndex = 1;
            ovthrCombo.SelectedIndex = 3;
            satDiagThrLvlCombo.SelectedIndex = 1;
            activateProgressBar(false);
        }

        private void groupBox94_Enter(object sender, EventArgs e)
        {
        }

        private void disableBPCommCheck_CheckedChanged(object sender, EventArgs e)
        {
            if (disableBPCommCheck.Checked)
            {
                common.u2a.GPIO_SetPort(0, 1);
                common.u2a.GPIO_WritePort(0, 2);
            }
            else
            {
                common.u2a.GPIO_SetPort(0, 1);
                common.u2a.GPIO_WritePort(0, 1);
            }
        }

        private void dropdownReady(object sender, EventArgs e)
        {
            if (!forceDropdownFlag)
                thresholdInstantUpdate(null, null);
        }

        private void ts_updateCombo_Click(object sender, EventArgs e)
        {
            if (ts_updateCombo.SelectedIndex == 0)
            {
                GRID_USER_MEMSPACE.autoUpdateControl = true;
            }
            else
            {
                GRID_USER_MEMSPACE.autoUpdateControl = false;
            }
        }

        private void tciPgrmEEPROMBtn_Click(object sender, EventArgs e)
        {
            plotTCICheck.Checked = false;
            ind11EEPPW.Text = "0xD";
            ind11PgrmEEW.SelectedIndex = 0;
            writeIndexBtn_Click(null, null);
            ind11PgrmEEW.SelectedIndex = 1;
            writeIndexBtn_Click(null, null);
            plotTCICheck.Checked = true;
        }

        private void tciP1ThrEditBtn_Click(object sender, EventArgs e)
        {
            primaryTab.SelectTab(3);
            leftTreeNav.TopNode.Toggle();
        }

        private void tciP2ThrEditBtn_Click(object sender, EventArgs e)
        {
            primaryTab.SelectTab(3);
            leftTreeNav.TopNode.Toggle();
        }

        private void tciTVGEditBtn_Click(object sender, EventArgs e)
        {
            primaryTab.SelectTab(4);
            leftTreeNav.TopNode.Toggle();
        }

        private void burnIdleBtn_Click(object sender, EventArgs e)
        {
            if (!(eepromStatBox.Text == "TI EEPROM Unlocked"))
            {
                unlockTIEEPROM_Click(null, null);
            }
            pgrmTIEEPROMBtn_Click(null, null);
        }

        private void burnSlewBtn_Click(object sender, EventArgs e)
        {
            if (!(eepromStatBox.Text == "TI EEPROM Unlocked"))
            {
                unlockTIEEPROM_Click(null, null);
            }
            Tools.timeDelay(10, "MS");
            UART_Read_Write(59, (byte)((ioSlewRateCombo.SelectedIndex << 4) + 8), true);
            Tools.timeDelay(10, "MS");
            pgrmTIEEPROMBtn_Click(null, null);
        }

        private void unlockTIEEPROM_Click(object sender, EventArgs e)
        {
            byte[] array = new byte[]
            {
                65,
                83,
                67
            };
            commandByte = (byte)(((int)Convert.ToByte(uartAddrCombo.Text) << addrShift) + (int)uart_cmd31);
            Array.Clear(uart_return_data, 0, 64);
            MChecksumByte = calculate_UART_Checksum(new byte[]
            {
                commandByte,
                array[0],
                array[1],
                array[2]
            });
            common.u2a.UART_Write(6, new byte[]
            {
                syncByte,
                commandByte,
                array[0],
                array[1],
                array[2],
                MChecksumByte
            });
            eepromStatBox.Text = "TI EEPROM Unlocked";
        }

        private void readIOSlewBtn_Click(object sender, EventArgs e)
        {
            if (!(eepromStatBox.Text == "TI EEPROM Unlocked"))
            {
                unlockTIEEPROM_Click(null, null);
            }
            regAddrByte = 59;
            commandByte = (byte)(((int)Convert.ToByte(uartAddrCombo.Text) << addrShift) + (int)uart_cmd9);
            MChecksumByte = calculate_UART_Checksum(new byte[]
            {
                commandByte,
                regAddrByte
            });
            common.u2a.UART_Write(4, new byte[]
            {
                syncByte,
                commandByte,
                regAddrByte,
                MChecksumByte
            });
            common.u2a.UART_Read(3, uart_return_data);
            ioSlewReadBox.Text = Tools.int32_Into_stringBase16(uart_return_data[1] >> 4, 4);
        }

        private void plotADCBtn_Click(object sender, EventArgs e)
        {
            double num = 1.45;
            if (p1Radio.Checked)
            {
                int num2 = (p1RecordCombo.SelectedIndex + 1) * 16 - 1;
                adcChartMax = (long)((p1RecordCombo.SelectedIndex + 1) * 4000);
            }
            else
            {
                int num2 = (p2RecordCombo.SelectedIndex + 1) * 16 - 1;
                adcChartMax = (long)((p2RecordCombo.SelectedIndex + 1) * 4000);
            }
            if (adcChartMax >= 64000L)
            {
                adcChartMax = (long)(mem_return_buf_all.Length - 1);
            }
            if (contClearCheck.Checked)
            {
                adcChart.Series[0].Points.Clear();
            }
            for (int i = 0; i < adcShiftCountBox.SelectedIndex; i++)
            {
                ShiftRight(mem_return_buf_all);
            }
            if (reverseADCCheck.Checked)
            {
                int i = 0;
                while ((long)i < adcChartMax)
                {
                    byte b = ReverseWithLookupTable(mem_return_buf_all[i]);
                    mem_return_buf_all[i] = b;
                    i++;
                }
            }
            uint num3 = 0u;
            byte[] array = new byte[adcChartMax];
            if (dataDumpCheck.Checked)
            {
                int i;
                if (sampleOut12bitRadio.Checked)
                {
                    i = 0;
                    while ((long)i < adcChartMax)
                    {
                        uint num4 = (uint)((int)mem_return_buf_all[i] + ((int)mem_return_buf_all[i + 1] << 8));
                        if (adcRawMaskChk.Checked)
                        {
                            array[i] = (byte)(num4 & 15u);
                        }
                        else
                        {
                            array[i] = (byte)((num4 & 61440u) >> 12);
                        }
                        i++;
                    }
                }
                i = 0;
                while ((long)i < adcChartMax)
                {
                    if (sampleOut8bitRadio.Checked)
                    {
                        if (graphModeCombo.Text == "DSP - BP Filter")
                        {
                            adcChart.Series[0].Points.AddXY((double)i * num, (double)((sbyte)mem_return_buf_all[i]));
                        }
                        else
                        {
                            adcChart.Series[0].Points.AddXY((double)i * num, (double)mem_return_buf_all[i]);
                        }
                    }
                    else
                    {
                        uint num4 = (uint)((int)mem_return_buf_all[i] + ((int)mem_return_buf_all[i + 1] << 8));
                        if (logRawCounter.Checked && lastCorrection)
                        {
                            datalogTextBox.AppendText(Convert.ToString(array[i]));
                            datalogTextBox.AppendText(",");
                        }
                        if (adcRawMaskChk.Checked)
                        {
                            num4 = (num4 & 65520u) >> 4;
                        }
                        else
                        {
                            num4 &= 4095u;
                        }
                        if (adcRawFilter.Checked)
                        {
                            if (i > 121 && (long)i < adcChartMax - 5L)
                            {
                                if (((ulong)(num4 - num3) <= (ulong)((long)syncDiff.Value) || num3 >= num4) && ((ulong)(num3 - num4) <= (ulong)((long)syncDiff.Value) || num3 <= num4))
                                {
                                    adcChart.Series[0].Points.AddXY((double)i * num, num4);
                                }
                                num3 = num4;
                            }
                            else
                            {
                                adcChart.Series[0].Points.AddXY((double)i * num, 0.0);
                                num3 = num4;
                            }
                            i++;
                        }
                        else
                        {
                            if (graphModeCombo.Text == "DSP - BP Filter")
                            {
                                if (num4 > 2048u)
                                {
                                    adcChart.Series[0].Points.AddXY((double)i * num, (double)(num4 - 4095u));
                                }
                                else
                                {
                                    adcChart.Series[0].Points.AddXY((double)i * num, (double)num4);
                                }
                            }
                            else
                            {
                                adcChart.Series[0].Points.AddXY((double)i * num, num4);
                            }
                            i++;
                        }
                    }
                    i++;
                }
            }
        }

        private void mSPEXP430F5529LPToolStripMenuItem_Click(object sender, EventArgs e)
        {
            activateProgressBar(true);
            debugTabControl.SelectTab(utilTab);
            debugTabControl.SelectedTab.Focus();
            InitializeBackgroundWorker();
            batExplorer_Click(null, null);
            if (batGood)
            {
                loadBatchBtn_Click(null, null);
            }
            activateProgressBar(false);
        }

        private void groupBox10_Enter(object sender, EventArgs e)
        {
        }

        private void bd_freqCombo_SelectedIndexChanged(object sender, EventArgs e)
        {
            freqCombo.SelectedIndex = bd_freqCombo.SelectedIndex;
        }

        private void bd_pulseCombo_SelectedIndexChanged(object sender, EventArgs e)
        {
            p1PulsesCombo.SelectedIndex = bd_pulseCombo.SelectedIndex;
        }

        private void bd_currentLimCombo_SelectedIndexChanged(object sender, EventArgs e)
        {
            p1DriveCombo.SelectedIndex = bd_currentLimCombo.SelectedIndex;
        }

        private void bd_afeGainCombo_SelectedIndexChanged(object sender, EventArgs e)
        {
            gainCombo.SelectedIndex = bd_afeGainCombo.SelectedIndex;
            if (primaryTab.SelectedIndex == 2 || defaultingFlag)
            {
                tvgInstantUpdateCheck.Checked = true;
                allInitGainBtn_Click(null, null);
                tvgInstantUpdateCheck.Checked = false;
            }
        }

        private void bd_dgCombo_SelectedIndexChanged(object sender, EventArgs e)
        {
            p1DigGainLr.SelectedIndex = bd_dgCombo.SelectedIndex;
            if (primaryTab.SelectedIndex == 2)
            {
                p1DigGainSr.SelectedIndex = bd_dgCombo.SelectedIndex;
            }
        }

        private void bd_bpfbwCombo_SelectedIndexChanged(object sender, EventArgs e)
        {
            bpbwCombo.SelectedIndex = bd_bpfbwCombo.SelectedIndex;
        }

        private void bd_lpfCoCombo_SelectedIndexChanged(object sender, EventArgs e)
        {
            cutoffCombo.SelectedIndex = bd_lpfCoCombo.SelectedIndex;
        }

        private void bd_recCombo_SelectedIndexChanged(object sender, EventArgs e)
        {
            p1RecordCombo.SelectedIndex = bd_recCombo.SelectedIndex;
        }

        private void disableBOOSTXLPGA460CommunicationToolStripMenuItem_Click(object sender, EventArgs e)
        {
            if (disableBOOSTXLPGA460CommunicationToolStripMenuItem.Text == "Disable BOOSTXL-PGA460 Communication")
            {
                disableBPCommCheck.Checked = true;
                disableBOOSTXLPGA460CommunicationToolStripMenuItem.Text = "Enable BOOSTXL-PGA460 Communication";
            }
            else if (disableBOOSTXLPGA460CommunicationToolStripMenuItem.Text == "Enable BOOSTXL-PGA460 Communication")
            {
                disableBPCommCheck.Checked = false;
                disableBOOSTXLPGA460CommunicationToolStripMenuItem.Text = "Disable BOOSTXL-PGA460 Communication";
            }
        }

        private void pgrmUARTCheck_CheckedChanged(object sender, EventArgs e)
        {
            if (pgrmUARTCheck.Checked)
            {
                uartAddrCombo_SelectedIndexChanged(null, null);
                pgrmUARTCheck.Checked = false;
            }
        }

        private void uartRunBtn_Click(object sender, EventArgs e)
        {
            common.u2a.UART_Write(uartSendLength, uart_send_data);
            Array.Clear(uart_return_data, 0, 64);
            common.u2a.UART_Read(54, uart_return_data);
            SMRichTextBox.Clear();
            for (int i = 0; i < 54; i++)
            {
                SMRichTextBox.AppendText(Convert.ToString(Tools.StringBase2_Into_StringBase16(Tools.Byte_into_StringBase2(uart_return_data[i]))));
                SMRichTextBox.AppendText(" ");
            }
            uartDiagB = uart_return_data[0];
            fault_update();
        }

        private void runUartBtn_Click_1(object sender, EventArgs e)
        {
            MSRichTextBox.Clear();
            string text = "";
            if (uartCmdSingleRadio.Checked)
            {
                switch (uartCmdSingleCombo.SelectedIndex)
                {
                    case 0:
                        MSRichTextBox.AppendText(Tools.StringBase2_Into_StringBase16(Tools.StringBase10_Into_StringBase2(((int)Tools.int32_Into_Byte(uartAddrCombo.SelectedIndex << 5)).ToString(), 8, false)) + " 0" + (numObjToDetCombo.SelectedIndex + 1).ToString("X"));
                        break;
                    case 1:
                        MSRichTextBox.AppendText(Tools.StringBase2_Into_StringBase16(Tools.StringBase10_Into_StringBase2(((int)(Tools.int32_Into_Byte(uartAddrCombo.SelectedIndex << 5) + 1)).ToString(), 8, false)) + " 0" + (numObjToDetCombo.SelectedIndex + 1).ToString("X"));
                        break;
                    case 2:
                        MSRichTextBox.AppendText(Tools.StringBase2_Into_StringBase16(Tools.StringBase10_Into_StringBase2(((int)(Tools.int32_Into_Byte(uartAddrCombo.SelectedIndex << 5) + 2)).ToString(), 8, false)) + " 0" + (numObjToDetCombo.SelectedIndex + 1).ToString("X"));
                        break;
                    case 3:
                        MSRichTextBox.AppendText(Tools.StringBase2_Into_StringBase16(Tools.StringBase10_Into_StringBase2(((int)(Tools.int32_Into_Byte(uartAddrCombo.SelectedIndex << 5) + 3)).ToString(), 8, false)) + " 0" + (numObjToDetCombo.SelectedIndex + 1).ToString("X"));
                        break;
                    case 4:
                        MSRichTextBox.AppendText(Tools.StringBase2_Into_StringBase16(Tools.StringBase10_Into_StringBase2(((int)(Tools.int32_Into_Byte(uartAddrCombo.SelectedIndex << 5) + 4)).ToString(), 8, false)) + " 00");
                        break;
                    case 5:
                        MSRichTextBox.AppendText(Tools.StringBase2_Into_StringBase16(Tools.StringBase10_Into_StringBase2(((int)(Tools.int32_Into_Byte(uartAddrCombo.SelectedIndex << 5) + 5)).ToString(), 8, false)));
                        break;
                    case 6:
                        MSRichTextBox.AppendText(Tools.StringBase2_Into_StringBase16(Tools.StringBase10_Into_StringBase2(((int)(Tools.int32_Into_Byte(uartAddrCombo.SelectedIndex << 5) + 6)).ToString(), 8, false)));
                        break;
                    case 7:
                        MSRichTextBox.AppendText(Tools.StringBase2_Into_StringBase16(Tools.StringBase10_Into_StringBase2(((int)(Tools.int32_Into_Byte(uartAddrCombo.SelectedIndex << 5) + 7)).ToString(), 8, false)));
                        break;
                    case 8:
                        MSRichTextBox.AppendText(Tools.StringBase2_Into_StringBase16(Tools.StringBase10_Into_StringBase2(((int)(Tools.int32_Into_Byte(uartAddrCombo.SelectedIndex << 5) + 8)).ToString(), 8, false)));
                        break;
                    case 9:
                        MSRichTextBox.AppendText(Tools.StringBase2_Into_StringBase16(Tools.StringBase10_Into_StringBase2(((int)(Tools.int32_Into_Byte(uartAddrCombo.SelectedIndex << 5) + 9)).ToString(), 8, false)) + " 2B");
                        break;
                    case 10:
                        MSRichTextBox.AppendText(Tools.StringBase2_Into_StringBase16(Tools.StringBase10_Into_StringBase2(((int)(Tools.int32_Into_Byte(uartAddrCombo.SelectedIndex << 5) + 10)).ToString(), 8, false)) + " 00 01");
                        break;
                    case 11:
                        MSRichTextBox.AppendText(Tools.StringBase2_Into_StringBase16(Tools.StringBase10_Into_StringBase2(((int)(Tools.int32_Into_Byte(uartAddrCombo.SelectedIndex << 5) + 11)).ToString(), 8, false)));
                        break;
                    case 12:
                        {
                            string[] addressGridList = GRID_USER_MEMSPACE.getAddressGridList();
                            GRID_USER_MEMSPACE.HighlightRows(addressGridList);
                            string[,] array = GRID_USER_MEMSPACE.Get_HIGHLIGHTED_Grid_Values__R_D_A_O(false);
                            int numberOfHighlighedRows = GRID_USER_MEMSPACE.getNumberOfHighlighedRows();
                            SelectedGrid = "GRID_USER_MEMSPACE";
                            desel_grid_butt_Click(null, null);
                            for (int i = 0; i < 43; i++)
                            {
                                text = text + " " + array[i, 2];
                            }
                            int length = text.Length;
                            MSRichTextBox.AppendText(Tools.StringBase2_Into_StringBase16(Tools.StringBase10_Into_StringBase2(((int)(Tools.int32_Into_Byte(uartAddrCombo.SelectedIndex << 5) + 12)).ToString(), 8, false)) + text);
                            break;
                        }
                    case 13:
                        MSRichTextBox.AppendText(Tools.StringBase2_Into_StringBase16(Tools.StringBase10_Into_StringBase2(((int)(Tools.int32_Into_Byte(uartAddrCombo.SelectedIndex << 5) + 13)).ToString(), 8, false)));
                        break;
                    case 14:
                        {
                            string[] addressGridList = GRID_USER_MEMSPACE.getAddressGridList();
                            GRID_USER_MEMSPACE.HighlightRows(addressGridList);
                            string[,] array = GRID_USER_MEMSPACE.Get_HIGHLIGHTED_Grid_Values__R_D_A_O(false);
                            int numberOfHighlighedRows = GRID_USER_MEMSPACE.getNumberOfHighlighedRows();
                            SelectedGrid = "GRID_USER_MEMSPACE";
                            desel_grid_butt_Click(null, null);
                            for (int i = 20; i < 27; i++)
                            {
                                text = text + " " + array[i, 2];
                            }
                            int length = text.Length;
                            MSRichTextBox.AppendText(Tools.StringBase2_Into_StringBase16(Tools.StringBase10_Into_StringBase2(((int)(Tools.int32_Into_Byte(uartAddrCombo.SelectedIndex << 5) + 14)).ToString(), 8, false)) + text);
                            break;
                        }
                    case 15:
                        MSRichTextBox.AppendText(Tools.StringBase2_Into_StringBase16(Tools.StringBase10_Into_StringBase2(((int)(Tools.int32_Into_Byte(uartAddrCombo.SelectedIndex << 5) + 15)).ToString(), 8, false)));
                        break;
                    case 16:
                        {
                            string[] addressGridList = GRID_USER_MEMSPACE.getAddressGridList();
                            GRID_USER_MEMSPACE.HighlightRows(addressGridList);
                            string[,] array = GRID_USER_MEMSPACE.Get_HIGHLIGHTED_Grid_Values__R_D_A_O(false);
                            int numberOfHighlighedRows = GRID_USER_MEMSPACE.getNumberOfHighlighedRows();
                            SelectedGrid = "GRID_USER_MEMSPACE";
                            desel_grid_butt_Click(null, null);
                            for (int i = 58; i < 90; i++)
                            {
                                text = text + " " + array[i, 2];
                            }
                            int length = text.Length;
                            MSRichTextBox.AppendText(Tools.StringBase2_Into_StringBase16(Tools.StringBase10_Into_StringBase2(((int)(Tools.int32_Into_Byte(uartAddrCombo.SelectedIndex << 5) + 16)).ToString(), 8, false)) + text);
                            break;
                        }
                }
            }
            else
            {
                switch (uartCmdBroadCombo.SelectedIndex)
                {
                    case 0:
                        MSRichTextBox.AppendText("11 0" + (numObjToDetCombo.SelectedIndex + 1).ToString("X"));
                        break;
                    case 1:
                        MSRichTextBox.AppendText("12 0" + (numObjToDetCombo.SelectedIndex + 1).ToString("X"));
                        break;
                    case 2:
                        MSRichTextBox.AppendText("13 0" + (numObjToDetCombo.SelectedIndex + 1).ToString("X"));
                        break;
                    case 3:
                        MSRichTextBox.AppendText("14 0" + (numObjToDetCombo.SelectedIndex + 1).ToString("X"));
                        break;
                    case 4:
                        MSRichTextBox.AppendText("15 00");
                        break;
                    case 5:
                        MSRichTextBox.AppendText("16 00 01");
                        break;
                    case 6:
                        {
                            string[] addressGridList = GRID_USER_MEMSPACE.getAddressGridList();
                            GRID_USER_MEMSPACE.HighlightRows(addressGridList);
                            string[,] array = GRID_USER_MEMSPACE.Get_HIGHLIGHTED_Grid_Values__R_D_A_O(false);
                            int numberOfHighlighedRows = GRID_USER_MEMSPACE.getNumberOfHighlighedRows();
                            SelectedGrid = "GRID_USER_MEMSPACE";
                            desel_grid_butt_Click(null, null);
                            for (int i = 0; i < 43; i++)
                            {
                                text = text + " " + array[i, 2];
                            }
                            int length = text.Length;
                            MSRichTextBox.AppendText("17" + text);
                            break;
                        }
                    case 7:
                        {
                            string[] addressGridList = GRID_USER_MEMSPACE.getAddressGridList();
                            GRID_USER_MEMSPACE.HighlightRows(addressGridList);
                            string[,] array = GRID_USER_MEMSPACE.Get_HIGHLIGHTED_Grid_Values__R_D_A_O(false);
                            int numberOfHighlighedRows = GRID_USER_MEMSPACE.getNumberOfHighlighedRows();
                            SelectedGrid = "GRID_USER_MEMSPACE";
                            desel_grid_butt_Click(null, null);
                            for (int i = 20; i < 27; i++)
                            {
                                text = text + " " + array[i, 2];
                            }
                            int length = text.Length;
                            MSRichTextBox.AppendText("18" + text);
                            break;
                        }
                    case 8:
                        {
                            string[] addressGridList = GRID_USER_MEMSPACE.getAddressGridList();
                            GRID_USER_MEMSPACE.HighlightRows(addressGridList);
                            string[,] array = GRID_USER_MEMSPACE.Get_HIGHLIGHTED_Grid_Values__R_D_A_O(false);
                            int numberOfHighlighedRows = GRID_USER_MEMSPACE.getNumberOfHighlighedRows();
                            SelectedGrid = "GRID_USER_MEMSPACE";
                            desel_grid_butt_Click(null, null);
                            for (int i = 58; i < 90; i++)
                            {
                                text = text + " " + array[i, 2];
                            }
                            int length = text.Length;
                            MSRichTextBox.AppendText("19" + text);
                            break;
                        }
                }
            }
            calcAppendChecksumBtn_Click(null, null);
            uartRunBtn_Click(null, null);
        }

        private void flipADC_Click(object sender, EventArgs e)
        {
        }

        public static byte ReverseWithLookupTable(byte toReverse)
        {
            return BitReverseTable[(int)toReverse];
        }

        public static byte ReverseBitsWith4Operations(byte b)
        {
            return (byte)((((b * 0x80200802L) & 0x884422110L) * 0x101010101L) >> 32);
        }

        public static byte ReverseBitsWith3Operations(byte b)
        {
            return (byte)(((((b * 0x802) & 0x22110) | ((b * 0x8020) & 0x88440)) * 0x10101) >> 16);
        }

        public static byte ReverseBitsWith7Operations(byte b)
        {
            return (byte)((((uint)b * 2050u & 139536u) | ((uint)b * 32800u & 558144u)) * 65793u >> 16);
        }

        public static byte ReverseBitsWithLoop(byte v)
        {
            byte num = v;
            int num2 = 7;
            v = (byte)(v >> 1);
            while (v != 0)
            {
                num = (byte)(num << 1);
                num = (byte)(num | ((byte)(v & 1)));
                num2--;
                v = (byte)(v >> 1);
            }
            return (byte)(num << num2);
        }

        public static byte ReverseWithUnrolledLoop(byte b)
        {
            byte num = b;
            b = (byte)(b >> 1);
            num = (byte)(num << 1);
            num = (byte)(num | ((byte)(b & 1)));
            b = (byte)(b >> 1);
            num = (byte)(num << 1);
            num = (byte)(num | ((byte)(b & 1)));
            b = (byte)(b >> 1);
            num = (byte)(num << 1);
            num = (byte)(num | ((byte)(b & 1)));
            b = (byte)(b >> 1);
            num = (byte)(num << 1);
            num = (byte)(num | ((byte)(b & 1)));
            b = (byte)(b >> 1);
            num = (byte)(num << 1);
            num = (byte)(num | ((byte)(b & 1)));
            b = (byte)(b >> 1);
            num = (byte)(num << 1);
            num = (byte)(num | ((byte)(b & 1)));
            b = (byte)(b >> 1);
            num = (byte)(num << 1);
            num = (byte)(num | ((byte)(b & 1)));
            b = (byte)(b >> 1);
            return num;
        }

        private void coefCalcShift_CheckedChanged(object sender, EventArgs e)
        {
            if (coefCalcShift.Checked)
            {
                coefCalcFreq.Items.Clear();
                coefCalcFreq.Items.AddRange(new object[]
                {
                    "180",
                    "181.2",
                    "182.4",
                    "183.6",
                    "184.8",
                    "186",
                    "187.2",
                    "188.4",
                    "189.6",
                    "190.8",
                    "192",
                    "193.2",
                    "194.4",
                    "195.6",
                    "196.8",
                    "198",
                    "199.2",
                    "200.4",
                    "201.6",
                    "202.8",
                    "204",
                    "205.2",
                    "206.4",
                    "207.6",
                    "208.8",
                    "210",
                    "211.2",
                    "212.4",
                    "213.6",
                    "214.8",
                    "216",
                    "217.2",
                    "218.4",
                    "219.6",
                    "220.8",
                    "222",
                    "223.2",
                    "224.4",
                    "225.6",
                    "226.8",
                    "228",
                    "229.2",
                    "230.4",
                    "231.6",
                    "232.8",
                    "234",
                    "235.2",
                    "236.4",
                    "237.6",
                    "238.8",
                    "240",
                    "241.2",
                    "242.4",
                    "243.6",
                    "244.8",
                    "246",
                    "247.2",
                    "248.4",
                    "249.6",
                    "250.8",
                    "252",
                    "253.2",
                    "254.4",
                    "255.6",
                    "256.8",
                    "258",
                    "259.2",
                    "260.4",
                    "261.6",
                    "262.8",
                    "264",
                    "265.2",
                    "266.4",
                    "267.6",
                    "268.8",
                    "270",
                    "271.2",
                    "272.4",
                    "273.6",
                    "274.8",
                    "276",
                    "277.2",
                    "278.4",
                    "279.6",
                    "280.8",
                    "282",
                    "283.2",
                    "284.4",
                    "285.6",
                    "286.8",
                    "288",
                    "289.2",
                    "290.4",
                    "291.6",
                    "292.8",
                    "294",
                    "295.2",
                    "296.4",
                    "297.6",
                    "298.8",
                    "300",
                    "301.2",
                    "302.4",
                    "303.6",
                    "304.8",
                    "306",
                    "307.2",
                    "308.4",
                    "309.6",
                    "310.8",
                    "312",
                    "313.2",
                    "314.4",
                    "315.6",
                    "316.8",
                    "318",
                    "319.2",
                    "320.4",
                    "321.6",
                    "322.8",
                    "324",
                    "325.2",
                    "326.4",
                    "327.6",
                    "328.8",
                    "330",
                    "331.2",
                    "332.4",
                    "333.6",
                    "334.8",
                    "336",
                    "337.2",
                    "338.4",
                    "339.6",
                    "340.8",
                    "342",
                    "343.2",
                    "344.4",
                    "345.6",
                    "346.8",
                    "348",
                    "349.2",
                    "350.4",
                    "351.6",
                    "352.8",
                    "354",
                    "355.2",
                    "356.4",
                    "357.6",
                    "358.8",
                    "360",
                    "361.2",
                    "362.4",
                    "363.6",
                    "364.8",
                    "366",
                    "367.2",
                    "368.4",
                    "369.6",
                    "370.8",
                    "372",
                    "373.2",
                    "374.4",
                    "375.6",
                    "376.8",
                    "378",
                    "379.2",
                    "380.4",
                    "381.6",
                    "382.8",
                    "384",
                    "385.2",
                    "386.4",
                    "387.6",
                    "388.8",
                    "390",
                    "391.2",
                    "392.4",
                    "393.6",
                    "394.8",
                    "396",
                    "397.2",
                    "398.4",
                    "399.6",
                    "400.8",
                    "402",
                    "403.2",
                    "404.4",
                    "405.6",
                    "406.8",
                    "408",
                    "409.2",
                    "410.4",
                    "411.6",
                    "412.8",
                    "414",
                    "415.2",
                    "416.4",
                    "417.6",
                    "418.8",
                    "420",
                    "421.2",
                    "422.4",
                    "423.6",
                    "424.8",
                    "426",
                    "427.2",
                    "428.4",
                    "429.6",
                    "430.8",
                    "432",
                    "433.2",
                    "434.4",
                    "435.6",
                    "436.8",
                    "438",
                    "439.2",
                    "440.4",
                    "441.6",
                    "442.8",
                    "444",
                    "445.2",
                    "446.4",
                    "447.6",
                    "448.8",
                    "450",
                    "451.2",
                    "452.4",
                    "453.6",
                    "454.8",
                    "456",
                    "457.2",
                    "458.4",
                    "459.6",
                    "460.8",
                    "462",
                    "463.2",
                    "464.4",
                    "465.6",
                    "466.8",
                    "468",
                    "469.2",
                    "470.4",
                    "471.6",
                    "472.8",
                    "474",
                    "475.2",
                    "476.4",
                    "477.6",
                    "478.8",
                    "480"
                });
            }
            else
            {
                coefCalcFreq.Items.Clear();
                coefCalcFreq.Items.AddRange(new object[]
                {
                    "30",
                    "30.2",
                    "30.4",
                    "30.6",
                    "30.8",
                    "31",
                    "31.2",
                    "31.4",
                    "31.6",
                    "31.8",
                    "32",
                    "32.2",
                    "32.4",
                    "32.6",
                    "32.8",
                    "33",
                    "33.2",
                    "33.4",
                    "33.6",
                    "33.8",
                    "34",
                    "34.2",
                    "34.4",
                    "34.6",
                    "34.8",
                    "35",
                    "35.2",
                    "35.4",
                    "35.6",
                    "35.8",
                    "36",
                    "36.2",
                    "36.4",
                    "36.6",
                    "36.8",
                    "37",
                    "37.2",
                    "37.4",
                    "37.6",
                    "37.8",
                    "38",
                    "38.2",
                    "38.4",
                    "38.6",
                    "38.8",
                    "39",
                    "39.2",
                    "39.4",
                    "39.6",
                    "39.8",
                    "40",
                    "40.2",
                    "40.4",
                    "40.6",
                    "40.8",
                    "41",
                    "41.2",
                    "41.4",
                    "41.6",
                    "41.8",
                    "42",
                    "42.2",
                    "42.4",
                    "42.6",
                    "42.8",
                    "43",
                    "43.2",
                    "43.4",
                    "43.6",
                    "43.8",
                    "44",
                    "44.2",
                    "44.4",
                    "44.6",
                    "44.8",
                    "45",
                    "45.2",
                    "45.4",
                    "45.6",
                    "45.8",
                    "46",
                    "46.2",
                    "46.4",
                    "46.6",
                    "46.8",
                    "47",
                    "47.2",
                    "47.4",
                    "47.6",
                    "47.8",
                    "48",
                    "48.2",
                    "48.4",
                    "48.6",
                    "48.8",
                    "49",
                    "49.2",
                    "49.4",
                    "49.6",
                    "49.8",
                    "50",
                    "50.2",
                    "50.4",
                    "50.6",
                    "50.8",
                    "51",
                    "51.2",
                    "51.4",
                    "51.6",
                    "51.8",
                    "52",
                    "52.2",
                    "52.4",
                    "52.6",
                    "52.8",
                    "53",
                    "53.2",
                    "53.4",
                    "53.6",
                    "53.8",
                    "54",
                    "54.2",
                    "54.4",
                    "54.6",
                    "54.8",
                    "55",
                    "55.2",
                    "55.4",
                    "55.6",
                    "55.8",
                    "56",
                    "56.2",
                    "56.4",
                    "56.6",
                    "56.8",
                    "57",
                    "57.2",
                    "57.4",
                    "57.6",
                    "57.8",
                    "58",
                    "58.2",
                    "58.4",
                    "58.6",
                    "58.8",
                    "59",
                    "59.2",
                    "59.4",
                    "59.6",
                    "59.8",
                    "60",
                    "60.2",
                    "60.4",
                    "60.6",
                    "60.8",
                    "61",
                    "61.2",
                    "61.4",
                    "61.6",
                    "61.8",
                    "62",
                    "62.2",
                    "62.4",
                    "62.6",
                    "62.8",
                    "63",
                    "63.2",
                    "63.4",
                    "63.6",
                    "63.8",
                    "64",
                    "64.2",
                    "64.4",
                    "64.6",
                    "64.8",
                    "65",
                    "65.2",
                    "65.4",
                    "65.6",
                    "65.8",
                    "66",
                    "66.2",
                    "66.4",
                    "66.6",
                    "66.8",
                    "67",
                    "67.2",
                    "67.4",
                    "67.6",
                    "67.8",
                    "68",
                    "68.2",
                    "68.4",
                    "68.6",
                    "68.8",
                    "69",
                    "69.2",
                    "69.4",
                    "69.6",
                    "69.8",
                    "70",
                    "70.2",
                    "70.4",
                    "70.6",
                    "70.8",
                    "71",
                    "71.2",
                    "71.4",
                    "71.6",
                    "71.8",
                    "72",
                    "72.2",
                    "72.4",
                    "72.6",
                    "72.8",
                    "73",
                    "73.2",
                    "73.4",
                    "73.6",
                    "73.8",
                    "74",
                    "74.2",
                    "74.4",
                    "74.6",
                    "74.8",
                    "75",
                    "75.2",
                    "75.4",
                    "75.6",
                    "75.8",
                    "76",
                    "76.2",
                    "76.4",
                    "76.6",
                    "76.8",
                    "77",
                    "77.2",
                    "77.4",
                    "77.6",
                    "77.8",
                    "78",
                    "78.2",
                    "78.4",
                    "78.6",
                    "78.8",
                    "79",
                    "79.2",
                    "79.4",
                    "79.6",
                    "79.8",
                    "80"
                });
            }
        }

        private void coefCalcFreq_SelectedIndexChanged(object sender, EventArgs e)
        {
            if (coefCalcShift.Checked)
            {
                double num = 180.0 + (double)coefCalcFreq.SelectedIndex * 1.2;
            }
            else
            {
                double num = 30.0 + (double)coefCalcFreq.SelectedIndex * 0.2;
            }
            bpcoa2Box.Text = Tools.int32_Into_stringBase16(65535 & BPFCoefLookupTable(bpbwCombo.SelectedIndex, coefCalcFreq.SelectedIndex));
            switch (bpbwCombo.SelectedIndex)
            {
                case 0:
                    bpcoa3Box.Text = Tools.StringBase2_Into_StringBase16("1111110011001110", 16);
                    bpcob1Box.Text = Tools.StringBase2_Into_StringBase16("110011001", 16);
                    break;
                case 1:
                    bpcoa3Box.Text = Tools.StringBase2_Into_StringBase16("1111100110100101", 16);
                    bpcob1Box.Text = Tools.StringBase2_Into_StringBase16("1100101101", 16);
                    break;
                case 2:
                    bpcoa3Box.Text = Tools.StringBase2_Into_StringBase16("1111011010000111", 16);
                    bpcob1Box.Text = Tools.StringBase2_Into_StringBase16("10010111101", 16);
                    break;
                case 3:
                    bpcoa3Box.Text = Tools.StringBase2_Into_StringBase16("1111001101110010", 16);
                    bpcob1Box.Text = Tools.StringBase2_Into_StringBase16("11001000111", 16);
                    break;
            }
        }

        public static int BPFCoefLookupTable(int BW, int A2)
        {
            int result = 65535;
            switch (BW)
            {
                case 0:
                    result = Coef2kTable[A2];
                    break;
                case 1:
                    result = Coef4kTable[A2];
                    break;
                case 2:
                    result = Coef6kTable[A2];
                    break;
                case 3:
                    result = Coef8kTable[A2];
                    break;
            }
            return result;
        }

        public bool runBFWLoad()
        {
            mSPEXP430F5529LPToolStripMenuItem_Click(null, null);
            return true;
        }

        public void OnTimedEvent(object source, ElapsedEventArgs e)
        {
            aTimer.Enabled = false;

            checkForGUIUpdatesToolStripMenuItem_Click(null, null);
            checkGUIUpdateOnce = true;

            if (!fwMessageShown && USB_Controller_box.Text != "USB2ANY I/F Found" && !stillLoadingFW)
            {
                fwMessageShown = true;
                if (MessageBox.Show(
                    "Load 'boostxlpga460-firmware.bat' onto MSP-EXP430F5529LP? \n\r\n\r Flash programming may take a few minutes.",
                    "Flash Program BOOSTXL-PGA460 Firmware",
                    MessageBoxButtons.YesNo, MessageBoxIcon.Asterisk) == DialogResult.Yes)
                {
                    base.Invoke(new Action(delegate
                    {
                        debugTabControl.SelectTab(utilTab);
                        debugTabControl.SelectedTab.Focus();
                        mSPEXP430F5529LPToolStripMenuItem_Click(null, null);
                    }));
                    fwMessageShown = false;
                }
                else
                {
                    fwMessageShown = false;
                }
            }
        }

        public void loadBatchEvent(object source, ElapsedEventArgs e)
        {
        }

#if AUTO_CHECK_VERSION
        public void guiUpdate(object source, ElapsedEventArgs e)
        {
            checkVersionTimer.Enabled = false;
            checkForGUIUpdatesToolStripMenuItem_Click(null, null);
            checkGUIUpdateOnce = true;
        }
#endif
        public void dftBDBtn_Click(object sender, EventArgs e)
        {
            bd_afeGainCombo.SelectedIndex = 28;
            bd_bpfbwCombo.SelectedIndex = 1;
            bd_currentLimCombo.SelectedIndex = 28;
            bd_dgCombo.SelectedIndex = 1;
            bd_freqCombo.SelectedIndex = 143;
            freqshiftCheck.Checked = false;
            bd_lpfCoCombo.SelectedIndex = 1;
            bd_pulseCombo.SelectedIndex = 16;
            bd_recCombo.SelectedIndex = 8;
        }

        private void powerBudgetToolStripMenuItem_Click(object sender, EventArgs e)
        {
            PowerCalculator form = new PowerCalculator();
            form.Show();
        }

        private void InitializeBackgroundWorker()
        {
            backgroundWorker1 = new BackgroundWorker();
            backgroundWorker1.WorkerReportsProgress = true;
            backgroundWorker1.WorkerSupportsCancellation = true;
            backgroundWorker1.DoWork += backgroundWorker1_DoWork;
            backgroundWorker1.RunWorkerCompleted += backgroundWorker1_RunWorkerCompleted;
            backgroundWorker1.ProgressChanged += backgroundWorker1_ProgressChanged;
        }

        private void startAsyncButton_Click(object sender, EventArgs e)
        {
            numberToCompute = 2;
            highestPercentageReached = 0;
            proc.OutputDataReceived += delegate (object s, DataReceivedEventArgs ee)
            {
                try
                {
                    cmdOutput += ee.Data;
                }
                catch
                {
                }
            };
            backgroundWorker1.RunWorkerAsync(numberToCompute);
        }

        private void cancelAsyncButton_Click(object sender, EventArgs e)
        {
        }

        private void backgroundWorker1_DoWork(object sender, DoWorkEventArgs e)
        {
            BackgroundWorker backgroundWorker = sender as BackgroundWorker;
            datalogTextBox.Text = cmdOutput;
        }

        private void backgroundWorker1_RunWorkerCompleted(object sender, RunWorkerCompletedEventArgs e)
        {
            if (e.Error != null)
            {
                MessageBox.Show(e.Error.Message);
            }
            else if (!e.Cancelled)
            {
                datalogTextBox.Text = cmdOutput;
            }
            if (stillLoadingFW)
            {
                backgroundWorker1.RunWorkerAsync(numberToCompute);
            }
            else
            {
                backgroundWorker1.CancelAsync();
            }
        }

        private void backgroundWorker1_ProgressChanged(object sender, ProgressChangedEventArgs e)
        {
            toolStripProgressBar1.Value = e.ProgressPercentage;
        }

        private long ComputeFibonacci(int n, BackgroundWorker worker, DoWorkEventArgs e)
        {
            if (n < 0 || n > 91)
            {
                throw new ArgumentException("value must be >= 0 and <= 91", "n");
            }
            long result = 0L;
            if (worker.CancellationPending)
            {
                e.Cancel = true;
            }
            else
            {
                if (n < 2)
                {
                    result = 1L;
                }
                else
                {
                    result = ComputeFibonacci(n - 1, worker, e) + ComputeFibonacci(n - 2, worker, e);
                }
                int num = (int)((float)n / (float)numberToCompute * 100f);
                if (num > highestPercentageReached)
                {
                    highestPercentageReached = num;
                    worker.ReportProgress(num);
                }
            }
            return result;
        }

        public void dftBDDDBtn_Click(object sender, EventArgs e)
        {
            bd_afeGainCombo.SelectedIndex = 0;
            bd_bpfbwCombo.SelectedIndex = 1;
            bd_currentLimCombo.SelectedIndex = 0;
            bd_dgCombo.SelectedIndex = 1;
            bd_freqCombo.SelectedIndex = 50;
            freqshiftCheck.Checked = false;
            bd_lpfCoCombo.SelectedIndex = 1;
            bd_pulseCombo.SelectedIndex = 10;
            bd_recCombo.SelectedIndex = 8;
        }

        private void batchtestBtn_Click(object sender, EventArgs e)
        {
            TestBatchExecuteCommand("echo testing");
        }

        public void defaultAllGeneralDDBtn_Click(object sender, EventArgs e)
        {
            activateProgressBar(true);
            defaultingFlag = true;
            freqCombo.SelectedIndex = 50;
            freqshiftCheck.Checked = false;
            deadCombo.SelectedIndex = 0;
            AFEGainRngCombo.SelectedIndex = 1;
            gainCombo.SelectedIndex = 24;
            bd_afeGainCombo.SelectedIndex = 24;
            bpbwCombo.SelectedIndex = 1;
            cutoffCombo.SelectedIndex = 1;
            thrCmpDeglitchCombo.SelectedIndex = 8;
            decoupletimeRadio.Checked = true;
            decoupletimeBox.SelectedIndex = 15;
            tempgainCombo.SelectedIndex = 0;
            tempoffsetCombo.SelectedIndex = 0;
            p1DefaultBtn_Click(null, null);
            p2DefaultBtn_Click(null, null);
            p1DriveCombo.SelectedIndex = 0;
            p2DriveCombo.SelectedIndex = 0;
            nlsNoiseCombo.SelectedIndex = 0;
            nlsSECombo.SelectedIndex = 0;
            nlsTOPCombo.SelectedIndex = 0;
            p1DigGainSr.SelectedIndex = 0;
            p1DigGainLr.SelectedIndex = 3;
            p1DigGainLrSt.SelectedIndex = 0;
            p2DigGainSr.SelectedIndex = 0;
            p2DigGainLr.SelectedIndex = 3;
            p2DigGainLrSt.SelectedIndex = 0;
            defaultingFlag = false;
            activateProgressBar(false);
        }

        private void cINPBox_TextChanged(object sender, EventArgs e)
        {
            vdiagThBox_TextChanged(null, null);
        }

        private void rINPBox_TextChanged(object sender, EventArgs e)
        {
            vdiagThBox_TextChanged(null, null);
        }

        private void burstABTestMode_Click(object sender, EventArgs e)
        {
            unlockTIEEPROM_Click(null, null);
            Tools.timeDelay(10, "MS");
            Array.Clear(uart_return_data, 0, 64);
            UART_Read_Write(85, 42, true);
            Tools.timeDelay(10, "MS");
            UART_Read_Write(86, 42, true);
            Tools.timeDelay(10, "MS");
            burstABStat.Text = "Test mode active";
        }

        private void burstABEnBurnBtn_Click(object sender, EventArgs e)
        {
            if (!(eepromStatBox.Text == "TI EEPROM Unlocked"))
            {
                unlockTIEEPROM_Click(null, null);
            }
            pgrmTIEEPROMBtn_Click(null, null);
            burstABStat.Text = "TI EEPROM burned";
        }

        private void burstABStat_TextChanged(object sender, EventArgs e)
        {
        }

        public void ISOClosedDefaultsBtn_Click(object sender, EventArgs e)
        {
            activateProgressBar(true);
            defaultingFlag = true;
            freqCombo.SelectedIndex = 143;
            freqshiftCheck.Checked = false;
            deadCombo.SelectedIndex = 0;
            AFEGainRngCombo.SelectedIndex = 0;
            bpbwCombo.SelectedIndex = 0;
            cutoffCombo.SelectedIndex = 3;
            thrCmpDeglitchCombo.SelectedIndex = 8;
            decoupletimeRadio.Checked = true;
            decoupletimeBox.SelectedIndex = 15;
            tempgainCombo.SelectedIndex = 0;
            tempoffsetCombo.SelectedIndex = 0;
            p1PulsesCombo.SelectedIndex = 10;
            p1DriveCombo.SelectedIndex = 31;
            p1RecordCombo.SelectedIndex = 0;
            p2PulsesCombo.SelectedIndex = 20;
            p2DriveCombo.SelectedIndex = 63;
            p2RecordCombo.SelectedIndex = 7;
            nlsNoiseCombo.SelectedIndex = 0;
            nlsSECombo.SelectedIndex = 0;
            nlsTOPCombo.SelectedIndex = 0;
            p1DigGainSr.SelectedIndex = 1;
            p1DigGainLr.SelectedIndex = 3;
            p1DigGainLrSt.SelectedIndex = 0;
            p2DigGainSr.SelectedIndex = 2;
            p2DigGainLr.SelectedIndex = 4;
            p2DigGainLrSt.SelectedIndex = 0;
            defaultingFlag = false;
            tvgInstantUpdateCheck.Checked = false;
            tvgg0.SelectedIndex = 0;
            tvgg1.SelectedIndex = 4;
            tvgg2.SelectedIndex = 10;
            tvgg3.SelectedIndex = 20;
            tvgg4.SelectedIndex = 32;
            tvgg5.SelectedIndex = 32;
            tvgt0.SelectedIndex = 8;
            tvgt1.SelectedIndex = 13;
            tvgt2.SelectedIndex = 14;
            tvgt3.SelectedIndex = 14;
            tvgt4.SelectedIndex = 14;
            tvgt5.SelectedIndex = 15;
            writeTVGMemBtn_Click(null, null);
            tvgInstantUpdateCheck.Checked = true;
            updateTVGBtn_Click(null, null);
            activateProgressBar(false);
        }

        public void ISOOpenDefaultsBtn_Click(object sender, EventArgs e)
        {
            activateProgressBar(true);
            defaultingFlag = true;
            freqCombo.SelectedIndex = 50;
            freqshiftCheck.Checked = false;
            deadCombo.SelectedIndex = 0;
            AFEGainRngCombo.SelectedIndex = 1;
            bpbwCombo.SelectedIndex = 0;
            cutoffCombo.SelectedIndex = 3;
            thrCmpDeglitchCombo.SelectedIndex = 8;
            decoupletimeRadio.Checked = true;
            decoupletimeBox.SelectedIndex = 15;
            tempgainCombo.SelectedIndex = 0;
            tempoffsetCombo.SelectedIndex = 0;
            p1PulsesCombo.SelectedIndex = 10;
            p1DriveCombo.SelectedIndex = 0;
            p1RecordCombo.SelectedIndex = 0;
            p2PulsesCombo.SelectedIndex = 20;
            p2DriveCombo.SelectedIndex = 0;
            p2RecordCombo.SelectedIndex = 7;
            nlsNoiseCombo.SelectedIndex = 0;
            nlsSECombo.SelectedIndex = 0;
            nlsTOPCombo.SelectedIndex = 0;
            p1DigGainSr.SelectedIndex = 1;
            p1DigGainLr.SelectedIndex = 3;
            p1DigGainLrSt.SelectedIndex = 0;
            p2DigGainSr.SelectedIndex = 2;
            p2DigGainLr.SelectedIndex = 4;
            p2DigGainLrSt.SelectedIndex = 0;
            defaultingFlag = false;
            tvgInstantUpdateCheck.Checked = false;
            tvgg0.SelectedIndex = 0;
            tvgg1.SelectedIndex = 4;
            tvgg2.SelectedIndex = 10;
            tvgg3.SelectedIndex = 20;
            tvgg4.SelectedIndex = 32;
            tvgg5.SelectedIndex = 40;
            tvgt0.SelectedIndex = 8;
            tvgt1.SelectedIndex = 13;
            tvgt2.SelectedIndex = 14;
            tvgt3.SelectedIndex = 14;
            tvgt4.SelectedIndex = 14;
            tvgt5.SelectedIndex = 15;
            writeTVGMemBtn_Click(null, null);
            tvgInstantUpdateCheck.Checked = true;
            updateTVGBtn_Click(null, null);
            activateProgressBar(false);
        }

        private void scriptingToolStripMenuItem_Click(object sender, EventArgs e)
        {
            new Scripting(this).Show();
        }

        private void DlSDBCheck_CheckedChanged(object sender, EventArgs e)
        {
            if (!uartDiagSysRadio.Checked && DlSDBCheck.Checked)
                uartDiagSysRadio.Checked = true;
        }

        private void echoDataDumpPlotterToolStripMenuItem_Click(object sender, EventArgs e)
        {
            new DataPlotter().Show();
        }

        public void saveChartImgBtn_Click(object sender, EventArgs e)
        {
            Chart chart =
                graphModeCombo.Text == "Data Dump"
                ? dumpChart
                : adcChart;

            chart.SaveImage(Environment.GetFolderPath(Environment.SpecialFolder.Personal) + "\\BOOSTXL-PGA460\\EDDChartImage-" + DateTime.Now.ToString("MMddyyyyhhmmss") + ".png", ImageFormat.Png);
            chart.BackColor = Color.Yellow;
            Tools.timeDelay(250, "MS");
            chart.BackColor = Color.White;
        }

        private void checkForGUIUpdatesToolStripMenuItem_Click(object sender, EventArgs e)
        {
            Ping ping = new Ping();
            WebClient webClient = new WebClient();
            try
            {
                if (ping.Send("www.ti.com").Status == IPStatus.Success)
                {
                    string html_page = webClient.DownloadString(PGA460_ToolsSoftware_Link);
                    string current_version = guiVersion;
                    string version_pattern = "PGA460-Q1 EVM GUI (Version ";
                    string new_version = GetUntilOrEmpty(html_page.Substring(html_page.IndexOf(version_pattern) + version_pattern.Length));
                    int i_current_version = (int)Convert.ToInt16(current_version.Replace(".", string.Empty));
                    int i_new_version = (int)Convert.ToInt16(new_version.Replace(".", string.Empty));
                    bool flag = true;
                    if (current_version != new_version &&
                        new_version != "" &&
                        3 == new_version.Count((char f) => f == '.'))
                    {
                        flag = (i_new_version <= i_current_version);
                    }

                    if (!flag)
                    {
                        if (MessageBox.Show(
                            string.Concat(
                                "Updated GUI version ",
                                new_version,
                                " is available for download.\r\nCurrent GUI version ",
                                current_version,
                                ".\r\n\r\nInstall latest GUI now? (Recommended)\r\n\r\nTo install the latest PGA460-Q1 EVM GUI at any time, visit:\r\n ",
                                PGA460_ToolsSoftware_Link
                                ),
                            "GUI Update Available",
                            MessageBoxButtons.YesNo,
                            MessageBoxIcon.Exclamation) == DialogResult.Yes)
                        {
                            Process.Start("http://www.ti.com/lit/zip/slac739");
                        }
                    }
                    else if (checkGUIUpdateOnce)
                    {
                        MessageBox.Show(
                            string.Concat(
                                "No GUI updates available.\r\nLatest version ",
                                current_version,
                                " is installed.\r\nVersion ",
                                new_version,
                                " available on the web."
                                ),
                            "Latest GUI Installed",
                            MessageBoxButtons.OK, MessageBoxIcon.Exclamation);
                    }
                }
            }
            catch
            {
                if (checkGUIUpdateOnce)
                {
                    MessageBox.Show(
                        string.Concat(
                            "No web connection available.\nUnable to check for GUI updates.\n\n",
                            PGA460_ToolsSoftware_Link
                            ),
                        "No Web Connection",
                        MessageBoxButtons.OK,
                        MessageBoxIcon.Hand);
                }
            }
        }

        public string GetUntilOrEmpty(string text)
        {
            string value = ")";
            if (!string.IsNullOrWhiteSpace(text))
            {
                int num = text.IndexOf(value, StringComparison.Ordinal);
                if (num > 0)
                    return text.Substring(0, num);
            }
            return string.Empty;
        }

        private void videoTrainingToolStripMenuItem_Click(object sender, EventArgs e)
        {
            Process.Start("https://training.ti.com/ultrasonic-sensing-pga460-q1");
        }

        private void exportSAType_SelectedIndexChanged(object sender, EventArgs e)
        {
            exportSaveAs =
                exportSAType.SelectedIndex == 1
                ? "csv"
                : "txt";
        }

        private void fastUpdateChk_CheckedChanged(object sender, EventArgs e)
        {
            fastAcqUpDown.Visible = fastUpdateChk.Checked;
        }

        private void uartBaudCombo_SelectedIndexChanged(object sender, EventArgs e)
        {
            eddDelay = 100.0 - Math.Pow((double)(uartBaudCombo.SelectedIndex + 5), 2.0) + Math.Pow((double)(5 / (uartBaudCombo.SelectedIndex + 1)), 3.0);
            common.u2a.UART_Control((UART_BaudRate)uartBaudCombo.SelectedIndex, UART_Parity.None, UART_BitDirection.LSB_First, UART_CharacterLength._8_Bit, UART_StopBits.Two);
        }

        private void psExePriUp_Click(object sender, EventArgs e)
        {
            foreach (object obj in psExePriList.SelectedItems)
            {
                ListViewItem listViewItem = (ListViewItem)obj;
                if (listViewItem.Index > 0)
                {
                    int index = listViewItem.Index - 1;
                    psExePriList.Items.RemoveAt(listViewItem.Index);
                    psExePriList.Items.Insert(index, listViewItem);
                }
            }
        }

        private void psExePriDown_Click(object sender, EventArgs e)
        {
            foreach (object obj in psExePriList.SelectedItems)
            {
                ListViewItem listViewItem = (ListViewItem)obj;
                if (listViewItem.Index < psExePriList.Items.Count - 1)
                {
                    int index = listViewItem.Index + 1;
                    psExePriList.Items.RemoveAt(listViewItem.Index);
                    psExePriList.Items.Insert(index, listViewItem);
                }
            }
        }

        private void psExePriToggle_Click(object sender, EventArgs e)
        {
            for (int i = 0; i < psExePriList.SelectedItems.Count; i++)
            {
                psExePriList.SelectedItems[i].BackColor =
                    psExePriList.SelectedItems[i].BackColor == Color.Gray
                    ? Color.White
                    : Color.Gray;
            }
        }

        private void psEnableCheck_CheckedChanged(object sender, EventArgs e)
        {
            if (psEnableCheck.Checked)
            {
                psGroupBox.Visible = true;
                matchEnable.Checked = false;
                psGroupBox.Text = "Parameters Sweep";
            }
            else
            {
                psGroupBox.Visible = false;
            }
        }

        private void psRunBtn_Click(object sender, EventArgs e)
        {
            if (psEnableCheck.Checked)
            {
                if (psRunBtn.Text == "START")
                {
                    psRunBtn.Text = "STOP";
                    tempHoldUserConfig();
                    psRun();
                }
                else
                {
                    psRunBtn.Text = "START";
                }
                loadTempHoldUserConfig();
            }
            if (matchEnable.Checked)
            {
                ushort i2C_Address = 32;
                ushort i2C_Address2 = 33;
                byte registerAddress = 1;
                if (psRunBtn.Text == "START")
                {
                    int count = matchResQ.Items.Count;
                    if (matchResQ.Items.Count != matchCapQ.Items.Count)
                    {
                        count =
                            matchResQ.Items.Count < matchCapQ.Items.Count
                            ? matchResQ.Items.Count
                            : matchCapQ.Items.Count;
                    }

                    for (int i = 0; i < count; i++)
                    {
                        matchResQ.SelectedIndex = i;
                        matchCapQ.SelectedIndex = i;
                        common.u2a.I2C_RegisterWrite(i2C_Address, registerAddress, matchReturn(true, matchResQ.SelectedItem.ToString()));
                        common.u2a.I2C_RegisterWrite(i2C_Address2, registerAddress, matchReturn(false, matchCapQ.SelectedItem.ToString()));
                        psStatusBox.Text = matchResQ.SelectedItem.ToString() + "Ω & " + matchCapQ.SelectedItem.ToString() + "pF";
                        runBtn_Click(null, null);
                    }
                    common.u2a.I2C_RegisterWrite(i2C_Address, registerAddress, 0);
                    common.u2a.I2C_RegisterWrite(i2C_Address2, registerAddress, 0);
                }
                else
                {
                    psRunBtn.Text = "START";
                }
            }
        }

        private void psRun()
        {
            while (psRunBtn.Text == "STOP")
            {
                psStatusBox.Text = "Started!";
                for (int i = 0; i < psExePriList.Items.Count; i++)
                {
                    if (psRunBtn.Text == "START")
                    {
                        break;
                    }
                    loadTempHoldUserConfig();
                    if (psExePriList.Items[i].Text == "Frequency" && psExePriList.Items[i].BackColor != Color.Gray)
                    {
                        if (psFreqStart.Value >= psFreqEnd.Value)
                        {
                            psStatusBox.Text = "Frequency, Start !>= End";
                        }
                        else
                        {
                            int num = 0;
                            while (num <= (psFreqEnd.Value - psFreqStart.Value) / psFreqInc.Value)
                            {
                                if (psRunBtn.Text == "START")
                                {
                                    break;
                                }
                                string text = (psFreqStart.Value + psFreqInc.Value * num).ToString();
                                if (text.Contains(".0"))
                                {
                                    freqCombo.Text = text.Replace(".0", "");
                                }
                                else
                                {
                                    freqCombo.Text = text;
                                }
                                psStatusBox.Text = "Frequency, " + freqCombo.Text + "kHz";
                                psStatusBox.Update();
                                runBtn_Click(null, null);
                                num++;
                            }
                        }
                    }
                    if (psExePriList.Items[i].Text == "Burst Pulses" && psExePriList.Items[i].BackColor != Color.Gray)
                    {
                        if (psBPStart.Value >= psBPEnd.Value)
                        {
                            psStatusBox.Text = "Pulses, Start !>= End";
                        }
                        else
                        {
                            int num = 0;
                            while (num <= (psBPEnd.Value - psBPStart.Value) / psBPInc.Value)
                            {
                                if (psRunBtn.Text == "START")
                                {
                                    break;
                                }
                                p1PulsesCombo.Text = (psBPStart.Value + psBPInc.Value * num).ToString();
                                p2PulsesCombo.Text = p1PulsesCombo.Text;
                                psStatusBox.Text = "Pulses, " + p1PulsesCombo.Text;
                                runBtn_Click(null, null);
                                num++;
                            }
                        }
                    }
                    if (psExePriList.Items[i].Text == "Driver Current" && psExePriList.Items[i].BackColor != Color.Gray)
                    {
                        if (psILimStart.Value >= psILimEnd.Value)
                        {
                            psStatusBox.Text = "Driver, Start !>= End";
                        }
                        else
                        {
                            int num = 0;
                            while (num <= (psILimEnd.Value - psILimStart.Value) / psILimInc.Value)
                            {
                                if (psRunBtn.Text == "START")
                                {
                                    break;
                                }
                                p1DriveCombo.Text = (psILimStart.Value + psILimInc.Value * num).ToString();
                                p2DriveCombo.Text = p1DriveCombo.Text;
                                psStatusBox.Text = "Driver, " + p1DriveCombo.Text + "mA";
                                runBtn_Click(null, null);
                                num++;
                            }
                        }
                    }
                    if (psExePriList.Items[i].Text == "Fixed Gain" && psExePriList.Items[i].BackColor != Color.Gray)
                    {
                        if (psGainStart.Value >= psGainEnd.Value)
                        {
                            psStatusBox.Text = "Gain, Start !>= End";
                        }
                        else
                        {
                            int num = 0;
                            while (num <= (psGainEnd.Value - psGainStart.Value) / psGainInc.Value)
                            {
                                if (psRunBtn.Text == "START")
                                {
                                    break;
                                }
                                if (psGainStart.Value + psGainInc.Value * num < 58m)
                                {
                                    if (AFEGainRngCombo.SelectedIndex != 3)
                                        AFEGainRngCombo.SelectedIndex = 3;
                                }
                                else if (AFEGainRngCombo.SelectedIndex == 3)
                                {
                                    AFEGainRngCombo.SelectedIndex = 0;
                                }
                                string text = (psGainStart.Value + psGainInc.Value * num).ToString();
                                if (text.Contains(".0"))
                                {
                                    gainCombo.Text = text.Replace(".0", "");
                                }
                                else
                                {
                                    gainCombo.Text = text;
                                }
                                tvgInstantUpdateCheck.Checked = true;
                                allInitGainBtn_Click(null, null);
                                tvgInstantUpdateCheck.Checked = false;
                                psStatusBox.Text = "Gain, " + gainCombo.Text + "dB";
                                runBtn_Click(null, null);
                                num++;
                            }
                        }
                    }
                    if (psExePriList.Items[i].Text == "Digital Multiplier" && psExePriList.Items[i].BackColor != Color.Gray)
                    {
                        if (psDMStart.Value >= psDMEnd.Value)
                        {
                            psStatusBox.Text = "DM, Start !>= End";
                        }
                        else
                        {
                            int num = 0;
                            while (num <= (psDMEnd.Value - psDMStart.Value) / psDMInc.Value)
                            {
                                if (psRunBtn.Text == "START")
                                {
                                    break;
                                }
                                p1DigGainSr.Text = "x" + Math.Pow(2.0, Convert.ToDouble(psDMStart.Value + psDMInc.Value * num)).ToString();
                                p2DigGainLr.Text = (p2DigGainSr.Text = (p1DigGainLr.Text = p1DigGainSr.Text));
                                psStatusBox.Text = "Multipler, " + p1DigGainSr.Text;
                                runBtn_Click(null, null);
                                num++;
                            }
                        }
                    }
                    if (i == psExePriList.Items.Count - 1)
                    {
                        psRunBtn.Text = "START";
                        psStatusBox.Text = "Finished!";
                    }
                }
            }
        }

        private void tempHoldUserConfig()
        {
            HOLDfreqCombo = freqCombo.SelectedIndex;
            HOLDfreqshiftCheck = freqshiftCheck.Checked;
            HOLDdeadCombo = deadCombo.SelectedIndex;
            HOLDAFEGainRngCombo = AFEGainRngCombo.SelectedIndex;
            HOLDgainCombo = gainCombo.SelectedIndex;
            HOLDbpbwCombo = bpbwCombo.SelectedIndex;
            HOLDcutoffCombo = cutoffCombo.SelectedIndex;
            HOLDthrCmpDeglitchCombo = thrCmpDeglitchCombo.SelectedIndex;
            HOLDdecoupletimeRadio = decoupletimeRadio.Checked;
            HOLDdecoupletimeBox = decoupletimeBox.SelectedIndex;
            HOLDtempgainCombo = tempgainCombo.SelectedIndex;
            HOLDtempoffsetCombo = tempoffsetCombo.SelectedIndex;
            HOLDp1PulsesCombo = p1PulsesCombo.SelectedIndex;
            HOLDp1DriveCombo = p1DriveCombo.SelectedIndex;
            HOLDp1RecordCombo = p1RecordCombo.SelectedIndex;
            HOLDnlsNoiseCombo = nlsNoiseCombo.SelectedIndex;
            HOLDnlsTOPCombo = nlsTOPCombo.SelectedIndex;
            HOLDp1DigGainSr = p1DigGainSr.SelectedIndex;
            HOLDp1DigGainLr = p1DigGainLr.SelectedIndex;
            HOLDp1DigGainLrSt = p1DigGainLrSt.SelectedIndex;
            HOLDp2DigGainSr = p2DigGainSr.SelectedIndex;
            HOLDp2DigGainLr = p2DigGainLr.SelectedIndex;
            HOLDp2DigGainLrSt = p2DigGainLrSt.SelectedIndex;
            HOLDtvgg0 = tvgg0.SelectedIndex;
            HOLDtvgg1 = tvgg1.SelectedIndex;
            HOLDtvgg2 = tvgg2.SelectedIndex;
            HOLDtvgg3 = tvgg3.SelectedIndex;
            HOLDtvgg4 = tvgg4.SelectedIndex;
            HOLDtvgg5 = tvgg5.SelectedIndex;
            HOLDtvgt0 = tvgt0.SelectedIndex;
            HOLDtvgt1 = tvgt1.SelectedIndex;
            HOLDtvgt2 = tvgt2.SelectedIndex;
            HOLDtvgt3 = tvgt3.SelectedIndex;
            HOLDtvgt4 = tvgt4.SelectedIndex;
            HOLDtvgt5 = tvgt5.SelectedIndex;
        }

        private void loadTempHoldUserConfig()
        {
            freqCombo.SelectedIndex = HOLDfreqCombo;
            freqshiftCheck.Checked = HOLDfreqshiftCheck;
            deadCombo.SelectedIndex = HOLDdeadCombo;
            AFEGainRngCombo.SelectedIndex = HOLDAFEGainRngCombo;
            gainCombo.SelectedIndex = HOLDgainCombo;
            bpbwCombo.SelectedIndex = HOLDbpbwCombo;
            cutoffCombo.SelectedIndex = HOLDcutoffCombo;
            thrCmpDeglitchCombo.SelectedIndex = HOLDthrCmpDeglitchCombo;
            decoupletimeRadio.Checked = HOLDdecoupletimeRadio;
            decoupletimeBox.SelectedIndex = HOLDdecoupletimeBox;
            tempgainCombo.SelectedIndex = HOLDtempgainCombo;
            tempoffsetCombo.SelectedIndex = HOLDtempoffsetCombo;
            p1PulsesCombo.SelectedIndex = HOLDp1PulsesCombo;
            p1DriveCombo.SelectedIndex = HOLDp1DriveCombo;
            p1RecordCombo.SelectedIndex = HOLDp1RecordCombo;
            nlsNoiseCombo.SelectedIndex = HOLDnlsNoiseCombo;
            nlsTOPCombo.SelectedIndex = HOLDnlsTOPCombo;
            p1DigGainSr.SelectedIndex = HOLDp1DigGainSr;
            p1DigGainLr.SelectedIndex = HOLDp1DigGainLr;
            p1DigGainLrSt.SelectedIndex = HOLDp1DigGainLrSt;
            p2DigGainSr.SelectedIndex = HOLDp2DigGainSr;
            p2DigGainLr.SelectedIndex = HOLDp2DigGainLr;
            p2DigGainLrSt.SelectedIndex = HOLDp2DigGainLrSt;
            tvgInstantUpdateCheck.Checked = true;
            tvgg0.SelectedIndex = HOLDtvgg0;
            tvgg1.SelectedIndex = HOLDtvgg1;
            tvgg2.SelectedIndex = HOLDtvgg2;
            tvgg3.SelectedIndex = HOLDtvgg3;
            tvgg4.SelectedIndex = HOLDtvgg4;
            tvgg5.SelectedIndex = HOLDtvgg5;
            tvgt0.SelectedIndex = HOLDtvgt0;
            tvgt1.SelectedIndex = HOLDtvgt1;
            tvgt2.SelectedIndex = HOLDtvgt2;
            tvgt3.SelectedIndex = HOLDtvgt3;
            tvgt4.SelectedIndex = HOLDtvgt4;
            tvgt5.SelectedIndex = HOLDtvgt5;
            writeTVGMemBtn_Click(null, null);
            tvgInstantUpdateCheck.Checked = false;
        }

        private void bit12sampleRadio_CheckedChanged(object sender, EventArgs e)
        {
            if (bit12sampleRadio.Checked)
            {
                sampleOut12bitRadio.Checked = true;
            }
            else
            {
                sampleOut12bitRadio.Checked = false;
            }
        }

        private void adcShiftLeftBtn_Click(object sender, EventArgs e)
        {
            ShiftLeft(mem_return_buf_all);
            plotADCBtn_Click(null, null);
        }

        private void adcShiftRightBtn_Click(object sender, EventArgs e)
        {
            ShiftRight(mem_return_buf_all);
            plotADCBtn_Click(null, null);
        }

        private void adcTrackMin_Scroll(object sender, EventArgs e)
        {
            if (adcTrackMin.Value < adcTrackMax.Value)
            {
                double minimum = (double)adcTrackMin.Value / 100.0 * adcChart.ChartAreas[0].AxisX.Maximum;
                adcChart.ChartAreas[0].AxisX.Minimum = minimum;
                if (adcChart.ChartAreas[0].AxisX.Maximum > adcChart.ChartAreas[0].AxisX.Minimum)
                {
                    adcChart.Update();
                }
            }
            else
            {
                adcTrackMin.Value = adcTrackMin.Value;
            }
        }

        private void adcTrackMax_Scroll(object sender, EventArgs e)
        {
            if (adcTrackMin.Value < adcTrackMax.Value)
            {
                double maximum = (double)adcTrackMax.Value / 100.0 * (double)adcChartMax;
                adcChart.ChartAreas[0].AxisX.Maximum = maximum;
                if (adcChart.ChartAreas[0].AxisX.Maximum > adcChart.ChartAreas[0].AxisX.Minimum)
                {
                    adcChart.Update();
                }
            }
            else
            {
                adcTrackMax.Value = adcTrackMax.Value;
            }
        }

        private void nullADCFilter_Click(object sender, EventArgs e)
        {
            adcLowerLimitHigh.Value = 0m;
            adcLowerLimitLow.Value = 0m;
            adcUpperLimitHigh.Value = 255m;
            adcUpperLimitLow.Value = 255m;
        }

        private void togLED_VD_CheckedChanged(object sender, EventArgs e)
        {
            if (togLED_VD.Checked)
            {
                common.u2a.GPIO_WritePort(12, 2);
            }
            else
            {
                common.u2a.GPIO_WritePort(12, 1);
            }
        }

        private void togLED_FD_CheckedChanged(object sender, EventArgs e)
        {
            if (togLED_FD.Checked)
            {
                common.u2a.GPIO_WritePort(11, 2);
            }
            else
            {
                common.u2a.GPIO_WritePort(11, 1);
            }
        }

        private void togLED_DS_CheckedChanged(object sender, EventArgs e)
        {
            if (togLED_DS.Checked)
            {
                common.u2a.GPIO_WritePort(10, 2);
            }
            else
            {
                common.u2a.GPIO_WritePort(10, 1);
            }
        }

        private void togGPIO7_CheckedChanged(object sender, EventArgs e)
        {
            common.u2a.GPIO_SetPort(7, 1);
            if (togGPIO7.Checked)
            {
                common.u2a.GPIO_WritePort(7, 2);
            }
            else
            {
                common.u2a.GPIO_WritePort(7, 1);
            }
        }

        private void togGPIO6_CheckedChanged(object sender, EventArgs e)
        {
            common.u2a.GPIO_SetPort(6, 1);
            if (togGPIO6.Checked)
            {
                common.u2a.GPIO_WritePort(6, 2);
            }
            else
            {
                common.u2a.GPIO_WritePort(6, 1);
            }
        }

        private void thr_load_chart_Click(object sender, EventArgs e)
        {
            int num = 0;
            InitializeOpenFileDialog();
            DialogResult dialogResult = openFileDialog1.ShowDialog();
            thrChart.Series[3].Points.Clear();
            thrChart.Series[4].Points.Clear();
            thrChart.Series[4].ChartType = SeriesChartType.Line;
            if (dialogResult == DialogResult.OK)
            {
                foreach (string path in openFileDialog1.FileNames)
                {
                    try
                    {
                        string text = string.Empty;
                        double num2 = 0.0;
                        StreamReader streamReader = new StreamReader(path);
                        while ((text = streamReader.ReadLine()) != null)
                        {
                            string[] array = text.Split(new char[]
                            {
                                ','
                            });
                            for (int j = 0; j < array.Length; j++)
                            {
                                array[j] = array[j].Trim();
                            }
                            if (array.Count<string>() == 4)
                            {
                                if (num2 > 0.0 && double.Parse(array[1]) == 0.0)
                                {
                                    thrChart.Series[4].Points.AddXY(double.Parse(array[1]), -1.0);
                                    thrChart.Series[4].Points.AddXY(0.0, -1.0);
                                }
                                thrChart.Series[4].Points.AddXY(double.Parse(array[1]), double.Parse(array[2]));
                                num2 = double.Parse(array[2]);
                            }
                        }
                        num++;
                    }
                    catch
                    {
                        MessageBox.Show("Failed to load. Select the .TXT or .CSV file exported by the Data Monitor page.");
                    }
                }
            }
            thrChart.Update();
            updateThrBtn_Click(null, null);
        }

        private void InitializeOpenFileDialog()
        {
            openFileDialog1.Filter = "Text Files|*.txt";
            openFileDialog1.Multiselect = true;
            openFileDialog1.Title = "My Text Browser";
        }

        private void tvg_load_chart_Click(object sender, EventArgs e)
        {
            int num = 0;
            InitializeOpenFileDialog();
            DialogResult dialogResult = openFileDialog1.ShowDialog();
            tvgChart.Series[2].Points.Clear();
            tvgChart.Series[2].ChartType = SeriesChartType.Line;
            if (dialogResult == DialogResult.OK)
            {
                foreach (string path in openFileDialog1.FileNames)
                {
                    try
                    {
                        string text = string.Empty;
                        double num2 = 0.0;
                        StreamReader streamReader = new StreamReader(path);
                        while ((text = streamReader.ReadLine()) != null)
                        {
                            string[] array = text.Split(new char[]
                            {
                                ','
                            });
                            for (int j = 0; j < array.Length; j++)
                            {
                                array[j] = array[j].Trim();
                            }
                            if (array.Count<string>() == 4)
                            {
                                if (num2 > 0.0 && double.Parse(array[1]) == 0.0)
                                {
                                    tvgChart.Series[2].Points.AddXY(double.Parse(array[1]), -1.0);
                                    tvgChart.Series[2].Points.AddXY(0.0, -1.0);
                                }
                                tvgChart.Series[2].Points.AddXY(double.Parse(array[1]), double.Parse(array[2]));
                                tvgChart.Series[1].Points.AddXY(double.Parse(array[1]) * 343.0 / 2.0 / 1000.0, 0.0);
                                num2 = double.Parse(array[2]);
                            }
                        }
                        num++;
                    }
                    catch
                    {
                        MessageBox.Show("Failed to load. Select the .TXT or .CSV file exported by the Data Monitor page.");
                    }
                }
            }
            tvgChart.Update();
            updateTVGBtn_Click(null, null);
        }

        private void label197_Click(object sender, EventArgs e)
        {
        }

        private void ambientVPWR_SelectedIndexChanged(object sender, EventArgs e)
        {
            if (ambientTemp.Text != "")
            {
                ambientTemp.Text = Tools.Double_to_string(double.Parse(tempBox.Text) - 96.1 * (double.Parse(ambientVPWR.Text) * 0.012), 1);
            }
        }

        private void writeCoef_Click(object sender, EventArgs e)
        {
            debugTabControl.SelectTab(utilTab);
            debugTabControl.SelectedTab.Focus();
            List<string> list = new List<string>();
            int num = 2;
            for (int i = 0; i < bpcoa2Box.Text.Length; i += num)
            {
                if (bpcoa2Box.Text.Length - i >= num)
                {
                    list.Add(bpcoa2Box.Text.Substring(i, num));
                }
                else
                {
                    list.Add(bpcoa2Box.Text.Substring(i, bpcoa2Box.Text.Length - i));
                }
            }
            for (int i = 0; i < bpcoa3Box.Text.Length; i += num)
            {
                if (bpcoa3Box.Text.Length - i >= num)
                {
                    list.Add(bpcoa3Box.Text.Substring(i, num));
                }
                else
                {
                    list.Add(bpcoa3Box.Text.Substring(i, bpcoa3Box.Text.Length - i));
                }
            }
            for (int i = 0; i < bpcob1Box.Text.Length; i += num)
            {
                if (bpcob1Box.Text.Length - i >= num)
                {
                    list.Add(bpcob1Box.Text.Substring(i, num));
                }
                else
                {
                    list.Add(bpcob1Box.Text.Substring(i, bpcob1Box.Text.Length - i));
                }
            }
            for (int i = 0; i < 6; i++)
            {
                try
                {
                    UART_Read_Write((byte)(65 + i), Tools.StringBase16IntoByte(list[i]), true);
                }
                catch
                {
                }
            }
        }

        private void enableOWUSynchronousHardwareToolStripMenuItem_Click(object sender, EventArgs e)
        {
            if (enableOWUSynchronousHardwareToolStripMenuItem.Text == "Enable OWU && Synchronous Hardware")
            {
                common.u2a.GPIO_SetPort(1, 1);
                common.u2a.GPIO_WritePort(1, 2);
                enableOWUSynchronousHardwareToolStripMenuItem.Text = "Disable OWU && Synchronous Hardware";
            }
            else if (enableOWUSynchronousHardwareToolStripMenuItem.Text == "Disable OWU && Synchronous Hardware")
            {
                common.u2a.GPIO_SetPort(1, 1);
                common.u2a.GPIO_WritePort(1, 1);
                enableOWUSynchronousHardwareToolStripMenuItem.Text = "Enable OWU && Synchronous Hardware";
            }
        }

        private void extTrigGPIO_CheckedChanged(object sender, EventArgs e)
        {
            if (extTrigGPIO.Checked)
            {
                common.u2a.GPIO_SetPort(7, 4);
                extTrigByte = 1;
                syncPin.Checked = false;
            }
            else
            {
                extTrigByte = 0;
            }
        }

        private void syncPin_CheckedChanged(object sender, EventArgs e)
        {
            if (syncPin.Checked)
            {
                common.u2a.GPIO_SetPort(7, 1);
                extTrigByte = 2;
                extTrigGPIO.Checked = false;
            }
            else
            {
                extTrigByte = 0;
            }
        }

        public void extTrigTextCheck()
        {
            if (extTrigGPIO.Checked && extTrigByte == 1)
            {
                bool flag = false;
                string text = toolStripStatusLabel1.Text;
                updateStatusBar(text + "... Waiting for Desktop/pga460_trig.txt to contain the string DONE.");
                while (!flag)
                {
                    string a = File.ReadAllText(Environment.GetFolderPath(Environment.SpecialFolder.Desktop) + "\\pga460_trig.txt");
                    if (a == "DONE")
                    {
                        string path = Environment.GetFolderPath(Environment.SpecialFolder.Desktop) + "\\pga460_trig.txt";
                        File.WriteAllText(path, string.Empty);
                        flag = true;
                    }
                    Tools.timeDelay(1, "S");
                }
                updateStatusBar(text);
            }
        }

        private void rawMaskBtn_Click(object sender, EventArgs e)
        {
            rawMaskShift++;
            if (rawMaskShift >= 10)
            {
                rawMaskShift = 0;
            }
            plotADCBtn_Click(null, null);
        }

        private void rawNoiseFilterBtn_Click(object sender, EventArgs e)
        {
            adcRawFilter.Checked = true;
            reverseADCCheck.Checked = false;
            plotADCBtn_Click(null, null);
            adcRawFilter.Checked = false;
        }

        private void drvEnable_CheckedChanged(object sender, EventArgs e)
        {
            if (drvEnable.Checked)
            {
                common.u2a.GPIO_SetPort(2, 1);
                common.u2a.GPIO_WritePort(2, 2);
                common.u2a.GPIO_SetPort(3, 1);
                common.u2a.GPIO_WritePort(3, 2);
            }
        }

        private void drvCoast_Click(object sender, EventArgs e)
        {
            drvA.AppendText("0");
            drvB.AppendText("0");
        }

        private void drvRev_Click(object sender, EventArgs e)
        {
            drvA.AppendText("0");
            drvB.AppendText("1");
        }

        private void drvFor_Click(object sender, EventArgs e)
        {
            drvA.AppendText("1");
            drvB.AppendText("0");
        }

        private void drvBra_Click(object sender, EventArgs e)
        {
            drvA.AppendText("1");
            drvB.AppendText("1");
        }

        private void fixedGain_CheckedChanged(object sender, EventArgs e)
        {
            if (fixedGain.Checked)
            {
                gainCombo_SelectedIndexChanged(null, null);
            }
        }

        private void matchEnable_CheckedChanged(object sender, EventArgs e)
        {
            if (matchEnable.Checked)
            {
                psEnableCheck.Checked = false;
                psGroupBox.Visible = true;
                psGroupBox.Text = "Matching Sweep";
                common.u2a.I2C_Control(I2C_Speed._100kHz, I2C_AddressLength._7Bits, I2C_PullUps.OFF);
                ushort i2C_Address = 32;
                ushort i2C_Address2 = 33;
                byte registerAddress = 1;
                byte registerAddress2 = 3;
                Tools.timeDelay(10, "MS");
                common.u2a.I2C_RegisterWrite(i2C_Address, registerAddress2, 0);
                common.u2a.I2C_RegisterWrite(i2C_Address2, registerAddress2, 0);
                common.u2a.I2C_RegisterWrite(i2C_Address, registerAddress, 0);
                common.u2a.I2C_RegisterWrite(i2C_Address2, registerAddress, 0);
            }
            else
            {
                psGroupBox.Visible = false;
            }
        }

        private void matchQueueSel_Click(object sender, EventArgs e)
        {
            matchResQ.Items.Add(matchResList.SelectedItem.ToString());
            matchCapQ.Items.Add(matchCapList.SelectedItem.ToString());
        }

        private void matchQueueRemove_Click(object sender, EventArgs e)
        {
            try
            {
                matchResQ.Items.RemoveAt(matchResQ.SelectedIndex);
            }
            catch
            {
            }
            try
            {
                matchCapQ.Items.RemoveAt(matchCapQ.SelectedIndex);
            }
            catch
            {
            }
        }

        private void matchQueueUp_Click(object sender, EventArgs e)
        {
            MoveUp();
        }

        private void matchQueueDown_Click(object sender, EventArgs e)
        {
            MoveDown();
        }

        public void MoveUp()
        {
            MoveItem(-1);
        }

        public void MoveDown()
        {
            MoveItem(1);
        }

        public void MoveItem(int direction)
        {
            if (matchResQ.SelectedIndex != -1)
            {
                if (matchResQ.SelectedItem == null || matchResQ.SelectedIndex < 0)
                {
                    return;
                }
                int num = matchResQ.SelectedIndex + direction;
                if (num < 0 || num >= matchResQ.Items.Count)
                {
                    return;
                }
                object selectedItem = matchResQ.SelectedItem;
                matchResQ.Items.Remove(selectedItem);
                matchResQ.Items.Insert(num, selectedItem);
                matchResQ.SetSelected(num, true);
            }
            if (matchCapQ.SelectedIndex != -1)
            {
                if (matchCapQ.SelectedItem != null && matchCapQ.SelectedIndex >= 0)
                {
                    int num = matchCapQ.SelectedIndex + direction;
                    if (num >= 0 && num < matchCapQ.Items.Count)
                    {
                        object selectedItem = matchCapQ.SelectedItem;
                        matchCapQ.Items.Remove(selectedItem);
                        matchCapQ.Items.Insert(num, selectedItem);
                        matchCapQ.SetSelected(num, true);
                    }
                }
            }
        }

        private void matchQueueClear_Click(object sender, EventArgs e)
        {
            matchResQ.Items.Clear();
            matchCapQ.Items.Clear();
        }

        private void matchFixedResAdd_Click(object sender, EventArgs e)
        {
            int selectedIndex = matchCapCustomMin.SelectedIndex;
            int selectedIndex2 = matchCapCustomMax.SelectedIndex;
            int num = 0;
            if (matchResList.SelectedIndex != -1)
            {
                int num2 = 0;
                while (num2 <= (selectedIndex2 - selectedIndex) * (matchCustomCapPerRange.Value / 100m))
                {
                    matchCapCustomMin.SelectedIndex = (int)Math.Round(selectedIndex + num2 * ((selectedIndex2 - selectedIndex) / ((selectedIndex2 - selectedIndex) * (matchCustomCapPerRange.Value / 100m))));
                    if (num != matchCapCustomMin.SelectedIndex)
                    {
                        matchResQ.Items.Add(matchResList.SelectedItem.ToString());
                        matchCapQ.Items.Add(matchCapCustomMin.Text);
                    }
                    num = matchCapCustomMin.SelectedIndex;
                    num2++;
                }
                matchCapCustomMin.SelectedIndex = selectedIndex;
            }
        }

        private void matchFixedCapAdd_Click(object sender, EventArgs e)
        {
            int selectedIndex = matchResCustomMin.SelectedIndex;
            int selectedIndex2 = matchResCustomMax.SelectedIndex;
            int num = 0;
            if (matchCapList.SelectedIndex != -1)
            {
                int num2 = 0;
                while (num2 <= (selectedIndex2 - selectedIndex) * (matchCustomResPerRange.Value / 100m))
                {
                    matchResCustomMin.SelectedIndex = (int)Math.Round(selectedIndex + num2 * ((selectedIndex2 - selectedIndex) / ((selectedIndex2 - selectedIndex) * (matchCustomResPerRange.Value / 100m))));
                    if (num != matchResCustomMin.SelectedIndex)
                    {
                        matchCapQ.Items.Add(matchCapList.SelectedItem.ToString());
                        matchResQ.Items.Add(matchResCustomMin.Text);
                    }
                    num = matchCapCustomMin.SelectedIndex;
                    num2++;
                }
                matchResCustomMin.SelectedIndex = selectedIndex;
            }
        }

        private void matchCustomResAdd_Click(object sender, EventArgs e)
        {
            int selectedIndex = matchResCustomMin.SelectedIndex;
            int selectedIndex2 = matchResCustomMax.SelectedIndex;
            int num = 0;
            int selectedIndex3 = matchCapCustomMin.SelectedIndex;
            int selectedIndex4 = matchCapCustomMax.SelectedIndex;
            int num2 = 0;
            if (selectedIndex2 > selectedIndex && selectedIndex4 > selectedIndex3)
            {
                int num3 = 0;
                while (num3 <= (selectedIndex2 - selectedIndex) * (matchCustomResPerRange.Value / 100m))
                {
                    matchResCustomMin.SelectedIndex = (int)Math.Round(selectedIndex + num3 * ((selectedIndex2 - selectedIndex) / ((selectedIndex2 - selectedIndex) * (matchCustomResPerRange.Value / 100m))));
                    if (num != matchResCustomMin.SelectedIndex || num3 == 0)
                    {
                        int num4 = 0;
                        while (num4 <= (selectedIndex4 - selectedIndex3) * (matchCustomCapPerRange.Value / 100m))
                        {
                            matchCapCustomMin.SelectedIndex = (int)Math.Round(selectedIndex3 + num4 * ((selectedIndex4 - selectedIndex3) / ((selectedIndex4 - selectedIndex3) * (matchCustomCapPerRange.Value / 100m))));
                            if (num2 != matchCapCustomMin.SelectedIndex)
                            {
                                matchResQ.Items.Add(matchResCustomMin.Text);
                                matchCapQ.Items.Add(matchCapCustomMin.Text);
                            }
                            num2 = matchCapCustomMin.SelectedIndex;
                            num4++;
                        }
                        num2 = 0;
                        num = matchResCustomMin.SelectedIndex;
                    }
                    num = matchResCustomMin.SelectedIndex;
                    num3++;
                }
                matchCapCustomMin.SelectedIndex = selectedIndex3;
                matchResCustomMin.SelectedIndex = selectedIndex;
            }
        }

        private void matchCustomCapAdd_Click(object sender, EventArgs e)
        {
            int selectedIndex = matchResCustomMin.SelectedIndex;
            int selectedIndex2 = matchResCustomMax.SelectedIndex;
            int num = 0;
            int selectedIndex3 = matchCapCustomMin.SelectedIndex;
            int selectedIndex4 = matchCapCustomMax.SelectedIndex;
            int num2 = 0;
            if (selectedIndex2 > selectedIndex && selectedIndex4 > selectedIndex3)
            {
                int num3 = 0;
                while (num3 <= (selectedIndex4 - selectedIndex3) * (matchCustomCapPerRange.Value / 100m))
                {
                    matchCapCustomMin.SelectedIndex = (int)Math.Round(selectedIndex3 + num3 * ((selectedIndex4 - selectedIndex3) / ((selectedIndex4 - selectedIndex3) * (matchCustomResPerRange.Value / 100m))));
                    if (num2 != matchCapCustomMin.SelectedIndex || num3 == 0)
                    {
                        int num4 = 0;
                        while (num4 <= (selectedIndex2 - selectedIndex) * (matchCustomResPerRange.Value / 100m))
                        {
                            matchResCustomMin.SelectedIndex = (int)Math.Round(selectedIndex + num4 * ((selectedIndex2 - selectedIndex) / ((selectedIndex2 - selectedIndex) * (matchCustomResPerRange.Value / 100m))));
                            if (num != matchResCustomMin.SelectedIndex)
                            {
                                matchCapQ.Items.Add(matchCapCustomMin.Text);
                                matchResQ.Items.Add(matchResCustomMin.Text);
                            }
                            num = matchCapCustomMin.SelectedIndex;
                            num4++;
                        }
                        num = 0;
                        num2 = matchCapCustomMin.SelectedIndex;
                    }
                    num2 = matchCapCustomMin.SelectedIndex;
                    num3++;
                }
                matchCapCustomMin.SelectedIndex = selectedIndex3;
                matchResCustomMin.SelectedIndex = selectedIndex;
            }
        }

        private void matchCustomResPerRange_ValueChanged(object sender, EventArgs e)
        {
            int selectedIndex = matchResCustomMin.SelectedIndex;
            int selectedIndex2 = matchResCustomMax.SelectedIndex;
            matchCustomResCases.Text = Convert.ToString((selectedIndex2 - selectedIndex) * (matchCustomResPerRange.Value / 100m));
        }

        private void matchCustomCapPerRange_ValueChanged(object sender, EventArgs e)
        {
            int selectedIndex = matchCapCustomMin.SelectedIndex;
            int selectedIndex2 = matchCapCustomMax.SelectedIndex;
            matchCustomCapCases.Text = Convert.ToString((selectedIndex2 - selectedIndex) * (matchCustomCapPerRange.Value / 100m));
        }

        private void matchFullAdd_Click(object sender, EventArgs e)
        {
        }

        private void matchResQ_SelectedIndexChanged(object sender, EventArgs e)
        {
        }

        private void matchCapQ_SelectedIndexChanged(object sender, EventArgs e)
        {
        }

        private void matchResQ_Click(object sender, EventArgs e)
        {
            int selectedIndex = matchResQ.SelectedIndex;
            if (selectedIndex == oldResQ)
            {
                matchResQ.ClearSelected();
                oldResQ = -1;
            }
            else
            {
                oldResQ = selectedIndex;
            }
        }

        private void matchCapQ_Click(object sender, EventArgs e)
        {
            int selectedIndex = matchCapQ.SelectedIndex;
            if (selectedIndex == oldCapQ)
            {
                matchCapQ.ClearSelected();
                oldCapQ = -1;
            }
            else
            {
                oldCapQ = selectedIndex;
            }
        }

        private void matchForceCheck_CheckedChanged(object sender, EventArgs e)
        {
            ushort i2C_Address = 32;
            ushort i2C_Address2 = 33;
            byte registerAddress = 1;
            if (matchForceCheck.Checked)
            {
                matchForcingStat.Text = matchResList.SelectedItem.ToString() + "Ω & " + matchCapList.SelectedItem.ToString() + "pF";
                if (matchResList.SelectedIndex != -1)
                {
                    common.u2a.I2C_RegisterWrite(i2C_Address, registerAddress, matchReturn(true, matchResList.SelectedItem.ToString()));
                }
                if (matchCapList.SelectedIndex != -1)
                {
                    common.u2a.I2C_RegisterWrite(i2C_Address2, registerAddress, matchReturn(false, matchCapList.SelectedItem.ToString()));
                }
            }
            else
            {
                matchForcingStat.Text = "Waiting...";
                common.u2a.I2C_RegisterWrite(i2C_Address, registerAddress, 0);
                common.u2a.I2C_RegisterWrite(i2C_Address2, registerAddress, 0);
            }
        }

        private void matchingDaughtercardToolStripMenuItem_Click(object sender, EventArgs e)
        {
            leftTreeNav.SelectedNode = null;
            primaryTab.SelectTab("matchingTab");
            matchResList.SelectedIndex = 0;
            matchCapList.SelectedIndex = 0;
            matchCustomResPerRange_ValueChanged(null, null);
            matchCustomCapPerRange_ValueChanged(null, null);
            if (leftTreeNav.Nodes[0].Nodes.Count == 4)
            {
                leftTreeNav.Nodes[0].Nodes.Add("Matching");
            }
        }

        private void matchResCustomMin_SelectedIndexChanged(object sender, EventArgs e)
        {
            matchCustomResPerRange_ValueChanged(null, null);
        }

        private void matchResCustomMax_SelectedIndexChanged(object sender, EventArgs e)
        {
            matchCustomResPerRange_ValueChanged(null, null);
        }

        private void matchCapCustomMin_SelectedIndexChanged(object sender, EventArgs e)
        {
            matchCustomCapPerRange_ValueChanged(null, null);
        }

        private void matchCapCustomMax_SelectedIndexChanged(object sender, EventArgs e)
        {
            matchCustomCapPerRange_ValueChanged(null, null);
        }

        public byte matchReturn(bool ResTrue, string input)
        {
            byte result;
            if (ResTrue)
            {
                if (input == "Open")
                {
                    result = 0;
                }
                else if (input == "100000")
                {
                    result = 1;
                }
                else if (input == "20000")
                {
                    result = 2;
                }
                else if (input == "16666.7")
                {
                    result = 3;
                }
                else if (input == "15000")
                {
                    result = 4;
                }
                else if (input == "13043.5")
                {
                    result = 5;
                }
                else if (input == "8571.4")
                {
                    result = 6;
                }
                else if (input == "7894.7")
                {
                    result = 7;
                }
                else if (input == "10000")
                {
                    result = 8;
                }
                else if (input == "9090.9")
                {
                    result = 9;
                }
                else if (input == "6666.7")
                {
                    result = 10;
                }
                else if (input == "6250")
                {
                    result = 11;
                }
                else if (input == "6000")
                {
                    result = 12;
                }
                else if (input == "5660.4")
                {
                    result = 13;
                }
                else if (input == "4615.4")
                {
                    result = 14;
                }
                else if (input == "4411.8")
                {
                    result = 15;
                }
                else if (input == "5110")
                {
                    result = 16;
                }
                else if (input == "4861.6")
                {
                    result = 17;
                }
                else if (input == "4070.1")
                {
                    result = 18;
                }
                else if (input == "3910.9")
                {
                    result = 19;
                }
                else if (input == "3811.5")
                {
                    result = 20;
                }
                else if (input == "3671.6")
                {
                    result = 21;
                }
                else if (input == "3201.4")
                {
                    result = 22;
                }
                else if (input == "3102.1")
                {
                    result = 23;
                }
                else if (input == "3381.9")
                {
                    result = 24;
                }
                else if (input == "3271.2")
                {
                    result = 25;
                }
                else if (input == "2892.7")
                {
                    result = 26;
                }
                else if (input == "2811.4")
                {
                    result = 27;
                }
                else if (input == "2759.7")
                {
                    result = 28;
                }
                else if (input == "2685.6")
                {
                    result = 29;
                }
                else if (input == "2425.1")
                {
                    result = 30;
                }
                else if (input == "2367.6")
                {
                    result = 31;
                }
                else if (input == "1000")
                {
                    result = 32;
                }
                else if (input == "990.1")
                {
                    result = 33;
                }
                else if (input == "952.4")
                {
                    result = 34;
                }
                else if (input == "943.4")
                {
                    result = 35;
                }
                else if (input == "937.5")
                {
                    result = 36;
                }
                else if (input == "928.8")
                {
                    result = 37;
                }
                else if (input == "895.5")
                {
                    result = 38;
                }
                else if (input == "887.6")
                {
                    result = 39;
                }
                else if (input == "909.1")
                {
                    result = 40;
                }
                else if (input == "900.9")
                {
                    result = 41;
                }
                else if (input == "869.6")
                {
                    result = 42;
                }
                else if (input == "862.1")
                {
                    result = 43;
                }
                else if (input == "857.1")
                {
                    result = 44;
                }
                else if (input == "849.9")
                {
                    result = 45;
                }
                else if (input == "821.9")
                {
                    result = 46;
                }
                else if (input == "815.2")
                {
                    result = 47;
                }
                else if (input == "836.3")
                {
                    result = 48;
                }
                else if (input == "829.4")
                {
                    result = 49;
                }
                else if (input == "802.8")
                {
                    result = 50;
                }
                else if (input == "796.4")
                {
                    result = 51;
                }
                else if (input == "792.2")
                {
                    result = 52;
                }
                else if (input == "785.9")
                {
                    result = 53;
                }
                else if (input == "762")
                {
                    result = 54;
                }
                else if (input == "756.2")
                {
                    result = 55;
                }
                else if (input == "771.8")
                {
                    result = 56;
                }
                else if (input == "765.9")
                {
                    result = 57;
                }
                else if (input == "743.1")
                {
                    result = 58;
                }
                else if (input == "737.6")
                {
                    result = 59;
                }
                else if (input == "734")
                {
                    result = 60;
                }
                else if (input == "728.7")
                {
                    result = 61;
                }
                else if (input == "708")
                {
                    result = 62;
                }
                else if (input == "703.1")
                {
                    result = 63;
                }
                else if (input == "100")
                {
                    result = 64;
                }
                else if (input == "99.9")
                {
                    result = 65;
                }
                else if (input == "99.5")
                {
                    result = 66;
                }
                else if (input == "99.4")
                {
                    result = 67;
                }
                else if (input == "99.3")
                {
                    result = 68;
                }
                else if (input == "99.2")
                {
                    result = 69;
                }
                else if (input == "98.8")
                {
                    result = 70;
                }
                else if (input == "98.7")
                {
                    result = 71;
                }
                else if (input == "99")
                {
                    result = 72;
                }
                else if (input == "98.9")
                {
                    result = 73;
                }
                else if (input == "98.5")
                {
                    result = 74;
                }
                else if (input == "98.4")
                {
                    result = 75;
                }
                else if (input == "98.4")
                {
                    result = 76;
                }
                else if (input == "98.3")
                {
                    result = 77;
                }
                else if (input == "97.9")
                {
                    result = 78;
                }
                else if (input == "97.8")
                {
                    result = 79;
                }
                else if (input == "98.1")
                {
                    result = 80;
                }
                else if (input == "98")
                {
                    result = 81;
                }
                else if (input == "97.6")
                {
                    result = 82;
                }
                else if (input == "97.5")
                {
                    result = 83;
                }
                else if (input == "97.4")
                {
                    result = 84;
                }
                else if (input == "97.3")
                {
                    result = 85;
                }
                else if (input == "97")
                {
                    result = 86;
                }
                else if (input == "96.9")
                {
                    result = 87;
                }
                else if (input == "97.1")
                {
                    result = 88;
                }
                else if (input == "97")
                {
                    result = 89;
                }
                else if (input == "96.7")
                {
                    result = 90;
                }
                else if (input == "96.6")
                {
                    result = 91;
                }
                else if (input == "96.5")
                {
                    result = 92;
                }
                else if (input == "96.4")
                {
                    result = 93;
                }
                else if (input == "96")
                {
                    result = 94;
                }
                else if (input == "95.9")
                {
                    result = 95;
                }
                else if (input == "90.9")
                {
                    result = 96;
                }
                else if (input == "90.8")
                {
                    result = 97;
                }
                else if (input == "90.5")
                {
                    result = 98;
                }
                else if (input == "90.4")
                {
                    result = 99;
                }
                else if (input == "90.4")
                {
                    result = 100;
                }
                else if (input == "90.3")
                {
                    result = 101;
                }
                else if (input == "90")
                {
                    result = 102;
                }
                else if (input == "89.9")
                {
                    result = 103;
                }
                else if (input == "90.1")
                {
                    result = 104;
                }
                else if (input == "90")
                {
                    result = 105;
                }
                else if (input == "89.7")
                {
                    result = 106;
                }
                else if (input == "89.6")
                {
                    result = 107;
                }
                else if (input == "89.6")
                {
                    result = 108;
                }
                else if (input == "89.5")
                {
                    result = 109;
                }
                else if (input == "89.2")
                {
                    result = 110;
                }
                else if (input == "89.1")
                {
                    result = 111;
                }
                else if (input == "89.3")
                {
                    result = 112;
                }
                else if (input == "89.2")
                {
                    result = 113;
                }
                else if (input == "88.9")
                {
                    result = 114;
                }
                else if (input == "88.8")
                {
                    result = 115;
                }
                else if (input == "88.8")
                {
                    result = 116;
                }
                else if (input == "88.7")
                {
                    result = 117;
                }
                else if (input == "88.4")
                {
                    result = 118;
                }
                else if (input == "88.3")
                {
                    result = 119;
                }
                else if (input == "88.5")
                {
                    result = 120;
                }
                else if (input == "88.5")
                {
                    result = 121;
                }
                else if (input == "88.1")
                {
                    result = 122;
                }
                else if (input == "88.1")
                {
                    result = 123;
                }
                else if (input == "88")
                {
                    result = 124;
                }
                else if (input == "87.9")
                {
                    result = 125;
                }
                else if (input == "87.6")
                {
                    result = 126;
                }
                else if (input == "87.5")
                {
                    result = 127;
                }
                else
                {
                    result = 0;
                }
            }
            else if (input == "Open")
            {
                result = 0;
            }
            else if (input == "2000")
            {
                result = 1;
            }
            else if (input == "1000")
            {
                result = 2;
            }
            else if (input == "3000")
            {
                result = 3;
            }
            else if (input == "680")
            {
                result = 4;
            }
            else if (input == "2680")
            {
                result = 5;
            }
            else if (input == "1680")
            {
                result = 6;
            }
            else if (input == "3680")
            {
                result = 7;
            }
            else if (input == "470")
            {
                result = 8;
            }
            else if (input == "2470")
            {
                result = 9;
            }
            else if (input == "1470")
            {
                result = 10;
            }
            else if (input == "3470")
            {
                result = 11;
            }
            else if (input == "1150")
            {
                result = 12;
            }
            else if (input == "3150")
            {
                result = 13;
            }
            else if (input == "2150")
            {
                result = 14;
            }
            else if (input == "4150")
            {
                result = 15;
            }
            else if (input == "330")
            {
                result = 16;
            }
            else if (input == "2330")
            {
                result = 17;
            }
            else if (input == "1330")
            {
                result = 18;
            }
            else if (input == "3330")
            {
                result = 19;
            }
            else if (input == "1010")
            {
                result = 20;
            }
            else if (input == "3010")
            {
                result = 21;
            }
            else if (input == "2010")
            {
                result = 22;
            }
            else if (input == "4010")
            {
                result = 23;
            }
            else if (input == "800")
            {
                result = 24;
            }
            else if (input == "2800")
            {
                result = 25;
            }
            else if (input == "1800")
            {
                result = 26;
            }
            else if (input == "3800")
            {
                result = 27;
            }
            else if (input == "1480")
            {
                result = 28;
            }
            else if (input == "3480")
            {
                result = 29;
            }
            else if (input == "2480")
            {
                result = 30;
            }
            else if (input == "4480")
            {
                result = 31;
            }
            else if (input == "220")
            {
                result = 32;
            }
            else if (input == "2220")
            {
                result = 33;
            }
            else if (input == "1220")
            {
                result = 34;
            }
            else if (input == "3220")
            {
                result = 35;
            }
            else if (input == "900")
            {
                result = 36;
            }
            else if (input == "2900")
            {
                result = 37;
            }
            else if (input == "1900")
            {
                result = 38;
            }
            else if (input == "3900")
            {
                result = 39;
            }
            else if (input == "690")
            {
                result = 40;
            }
            else if (input == "2690")
            {
                result = 41;
            }
            else if (input == "1690")
            {
                result = 42;
            }
            else if (input == "3690")
            {
                result = 43;
            }
            else if (input == "1370")
            {
                result = 44;
            }
            else if (input == "3370")
            {
                result = 45;
            }
            else if (input == "2370")
            {
                result = 46;
            }
            else if (input == "4370")
            {
                result = 47;
            }
            else if (input == "550")
            {
                result = 48;
            }
            else if (input == "2550")
            {
                result = 49;
            }
            else if (input == "1550")
            {
                result = 50;
            }
            else if (input == "3550")
            {
                result = 51;
            }
            else if (input == "1230")
            {
                result = 52;
            }
            else if (input == "3230")
            {
                result = 53;
            }
            else if (input == "2230")
            {
                result = 54;
            }
            else if (input == "4230")
            {
                result = 55;
            }
            else if (input == "1020")
            {
                result = 56;
            }
            else if (input == "3020")
            {
                result = 57;
            }
            else if (input == "2020")
            {
                result = 58;
            }
            else if (input == "4020")
            {
                result = 59;
            }
            else if (input == "1700")
            {
                result = 60;
            }
            else if (input == "3700")
            {
                result = 61;
            }
            else if (input == "2700")
            {
                result = 62;
            }
            else if (input == "4700")
            {
                result = 63;
            }
            else if (input == "100")
            {
                result = 64;
            }
            else if (input == "2100")
            {
                result = 65;
            }
            else if (input == "1100")
            {
                result = 66;
            }
            else if (input == "3100")
            {
                result = 67;
            }
            else if (input == "780")
            {
                result = 68;
            }
            else if (input == "2780")
            {
                result = 69;
            }
            else if (input == "1780")
            {
                result = 70;
            }
            else if (input == "3780")
            {
                result = 71;
            }
            else if (input == "570")
            {
                result = 72;
            }
            else if (input == "2570")
            {
                result = 73;
            }
            else if (input == "1570")
            {
                result = 74;
            }
            else if (input == "3570")
            {
                result = 75;
            }
            else if (input == "1250")
            {
                result = 76;
            }
            else if (input == "3250")
            {
                result = 77;
            }
            else if (input == "2250")
            {
                result = 78;
            }
            else if (input == "4250")
            {
                result = 79;
            }
            else if (input == "430")
            {
                result = 80;
            }
            else if (input == "2430")
            {
                result = 81;
            }
            else if (input == "1430")
            {
                result = 82;
            }
            else if (input == "3430")
            {
                result = 83;
            }
            else if (input == "1110")
            {
                result = 84;
            }
            else if (input == "3110")
            {
                result = 85;
            }
            else if (input == "2110")
            {
                result = 86;
            }
            else if (input == "4110")
            {
                result = 87;
            }
            else if (input == "900")
            {
                result = 88;
            }
            else if (input == "2900")
            {
                result = 89;
            }
            else if (input == "1900")
            {
                result = 90;
            }
            else if (input == "3900")
            {
                result = 91;
            }
            else if (input == "1580")
            {
                result = 92;
            }
            else if (input == "3580")
            {
                result = 93;
            }
            else if (input == "2580")
            {
                result = 94;
            }
            else if (input == "4580")
            {
                result = 95;
            }
            else if (input == "320")
            {
                result = 96;
            }
            else if (input == "2320")
            {
                result = 97;
            }
            else if (input == "1320")
            {
                result = 98;
            }
            else if (input == "3320")
            {
                result = 99;
            }
            else if (input == "1000")
            {
                result = 100;
            }
            else if (input == "3000")
            {
                result = 101;
            }
            else if (input == "2000")
            {
                result = 102;
            }
            else if (input == "4000")
            {
                result = 103;
            }
            else if (input == "790")
            {
                result = 104;
            }
            else if (input == "2790")
            {
                result = 105;
            }
            else if (input == "1790")
            {
                result = 106;
            }
            else if (input == "3790")
            {
                result = 107;
            }
            else if (input == "1470")
            {
                result = 108;
            }
            else if (input == "3470")
            {
                result = 109;
            }
            else if (input == "2470")
            {
                result = 110;
            }
            else if (input == "4470")
            {
                result = 111;
            }
            else if (input == "650")
            {
                result = 112;
            }
            else if (input == "2650")
            {
                result = 113;
            }
            else if (input == "1650")
            {
                result = 114;
            }
            else if (input == "3650")
            {
                result = 115;
            }
            else if (input == "1330")
            {
                result = 116;
            }
            else if (input == "3330")
            {
                result = 117;
            }
            else if (input == "2330")
            {
                result = 118;
            }
            else if (input == "4330")
            {
                result = 119;
            }
            else if (input == "1120")
            {
                result = 120;
            }
            else if (input == "3120")
            {
                result = 121;
            }
            else if (input == "2120")
            {
                result = 122;
            }
            else if (input == "4120")
            {
                result = 123;
            }
            else if (input == "1800")
                result = 124;
            else if (input == "3800")
                result = 125;
            else if (input == "2800")
                result = 126;
            else if (input == "4800")
                result = 127;
            else
                result = 0;
            return result;
        }

        private void highFrequencyCoefficientsToolStripMenuItem_Click(object sender, EventArgs e)
        {
        }

        private void highFrequencyBPFCoeficientsToolStripMenuItem_Click(object sender, EventArgs e)
        {
            primaryTab.SelectTab("hfCoefTab");
            debugTabControl.SelectTab(statusTab);
            MemMap_Leave();
            runBtn.Text = "START";
            thrUpdateCheck.Checked = false;
            tvgInstantUpdateCheck.Checked = false;
            try
            {
                string[,] array = LoadCsv("PGA460_HiFreq_BPF_Coef.csv");
                int num = array.GetUpperBound(0) + 1;
                int num2 = array.GetUpperBound(1) + 1;
                dgvValues.Columns.Clear();
                for (int i = 0; i < num2; i++)
                {
                    dgvValues.Columns.Add(array[0, i], array[0, i]);
                }
                for (int j = 1; j < num; j++)
                {
                    dgvValues.Rows.Add();
                    for (int i = 0; i < num2; i++)
                    {
                        dgvValues.Rows[j - 1].Cells[i].Value = array[j, i];
                    }
                }
                dgvValues.AutoResizeColumns();
                dgvValues.ReadOnly = true;
            }
            catch
            {
            }
        }

        private string[,] LoadCsv(string filename)
        {
            string text = File.ReadAllText(filename);
            text = text.Replace('\n', '\r');
            string[] array = text.Split(new char[]
            {
                '\r'
            }, StringSplitOptions.RemoveEmptyEntries);
            int num = array.Length;
            int num2 = array[0].Split(new char[]
            {
                ','
            }).Length;
            string[,] array2 = new string[num, num2];
            for (int i = 0; i < num; i++)
            {
                string[] array3 = array[i].Split(new char[]
                {
                    ','
                });
                for (int j = 0; j < num2; j++)
                {
                    array2[i, j] = array3[j];
                }
            }
            return array2;
        }

        private void DirEERAll_Click(object sender, EventArgs e)
        {
            datalogTextBox.AppendText("\nDirect EE Read All:\n");
            for (byte b = 0; b < 32; b += 1)
            {
                UART_Read_Write(78, 224, true);
                UART_Read_Write(79, 0, true);
                byte data_H = (byte)(((int)b << 3) + 4);
                UART_Read_Write(79, data_H, true);
                UART_Read_Write(79, 0, true);
                string str = UART_Read_Write(82, 0, false);
                string str2 = UART_Read_Write(83, 0, false);
                UART_Read_Write(78, 32, true);
                datalogTextBox.AppendText(string.Format("{0:X2}", (int)(b * 2)) + ": " + str + " \n");
                datalogTextBox.AppendText(string.Format("{0:X2}", (int)(b * 2 + 1)) + ": " + str2 + " \n");
            }
        }

        private void dirEERSel_Click(object sender, EventArgs e)
        {
            datalogTextBox.AppendText("\nDirect EE Sel Read:\n");
            UART_Read_Write(78, 224, true);
            UART_Read_Write(79, 0, true);
            byte data_H = (byte)((dirEERSelCombo.SelectedIndex << 3) + 4);
            UART_Read_Write(79, data_H, true);
            UART_Read_Write(79, 0, true);
            string str = UART_Read_Write(82, 0, false);
            string str2 = UART_Read_Write(83, 0, false);
            UART_Read_Write(78, 32, true);
            datalogTextBox.AppendText(string.Format("{0:X2}", dirEERSelCombo.SelectedIndex * 2) + ": " + str + " \n");
            datalogTextBox.AppendText(string.Format("{0:X2}", dirEERSelCombo.SelectedIndex * 2 + 1) + ": " + str2 + " \n");
        }

        private void dirEEWSel_Click(object sender, EventArgs e)
        {
            datalogTextBox.AppendText("\nDirect EE Sel Write:\n");
            UART_Read_Write(78, 224, true);
            UART_Read_Write(79, 0, true);
            UART_Read_Write(80, (byte)dirEEWLsb.SelectedIndex, true);
            UART_Read_Write(81, (byte)dirEEWMsb.SelectedIndex, true);
            byte data_H = (byte)((dirEERSelCombo.SelectedIndex << 3) + 1);
            UART_Read_Write(79, data_H, true);
            UART_Read_Write(79, 0, true);
            data_H = (byte)((dirEERSelCombo.SelectedIndex << 3) + 2);
            UART_Read_Write(79, data_H, true);
            UART_Read_Write(79, 0, true);
            UART_Read_Write(78, 32, true);
            datalogTextBox.AppendText(string.Concat(new string[]
            {
                string.Format("{0:X2}", dirEERSelCombo.SelectedIndex),
                ": programmed to 0x",
                string.Format("{0:X2}", dirEEWLsb.SelectedIndex),
                string.Format("{0:X2}", dirEEWMsb.SelectedIndex),
                " \n"
            }));
        }

        private void dirEEWUDx00_Click(object sender, EventArgs e)
        {
            datalogTextBox.AppendText("\nDirect EE-W USER_DATAx to 0x00: \n");
            for (byte b = 0; b < 10; b += 1)
            {
                UART_Read_Write(78, 224, true);
                UART_Read_Write(79, 0, true);
                UART_Read_Write(80, 0, true);
                UART_Read_Write(81, 0, true);
                byte data_H = (byte)(((int)b << 3) + 1);
                UART_Read_Write(79, data_H, true);
                UART_Read_Write(79, 0, true);
                data_H = (byte)(((int)b << 3) + 2);
                UART_Read_Write(79, data_H, true);
                UART_Read_Write(79, 0, true);
                UART_Read_Write(78, 32, true);
                datalogTextBox.AppendText(string.Format("{0:X2}", b) + ": programmed to 0x0000 \n");
            }
        }

        private void dirEEWUDxFF_Click(object sender, EventArgs e)
        {
            datalogTextBox.AppendText("\nDirect EE-W USER_DATAx to 0xFF: \n");
            for (byte b = 0; b < 10; b += 1)
            {
                UART_Read_Write(78, 224, true);
                UART_Read_Write(79, 0, true);
                UART_Read_Write(80, byte.MaxValue, true);
                UART_Read_Write(81, byte.MaxValue, true);
                byte data_H = (byte)((b << 3) + 1);
                UART_Read_Write(79, data_H, true);
                UART_Read_Write(79, 0, true);
                data_H = (byte)((b << 3) + 2);
                UART_Read_Write(79, data_H, true);
                UART_Read_Write(79, 0, true);
                UART_Read_Write(78, 32, true);
                datalogTextBox.AppendText(string.Format("{0:X2}", b) + ": programmed to 0xFFFF \n");
            }
        }

        private void psFreqStart_ValueChanged(object sender, EventArgs e)
        {
            if (freqshiftCheck.Checked && psFreqStart.Value % 1.2m != 0m)
            {
                MessageBox.Show("Invalid frequency value.");
                psFreqStart.Value = 180m;
            }
        }

        private void psFreqEnd_ValueChanged(object sender, EventArgs e)
        {
            if (freqshiftCheck.Checked && psFreqEnd.Value % 1.2m != 0m)
            {
                MessageBox.Show("Invalid frequency value.");
                psFreqEnd.Value = 480m;
            }
        }

        private const ushort MAX_PACKET_SIZE = 64;
        public const uint MyDeviceVID = 8263u;
        public const uint MyDevicePID = 769u;
        public static bool Device_unlocked = false;
        private SerialPort serial_port = new SerialPort();
        private string[] regArray;
        public static bool msg_displayed_once = false;
        private long tempvalue = 0L;
        private bool firstTime = true;
        private bool firstTimeMemMapTIEE = true;
        private bool firstTimeMemMapTITM = true;
        private int firstTimeCheckforPGA = 0;
        private bool firstReadAllPass = false;
        private string SelectedGrid = "sel";
        private string PrevSelectedGrid = "prev";
        private int u2a_status = 0;
        private byte[] uart_return_data = new byte[64];
        private byte[] uart_send_data = new byte[64];
        private byte uartSendLength;
        private byte uartDiagB = 0;
        private bool thrP1PlotAddOnFlag = false;
        private bool thrP2PlotAddOnFlag = false;
        private bool tvgPlotAddOnFlag = false;
        private bool nlsPlotAddOnFlag = false;
        private bool dgPlotAddOnFlag = false;
        private int Monitor_NumberLoops_done = 0;
        private int Monitor_NumberLoops_done_Temp = 0;
        private int multipleLoopCount = 1;
        private List<string> valuesToExport = new List<string>();
        private bool readRegsOffPage = false;
        private bool firstTimeNoise = true;
        private bool noiseOnlyFlag = false;
        private bool tempOnlyFlag = false;
        private bool infErrorFlag = false;
        private bool infLoopSet = false;
        private bool txtToken = false;
        public static bool changeUartAddr = false;
        public static string uartAddrOld = "0";
        public static string uartAddrComboText = "0";
        public static string diagnosticByte = "0";
        public static string stat0Byte = "0";
        public static string stat1Byte = "0";
        public static bool singleRegImmediateUpdateFlag = false;
        private bool readSuccessBool = false;
        private bool DlSysDiagTCIFlag = false;
        private bool readingRegsFlag = false;
        private bool tvgReady = false;
        private int lastCursorPosX = 0;
        private int lastCursorPosY = 0;
        private double sampleToDistance = 0.0;
        private int addrShift = 5;
        private bool tmrChangeFlag = false;
        private bool autoCRCFirst = false;
        private byte tciFailLoop = 0;
        private bool batGood = false;
        private bool defaultingFlag = false;
        private byte extTrigByte = 0;
        private bool lastCorrection = false;
        private int rawMaskShift = 0;

        private byte[] run_buffer = new byte[1];

        private byte uart_cmd0 = 0;
        private byte uart_cmd1 = 1;
        private byte uart_cmd2 = 2;
        private byte uart_cmd3 = 3;
        private byte uart_cmd4 = 4;
        private byte uart_cmd5 = 5;
        private byte uart_cmd6 = 6;
        private byte uart_cmd7 = 7;
        private byte uart_cmd8 = 8;
        private byte uart_cmd9 = 9;
        private byte uart_cmd10 = 10;
        private byte uart_cmd31 = 31;

        private byte syncByte = 85;
        private byte commandByte = 0;
        private byte regAddrByte = 0;
        private byte regDataByte = 0;
        private byte MChecksumByte = 0;
        private ulong p2TimeMain = 0;
        private ulong p2LevelMain = 0;
        private ulong p1TimeMain = 0;
        private ulong p1LevelMain = 0;
        private ulong tvgTimeMain = 0;

        private ulong tvgLevelMain = 0;
        private ulong pXtlXMask = 0;
        private int p1thrl2_merged_pt1 = 0;
        private int p1thrl2_merged_pt2 = 0;
        private bool p1thrl2_merged_flag = false;
        private int p1thrl4_merged_pt1 = 0;
        private int p1thrl4_merged_pt2 = 0;
        private bool p1thrl4_merged_flag = false;
        private int p1thrl5_merged_pt1 = 0;
        private int p1thrl5_merged_pt2 = 0;
        private bool p1thrl5_merged_flag = false;
        private int p1thrl7_merged_pt1 = 0;
        private int p1thrl7_merged_pt2 = 0;
        private bool p1thrl7_merged_flag = false;
        private int p2thrl2_merged_pt1 = 0;
        private int p2thrl2_merged_pt2 = 0;
        private bool p2thrl2_merged_flag = false;
        private int p2thrl4_merged_pt1 = 0;
        private int p2thrl4_merged_pt2 = 0;
        private bool p2thrl4_merged_flag = false;
        private int p2thrl5_merged_pt1 = 0;
        private int p2thrl5_merged_pt2 = 0;
        private bool p2thrl5_merged_flag = false;
        private int p2thrl7_merged_pt1 = 0;
        private int p2thrl7_merged_pt2 = 0;
        private bool p2thrl7_merged_flag = false;
        private int tvgg2_merged_pt1 = 0;
        private int tvgg2_merged_pt2 = 0;
        private bool tvgg2_merged_flag = false;
        private int tvgg3_merged_pt1 = 0;
        private int tvgg3_merged_pt2 = 0;
        private bool tvgg3_merged_flag = false;
        private string globalUserCRC;
        private string globalThrCRC;
        private byte[] tci_return_buf = new byte[54];
        private byte[] tci_return_buf_all = new byte[3240];
        private byte[] tci_return_buf_all_dump = new byte[3200];
        private byte[] tci_return_buf_all0 = new byte[3240];
        private int tciReadCount = 0;
        private byte[] mem_return_buf = new byte[35];
        private byte[] mem_return_buf_all = new byte[64000];
        private double eddDelay = 15.0;
        private int HOLDfreqCombo;
        private bool HOLDfreqshiftCheck;
        private int HOLDdeadCombo;
        private int HOLDAFEGainRngCombo;
        private int HOLDgainCombo;
        private int HOLDbpbwCombo;
        private int HOLDcutoffCombo;
        private int HOLDthrCmpDeglitchCombo;
        private bool HOLDdecoupletimeRadio;
        private int HOLDdecoupletimeBox;
        private int HOLDtempgainCombo;
        private int HOLDtempoffsetCombo;
        private int HOLDp1PulsesCombo;
        private int HOLDp1DriveCombo;
        private int HOLDp1RecordCombo;
        private int HOLDnlsNoiseCombo;
        private int HOLDnlsTOPCombo;
        private int HOLDp1DigGainSr;
        private int HOLDp1DigGainLr;
        private int HOLDp1DigGainLrSt;
        private int HOLDp2DigGainSr;
        private int HOLDp2DigGainLr;
        private int HOLDp2DigGainLrSt;
        private int HOLDtvgg0;
        private int HOLDtvgg1;
        private int HOLDtvgg2;
        private int HOLDtvgg3;
        private int HOLDtvgg4;
        private int HOLDtvgg5;
        private int HOLDtvgt0;
        private int HOLDtvgt1;
        private int HOLDtvgt2;
        private int HOLDtvgt3;
        private int HOLDtvgt4;
        private int HOLDtvgt5;
        private int oldResQ = -1;
        private int oldCapQ = -1;
        private bool tciWriteInProcess = false;
        private ProgressUpdater updater = new ProgressUpdater();
        private int sState_loopControl;
        private bool initThrFlag = false;
        private string guiVersion = "1.0.1.2";
        private string guiDate = "2018-01-18";
        private int evmUSBcount = 0;
        private string evmStatusText = "";
        public string batPathDir = "";
        public string batPathFile = "";
        private bool thrUpdatedAtLeastOnce = false;
        private bool thrReady = false;
        private Rectangle OldRect = Rectangle.Empty;
        private bool forceDropdownFlag = false;
        private Process proc = new Process();
        private string cmdOutput = "";
        private static System.Timers.Timer aTimer;
        private static System.Timers.Timer bTimer;
#if AUTO_CHECK_VERSION
        private static System.Timers.Timer checkVersionTimer;
#endif
        private static bool fwMessageShown = false;
        private bool stillLoadingFW = false;
        private int numberToCompute = 0;
        private int highestPercentageReached = 0;
        private BackgroundWorker backgroundWorker1;
        private string exportSaveAs = "txt";
        private bool checkGUIUpdateOnce = false;
        private long adcChartMax = 1L;
        public Thread splashthread;
        private List<search> searchlist = new List<search>();
        private RegisterValueGridEditor GRID_USER_MEMSPACE;
        private RegisterValueGridEditor GRID_TIEEPROM_MEMSPACE;
        private RegisterValueGridEditor GRID_TITESTMODE_MEMSPACE;
        private RegisterValueGridEditor GRID_USERDATA_MEMSPACE = null;
        private RegisterValueGridEditor GRID_THRESHOLD_MEMSPACE = null;
        private RegisterValueGridEditor GRID_DATADUMP_MEMSPACE;

        #region BitReverseTable
        public static byte[] BitReverseTable = new byte[]
        {
            0,
            128,
            64,
            192,
            32,
            160,
            96,
            224,
            16,
            144,
            80,
            208,
            48,
            176,
            112,
            240,
            8,
            136,
            72,
            200,
            40,
            168,
            104,
            232,
            24,
            152,
            88,
            216,
            56,
            184,
            120,
            248,
            4,
            132,
            68,
            196,
            36,
            164,
            100,
            228,
            20,
            148,
            84,
            212,
            52,
            180,
            116,
            244,
            12,
            140,
            76,
            204,
            44,
            172,
            108,
            236,
            28,
            156,
            92,
            220,
            60,
            188,
            124,
            252,
            2,
            130,
            66,
            194,
            34,
            162,
            98,
            226,
            18,
            146,
            82,
            210,
            50,
            178,
            114,
            242,
            10,
            138,
            74,
            202,
            42,
            170,
            106,
            234,
            26,
            154,
            90,
            218,
            58,
            186,
            122,
            250,
            6,
            134,
            70,
            198,
            38,
            166,
            102,
            230,
            22,
            150,
            86,
            214,
            54,
            182,
            118,
            246,
            14,
            142,
            78,
            206,
            46,
            174,
            110,
            238,
            30,
            158,
            94,
            222,
            62,
            190,
            126,
            254,
            1,
            129,
            65,
            193,
            33,
            161,
            97,
            225,
            17,
            145,
            81,
            209,
            49,
            177,
            113,
            241,
            9,
            137,
            73,
            201,
            41,
            169,
            105,
            233,
            25,
            153,
            89,
            217,
            57,
            185,
            121,
            249,
            5,
            133,
            69,
            197,
            37,
            165,
            101,
            229,
            21,
            149,
            85,
            213,
            53,
            181,
            117,
            245,
            13,
            141,
            77,
            205,
            45,
            173,
            109,
            237,
            29,
            157,
            93,
            221,
            61,
            189,
            125,
            253,
            3,
            131,
            67,
            195,
            35,
            163,
            99,
            227,
            19,
            147,
            83,
            211,
            51,
            179,
            115,
            243,
            11,
            139,
            75,
            203,
            43,
            171,
            107,
            235,
            27,
            155,
            91,
            219,
            59,
            187,
            123,
            251,
            7,
            135,
            71,
            199,
            39,
            167,
            103,
            231,
            23,
            151,
            87,
            215,
            55,
            183,
            119,
            247,
            15,
            143,
            79,
            207,
            47,
            175,
            111,
            239,
            31,
            159,
            95,
            223,
            63,
            191,
            127,
            255
        };
        #endregion
        #region Coef2kTable
        public static int[] Coef2kTable = new int[]
        {
            -11954,
            -11748,
            -11538,
            -11324,
            -11106,
            -10884,
            -10658,
            -10428,
            -10195,
            -9957,
            -9716,
            -9470,
            -9222,
            -8969,
            -8713,
            -8453,
            -8190,
            -7923,
            -7652,
            -7378,
            -7101,
            -6821,
            -6537,
            -6249,
            -5959,
            -5665,
            -5368,
            -5069,
            -4766,
            -4459,
            -4150,
            -3838,
            -3524,
            -3206,
            -2885,
            -2562,
            -2235,
            -1907,
            -1575,
            -1241,
            -904,
            -565,
            -223,
            122,
            468,
            817,
            1169,
            1523,
            1879,
            2237,
            2598,
            2960,
            3325,
            3692,
            4061,
            4432,
            4805,
            5179,
            5556,
            5934,
            6314,
            6696,
            7080,
            7465,
            7852,
            8241,
            8631,
            9022,
            9415,
            9810,
            10205,
            10603,
            11001,
            11401,
            11802,
            12204,
            12607,
            13011,
            13417,
            13823,
            14231,
            14639,
            15048,
            15458,
            15869,
            16281,
            16693,
            17106,
            17520,
            17934,
            18349,
            18765,
            19181,
            19597,
            20014,
            20431,
            20848,
            21266,
            21684,
            22103,
            22521,
            22940,
            23358,
            23777,
            24196,
            24614,
            25033,
            25451,
            25870,
            26288,
            26705,
            27123,
            27540,
            27957,
            28374,
            28790,
            29205,
            29621,
            30035,
            30449,
            30862,
            31275,
            31687,
            32098,
            32508,
            32918,
            33326,
            33734,
            34141,
            34546,
            34951,
            35354,
            35757,
            36158,
            36558,
            36957,
            37354,
            37751,
            38145,
            38539,
            38931,
            39321,
            39710,
            40098,
            40483,
            40867,
            41250,
            41631,
            42009,
            42387,
            42762,
            43135,
            43507,
            43876,
            44243,
            44609,
            44972,
            45333,
            45692,
            46049,
            46403,
            46755,
            47105,
            47453,
            47798,
            48140,
            48480,
            48818,
            49153,
            49485,
            49815,
            50142,
            50466,
            50787,
            51106,
            51422,
            51735,
            52044,
            52351,
            52655,
            52956,
            53254,
            53548,
            53840,
            54128,
            54413,
            54694,
            54972,
            55247,
            55519,
            55786,
            56051,
            56312,
            56569,
            56823,
            57072,
            57319,
            57561,
            57800,
            58035,
            58266,
            58493,
            58716,
            58935,
            59150,
            59361,
            59568,
            59770,
            59969,
            60163,
            60353,
            60539,
            60720,
            60897,
            61070,
            61237,
            61401,
            61560,
            61714,
            61864,
            62009,
            62149,
            62284,
            62415,
            62541,
            62662,
            62778,
            62889,
            62995,
            63096,
            63192,
            63282,
            63368,
            63448,
            63523,
            63593,
            63658,
            63717,
            63771,
            63819,
            63862,
            63899,
            63931,
            63957,
            63977,
            63992,
            64001,
            64004,
            64001,
            63993,
            63978
        };
        #endregion
        #region Coef4kTable
        public static int[] Coef4kTable = new int[]
        {
            -11890,
            -11681,
            -11467,
            -11249,
            -11026,
            -10800,
            -10570,
            -10336,
            -10097,
            -9855,
            -9609,
            -9359,
            -9106,
            -8848,
            -8587,
            -8322,
            -8054,
            -7782,
            -7506,
            -7227,
            -6945,
            -6659,
            -6370,
            -6077,
            -5781,
            -5482,
            -5179,
            -4874,
            -4565,
            -4253,
            -3938,
            -3620,
            -3299,
            -2975,
            -2649,
            -2319,
            -1987,
            -1651,
            -1313,
            -973,
            -629,
            -284,
            65,
            416,
            770,
            1126,
            1484,
            1845,
            2208,
            2573,
            2941,
            3311,
            3683,
            4057,
            4433,
            4811,
            5192,
            5574,
            5958,
            6344,
            6732,
            7122,
            7514,
            7907,
            8302,
            8698,
            9097,
            9496,
            9898,
            10300,
            10704,
            11110,
            11517,
            11925,
            12335,
            12746,
            13158,
            13571,
            13985,
            14401,
            14817,
            15234,
            15653,
            16072,
            16492,
            16913,
            17335,
            17757,
            18181,
            18605,
            19029,
            19454,
            19880,
            20306,
            20733,
            21160,
            21588,
            22016,
            22444,
            22872,
            23301,
            23730,
            24159,
            24588,
            25017,
            25447,
            25876,
            26305,
            26734,
            27163,
            27592,
            28020,
            28449,
            28877,
            29304,
            29732,
            30158,
            30585,
            31011,
            31436,
            31861,
            32285,
            32709,
            33132,
            33554,
            33975,
            34395,
            34815,
            35234,
            35652,
            36068,
            36484,
            36899,
            37312,
            37725,
            38136,
            38546,
            38955,
            39362,
            39769,
            40173,
            40577,
            40979,
            41379,
            41778,
            42175,
            42570,
            42964,
            43357,
            43747,
            44136,
            44522,
            44907,
            45290,
            45672,
            46051,
            46428,
            46803,
            47175,
            47546,
            47915,
            48281,
            48645,
            49007,
            49366,
            49723,
            50077,
            50429,
            50779,
            51126,
            51470,
            51812,
            52151,
            52487,
            52821,
            53151,
            53479,
            53804,
            54126,
            54445,
            54762,
            55075,
            55385,
            55691,
            55995,
            56296,
            56593,
            56887,
            57177,
            57465,
            57748,
            58029,
            58306,
            58579,
            58849,
            59115,
            59378,
            59636,
            59892,
            60143,
            60390,
            60634,
            60874,
            61110,
            61341,
            61569,
            61793,
            62013,
            62228,
            62440,
            62647,
            62850,
            63048,
            63243,
            63433,
            63618,
            63799,
            63976,
            64148,
            64315,
            64478,
            64636,
            64789,
            64938,
            65082,
            65221,
            65355,
            65484,
            65609,
            65728,
            65842,
            65951,
            66056,
            66155,
            66248,
            66337,
            66420,
            66498,
            66571,
            66638,
            66700,
            66756,
            66807,
            66853,
            66892,
            66926,
            66955,
            66977,
            66994,
            67005,
            67011
        };
        #endregion
        #region Coef6kTable
        public static int[] Coef6kTable = new int[]
        {
            -11994,
            -11792,
            -11586,
            -11375,
            -11161,
            -10943,
            -10721,
            -10495,
            -10265,
            -10032,
            -9794,
            -9553,
            -9308,
            -9060,
            -8808,
            -8553,
            -8294,
            -8031,
            -7765,
            -7496,
            -7223,
            -6947,
            -6668,
            -6386,
            -6100,
            -5811,
            -5519,
            -5224,
            -4926,
            -4625,
            -4321,
            -4015,
            -3705,
            -3392,
            -3077,
            -2759,
            -2438,
            -2115,
            -1789,
            -1460,
            -1129,
            -795,
            -459,
            -121,
            220,
            563,
            909,
            1257,
            1607,
            1959,
            2313,
            2670,
            3028,
            3389,
            3751,
            4116,
            4482,
            4850,
            5220,
            5592,
            5965,
            6340,
            6717,
            7096,
            7476,
            7857,
            8240,
            8625,
            9011,
            9398,
            9786,
            10176,
            10567,
            10960,
            11353,
            11748,
            12143,
            12540,
            12938,
            13336,
            13736,
            14137,
            14538,
            14940,
            15343,
            15747,
            16151,
            16556,
            16961,
            17367,
            17774,
            18181,
            18589,
            18997,
            19405,
            19814,
            20222,
            20632,
            21041,
            21450,
            21860,
            22270,
            22679,
            23089,
            23499,
            23908,
            24318,
            24727,
            25136,
            25545,
            25953,
            26361,
            26769,
            27177,
            27584,
            27990,
            28396,
            28801,
            29206,
            29610,
            30014,
            30416,
            30818,
            31219,
            31619,
            32019,
            32417,
            32814,
            33211,
            33606,
            34000,
            34393,
            34785,
            35176,
            35565,
            35954,
            36340,
            36726,
            37110,
            37492,
            37873,
            38253,
            38631,
            39007,
            39381,
            39754,
            40125,
            40495,
            40862,
            41228,
            41592,
            41954,
            42313,
            42671,
            43027,
            43380,
            43732,
            44081,
            44428,
            44773,
            45115,
            45456,
            45793,
            46129,
            46461,
            46792,
            47119,
            47445,
            47767,
            48087,
            48404,
            48718,
            49030,
            49339,
            49645,
            49947,
            50247,
            50544,
            50838,
            51129,
            51417,
            51702,
            51983,
            52261,
            52536,
            52807,
            53076,
            53340,
            53602,
            53859,
            54114,
            54365,
            54612,
            54855,
            55095,
            55331,
            55563,
            55792,
            56016,
            56237,
            56454,
            56667,
            56876,
            57081,
            57281,
            57478,
            57670,
            57859,
            58042,
            58222,
            58397,
            58568,
            58735,
            58897,
            59055,
            59208,
            59356,
            59500,
            59639,
            59774,
            59903,
            60028,
            60149,
            60264,
            60374,
            60480,
            60580,
            60676,
            60766,
            60851,
            60932,
            61007,
            61076,
            61141,
            61200,
            61254,
            61302,
            61345,
            61383,
            61415,
            61441,
            61462,
            61478,
            61487,
            61491,
            61489,
            61482,
            61468,
            61449,
            61424,
            61393
        };
        #endregion
        #region Coef8kTable
        public static int[] Coef8kTable = new int[]
        {
            -12071,
            -11874,
            -11672,
            -11466,
            -11256,
            -11042,
            -10825,
            -10603,
            -10378,
            -10149,
            -9917,
            -9680,
            -9441,
            -9197,
            -8950,
            -8700,
            -8446,
            -8189,
            -7928,
            -7664,
            -7397,
            -7127,
            -6853,
            -6576,
            -6296,
            -6013,
            -5727,
            -5438,
            -5146,
            -4851,
            -4553,
            -4253,
            -3949,
            -3643,
            -3334,
            -3022,
            -2708,
            -2391,
            -2072,
            -1750,
            -1426,
            -1099,
            -770,
            -438,
            -104,
            232,
            570,
            911,
            1254,
            1599,
            1945,
            2294,
            2645,
            2998,
            3353,
            3710,
            4068,
            4428,
            4790,
            5154,
            5520,
            5887,
            6255,
            6625,
            6997,
            7370,
            7745,
            8121,
            8498,
            8876,
            9256,
            9637,
            10019,
            10403,
            10787,
            11173,
            11559,
            11947,
            12335,
            12725,
            13115,
            13506,
            13898,
            14290,
            14684,
            15077,
            15472,
            15867,
            16263,
            16659,
            17055,
            17452,
            17850,
            18247,
            18645,
            19044,
            19442,
            19841,
            20239,
            20638,
            21037,
            21436,
            21835,
            22233,
            22632,
            23031,
            23429,
            23827,
            24225,
            24622,
            25019,
            25416,
            25812,
            26208,
            26604,
            26998,
            27393,
            27786,
            28179,
            28571,
            28962,
            29353,
            29743,
            30131,
            30519,
            30906,
            31292,
            31677,
            32061,
            32444,
            32825,
            33205,
            33584,
            33962,
            34339,
            34714,
            35087,
            35460,
            35830,
            36200,
            36567,
            36933,
            37298,
            37660,
            38021,
            38381,
            38738,
            39094,
            39447,
            39799,
            40149,
            40496,
            40842,
            41186,
            41527,
            41866,
            42203,
            42538,
            42871,
            43201,
            43529,
            43854,
            44177,
            44497,
            44815,
            45130,
            45443,
            45753,
            46060,
            46365,
            46667,
            46966,
            47262,
            47555,
            47845,
            48133,
            48417,
            48698,
            48976,
            49251,
            49523,
            49791,
            50056,
            50318,
            50577,
            50832,
            51084,
            51332,
            51577,
            51818,
            52056,
            52290,
            52520,
            52747,
            52970,
            53189,
            53404,
            53616,
            53823,
            54027,
            54226,
            54422,
            54613,
            54800,
            54983,
            55162,
            55337,
            55508,
            55674,
            55835,
            55993,
            56146,
            56294,
            56438,
            56577,
            56712,
            56842,
            56968,
            57088,
            57204,
            57315,
            57422,
            57523,
            57619,
            57711,
            57798,
            57879,
            57955,
            58027,
            58093,
            58154,
            58209,
            58259,
            58304,
            58344,
            58378,
            58407,
            58430,
            58448,
            58460,
            58467,
            58468,
            58463,
            58452,
            58436,
            58414,
            58386,
            58352,
            58312,
            58267,
            58215
        };
        #endregion

        private USBClass USBPort;
        private List<USBClass.DeviceProperties> ListOfUSBDeviceProperties;

        private bool MyUSBDeviceConnected;

        public class search
        {
            public search(string reg_namex, string reg_addressx, string descriptionx)
            {
                reg_name = reg_namex;
                reg_address = reg_addressx;
                description = descriptionx;
            }

            public string reg_name;
            public string reg_address;
            public string description;
        }

        #region class Register
        public class Register
        {
            public Register()
            {
            }

            public Register(byte loc, string r_a, string w_a, List<Bit_Field> b_f)
            {
                location = loc;
                read_access = r_a;
                write_access = w_a;
                bit_fields = b_f;
            }

            public Register(byte loc, List<Bit_Field> b_f)
            {
                location = loc;
                bit_fields = b_f;
            }

            public Register(byte loc, List<Bit_Field> b_f, bool Diag)
            {
                location = loc;
                bit_fields = b_f;
                DiagMode = Diag;
            }

            public void ReadFromUART()
            {
                string text = Tools.StringBase16_Into_StringBase2(UART_Read_Write(location, 0, false), false);
                foreach (Bit_Field bit_Field in bit_fields)
                    bit_Field.value = text.Substring(bit_Field.start_index, bit_Field.length);
            }

            public void WriteToUART()
            {
                char[] array = new char[8];
                foreach (Bit_Field bit_Field in bit_fields)
                    for (int i = 0; i < bit_Field.length; i++)
                        array[bit_Field.start_index + i] = bit_Field.value[i];

                string text = new string(array);
                byte data_H = Convert.ToByte(text.ToString().Substring(0, 8), 2);
                Tools.StringBase16_Into_StringBase2(UART_Read_Write(location, data_H, true), false);
            }

            public byte location;
            public string read_access;
            public string write_access;
            public List<Bit_Field> bit_fields;
            private bool DiagMode = false;
        }
        #endregion

        #region class Bit_Field
        public class Bit_Field
        {
            public Bit_Field(int start_index_a, int length_a, string value_a)
            {
                start_index = 8 - start_index_a - length_a;
                length = length_a;
                value = value_a;
            }

            public Bit_Field(int start_index_a, int length_a)
            {
                start_index = 8 - start_index_a - length_a;
                length = length_a;
            }

            public int length;

            public int start_index;

            public string value;
        }
        #endregion

        #region class RegDefs
        public class RegDefs
        {
            public RegDefs()
            {
                TVGAIN0 = new Register(20, new List<Bit_Field>
                {
                    TVG_T1,
                    TVG_T0
                }, true);
                TVGAIN1 = new Register(21, new List<Bit_Field>
                {
                    TVG_T3,
                    TVG_T2
                }, true);
                TVGAIN2 = new Register(22, new List<Bit_Field>
                {
                    TVG_T5,
                    TVG_T4
                }, true);
                TVGAIN3 = new Register(23, new List<Bit_Field>
                {
                    TVG_G2a,
                    TVG_G1
                }, true);
                TVGAIN4 = new Register(24, new List<Bit_Field>
                {
                    TVG_G3a,
                    TVG_G2b
                }, true);
                TVGAIN5 = new Register(25, new List<Bit_Field>
                {
                    TVG_G4,
                    TVG_G3b
                }, true);
                TVGAIN6 = new Register(26, new List<Bit_Field>
                {
                    FREQ_SHIFT,
                    TVGAIN6_Reserved0,
                    TVG_G5
                }, true);
                INIT_GAIN = new Register(27, new List<Bit_Field>
                {
                    GAIN_INIT,
                    BPF_BW
                }, true);
                FREQUENCY = new Register(28, new List<Bit_Field>
                {
                    FREQ
                }, true);
                DEADTIME = new Register(29, new List<Bit_Field>
                {
                    PULSE_DT,
                    THR_CMP_DEGLTCH
                }, true);
                PULSE_P1 = new Register(30, new List<Bit_Field>
                {
                    P1_PULSE,
                    IO_DIS,
                    UART_DIAG,
                    IO_IF_SEL
                }, true);
                PULSE_P2 = new Register(31, new List<Bit_Field>
                {
                    P2_PULSE,
                    UART_ADDR
                }, true);
                CURR_LIM_P1 = new Register(32, new List<Bit_Field>
                {
                    CURR_LIM1,
                    IDLE_MD_DIS,
                    DIS_CL
                }, true);
                CURR_LIM_P2 = new Register(33, new List<Bit_Field>
                {
                    CURR_LIM2,
                    LPF_CO
                }, true);
                REC_LENGTH = new Register(34, new List<Bit_Field>
                {
                    P2_REC,
                    P1_REC
                }, true);
                FREQ_DIAG = new Register(35, new List<Bit_Field>
                {
                    FDIAG_START,
                    FDIAG_LEN
                }, true);
                SAT_FDIAG_TH = new Register(36, new List<Bit_Field>
                {
                    P1_NLS_EN,
                    SAT_TH,
                    FDIAG_ERR_TH
                }, true);
                FVOLT_DEC = new Register(37, new List<Bit_Field>
                {
                    FVOLT_ERR_TH,
                    LPM_TMR,
                    VPWR_OV_TH,
                    P2_NLS_EN
                }, true);
                DECPL_TEMP = new Register(38, new List<Bit_Field>
                {
                    DECPL_T,
                    DECPL_TEMP_SEL,
                    LPM_EN,
                    AFE_GAIN_RNG
                }, true);
                DSP_SCALE = new Register(39, new List<Bit_Field>
                {
                    SCALE_N,
                    SCALE_K,
                    NOISE_LVL
                }, true);
                TEMP_TRIM = new Register(40, new List<Bit_Field>
                {
                    TEMP_OFF,
                    TEMP_GAIN
                }, true);
                P1_GAIN_CTRL = new Register(41, new List<Bit_Field>
                {
                    P1_DIG_GAIN_SR,
                    P1_DIG_GAIN_LR,
                    P1_DIG_GAIN_LR_ST
                }, true);
                P2_GAIN_CTRL = new Register(42, new List<Bit_Field>
                {
                    P2_DIG_GAIN_SR,
                    P2_DIG_GAIN_LR,
                    P2_DIG_GAIN_LR_ST
                }, true);
                EE_CRC = new Register(43, new List<Bit_Field>
                {
                    EE_CRC_B
                }, true);
                EE_CNTRL = new Register(64, new List<Bit_Field>
                {
                    EE_PRGM,
                    EE_RLOAD,
                    EE_PRGM_OK,
                    EE_UNLCK,
                    DATADUMP_EN
                }, true);
                TEST_MUX = new Register(75, new List<Bit_Field>
                {
                    DP_MUX,
                    SAMPLE_SEL,
                    TEST_MUX_Reserved0,
                    TEST_MUX_B
                }, true);
                P1_THR_0 = new Register(95, new List<Bit_Field>
                {
                    TH_P1_T2,
                    TH_P1_T1
                }, true);
                P1_THR_1 = new Register(96, new List<Bit_Field>
                {
                    TH_P1_T4,
                    TH_P1_T3
                }, true);
                P1_THR_2 = new Register(97, new List<Bit_Field>
                {
                    TH_P1_T6,
                    TH_P1_T5
                }, true);
                P1_THR_3 = new Register(98, new List<Bit_Field>
                {
                    TH_P1_T8,
                    TH_P1_T7
                }, true);
                P1_THR_4 = new Register(99, new List<Bit_Field>
                {
                    TH_P1_T10,
                    TH_P1_T9
                }, true);
                P1_THR_5 = new Register(100, new List<Bit_Field>
                {
                    TH_P1_T12,
                    TH_P1_T11
                }, true);
                P1_THR_6 = new Register(101, new List<Bit_Field>
                {
                    TH_P1_L2a,
                    TH_P1_L1
                }, true);
                P1_THR_7 = new Register(102, new List<Bit_Field>
                {
                    TH_P1_L4a,
                    TH_P1_L3,
                    TH_P1_L2b
                }, true);
                P1_THR_8 = new Register(103, new List<Bit_Field>
                {
                    TH_P1_L5a,
                    TH_P1_L4b
                }, true);
                P1_THR_9 = new Register(104, new List<Bit_Field>
                {
                    TH_P1_L7a,
                    TH_P1_L6,
                    TH_P1_L5b
                }, true);
                P1_THR_10 = new Register(105, new List<Bit_Field>
                {
                    TH_P1_L8,
                    TH_P1_L7b
                }, true);
                P1_THR_11 = new Register(106, new List<Bit_Field>
                {
                    TH_P1_L9
                }, true);
                P1_THR_12 = new Register(107, new List<Bit_Field>
                {
                    TH_P1_L10
                }, true);
                P1_THR_13 = new Register(108, new List<Bit_Field>
                {
                    TH_P1_L11
                }, true);
                P1_THR_14 = new Register(109, new List<Bit_Field>
                {
                    TH_P1_L12
                }, true);
                P1_THR_15 = new Register(110, new List<Bit_Field>
                {
                    TH_P1_Reserved,
                    TH_P1_OFF
                }, true);
                P2_THR_0 = new Register(111, new List<Bit_Field>
                {
                    TH_P2_T2,
                    TH_P2_T1
                }, true);
                P2_THR_1 = new Register(112, new List<Bit_Field>
                {
                    TH_P2_T4,
                    TH_P2_T3
                }, true);
                P2_THR_2 = new Register(113, new List<Bit_Field>
                {
                    TH_P2_T6,
                    TH_P2_T5
                }, true);
                P2_THR_3 = new Register(114, new List<Bit_Field>
                {
                    TH_P2_T8,
                    TH_P2_T7
                }, true);
                P2_THR_4 = new Register(115, new List<Bit_Field>
                {
                    TH_P2_T10,
                    TH_P2_T9
                }, true);
                P2_THR_5 = new Register(116, new List<Bit_Field>
                {
                    TH_P2_T12,
                    TH_P2_T11
                }, true);
                P2_THR_6 = new Register(117, new List<Bit_Field>
                {
                    TH_P2_L2a,
                    TH_P2_L1
                }, true);
                P2_THR_7 = new Register(118, new List<Bit_Field>
                {
                    TH_P2_L4a,
                    TH_P2_L3,
                    TH_P2_L2b
                }, true);
                P2_THR_8 = new Register(119, new List<Bit_Field>
                {
                    TH_P2_L5a,
                    TH_P2_L4b
                }, true);
                P2_THR_9 = new Register(120, new List<Bit_Field>
                {
                    TH_P2_L7a,
                    TH_P2_L6,
                    TH_P2_L5b
                }, true);
                P2_THR_10 = new Register(121, new List<Bit_Field> { TH_P2_L8, TH_P2_L7b }, true);
                P2_THR_11 = new Register(122, new List<Bit_Field> { TH_P2_L9 }, true);
                P2_THR_12 = new Register(123, new List<Bit_Field>
                {
                    TH_P2_L10
                }, true);
                P2_THR_13 = new Register(124, new List<Bit_Field>
                {
                    TH_P2_L11
                }, true);
                P2_THR_14 = new Register(125, new List<Bit_Field>
                {
                    TH_P2_L12
                }, true);
                P2_THR_15 = new Register(126, new List<Bit_Field>
                {
                    TH_P2_Reserved,
                    TH_P2_OFF
                }, true);
                THR_CRC = new Register(127, new List<Bit_Field>
                {
                    THR_CRC_B
                }, true);
            }

            public RegDefs(string reg_name)
            {
                switch (reg_name)
                {
                    case "14 (TVGAIN0)":
                        TVGAIN0 = new Register(20, new List<Bit_Field> { TVG_T0, TVG_T1 }, true);
                        break;
                    case "15 (TVGAIN1)":
                        TVGAIN1 = new Register(21, new List<Bit_Field> { TVG_T2, TVG_T3 }, true);
                        break;
                    case "16 (TVGAIN2)":
                        TVGAIN2 = new Register(22, new List<Bit_Field> { TVG_T4, TVG_T5 }, true);
                        break;
                    case "17 (TVGAIN3)":
                        TVGAIN3 = new Register(23, new List<Bit_Field> { TVG_G1, TVG_G2a }, true);
                        break;
                    case "18 (TVGAIN4)":
                        TVGAIN4 = new Register(24, new List<Bit_Field> { TVG_G2b, TVG_G3a }, true);
                        break;
                    case "19 (TVGAIN5)":
                        TVGAIN5 = new Register(25, new List<Bit_Field> { TVG_G3b, TVG_G4 }, true);
                        break;
                    case "1A (TVGAIN6)":
                        TVGAIN6 = new Register(26, new List<Bit_Field> { TVG_G5, TVGAIN6_Reserved0, FREQ_SHIFT }, true);
                        break;
                    case "1B (INIT_GAIN)":
                        INIT_GAIN = new Register(27, new List<Bit_Field>
                    {
                        GAIN_INIT,
                        BPF_BW
                    }, true);
                        break;
                    case "1C (FREQUENCY)":
                        FREQUENCY = new Register(28, new List<Bit_Field>
                    {
                        FREQ
                    }, true);
                        break;
                    case "1D (DEADTIME)":
                        DEADTIME = new Register(29, new List<Bit_Field>
                    {
                        PULSE_DT,
                        THR_CMP_DEGLTCH
                    }, true);
                        break;
                    case "1E (PULSE_P1)":
                        PULSE_P1 = new Register(30, new List<Bit_Field>
                    {
                        P1_PULSE,
                        IO_DIS,
                        UART_DIAG,
                        IO_IF_SEL
                    }, true);
                        break;
                    case "1F (PULSE_P2)":
                        PULSE_P2 = new Register(31, new List<Bit_Field>
                    {
                        P2_PULSE,
                        UART_ADDR
                    }, true);
                        break;
                    case "20 (CURR_LIM_P1)":
                        CURR_LIM_P1 = new Register(32, new List<Bit_Field>
                    {
                        CURR_LIM1,
                        IDLE_MD_DIS,
                        DIS_CL
                    }, true);
                        break;
                    case "21 (CURR_LIM_P2)":
                        CURR_LIM_P2 = new Register(33, new List<Bit_Field>
                    {
                        CURR_LIM2,
                        LPF_CO
                    }, true);
                        break;
                    case "22 (REC_LENGTH)":
                        REC_LENGTH = new Register(34, new List<Bit_Field>
                    {
                        P2_REC,
                        P1_REC
                    }, true);
                        break;
                    case "23 (FREQ_DIAG)":
                        FREQ_DIAG = new Register(35, new List<Bit_Field>
                    {
                        FDIAG_START,
                        FDIAG_LEN
                    }, true);
                        break;
                    case "24 (SAT_FDIAG_TH)":
                        SAT_FDIAG_TH = new Register(36, new List<Bit_Field>
                    {
                        P1_NLS_EN,
                        SAT_TH,
                        FDIAG_ERR_TH
                    }, true);
                        break;
                    case "25 (FVOLT_DEC)":
                        FVOLT_DEC = new Register(37, new List<Bit_Field>
                    {
                        FVOLT_ERR_TH,
                        LPM_TMR,
                        VPWR_OV_TH,
                        P2_NLS_EN
                    }, true);
                        break;
                    case "26 (DECPL_TEMP)":
                        DECPL_TEMP = new Register(38, new List<Bit_Field>
                    {
                        DECPL_T,
                        DECPL_TEMP_SEL,
                        LPM_EN,
                        AFE_GAIN_RNG
                    }, true);
                        break;
                    case "27 (DSP_SCALE)":
                        DSP_SCALE = new Register(39, new List<Bit_Field>
                    {
                        SCALE_N,
                        SCALE_K,
                        NOISE_LVL
                    }, true);
                        break;
                    case "28 (TEMP_TRIM)":
                        TEMP_TRIM = new Register(40, new List<Bit_Field>
                    {
                        TEMP_OFF,
                        TEMP_GAIN
                    }, true);
                        break;
                    case "29 (P1_GAIN_CTRL)":
                        P1_GAIN_CTRL = new Register(41, new List<Bit_Field>
                    {
                        P1_DIG_GAIN_SR,
                        P1_DIG_GAIN_LR,
                        P1_DIG_GAIN_LR_ST
                    }, true);
                        break;
                    case "2A (P2_GAIN_CTRL)":
                        P2_GAIN_CTRL = new Register(42, new List<Bit_Field>
                    {
                        P2_DIG_GAIN_SR,
                        P2_DIG_GAIN_LR,
                        P2_DIG_GAIN_LR_ST
                    }, true);
                        break;
                    case "2B (EE_CRC)":
                        EE_CRC = new Register(43, new List<Bit_Field>
                    {
                        EE_CRC_B
                    }, true);
                        break;
                    case "40 (EE_CNTRL)":
                        EE_CNTRL = new Register(64, new List<Bit_Field>
                    {
                        EE_PRGM,
                        EE_RLOAD,
                        EE_PRGM_OK,
                        EE_UNLCK,
                        DATADUMP_EN
                    }, true);
                        break;
                    case "4B (TEST_MUX)":
                        TEST_MUX = new Register(75, new List<Bit_Field>
                    {
                        DP_MUX,
                        SAMPLE_SEL,
                        TEST_MUX_Reserved0,
                        TEST_MUX_B
                    }, true);
                        break;
                    case "5F (P1_THR_0)":
                        P1_THR_0 = new Register(95, new List<Bit_Field>
                    {
                        TH_P1_T2,
                        TH_P1_T1
                    }, true);
                        break;
                    case "60 (P1_THR_1)":
                        P1_THR_1 = new Register(96, new List<Bit_Field>
                    {
                        TH_P1_T4,
                        TH_P1_T3
                    }, true);
                        break;
                    case "61 (P1_THR_2)":
                        P1_THR_2 = new Register(97, new List<Bit_Field>
                    {
                        TH_P1_T6,
                        TH_P1_T5
                    }, true);
                        break;
                    case "62 (P1_THR_3)":
                        P1_THR_3 = new Register(98, new List<Bit_Field>
                    {
                        TH_P1_T8,
                        TH_P1_T7
                    }, true);
                        break;
                    case "63 (P1_THR_4)":
                        P1_THR_4 = new Register(99, new List<Bit_Field>
                    {
                        TH_P1_T10,
                        TH_P1_T9
                    }, true);
                        break;
                    case "64 (P1_THR_5)":
                        P1_THR_5 = new Register(100, new List<Bit_Field>
                    {
                        TH_P1_T12,
                        TH_P1_T11
                    }, true);
                        break;
                    case "65 (P1_THR_6)":
                        P1_THR_6 = new Register(101, new List<Bit_Field>
                    {
                        TH_P1_L2a,
                        TH_P1_L1
                    }, true);
                        break;
                    case "66 (P1_THR_7)":
                        P1_THR_7 = new Register(102, new List<Bit_Field>
                    {
                        TH_P1_L4a,
                        TH_P1_L3,
                        TH_P1_L2b
                    }, true);
                        break;
                    case "67 (P1_THR_8)":
                        P1_THR_8 = new Register(103, new List<Bit_Field>
                    {
                        TH_P1_L5a,
                        TH_P1_L4b
                    }, true);
                        break;
                    case "68 (P1_THR_9)":
                        P1_THR_9 = new Register(104, new List<Bit_Field>
                    {
                        TH_P1_L7a,
                        TH_P1_L6,
                        TH_P1_L5b
                    }, true);
                        break;
                    case "69 (P1_THR_10)":
                        P1_THR_10 = new Register(105, new List<Bit_Field>
                    {
                        TH_P1_L8,
                        TH_P1_L7b
                    }, true);
                        break;
                    case "6A (P1_THR_11)":
                        P1_THR_11 = new Register(106, new List<Bit_Field>
                    {
                        TH_P1_L9
                    }, true);
                        break;
                    case "6B (P1_THR_12)":
                        P1_THR_12 = new Register(107, new List<Bit_Field>
                    {
                        TH_P1_L10
                    }, true);
                        break;
                    case "6C (P1_THR_13)":
                        P1_THR_13 = new Register(108, new List<Bit_Field>
                    {
                        TH_P1_L11
                    }, true);
                        break;
                    case "6D (P1_THR_14)":
                        P1_THR_14 = new Register(109, new List<Bit_Field>
                    {
                        TH_P1_L12
                    }, true);
                        break;
                    case "6E (P1_THR_15)":
                        P1_THR_15 = new Register(110, new List<Bit_Field>
                    {
                        TH_P1_Reserved,
                        TH_P1_OFF
                    }, true);
                        break;
                    case "6F (P2_THR_0)":
                        P2_THR_0 = new Register(111, new List<Bit_Field>
                    {
                        TH_P2_T2,
                        TH_P2_T1
                    }, true);
                        break;
                    case "70 (P2_THR_1)":
                        P2_THR_1 = new Register(112, new List<Bit_Field>
                    {
                        TH_P2_T4,
                        TH_P2_T3
                    }, true);
                        break;
                    case "71 (P2_THR_2)":
                        P2_THR_2 = new Register(113, new List<Bit_Field>
                    {
                        TH_P2_T6,
                        TH_P2_T5
                    }, true);
                        break;
                    case "72 (P2_THR_3)":
                        P2_THR_3 = new Register(114, new List<Bit_Field>
                    {
                        TH_P2_T8,
                        TH_P2_T7
                    }, true);
                        break;
                    case "73 (P2_THR_4)":
                        P2_THR_4 = new Register(115, new List<Bit_Field>
                    {
                        TH_P2_T10,
                        TH_P2_T9
                    }, true);
                        break;
                    case "74 (P2_THR_5)":
                        P2_THR_5 = new Register(116, new List<Bit_Field>
                    {
                        TH_P2_T12,
                        TH_P2_T11
                    }, true);
                        break;
                    case "75 (P2_THR_6)":
                        P2_THR_6 = new Register(117, new List<Bit_Field>
                    {
                        TH_P2_L2a,
                        TH_P2_L1
                    }, true);
                        break;
                    case "76 (P2_THR_7)":
                        P2_THR_7 = new Register(118, new List<Bit_Field>
                    {
                        TH_P2_L4a,
                        TH_P2_L3,
                        TH_P2_L2b
                    }, true);
                        break;
                    case "77 (P2_THR_8)":
                        P2_THR_8 = new Register(119, new List<Bit_Field>
                    {
                        TH_P2_L5a,
                        TH_P2_L4b
                    }, true);
                        break;
                    case "78 (P2_THR_9)":
                        P2_THR_9 = new Register(120, new List<Bit_Field>
                    {
                        TH_P2_L7a,
                        TH_P2_L6,
                        TH_P2_L5b
                    }, true);
                        break;
                    case "79 (P2_THR_10)":
                        P2_THR_10 = new Register(121, new List<Bit_Field>
                    {
                        TH_P2_L8,
                        TH_P2_L7b
                    }, true);
                        break;
                    case "7A (P2_THR_11)":
                        P2_THR_11 = new Register(122, new List<Bit_Field>
                    {
                        TH_P2_L9
                    }, true);
                        break;
                    case "7B (P2_THR_12)":
                        P2_THR_12 = new Register(123, new List<Bit_Field>
                    {
                        TH_P2_L10
                    }, true);
                        break;
                    case "7C (P2_THR_13)":
                        P2_THR_13 = new Register(124, new List<Bit_Field>
                    {
                        TH_P2_L11
                    }, true);
                        break;
                    case "7D (P2_THR_14)":
                        P2_THR_14 = new Register(125, new List<Bit_Field>
                    {
                        TH_P2_L12
                    }, true);
                        break;
                    case "7E (P2_THR_15)":
                        P2_THR_15 = new Register(126, new List<Bit_Field>
                    {
                        TH_P2_Reserved,
                        TH_P2_OFF
                    }, true);
                        break;
                    case "7F (THR_CRC)":
                        THR_CRC = new Register(127, new List<Bit_Field>
                    {
                        THR_CRC_B
                    }, true);
                        break;
                }
            }

            public Register TVGAIN0;
            public Register TVGAIN1;
            public Register TVGAIN2;
            public Register TVGAIN3;
            public Register TVGAIN4;
            public Register TVGAIN5;
            public Register TVGAIN6;
            public Register INIT_GAIN;

            public Register FREQUENCY;

            public Register DEADTIME;

            public Register PULSE_P1;

            public Register PULSE_P2;

            public Register CURR_LIM_P1;

            public Register CURR_LIM_P2;

            public Register REC_LENGTH;

            public Register FREQ_DIAG;

            public Register SAT_FDIAG_TH;

            public Register FVOLT_DEC;

            public Register DECPL_TEMP;

            public Register DSP_SCALE;

            public Register TEMP_TRIM;

            public Register P1_GAIN_CTRL;

            public Register P2_GAIN_CTRL;

            public Register EE_CRC;

            public Register EE_CNTRL;

            public Register TEST_MUX;

            public Register P1_THR_0;
            public Register P1_THR_1;
            public Register P1_THR_2;

            public Register P1_THR_3;

            public Register P1_THR_4;

            public Register P1_THR_5;

            public Register P1_THR_6;

            public Register P1_THR_7;

            public Register P1_THR_8;

            public Register P1_THR_9;

            public Register P1_THR_10;

            public Register P1_THR_11;

            public Register P1_THR_12;

            public Register P1_THR_13;

            public Register P1_THR_14;

            public Register P1_THR_15;

            public Register P2_THR_0;

            public Register P2_THR_1;

            public Register P2_THR_2;

            public Register P2_THR_3;

            public Register P2_THR_4;

            public Register P2_THR_5;

            public Register P2_THR_6;

            public Register P2_THR_7;

            public Register P2_THR_8;

            public Register P2_THR_9;

            public Register P2_THR_10;

            public Register P2_THR_11;

            public Register P2_THR_12;

            public Register P2_THR_13;

            public Register P2_THR_14;

            public Register P2_THR_15;

            public Register THR_CRC;

            public Bit_Field TVG_T1 = new Bit_Field(0, 4);

            public Bit_Field TVG_T0 = new Bit_Field(4, 4);

            public Bit_Field TVG_T3 = new Bit_Field(0, 4);

            public Bit_Field TVG_T2 = new Bit_Field(4, 4);

            public Bit_Field TVG_T5 = new Bit_Field(0, 4);

            public Bit_Field TVG_T4 = new Bit_Field(4, 4);

            public Bit_Field TVG_G2a = new Bit_Field(0, 2);

            public Bit_Field TVG_G1 = new Bit_Field(2, 6);

            public Bit_Field TVG_G3a = new Bit_Field(0, 4);

            public Bit_Field TVG_G2b = new Bit_Field(4, 4);

            public Bit_Field TVG_G4 = new Bit_Field(0, 6);

            public Bit_Field TVG_G3b = new Bit_Field(6, 2);

            public Bit_Field FREQ_SHIFT = new Bit_Field(0, 1);

            public Bit_Field TVGAIN6_Reserved0 = new Bit_Field(1, 1);

            public Bit_Field TVG_G5 = new Bit_Field(2, 6);

            public Bit_Field GAIN_INIT = new Bit_Field(0, 6);

            public Bit_Field BPF_BW = new Bit_Field(6, 2);

            public Bit_Field FREQ = new Bit_Field(0, 8);

            public Bit_Field PULSE_DT = new Bit_Field(0, 4);

            public Bit_Field THR_CMP_DEGLTCH = new Bit_Field(4, 4);

            public Bit_Field P1_PULSE = new Bit_Field(0, 5);

            public Bit_Field IO_DIS = new Bit_Field(5, 1);

            public Bit_Field UART_DIAG = new Bit_Field(6, 1);

            public Bit_Field IO_IF_SEL = new Bit_Field(7, 1);

            public Bit_Field P2_PULSE = new Bit_Field(0, 5);

            public Bit_Field UART_ADDR = new Bit_Field(5, 3);

            public Bit_Field CURR_LIM1 = new Bit_Field(0, 6);

            public Bit_Field IDLE_MD_DIS = new Bit_Field(6, 1);

            public Bit_Field DIS_CL = new Bit_Field(7, 1);

            public Bit_Field CURR_LIM2 = new Bit_Field(0, 6);

            public Bit_Field LPF_CO = new Bit_Field(6, 2);

            public Bit_Field P2_REC = new Bit_Field(0, 4);

            public Bit_Field P1_REC = new Bit_Field(4, 4);

            public Bit_Field FDIAG_START = new Bit_Field(0, 4);

            public Bit_Field FDIAG_LEN = new Bit_Field(4, 4);

            public Bit_Field P1_NLS_EN = new Bit_Field(0, 1);

            public Bit_Field SAT_TH = new Bit_Field(1, 4);

            public Bit_Field FDIAG_ERR_TH = new Bit_Field(5, 3);

            public Bit_Field FVOLT_ERR_TH = new Bit_Field(0, 3);

            public Bit_Field LPM_TMR = new Bit_Field(3, 2);

            public Bit_Field VPWR_OV_TH = new Bit_Field(5, 2);

            public Bit_Field P2_NLS_EN = new Bit_Field(7, 1);

            public Bit_Field DECPL_T = new Bit_Field(0, 4);

            public Bit_Field DECPL_TEMP_SEL = new Bit_Field(4, 1);

            public Bit_Field LPM_EN = new Bit_Field(5, 1);

            public Bit_Field AFE_GAIN_RNG = new Bit_Field(6, 2);

            public Bit_Field SCALE_N = new Bit_Field(0, 2);

            public Bit_Field SCALE_K = new Bit_Field(2, 1);

            public Bit_Field NOISE_LVL = new Bit_Field(3, 5);

            public Bit_Field TEMP_OFF = new Bit_Field(0, 4);

            public Bit_Field TEMP_GAIN = new Bit_Field(4, 4);

            public Bit_Field P1_DIG_GAIN_SR = new Bit_Field(0, 3);

            public Bit_Field P1_DIG_GAIN_LR = new Bit_Field(3, 3);

            public Bit_Field P1_DIG_GAIN_LR_ST = new Bit_Field(6, 2);

            public Bit_Field P2_DIG_GAIN_SR = new Bit_Field(0, 3);
            public Bit_Field P2_DIG_GAIN_LR = new Bit_Field(3, 3);
            public Bit_Field P2_DIG_GAIN_LR_ST = new Bit_Field(6, 2);
            public Bit_Field EE_CRC_B = new Bit_Field(0, 8);
            public Bit_Field EE_PRGM = new Bit_Field(0, 1);
            public Bit_Field EE_RLOAD = new Bit_Field(1, 1);
            public Bit_Field EE_PRGM_OK = new Bit_Field(2, 1);
            public Bit_Field EE_UNLCK = new Bit_Field(3, 4);
            public Bit_Field DATADUMP_EN = new Bit_Field(7, 1);
            public Bit_Field DP_MUX = new Bit_Field(0, 3);
            public Bit_Field SAMPLE_SEL = new Bit_Field(3, 1);
            public Bit_Field TEST_MUX_Reserved0 = new Bit_Field(4, 1);
            public Bit_Field TEST_MUX_B = new Bit_Field(5, 3);
            public Bit_Field TH_P1_T2 = new Bit_Field(0, 4);
            public Bit_Field TH_P1_T1 = new Bit_Field(4, 4);
            public Bit_Field TH_P1_T4 = new Bit_Field(0, 4);
            public Bit_Field TH_P1_T3 = new Bit_Field(4, 4);
            public Bit_Field TH_P1_T6 = new Bit_Field(0, 4);
            public Bit_Field TH_P1_T5 = new Bit_Field(4, 4);
            public Bit_Field TH_P1_T8 = new Bit_Field(0, 4);
            public Bit_Field TH_P1_T7 = new Bit_Field(4, 4);
            public Bit_Field TH_P1_T10 = new Bit_Field(0, 4);
            public Bit_Field TH_P1_T9 = new Bit_Field(4, 4);
            public Bit_Field TH_P1_T12 = new Bit_Field(0, 4);
            public Bit_Field TH_P1_T11 = new Bit_Field(4, 4);
            public Bit_Field TH_P1_L2a = new Bit_Field(0, 3);
            public Bit_Field TH_P1_L1 = new Bit_Field(3, 5);

            public Bit_Field TH_P1_L4a = new Bit_Field(0, 1);

            public Bit_Field TH_P1_L3 = new Bit_Field(1, 5);

            public Bit_Field TH_P1_L2b = new Bit_Field(6, 2);

            public Bit_Field TH_P1_L5a = new Bit_Field(0, 4);

            public Bit_Field TH_P1_L4b = new Bit_Field(4, 4);

            public Bit_Field TH_P1_L7a = new Bit_Field(0, 2);

            public Bit_Field TH_P1_L6 = new Bit_Field(2, 5);

            public Bit_Field TH_P1_L5b = new Bit_Field(7, 1);

            public Bit_Field TH_P1_L8 = new Bit_Field(0, 5);

            public Bit_Field TH_P1_L7b = new Bit_Field(5, 3);

            public Bit_Field TH_P1_L9 = new Bit_Field(0, 8);

            public Bit_Field TH_P1_L10 = new Bit_Field(0, 8);

            public Bit_Field TH_P1_L11 = new Bit_Field(0, 8);

            public Bit_Field TH_P1_L12 = new Bit_Field(0, 8);

            public Bit_Field TH_P1_OFF = new Bit_Field(0, 4);

            public Bit_Field TH_P1_Reserved = new Bit_Field(0, 4);

            public Bit_Field TH_P2_T2 = new Bit_Field(0, 4);

            public Bit_Field TH_P2_T1 = new Bit_Field(4, 4);

            public Bit_Field TH_P2_T4 = new Bit_Field(0, 4);

            public Bit_Field TH_P2_T3 = new Bit_Field(4, 4);

            public Bit_Field TH_P2_T6 = new Bit_Field(0, 4);

            public Bit_Field TH_P2_T5 = new Bit_Field(4, 4);

            public Bit_Field TH_P2_T8 = new Bit_Field(0, 4);

            public Bit_Field TH_P2_T7 = new Bit_Field(4, 4);

            public Bit_Field TH_P2_T10 = new Bit_Field(0, 4);

            public Bit_Field TH_P2_T9 = new Bit_Field(4, 4);

            public Bit_Field TH_P2_T12 = new Bit_Field(0, 4);

            public Bit_Field TH_P2_T11 = new Bit_Field(4, 4);

            public Bit_Field TH_P2_L2a = new Bit_Field(0, 3);

            public Bit_Field TH_P2_L1 = new Bit_Field(3, 5);

            public Bit_Field TH_P2_L4a = new Bit_Field(0, 1);

            public Bit_Field TH_P2_L3 = new Bit_Field(1, 5);
            public Bit_Field TH_P2_L2b = new Bit_Field(6, 2);
            public Bit_Field TH_P2_L5a = new Bit_Field(0, 4);
            public Bit_Field TH_P2_L4b = new Bit_Field(4, 4);
            public Bit_Field TH_P2_L7a = new Bit_Field(0, 2);
            public Bit_Field TH_P2_L6 = new Bit_Field(2, 5);
            public Bit_Field TH_P2_L5b = new Bit_Field(7, 1);
            public Bit_Field TH_P2_L8 = new Bit_Field(0, 5);
            public Bit_Field TH_P2_L7b = new Bit_Field(5, 3);
            public Bit_Field TH_P2_L9 = new Bit_Field(0, 8);
            public Bit_Field TH_P2_L10 = new Bit_Field(0, 8);
            public Bit_Field TH_P2_L11 = new Bit_Field(0, 8);
            public Bit_Field TH_P2_L12 = new Bit_Field(0, 8);
            public Bit_Field TH_P2_OFF = new Bit_Field(0, 4);
            public Bit_Field TH_P2_Reserved = new Bit_Field(0, 4);
            public Bit_Field THR_CRC_B = new Bit_Field(0, 8);
        }
        #endregion

        public enum CRC8_POLY
        {
            CRC8 = 213,
            CRC8_CCITT = 7,
            CRC8_DALLAS_MAXIM = 49,
            CRC8_SAE_J1850 = 29,
            CRC_8_WCDMA = 155
        }

        #region class CRC8Calc
        public class CRC8Calc
        {
            public byte Checksum(params byte[] val)
            {
                if (val == null)
                    throw new ArgumentNullException("val");

                byte b = 0;
                foreach (byte b2 in val)
                    b = table[(int)(b ^ b2)];
                return b;
            }

            public byte[] Table
            {
                get
                {
                    return table;
                }
                set
                {
                    table = value;
                }
            }

            public byte[] GenerateTable(CRC8_POLY polynomial)
            {
                byte[] array = new byte[256];
                for (int i = 0; i < 256; i++)
                {
                    int num = i;
                    for (int j = 0; j < 8; j++)
                    {
                        if ((num & 128) != 0)
                            num = (num << 1 ^ (int)polynomial);
                        else
                            num <<= 1;
                    }
                    array[i] = (byte)num;
                }
                return array;
            }

            public CRC8Calc(CRC8_POLY polynomial)
            {
                table = GenerateTable(polynomial);
            }

            public byte[] table = new byte[256];
        }
        #endregion
    }
}
