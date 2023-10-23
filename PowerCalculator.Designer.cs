namespace TI.eLAB.EVM
{
	public partial class PowerCalculator : global::System.Windows.Forms.Form
	{
		protected override void Dispose(bool disposing)
		{
			if (disposing && this.components != null)
			{
				this.components.Dispose();
			}
			base.Dispose(disposing);
		}

		private void InitializeComponent()
		{
            System.ComponentModel.ComponentResourceManager resources = new System.ComponentModel.ComponentResourceManager(typeof(PowerCalculator));
            this.pb_freq = new System.Windows.Forms.ComboBox();
            this.pb_rec = new System.Windows.Forms.ComboBox();
            this.pb_pulses = new System.Windows.Forms.ComboBox();
            this.pb_limit = new System.Windows.Forms.ComboBox();
            this.label62 = new System.Windows.Forms.Label();
            this.label61 = new System.Windows.Forms.Label();
            this.label16 = new System.Windows.Forms.Label();
            this.label15 = new System.Windows.Forms.Label();
            this.groupBox1 = new System.Windows.Forms.GroupBox();
            this.pb_dist = new System.Windows.Forms.TextBox();
            this.pb_disc = new System.Windows.Forms.CheckBox();
            this.label8 = new System.Windows.Forms.Label();
            this.label7 = new System.Windows.Forms.Label();
            this.pb_vpwr = new System.Windows.Forms.ComboBox();
            this.cmdIntTextbox = new System.Windows.Forms.TextBox();
            this.label1 = new System.Windows.Forms.Label();
            this.pb_idlet = new System.Windows.Forms.ComboBox();
            this.pb_idlec = new System.Windows.Forms.CheckBox();
            this.label459 = new System.Windows.Forms.Label();
            this.pb_lpmt = new System.Windows.Forms.ComboBox();
            this.pb_lpbc = new System.Windows.Forms.CheckBox();
            this.label285 = new System.Windows.Forms.Label();
            this.groupBox2 = new System.Windows.Forms.GroupBox();
            this.out_inactiveTime = new System.Windows.Forms.TextBox();
            this.out_actlisTime = new System.Windows.Forms.TextBox();
            this.out_actburstTime = new System.Windows.Forms.TextBox();
            this.button1 = new System.Windows.Forms.Button();
            this.avgPower = new System.Windows.Forms.TextBox();
            this.label6 = new System.Windows.Forms.Label();
            this.avgCurrent = new System.Windows.Forms.TextBox();
            this.label5 = new System.Windows.Forms.Label();
            this.out_inactive = new System.Windows.Forms.TextBox();
            this.label4 = new System.Windows.Forms.Label();
            this.out_actlis = new System.Windows.Forms.TextBox();
            this.label3 = new System.Windows.Forms.Label();
            this.out_actburst = new System.Windows.Forms.TextBox();
            this.label2 = new System.Windows.Forms.Label();
            this.groupBox1.SuspendLayout();
            this.groupBox2.SuspendLayout();
            this.SuspendLayout();
            // 
            // pb_freq
            // 
            this.pb_freq.AutoCompleteMode = System.Windows.Forms.AutoCompleteMode.SuggestAppend;
            this.pb_freq.AutoCompleteSource = System.Windows.Forms.AutoCompleteSource.ListItems;
            this.pb_freq.DropDownHeight = 120;
            this.pb_freq.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList;
            this.pb_freq.Font = new System.Drawing.Font("Arial", 9F);
            this.pb_freq.FormattingEnabled = true;
            this.pb_freq.IntegralHeight = false;
            this.pb_freq.Items.AddRange(new object[] {
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
            "80"});
            this.pb_freq.Location = new System.Drawing.Point(135, 19);
            this.pb_freq.Name = "pb_freq";
            this.pb_freq.Size = new System.Drawing.Size(77, 23);
            this.pb_freq.TabIndex = 26;
            // 
            // pb_rec
            // 
            this.pb_rec.AutoCompleteMode = System.Windows.Forms.AutoCompleteMode.SuggestAppend;
            this.pb_rec.AutoCompleteSource = System.Windows.Forms.AutoCompleteSource.ListItems;
            this.pb_rec.BackColor = System.Drawing.SystemColors.Window;
            this.pb_rec.DropDownHeight = 120;
            this.pb_rec.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList;
            this.pb_rec.Font = new System.Drawing.Font("Arial", 9F);
            this.pb_rec.FormattingEnabled = true;
            this.pb_rec.IntegralHeight = false;
            this.pb_rec.ItemHeight = 15;
            this.pb_rec.Items.AddRange(new object[] {
            "4.096",
            "8.192",
            "12.288",
            "16.384",
            "20.48",
            "24.576",
            "28.672",
            "32.768",
            "36.864",
            "40.96",
            "45.056",
            "49.152",
            "53.248",
            "57.344",
            "61.44",
            "65.536"});
            this.pb_rec.Location = new System.Drawing.Point(135, 106);
            this.pb_rec.Name = "pb_rec";
            this.pb_rec.Size = new System.Drawing.Size(77, 23);
            this.pb_rec.TabIndex = 29;
            // 
            // pb_pulses
            // 
            this.pb_pulses.AutoCompleteMode = System.Windows.Forms.AutoCompleteMode.SuggestAppend;
            this.pb_pulses.AutoCompleteSource = System.Windows.Forms.AutoCompleteSource.ListItems;
            this.pb_pulses.DropDownHeight = 120;
            this.pb_pulses.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList;
            this.pb_pulses.Font = new System.Drawing.Font("Arial", 9F);
            this.pb_pulses.FormattingEnabled = true;
            this.pb_pulses.IntegralHeight = false;
            this.pb_pulses.Items.AddRange(new object[] {
            "0",
            "1",
            "2",
            "3",
            "4",
            "5",
            "6",
            "7",
            "8",
            "9",
            "10",
            "11",
            "12",
            "13",
            "14",
            "15",
            "16",
            "17",
            "18",
            "19",
            "20",
            "21",
            "22",
            "23",
            "24",
            "25",
            "26",
            "27",
            "28",
            "29",
            "30",
            "31"});
            this.pb_pulses.Location = new System.Drawing.Point(135, 48);
            this.pb_pulses.Name = "pb_pulses";
            this.pb_pulses.Size = new System.Drawing.Size(77, 23);
            this.pb_pulses.TabIndex = 28;
            // 
            // pb_limit
            // 
            this.pb_limit.AutoCompleteMode = System.Windows.Forms.AutoCompleteMode.SuggestAppend;
            this.pb_limit.AutoCompleteSource = System.Windows.Forms.AutoCompleteSource.ListItems;
            this.pb_limit.DropDownHeight = 120;
            this.pb_limit.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList;
            this.pb_limit.Font = new System.Drawing.Font("Arial", 9F);
            this.pb_limit.FormattingEnabled = true;
            this.pb_limit.IntegralHeight = false;
            this.pb_limit.Items.AddRange(new object[] {
            "50",
            "58",
            "65",
            "72",
            "79",
            "86",
            "93",
            "100",
            "108",
            "115",
            "122",
            "129",
            "136",
            "143",
            "150",
            "158",
            "165",
            "172",
            "179",
            "186",
            "193",
            "200",
            "208",
            "215",
            "222",
            "229",
            "236",
            "243",
            "250",
            "258",
            "265",
            "272",
            "279",
            "286",
            "293",
            "300",
            "308",
            "315",
            "322",
            "329",
            "336",
            "343",
            "350",
            "358",
            "365",
            "372",
            "379",
            "386",
            "393",
            "400",
            "408",
            "415",
            "422",
            "429",
            "436",
            "443",
            "450",
            "458",
            "465",
            "472",
            "479",
            "486",
            "493",
            "500"});
            this.pb_limit.Location = new System.Drawing.Point(135, 77);
            this.pb_limit.Name = "pb_limit";
            this.pb_limit.Size = new System.Drawing.Size(77, 23);
            this.pb_limit.TabIndex = 27;
            // 
            // label62
            // 
            this.label62.AutoSize = true;
            this.label62.Location = new System.Drawing.Point(23, 110);
            this.label62.Name = "label62";
            this.label62.Size = new System.Drawing.Size(106, 13);
            this.label62.TabIndex = 33;
            this.label62.Text = "Record Length [ms] :";
            // 
            // label61
            // 
            this.label61.AutoSize = true;
            this.label61.BackColor = System.Drawing.Color.Transparent;
            this.label61.Location = new System.Drawing.Point(63, 52);
            this.label61.Name = "label61";
            this.label61.Size = new System.Drawing.Size(66, 13);
            this.label61.TabIndex = 32;
            this.label61.Text = "# of Pulses :";
            // 
            // label16
            // 
            this.label16.AutoSize = true;
            this.label16.Location = new System.Drawing.Point(30, 81);
            this.label16.Name = "label16";
            this.label16.Size = new System.Drawing.Size(99, 13);
            this.label16.TabIndex = 31;
            this.label16.Text = "Drive Current [mA] :";
            // 
            // label15
            // 
            this.label15.AutoSize = true;
            this.label15.Location = new System.Drawing.Point(10, 23);
            this.label15.Name = "label15";
            this.label15.Size = new System.Drawing.Size(119, 13);
            this.label15.TabIndex = 30;
            this.label15.Text = "Drive Frequency [kHz] :";
            // 
            // groupBox1
            // 
            this.groupBox1.Controls.Add(this.pb_dist);
            this.groupBox1.Controls.Add(this.pb_disc);
            this.groupBox1.Controls.Add(this.label8);
            this.groupBox1.Controls.Add(this.label7);
            this.groupBox1.Controls.Add(this.pb_vpwr);
            this.groupBox1.Controls.Add(this.cmdIntTextbox);
            this.groupBox1.Controls.Add(this.label1);
            this.groupBox1.Controls.Add(this.pb_idlet);
            this.groupBox1.Controls.Add(this.label15);
            this.groupBox1.Controls.Add(this.pb_idlec);
            this.groupBox1.Controls.Add(this.label459);
            this.groupBox1.Controls.Add(this.pb_lpmt);
            this.groupBox1.Controls.Add(this.pb_lpbc);
            this.groupBox1.Controls.Add(this.label62);
            this.groupBox1.Controls.Add(this.pb_limit);
            this.groupBox1.Controls.Add(this.label61);
            this.groupBox1.Controls.Add(this.label285);
            this.groupBox1.Controls.Add(this.pb_pulses);
            this.groupBox1.Controls.Add(this.label16);
            this.groupBox1.Controls.Add(this.pb_rec);
            this.groupBox1.Controls.Add(this.pb_freq);
            this.groupBox1.Location = new System.Drawing.Point(12, 12);
            this.groupBox1.Name = "groupBox1";
            this.groupBox1.Size = new System.Drawing.Size(479, 198);
            this.groupBox1.TabIndex = 34;
            this.groupBox1.TabStop = false;
            this.groupBox1.Text = "Input";
            // 
            // pb_dist
            // 
            this.pb_dist.Location = new System.Drawing.Point(396, 130);
            this.pb_dist.Name = "pb_dist";
            this.pb_dist.Size = new System.Drawing.Size(77, 20);
            this.pb_dist.TabIndex = 87;
            this.pb_dist.Text = "300";
            this.pb_dist.TextChanged += new System.EventHandler(this.pb_dist_TextChanged);
            // 
            // pb_disc
            // 
            this.pb_disc.AutoSize = true;
            this.pb_disc.CheckAlign = System.Drawing.ContentAlignment.MiddleRight;
            this.pb_disc.Location = new System.Drawing.Point(349, 110);
            this.pb_disc.Name = "pb_disc";
            this.pb_disc.Size = new System.Drawing.Size(124, 17);
            this.pb_disc.TabIndex = 86;
            this.pb_disc.Text = "Cut Power to Device";
            this.pb_disc.UseVisualStyleBackColor = true;
            // 
            // label8
            // 
            this.label8.AutoSize = true;
            this.label8.Location = new System.Drawing.Point(252, 133);
            this.label8.Name = "label8";
            this.label8.Size = new System.Drawing.Size(138, 13);
            this.label8.TabIndex = 84;
            this.label8.Text = "Cut Power Enter Time [ms] :";
            // 
            // label7
            // 
            this.label7.AutoSize = true;
            this.label7.BackColor = System.Drawing.Color.Transparent;
            this.label7.Location = new System.Drawing.Point(67, 165);
            this.label7.Name = "label7";
            this.label7.Size = new System.Drawing.Size(62, 13);
            this.label7.TabIndex = 83;
            this.label7.Text = "VPWR [V] :";
            // 
            // pb_vpwr
            // 
            this.pb_vpwr.AutoCompleteMode = System.Windows.Forms.AutoCompleteMode.SuggestAppend;
            this.pb_vpwr.AutoCompleteSource = System.Windows.Forms.AutoCompleteSource.ListItems;
            this.pb_vpwr.DropDownHeight = 120;
            this.pb_vpwr.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList;
            this.pb_vpwr.Font = new System.Drawing.Font("Arial", 9F);
            this.pb_vpwr.FormattingEnabled = true;
            this.pb_vpwr.IntegralHeight = false;
            this.pb_vpwr.Items.AddRange(new object[] {
            "6",
            "7",
            "8",
            "9",
            "10",
            "11",
            "12",
            "13",
            "14",
            "15",
            "16",
            "17",
            "18",
            "19",
            "20",
            "21",
            "22",
            "23",
            "24",
            "25",
            "26",
            "27",
            "28"});
            this.pb_vpwr.Location = new System.Drawing.Point(135, 161);
            this.pb_vpwr.Name = "pb_vpwr";
            this.pb_vpwr.Size = new System.Drawing.Size(77, 23);
            this.pb_vpwr.TabIndex = 82;
            // 
            // cmdIntTextbox
            // 
            this.cmdIntTextbox.Location = new System.Drawing.Point(135, 135);
            this.cmdIntTextbox.Name = "cmdIntTextbox";
            this.cmdIntTextbox.Size = new System.Drawing.Size(77, 20);
            this.cmdIntTextbox.TabIndex = 81;
            this.cmdIntTextbox.KeyPress += new System.Windows.Forms.KeyPressEventHandler(this.onlyNumbers);
            // 
            // label1
            // 
            this.label1.AutoSize = true;
            this.label1.Location = new System.Drawing.Point(9, 138);
            this.label1.Name = "label1";
            this.label1.Size = new System.Drawing.Size(120, 13);
            this.label1.TabIndex = 80;
            this.label1.Text = "Command Interval [ms] :";
            // 
            // pb_idlet
            // 
            this.pb_idlet.AutoCompleteMode = System.Windows.Forms.AutoCompleteMode.SuggestAppend;
            this.pb_idlet.AutoCompleteSource = System.Windows.Forms.AutoCompleteSource.ListItems;
            this.pb_idlet.DropDownHeight = 120;
            this.pb_idlet.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList;
            this.pb_idlet.FormattingEnabled = true;
            this.pb_idlet.IntegralHeight = false;
            this.pb_idlet.Items.AddRange(new object[] {
            "2.5",
            "7.5",
            "5",
            "10"});
            this.pb_idlet.Location = new System.Drawing.Point(396, 172);
            this.pb_idlet.Name = "pb_idlet";
            this.pb_idlet.Size = new System.Drawing.Size(77, 21);
            this.pb_idlet.TabIndex = 79;
            this.pb_idlet.Visible = false;
            // 
            // pb_idlec
            // 
            this.pb_idlec.AutoSize = true;
            this.pb_idlec.CheckAlign = System.Drawing.ContentAlignment.MiddleRight;
            this.pb_idlec.Location = new System.Drawing.Point(358, 152);
            this.pb_idlec.Name = "pb_idlec";
            this.pb_idlec.Size = new System.Drawing.Size(115, 17);
            this.pb_idlec.TabIndex = 77;
            this.pb_idlec.Text = "Idle Mode Enabled";
            this.pb_idlec.UseVisualStyleBackColor = true;
            this.pb_idlec.Visible = false;
            this.pb_idlec.CheckedChanged += new System.EventHandler(this.pb_idlec_CheckedChanged);
            // 
            // label459
            // 
            this.label459.AutoSize = true;
            this.label459.Location = new System.Drawing.Point(254, 175);
            this.label459.Name = "label459";
            this.label459.Size = new System.Drawing.Size(136, 13);
            this.label459.TabIndex = 78;
            this.label459.Text = "Idle Mode Enter Time [ms] :";
            this.label459.Visible = false;
            // 
            // pb_lpmt
            // 
            this.pb_lpmt.AutoCompleteMode = System.Windows.Forms.AutoCompleteMode.SuggestAppend;
            this.pb_lpmt.AutoCompleteSource = System.Windows.Forms.AutoCompleteSource.ListItems;
            this.pb_lpmt.DropDownHeight = 120;
            this.pb_lpmt.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList;
            this.pb_lpmt.FormattingEnabled = true;
            this.pb_lpmt.IntegralHeight = false;
            this.pb_lpmt.Items.AddRange(new object[] {
            "0.25",
            "0.5",
            "1",
            "4"});
            this.pb_lpmt.Location = new System.Drawing.Point(396, 83);
            this.pb_lpmt.Name = "pb_lpmt";
            this.pb_lpmt.Size = new System.Drawing.Size(77, 21);
            this.pb_lpmt.TabIndex = 75;
            // 
            // pb_lpbc
            // 
            this.pb_lpbc.AutoSize = true;
            this.pb_lpbc.CheckAlign = System.Drawing.ContentAlignment.MiddleRight;
            this.pb_lpbc.Location = new System.Drawing.Point(322, 63);
            this.pb_lpbc.Name = "pb_lpbc";
            this.pb_lpbc.Size = new System.Drawing.Size(151, 17);
            this.pb_lpbc.TabIndex = 76;
            this.pb_lpbc.Text = "Low Power Mode Enabled";
            this.pb_lpbc.UseVisualStyleBackColor = true;
            this.pb_lpbc.CheckedChanged += new System.EventHandler(this.pb_lpbc_CheckedChanged);
            // 
            // label285
            // 
            this.label285.AutoSize = true;
            this.label285.Location = new System.Drawing.Point(226, 86);
            this.label285.Name = "label285";
            this.label285.Size = new System.Drawing.Size(164, 13);
            this.label285.TabIndex = 74;
            this.label285.Text = "Low Power Mode Enter Time [s] :";
            // 
            // groupBox2
            // 
            this.groupBox2.Controls.Add(this.out_inactiveTime);
            this.groupBox2.Controls.Add(this.out_actlisTime);
            this.groupBox2.Controls.Add(this.out_actburstTime);
            this.groupBox2.Controls.Add(this.button1);
            this.groupBox2.Controls.Add(this.avgPower);
            this.groupBox2.Controls.Add(this.label6);
            this.groupBox2.Controls.Add(this.avgCurrent);
            this.groupBox2.Controls.Add(this.label5);
            this.groupBox2.Controls.Add(this.out_inactive);
            this.groupBox2.Controls.Add(this.label4);
            this.groupBox2.Controls.Add(this.out_actlis);
            this.groupBox2.Controls.Add(this.label3);
            this.groupBox2.Controls.Add(this.out_actburst);
            this.groupBox2.Controls.Add(this.label2);
            this.groupBox2.Location = new System.Drawing.Point(12, 216);
            this.groupBox2.Name = "groupBox2";
            this.groupBox2.Size = new System.Drawing.Size(479, 176);
            this.groupBox2.TabIndex = 35;
            this.groupBox2.TabStop = false;
            this.groupBox2.Text = "Output";
            // 
            // out_inactiveTime
            // 
            this.out_inactiveTime.Location = new System.Drawing.Point(354, 69);
            this.out_inactiveTime.Name = "out_inactiveTime";
            this.out_inactiveTime.ReadOnly = true;
            this.out_inactiveTime.Size = new System.Drawing.Size(77, 20);
            this.out_inactiveTime.TabIndex = 95;
            // 
            // out_actlisTime
            // 
            this.out_actlisTime.Location = new System.Drawing.Point(354, 44);
            this.out_actlisTime.Name = "out_actlisTime";
            this.out_actlisTime.ReadOnly = true;
            this.out_actlisTime.Size = new System.Drawing.Size(77, 20);
            this.out_actlisTime.TabIndex = 94;
            // 
            // out_actburstTime
            // 
            this.out_actburstTime.Location = new System.Drawing.Point(354, 19);
            this.out_actburstTime.Name = "out_actburstTime";
            this.out_actburstTime.ReadOnly = true;
            this.out_actburstTime.Size = new System.Drawing.Size(77, 20);
            this.out_actburstTime.TabIndex = 93;
            // 
            // button1
            // 
            this.button1.Location = new System.Drawing.Point(132, 143);
            this.button1.Name = "button1";
            this.button1.Size = new System.Drawing.Size(213, 23);
            this.button1.TabIndex = 92;
            this.button1.Text = "Update Output Results";
            this.button1.UseVisualStyleBackColor = true;
            this.button1.Click += new System.EventHandler(this.button1_Click);
            // 
            // avgPower
            // 
            this.avgPower.Location = new System.Drawing.Point(271, 117);
            this.avgPower.Name = "avgPower";
            this.avgPower.ReadOnly = true;
            this.avgPower.Size = new System.Drawing.Size(77, 20);
            this.avgPower.TabIndex = 91;
            // 
            // label6
            // 
            this.label6.AutoSize = true;
            this.label6.Location = new System.Drawing.Point(98, 120);
            this.label6.Name = "label6";
            this.label6.Size = new System.Drawing.Size(170, 13);
            this.label6.TabIndex = 90;
            this.label6.Text = "Average Power per Interval (mW) :";
            // 
            // avgCurrent
            // 
            this.avgCurrent.Location = new System.Drawing.Point(271, 94);
            this.avgCurrent.Name = "avgCurrent";
            this.avgCurrent.ReadOnly = true;
            this.avgCurrent.Size = new System.Drawing.Size(77, 20);
            this.avgCurrent.TabIndex = 89;
            // 
            // label5
            // 
            this.label5.AutoSize = true;
            this.label5.Location = new System.Drawing.Point(98, 97);
            this.label5.Name = "label5";
            this.label5.Size = new System.Drawing.Size(170, 13);
            this.label5.TabIndex = 88;
            this.label5.Text = "Average Current per Interval (mA) :";
            // 
            // out_inactive
            // 
            this.out_inactive.Location = new System.Drawing.Point(271, 69);
            this.out_inactive.Name = "out_inactive";
            this.out_inactive.ReadOnly = true;
            this.out_inactive.Size = new System.Drawing.Size(77, 20);
            this.out_inactive.TabIndex = 87;
            // 
            // label4
            // 
            this.label4.AutoSize = true;
            this.label4.Location = new System.Drawing.Point(72, 72);
            this.label4.Name = "label4";
            this.label4.Size = new System.Drawing.Size(196, 13);
            this.label4.TabIndex = 86;
            this.label4.Text = "Inactive: Listen or Low Power [mA, ms] :";
            // 
            // out_actlis
            // 
            this.out_actlis.Location = new System.Drawing.Point(271, 44);
            this.out_actlis.Name = "out_actlis";
            this.out_actlis.ReadOnly = true;
            this.out_actlis.Size = new System.Drawing.Size(77, 20);
            this.out_actlis.TabIndex = 85;
            // 
            // label3
            // 
            this.label3.AutoSize = true;
            this.label3.Location = new System.Drawing.Point(97, 47);
            this.label3.Name = "label3";
            this.label3.Size = new System.Drawing.Size(171, 13);
            this.label3.TabIndex = 84;
            this.label3.Text = "Active: Listening Current [mA, ms] :";
            // 
            // out_actburst
            // 
            this.out_actburst.Location = new System.Drawing.Point(271, 19);
            this.out_actburst.Name = "out_actburst";
            this.out_actburst.ReadOnly = true;
            this.out_actburst.Size = new System.Drawing.Size(77, 20);
            this.out_actburst.TabIndex = 83;
            // 
            // label2
            // 
            this.label2.AutoSize = true;
            this.label2.Location = new System.Drawing.Point(101, 22);
            this.label2.Name = "label2";
            this.label2.Size = new System.Drawing.Size(167, 13);
            this.label2.TabIndex = 82;
            this.label2.Text = "Active: Bursting Current [mA, ms] :";
            // 
            // PowerCalculator
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(96F, 96F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Dpi;
            this.BackColor = System.Drawing.SystemColors.Control;
            this.ClientSize = new System.Drawing.Size(506, 397);
            this.Controls.Add(this.groupBox2);
            this.Controls.Add(this.groupBox1);
            this.Icon = ((System.Drawing.Icon)(resources.GetObject("$this.Icon")));
            this.Name = "PowerCalculator";
            this.Text = "Power Budget Calculator for PGA460-Q1";
            this.groupBox1.ResumeLayout(false);
            this.groupBox1.PerformLayout();
            this.groupBox2.ResumeLayout(false);
            this.groupBox2.PerformLayout();
            this.ResumeLayout(false);

		}

		private global::System.ComponentModel.IContainer components = null;
		private global::System.Windows.Forms.ComboBox pb_freq;
		private global::System.Windows.Forms.ComboBox pb_rec;
		private global::System.Windows.Forms.ComboBox pb_pulses;
		private global::System.Windows.Forms.ComboBox pb_limit;
		private global::System.Windows.Forms.Label label62;
		private global::System.Windows.Forms.Label label61;
		private global::System.Windows.Forms.Label label16;
		private global::System.Windows.Forms.Label label15;
		private global::System.Windows.Forms.GroupBox groupBox1;
		private global::System.Windows.Forms.ComboBox pb_idlet;
		private global::System.Windows.Forms.Label label459;
		private global::System.Windows.Forms.CheckBox pb_idlec;
		private global::System.Windows.Forms.ComboBox pb_lpmt;
		private global::System.Windows.Forms.Label label285;
		private global::System.Windows.Forms.CheckBox pb_lpbc;
		private global::System.Windows.Forms.TextBox cmdIntTextbox;
		private global::System.Windows.Forms.Label label1;
		private global::System.Windows.Forms.GroupBox groupBox2;
		private global::System.Windows.Forms.Label label4;
		private global::System.Windows.Forms.TextBox out_actlis;
		private global::System.Windows.Forms.Label label3;
		private global::System.Windows.Forms.TextBox out_actburst;
		private global::System.Windows.Forms.Label label2;
		private global::System.Windows.Forms.TextBox out_inactive;
		private global::System.Windows.Forms.TextBox avgCurrent;
		private global::System.Windows.Forms.Label label5;
		private global::System.Windows.Forms.Label label7;
		private global::System.Windows.Forms.ComboBox pb_vpwr;
		private global::System.Windows.Forms.TextBox avgPower;
		private global::System.Windows.Forms.Label label6;
		private global::System.Windows.Forms.Button button1;
		private global::System.Windows.Forms.TextBox out_inactiveTime;
		private global::System.Windows.Forms.TextBox out_actlisTime;
		private global::System.Windows.Forms.TextBox out_actburstTime;
		private global::System.Windows.Forms.TextBox pb_dist;
		private global::System.Windows.Forms.CheckBox pb_disc;
		private global::System.Windows.Forms.Label label8;
	}
}
