namespace TI.eLAB.EVM
{
	public partial class Scripting : global::System.Windows.Forms.Form
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
            System.ComponentModel.ComponentResourceManager resources = new System.ComponentModel.ComponentResourceManager(typeof(Scripting));
            this.tableLayoutPanel1 = new System.Windows.Forms.TableLayoutPanel();
            this.groupBox1 = new System.Windows.Forms.GroupBox();
            this.script_input = new System.Windows.Forms.RichTextBox();
            this.groupBox2 = new System.Windows.Forms.GroupBox();
            this.script_status = new System.Windows.Forms.RichTextBox();
            this.tableLayoutPanel2 = new System.Windows.Forms.TableLayoutPanel();
            this.tableLayoutPanel3 = new System.Windows.Forms.TableLayoutPanel();
            this.script_run_btn = new System.Windows.Forms.Button();
            this.script_stop_btn = new System.Windows.Forms.Button();
            this.script_pause_btn = new System.Windows.Forms.Button();
            this.script_load_btn = new System.Windows.Forms.Button();
            this.script_saveInput_btn = new System.Windows.Forms.Button();
            this.script_saveStat_btn = new System.Windows.Forms.Button();
            this.script_clearStat_btn = new System.Windows.Forms.Button();
            this.script_clearInput_btn = new System.Windows.Forms.Button();
            this.script_help_btn = new System.Windows.Forms.Button();
            this.groupBox3 = new System.Windows.Forms.GroupBox();
            this.label1 = new System.Windows.Forms.Label();
            this.script_loop_count = new System.Windows.Forms.TextBox();
            this.script_loop_check = new System.Windows.Forms.CheckBox();
            this.openFileDialog1 = new System.Windows.Forms.OpenFileDialog();
            this.saveFileDialog1 = new System.Windows.Forms.SaveFileDialog();
            this.tableLayoutPanel1.SuspendLayout();
            this.groupBox1.SuspendLayout();
            this.groupBox2.SuspendLayout();
            this.tableLayoutPanel2.SuspendLayout();
            this.tableLayoutPanel3.SuspendLayout();
            this.groupBox3.SuspendLayout();
            this.SuspendLayout();
            // 
            // tableLayoutPanel1
            // 
            this.tableLayoutPanel1.ColumnCount = 2;
            this.tableLayoutPanel1.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Percent, 50F));
            this.tableLayoutPanel1.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Percent, 50F));
            this.tableLayoutPanel1.Controls.Add(this.groupBox1, 0, 0);
            this.tableLayoutPanel1.Controls.Add(this.groupBox2, 1, 0);
            this.tableLayoutPanel1.Dock = System.Windows.Forms.DockStyle.Fill;
            this.tableLayoutPanel1.Location = new System.Drawing.Point(3, 3);
            this.tableLayoutPanel1.Name = "tableLayoutPanel1";
            this.tableLayoutPanel1.RowCount = 1;
            this.tableLayoutPanel1.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Percent, 100F));
            this.tableLayoutPanel1.Size = new System.Drawing.Size(542, 458);
            this.tableLayoutPanel1.TabIndex = 0;
            // 
            // groupBox1
            // 
            this.groupBox1.Controls.Add(this.script_input);
            this.groupBox1.Dock = System.Windows.Forms.DockStyle.Fill;
            this.groupBox1.Location = new System.Drawing.Point(3, 3);
            this.groupBox1.Name = "groupBox1";
            this.groupBox1.Size = new System.Drawing.Size(265, 452);
            this.groupBox1.TabIndex = 0;
            this.groupBox1.TabStop = false;
            this.groupBox1.Text = "Script INPUT";
            // 
            // script_input
            // 
            this.script_input.Dock = System.Windows.Forms.DockStyle.Fill;
            this.script_input.Location = new System.Drawing.Point(3, 16);
            this.script_input.Name = "script_input";
            this.script_input.Size = new System.Drawing.Size(259, 433);
            this.script_input.TabIndex = 0;
            this.script_input.Text = "";
            // 
            // groupBox2
            // 
            this.groupBox2.Controls.Add(this.script_status);
            this.groupBox2.Dock = System.Windows.Forms.DockStyle.Fill;
            this.groupBox2.Location = new System.Drawing.Point(274, 3);
            this.groupBox2.Name = "groupBox2";
            this.groupBox2.Size = new System.Drawing.Size(265, 452);
            this.groupBox2.TabIndex = 1;
            this.groupBox2.TabStop = false;
            this.groupBox2.Text = "Script STATUS";
            // 
            // script_status
            // 
            this.script_status.BackColor = System.Drawing.SystemColors.ControlDark;
            this.script_status.Dock = System.Windows.Forms.DockStyle.Fill;
            this.script_status.Location = new System.Drawing.Point(3, 16);
            this.script_status.Name = "script_status";
            this.script_status.ReadOnly = true;
            this.script_status.Size = new System.Drawing.Size(259, 433);
            this.script_status.TabIndex = 0;
            this.script_status.Text = "";
            // 
            // tableLayoutPanel2
            // 
            this.tableLayoutPanel2.ColumnCount = 1;
            this.tableLayoutPanel2.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Percent, 100F));
            this.tableLayoutPanel2.Controls.Add(this.tableLayoutPanel1, 0, 0);
            this.tableLayoutPanel2.Controls.Add(this.tableLayoutPanel3, 0, 1);
            this.tableLayoutPanel2.Dock = System.Windows.Forms.DockStyle.Fill;
            this.tableLayoutPanel2.Location = new System.Drawing.Point(0, 0);
            this.tableLayoutPanel2.Name = "tableLayoutPanel2";
            this.tableLayoutPanel2.RowCount = 2;
            this.tableLayoutPanel2.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Percent, 100F));
            this.tableLayoutPanel2.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Absolute, 154F));
            this.tableLayoutPanel2.Size = new System.Drawing.Size(548, 618);
            this.tableLayoutPanel2.TabIndex = 1;
            // 
            // tableLayoutPanel3
            // 
            this.tableLayoutPanel3.ColumnCount = 3;
            this.tableLayoutPanel3.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Percent, 33.33334F));
            this.tableLayoutPanel3.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Percent, 33.33333F));
            this.tableLayoutPanel3.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Percent, 33.33333F));
            this.tableLayoutPanel3.Controls.Add(this.script_run_btn, 0, 0);
            this.tableLayoutPanel3.Controls.Add(this.script_stop_btn, 2, 0);
            this.tableLayoutPanel3.Controls.Add(this.script_pause_btn, 1, 0);
            this.tableLayoutPanel3.Controls.Add(this.script_load_btn, 1, 1);
            this.tableLayoutPanel3.Controls.Add(this.script_saveInput_btn, 0, 1);
            this.tableLayoutPanel3.Controls.Add(this.script_saveStat_btn, 2, 1);
            this.tableLayoutPanel3.Controls.Add(this.script_clearStat_btn, 2, 2);
            this.tableLayoutPanel3.Controls.Add(this.script_clearInput_btn, 0, 2);
            this.tableLayoutPanel3.Controls.Add(this.script_help_btn, 1, 2);
            this.tableLayoutPanel3.Controls.Add(this.groupBox3, 2, 3);
            this.tableLayoutPanel3.Dock = System.Windows.Forms.DockStyle.Fill;
            this.tableLayoutPanel3.Location = new System.Drawing.Point(3, 467);
            this.tableLayoutPanel3.Name = "tableLayoutPanel3";
            this.tableLayoutPanel3.RowCount = 4;
            this.tableLayoutPanel3.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Percent, 23.33233F));
            this.tableLayoutPanel3.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Percent, 23.33233F));
            this.tableLayoutPanel3.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Percent, 23.33233F));
            this.tableLayoutPanel3.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Percent, 30.003F));
            this.tableLayoutPanel3.Size = new System.Drawing.Size(542, 148);
            this.tableLayoutPanel3.TabIndex = 1;
            // 
            // script_run_btn
            // 
            this.script_run_btn.BackColor = System.Drawing.SystemColors.Control;
            this.script_run_btn.Dock = System.Windows.Forms.DockStyle.Fill;
            this.script_run_btn.Location = new System.Drawing.Point(3, 3);
            this.script_run_btn.Name = "script_run_btn";
            this.script_run_btn.Size = new System.Drawing.Size(174, 28);
            this.script_run_btn.TabIndex = 0;
            this.script_run_btn.Text = "RUN Script";
            this.script_run_btn.UseVisualStyleBackColor = false;
            this.script_run_btn.Click += new System.EventHandler(this.script_run_btn_Click);
            // 
            // script_stop_btn
            // 
            this.script_stop_btn.BackColor = System.Drawing.SystemColors.Control;
            this.script_stop_btn.Dock = System.Windows.Forms.DockStyle.Fill;
            this.script_stop_btn.Location = new System.Drawing.Point(363, 3);
            this.script_stop_btn.Name = "script_stop_btn";
            this.script_stop_btn.Size = new System.Drawing.Size(176, 28);
            this.script_stop_btn.TabIndex = 1;
            this.script_stop_btn.Text = "STOP Script";
            this.script_stop_btn.UseVisualStyleBackColor = false;
            this.script_stop_btn.Click += new System.EventHandler(this.script_stop_btn_Click);
            // 
            // script_pause_btn
            // 
            this.script_pause_btn.BackColor = System.Drawing.SystemColors.Control;
            this.script_pause_btn.Dock = System.Windows.Forms.DockStyle.Fill;
            this.script_pause_btn.Location = new System.Drawing.Point(183, 3);
            this.script_pause_btn.Name = "script_pause_btn";
            this.script_pause_btn.Size = new System.Drawing.Size(174, 28);
            this.script_pause_btn.TabIndex = 2;
            this.script_pause_btn.Text = "PAUSE Script";
            this.script_pause_btn.UseVisualStyleBackColor = false;
            this.script_pause_btn.Click += new System.EventHandler(this.script_pause_btn_Click);
            // 
            // script_load_btn
            // 
            this.script_load_btn.BackColor = System.Drawing.SystemColors.Control;
            this.script_load_btn.Dock = System.Windows.Forms.DockStyle.Fill;
            this.script_load_btn.Location = new System.Drawing.Point(183, 37);
            this.script_load_btn.Name = "script_load_btn";
            this.script_load_btn.Size = new System.Drawing.Size(174, 28);
            this.script_load_btn.TabIndex = 4;
            this.script_load_btn.Text = "LOAD Script";
            this.script_load_btn.UseVisualStyleBackColor = false;
            this.script_load_btn.Click += new System.EventHandler(this.script_load_btn_Click);
            // 
            // script_saveInput_btn
            // 
            this.script_saveInput_btn.BackColor = System.Drawing.SystemColors.Control;
            this.script_saveInput_btn.Dock = System.Windows.Forms.DockStyle.Fill;
            this.script_saveInput_btn.Location = new System.Drawing.Point(3, 37);
            this.script_saveInput_btn.Name = "script_saveInput_btn";
            this.script_saveInput_btn.Size = new System.Drawing.Size(174, 28);
            this.script_saveInput_btn.TabIndex = 3;
            this.script_saveInput_btn.Text = "SAVE Input";
            this.script_saveInput_btn.UseVisualStyleBackColor = false;
            this.script_saveInput_btn.Click += new System.EventHandler(this.script_saveInput_btn_Click);
            // 
            // script_saveStat_btn
            // 
            this.script_saveStat_btn.BackColor = System.Drawing.SystemColors.Control;
            this.script_saveStat_btn.Dock = System.Windows.Forms.DockStyle.Fill;
            this.script_saveStat_btn.Location = new System.Drawing.Point(363, 37);
            this.script_saveStat_btn.Name = "script_saveStat_btn";
            this.script_saveStat_btn.Size = new System.Drawing.Size(176, 28);
            this.script_saveStat_btn.TabIndex = 5;
            this.script_saveStat_btn.Text = "SAVE Status";
            this.script_saveStat_btn.UseVisualStyleBackColor = false;
            this.script_saveStat_btn.Click += new System.EventHandler(this.script_saveStat_btn_Click);
            // 
            // script_clearStat_btn
            // 
            this.script_clearStat_btn.BackColor = System.Drawing.SystemColors.Control;
            this.script_clearStat_btn.Dock = System.Windows.Forms.DockStyle.Fill;
            this.script_clearStat_btn.Location = new System.Drawing.Point(363, 71);
            this.script_clearStat_btn.Name = "script_clearStat_btn";
            this.script_clearStat_btn.Size = new System.Drawing.Size(176, 28);
            this.script_clearStat_btn.TabIndex = 6;
            this.script_clearStat_btn.Text = "Clear Status";
            this.script_clearStat_btn.UseVisualStyleBackColor = false;
            this.script_clearStat_btn.Click += new System.EventHandler(this.script_clearStat_btn_Click);
            // 
            // script_clearInput_btn
            // 
            this.script_clearInput_btn.BackColor = System.Drawing.SystemColors.Control;
            this.script_clearInput_btn.Dock = System.Windows.Forms.DockStyle.Fill;
            this.script_clearInput_btn.Location = new System.Drawing.Point(3, 71);
            this.script_clearInput_btn.Name = "script_clearInput_btn";
            this.script_clearInput_btn.Size = new System.Drawing.Size(174, 28);
            this.script_clearInput_btn.TabIndex = 7;
            this.script_clearInput_btn.Text = "Clear Input";
            this.script_clearInput_btn.UseVisualStyleBackColor = false;
            this.script_clearInput_btn.Click += new System.EventHandler(this.script_clearInput_btn_Click);
            // 
            // script_help_btn
            // 
            this.script_help_btn.BackColor = System.Drawing.SystemColors.Control;
            this.script_help_btn.Dock = System.Windows.Forms.DockStyle.Fill;
            this.script_help_btn.Location = new System.Drawing.Point(183, 71);
            this.script_help_btn.Name = "script_help_btn";
            this.script_help_btn.Size = new System.Drawing.Size(174, 28);
            this.script_help_btn.TabIndex = 8;
            this.script_help_btn.Text = "HELP";
            this.script_help_btn.UseVisualStyleBackColor = false;
            this.script_help_btn.Click += new System.EventHandler(this.script_help_btn_Click);
            // 
            // groupBox3
            // 
            this.groupBox3.Controls.Add(this.label1);
            this.groupBox3.Controls.Add(this.script_loop_count);
            this.groupBox3.Controls.Add(this.script_loop_check);
            this.groupBox3.Location = new System.Drawing.Point(363, 105);
            this.groupBox3.Name = "groupBox3";
            this.groupBox3.Size = new System.Drawing.Size(176, 40);
            this.groupBox3.TabIndex = 9;
            this.groupBox3.TabStop = false;
            this.groupBox3.Text = "Loop Script";
            // 
            // label1
            // 
            this.label1.AutoSize = true;
            this.label1.Location = new System.Drawing.Point(87, 18);
            this.label1.Name = "label1";
            this.label1.Size = new System.Drawing.Size(42, 13);
            this.label1.TabIndex = 2;
            this.label1.Text = "Loops :";
            // 
            // script_loop_count
            // 
            this.script_loop_count.Location = new System.Drawing.Point(131, 15);
            this.script_loop_count.Name = "script_loop_count";
            this.script_loop_count.ReadOnly = true;
            this.script_loop_count.Size = new System.Drawing.Size(42, 20);
            this.script_loop_count.TabIndex = 1;
            this.script_loop_count.Text = "1";
            this.script_loop_count.TextChanged += new System.EventHandler(this.script_loop_count_TextChanged);
            // 
            // script_loop_check
            // 
            this.script_loop_check.AutoSize = true;
            this.script_loop_check.CheckAlign = System.Drawing.ContentAlignment.MiddleRight;
            this.script_loop_check.Location = new System.Drawing.Point(10, 17);
            this.script_loop_check.Name = "script_loop_check";
            this.script_loop_check.Size = new System.Drawing.Size(71, 17);
            this.script_loop_check.TabIndex = 0;
            this.script_loop_check.Text = "Enabled :";
            this.script_loop_check.UseVisualStyleBackColor = true;
            this.script_loop_check.CheckedChanged += new System.EventHandler(this.script_loop_check_CheckedChanged);
            // 
            // openFileDialog1
            // 
            this.openFileDialog1.FileName = "openFileDialog1";
            // 
            // Scripting
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(96F, 96F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Dpi;
            this.ClientSize = new System.Drawing.Size(548, 618);
            this.Controls.Add(this.tableLayoutPanel2);
            this.Icon = ((System.Drawing.Icon)(resources.GetObject("$this.Icon")));
            this.Name = "Scripting";
            this.Text = "Scripting for PGA460-Q1";
            this.tableLayoutPanel1.ResumeLayout(false);
            this.groupBox1.ResumeLayout(false);
            this.groupBox2.ResumeLayout(false);
            this.tableLayoutPanel2.ResumeLayout(false);
            this.tableLayoutPanel3.ResumeLayout(false);
            this.groupBox3.ResumeLayout(false);
            this.groupBox3.PerformLayout();
            this.ResumeLayout(false);

		}

		private global::System.ComponentModel.IContainer components = null;
		private global::System.Windows.Forms.TableLayoutPanel tableLayoutPanel1;
		private global::System.Windows.Forms.GroupBox groupBox1;
		private global::System.Windows.Forms.GroupBox groupBox2;
		private global::System.Windows.Forms.TableLayoutPanel tableLayoutPanel2;
		private global::System.Windows.Forms.TableLayoutPanel tableLayoutPanel3;
		private global::System.Windows.Forms.RichTextBox script_input;
		private global::System.Windows.Forms.RichTextBox script_status;
		private global::System.Windows.Forms.Button script_run_btn;
		private global::System.Windows.Forms.Button script_stop_btn;
		private global::System.Windows.Forms.Button script_pause_btn;
		private global::System.Windows.Forms.OpenFileDialog openFileDialog1;
		private global::System.Windows.Forms.SaveFileDialog saveFileDialog1;
		private global::System.Windows.Forms.Button script_load_btn;
		private global::System.Windows.Forms.Button script_saveInput_btn;
		private global::System.Windows.Forms.Button script_saveStat_btn;
		private global::System.Windows.Forms.Button script_clearStat_btn;
		private global::System.Windows.Forms.Button script_clearInput_btn;
		private global::System.Windows.Forms.Button script_help_btn;
		private global::System.Windows.Forms.GroupBox groupBox3;
		private global::System.Windows.Forms.CheckBox script_loop_check;
		private global::System.Windows.Forms.Label label1;
		private global::System.Windows.Forms.TextBox script_loop_count;
	}
}
