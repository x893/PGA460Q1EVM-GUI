using System;
using System.ComponentModel;
using System.Drawing;
using System.Windows.Forms;
using TI.eLAB.EVM.Properties;

namespace TI.eLAB.EVM
{
	public partial class SplashScreenForm : Form
	{
		public SplashScreenForm()
		{
			InitializeComponent();
			progressBar1.Show();
		}

		public void ShowSplashScreen()
		{
			if (onlyOnce == 0)
			{
				if (InvokeRequired)
					BeginInvoke(new SplashScreenForm.SplashShowCloseDelegate(ShowSplashScreen));
				else
				{
					Show();
					Application.Run(this);
					onlyOnce++;
				}
			}
			else
				CloseSplashScreen();
		}

		public void CloseSplashScreen()
		{
			if (InvokeRequired)
				BeginInvoke(new SplashScreenForm.SplashShowCloseDelegate(CloseSplashScreen));
			else
			{
				CloseSplashScreenFlag = true;
				Dispose();
				Close();
			}
		}

		public void UdpateStatusText(string Text)
		{
			if (InvokeRequired)
			{
				BeginInvoke(new SplashScreenForm.StringParameterDelegate(UdpateStatusText), new object[] { Text });
			}
			else
			{
				loadingTextLabel.Text = Text;
				progressBar1.Value = 100;
			}
		}

		public void UdpateStatusTextWithStatus(string Text, TypeOfMessage tom)
		{
			if (InvokeRequired)
			{
				BeginInvoke(new SplashScreenForm.StringParameterWithStatusDelegate(UdpateStatusTextWithStatus), new object[] { Text, tom });
			}
			else
			{
				switch (tom)
				{
				case TypeOfMessage.Success:
					loadingTextLabel.ForeColor = Color.Green;
					break;
				case TypeOfMessage.Warning:
					loadingTextLabel.ForeColor = Color.Yellow;
					break;
				case TypeOfMessage.Error:
					loadingTextLabel.ForeColor = Color.Red;
					break;
				}
				loadingTextLabel.Text = Text;
			}
		}

		public void SplashForm_FormClosing(object sender, FormClosingEventArgs e)
		{
			if (!CloseSplashScreenFlag)
				e.Cancel = true;
		}

		public int ProgressBarValue
		{
			get
			{
				return progressBar1.Value;
			}
			set
			{
				progressBar1.Value = value;
			}
		}

		public void InitializeComponent()
		{
			this.components = new System.ComponentModel.Container();
			this.progressBar1 = new System.Windows.Forms.ProgressBar();
			this.loadingTextLabel = new System.Windows.Forms.Label();
			this.timer1 = new System.Windows.Forms.Timer(this.components);
			this.PGA46xSplashLabel = new System.Windows.Forms.Label();
			this.label2 = new System.Windows.Forms.Label();
			this.label3 = new System.Windows.Forms.Label();
			this.pictureBox2 = new System.Windows.Forms.PictureBox();
			this.initTextLabel = new System.Windows.Forms.Label();
			this.startingTextLabel = new System.Windows.Forms.Label();
			this.tableLayoutPanel1 = new System.Windows.Forms.TableLayoutPanel();
			this.tableLayoutPanel3 = new System.Windows.Forms.TableLayoutPanel();
			this.tableLayoutPanel5 = new System.Windows.Forms.TableLayoutPanel();
			this.tableLayoutPanel2 = new System.Windows.Forms.TableLayoutPanel();
			this.tableLayoutPanel6 = new System.Windows.Forms.TableLayoutPanel();
			this.tableLayoutPanel4 = new System.Windows.Forms.TableLayoutPanel();
			((System.ComponentModel.ISupportInitialize)(this.pictureBox2)).BeginInit();
			this.tableLayoutPanel1.SuspendLayout();
			this.tableLayoutPanel3.SuspendLayout();
			this.tableLayoutPanel5.SuspendLayout();
			this.tableLayoutPanel2.SuspendLayout();
			this.tableLayoutPanel6.SuspendLayout();
			this.tableLayoutPanel4.SuspendLayout();
			this.SuspendLayout();
			// 
			// progressBar1
			// 
			this.progressBar1.Dock = System.Windows.Forms.DockStyle.Fill;
			this.progressBar1.Location = new System.Drawing.Point(0, 387);
			this.progressBar1.Margin = new System.Windows.Forms.Padding(0);
			this.progressBar1.Name = "progressBar1";
			this.progressBar1.Size = new System.Drawing.Size(775, 16);
			this.progressBar1.TabIndex = 0;
			this.progressBar1.Value = 100;
			// 
			// loadingTextLabel
			// 
			this.loadingTextLabel.Anchor = System.Windows.Forms.AnchorStyles.Left;
			this.loadingTextLabel.AutoSize = true;
			this.loadingTextLabel.BackColor = System.Drawing.Color.Transparent;
			this.loadingTextLabel.Font = new System.Drawing.Font("Arial", 9F, System.Drawing.FontStyle.Bold);
			this.loadingTextLabel.ForeColor = System.Drawing.Color.Silver;
			this.loadingTextLabel.Location = new System.Drawing.Point(3, 25);
			this.loadingTextLabel.Name = "loadingTextLabel";
			this.loadingTextLabel.Size = new System.Drawing.Size(167, 15);
			this.loadingTextLabel.TabIndex = 4;
			this.loadingTextLabel.Text = "... Now Loading Components";
			this.loadingTextLabel.TextAlign = System.Drawing.ContentAlignment.MiddleCenter;
			// 
			// timer1
			// 
			this.timer1.Enabled = true;
			this.timer1.Interval = 5;
			// 
			// PGA46xSplashLabel
			// 
			this.PGA46xSplashLabel.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Right)));
			this.PGA46xSplashLabel.AutoSize = true;
			this.PGA46xSplashLabel.BackColor = System.Drawing.Color.Transparent;
			this.PGA46xSplashLabel.Font = new System.Drawing.Font("Arial", 48F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
			this.PGA46xSplashLabel.ForeColor = System.Drawing.Color.White;
			this.PGA46xSplashLabel.Location = new System.Drawing.Point(344, 62);
			this.PGA46xSplashLabel.Name = "PGA46xSplashLabel";
			this.PGA46xSplashLabel.Size = new System.Drawing.Size(385, 75);
			this.PGA46xSplashLabel.TabIndex = 0;
			this.PGA46xSplashLabel.Text = "PGA460-Q1";
			// 
			// label2
			// 
			this.label2.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Right)));
			this.label2.AutoSize = true;
			this.label2.Font = new System.Drawing.Font("Microsoft Sans Serif", 8.25F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
			this.label2.ForeColor = System.Drawing.Color.White;
			this.label2.Location = new System.Drawing.Point(431, 51);
			this.label2.Name = "label2";
			this.label2.Size = new System.Drawing.Size(337, 13);
			this.label2.TabIndex = 5;
			this.label2.Text = "© Copyright 2017. Texas Instruments Incorporated. All rights reserved.";
			// 
			// label3
			// 
			this.label3.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Right)));
			this.label3.AutoSize = true;
			this.label3.BackColor = System.Drawing.Color.Transparent;
			this.label3.Font = new System.Drawing.Font("Arial", 48F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
			this.label3.ForeColor = System.Drawing.Color.FromArgb(((int)(((byte)(222)))), ((int)(((byte)(0)))), ((int)(((byte)(0)))));
			this.label3.Location = new System.Drawing.Point(558, 137);
			this.label3.Name = "label3";
			this.label3.Size = new System.Drawing.Size(171, 75);
			this.label3.TabIndex = 1;
			this.label3.Text = "EVM";
			// 
			// pictureBox2
			// 
			this.pictureBox2.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(222)))), ((int)(((byte)(0)))), ((int)(((byte)(0)))));
			this.pictureBox2.Dock = System.Windows.Forms.DockStyle.Fill;
			this.pictureBox2.Image = global::TI.eLAB.EVM.Properties.Resources.TI_platform_bar_red;
			this.pictureBox2.Location = new System.Drawing.Point(3, 3);
			this.pictureBox2.Name = "pictureBox2";
			this.pictureBox2.Size = new System.Drawing.Size(698, 66);
			this.pictureBox2.SizeMode = System.Windows.Forms.PictureBoxSizeMode.StretchImage;
			this.pictureBox2.TabIndex = 6;
			this.pictureBox2.TabStop = false;
			// 
			// initTextLabel
			// 
			this.initTextLabel.Anchor = System.Windows.Forms.AnchorStyles.Left;
			this.initTextLabel.AutoSize = true;
			this.initTextLabel.BackColor = System.Drawing.Color.Transparent;
			this.initTextLabel.Font = new System.Drawing.Font("Arial", 9F, System.Drawing.FontStyle.Bold);
			this.initTextLabel.ForeColor = System.Drawing.Color.White;
			this.initTextLabel.Location = new System.Drawing.Point(3, 3);
			this.initTextLabel.Name = "initTextLabel";
			this.initTextLabel.Size = new System.Drawing.Size(75, 15);
			this.initTextLabel.TabIndex = 7;
			this.initTextLabel.Text = "... Initializing";
			this.initTextLabel.TextAlign = System.Drawing.ContentAlignment.MiddleCenter;
			// 
			// startingTextLabel
			// 
			this.startingTextLabel.Anchor = System.Windows.Forms.AnchorStyles.Left;
			this.startingTextLabel.AutoSize = true;
			this.startingTextLabel.BackColor = System.Drawing.Color.Transparent;
			this.startingTextLabel.Font = new System.Drawing.Font("Arial", 9F, System.Drawing.FontStyle.Bold);
			this.startingTextLabel.ForeColor = System.Drawing.Color.Silver;
			this.startingTextLabel.Location = new System.Drawing.Point(3, 44);
			this.startingTextLabel.Name = "startingTextLabel";
			this.startingTextLabel.Size = new System.Drawing.Size(86, 15);
			this.startingTextLabel.TabIndex = 8;
			this.startingTextLabel.Text = "... Starting GUI";
			this.startingTextLabel.TextAlign = System.Drawing.ContentAlignment.MiddleCenter;
			// 
			// tableLayoutPanel1
			// 
			this.tableLayoutPanel1.ColumnCount = 1;
			this.tableLayoutPanel1.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Percent, 100F));
			this.tableLayoutPanel1.Controls.Add(this.tableLayoutPanel3, 0, 2);
			this.tableLayoutPanel1.Controls.Add(this.progressBar1, 0, 3);
			this.tableLayoutPanel1.Controls.Add(this.tableLayoutPanel2, 0, 0);
			this.tableLayoutPanel1.Controls.Add(this.tableLayoutPanel6, 0, 1);
			this.tableLayoutPanel1.Dock = System.Windows.Forms.DockStyle.Fill;
			this.tableLayoutPanel1.Location = new System.Drawing.Point(0, 0);
			this.tableLayoutPanel1.Margin = new System.Windows.Forms.Padding(0, 2, 2, 2);
			this.tableLayoutPanel1.Name = "tableLayoutPanel1";
			this.tableLayoutPanel1.RowCount = 4;
			this.tableLayoutPanel1.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Percent, 100F));
			this.tableLayoutPanel1.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Absolute, 81F));
			this.tableLayoutPanel1.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Absolute, 68F));
			this.tableLayoutPanel1.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Absolute, 16F));
			this.tableLayoutPanel1.Size = new System.Drawing.Size(775, 403);
			this.tableLayoutPanel1.TabIndex = 9;
			// 
			// tableLayoutPanel3
			// 
			this.tableLayoutPanel3.ColumnCount = 2;
			this.tableLayoutPanel3.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Percent, 25F));
			this.tableLayoutPanel3.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Percent, 75F));
			this.tableLayoutPanel3.Controls.Add(this.label2, 1, 0);
			this.tableLayoutPanel3.Controls.Add(this.tableLayoutPanel5, 0, 0);
			this.tableLayoutPanel3.Dock = System.Windows.Forms.DockStyle.Fill;
			this.tableLayoutPanel3.Location = new System.Drawing.Point(2, 321);
			this.tableLayoutPanel3.Margin = new System.Windows.Forms.Padding(2);
			this.tableLayoutPanel3.Name = "tableLayoutPanel3";
			this.tableLayoutPanel3.RowCount = 1;
			this.tableLayoutPanel3.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Percent, 100F));
			this.tableLayoutPanel3.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Absolute, 64F));
			this.tableLayoutPanel3.Size = new System.Drawing.Size(771, 64);
			this.tableLayoutPanel3.TabIndex = 10;
			// 
			// tableLayoutPanel5
			// 
			this.tableLayoutPanel5.ColumnCount = 1;
			this.tableLayoutPanel5.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Percent, 50F));
			this.tableLayoutPanel5.Controls.Add(this.startingTextLabel, 0, 2);
			this.tableLayoutPanel5.Controls.Add(this.loadingTextLabel, 0, 1);
			this.tableLayoutPanel5.Controls.Add(this.initTextLabel, 0, 0);
			this.tableLayoutPanel5.Dock = System.Windows.Forms.DockStyle.Fill;
			this.tableLayoutPanel5.Location = new System.Drawing.Point(2, 2);
			this.tableLayoutPanel5.Margin = new System.Windows.Forms.Padding(2);
			this.tableLayoutPanel5.Name = "tableLayoutPanel5";
			this.tableLayoutPanel5.RowCount = 3;
			this.tableLayoutPanel5.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Percent, 50F));
			this.tableLayoutPanel5.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Percent, 50F));
			this.tableLayoutPanel5.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Absolute, 16F));
			this.tableLayoutPanel5.Size = new System.Drawing.Size(188, 60);
			this.tableLayoutPanel5.TabIndex = 6;
			// 
			// tableLayoutPanel2
			// 
			this.tableLayoutPanel2.ColumnCount = 2;
			this.tableLayoutPanel2.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Percent, 95F));
			this.tableLayoutPanel2.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Percent, 5F));
			this.tableLayoutPanel2.Controls.Add(this.label3, 0, 2);
			this.tableLayoutPanel2.Controls.Add(this.PGA46xSplashLabel, 0, 1);
			this.tableLayoutPanel2.Dock = System.Windows.Forms.DockStyle.Fill;
			this.tableLayoutPanel2.Location = new System.Drawing.Point(2, 2);
			this.tableLayoutPanel2.Margin = new System.Windows.Forms.Padding(2);
			this.tableLayoutPanel2.Name = "tableLayoutPanel2";
			this.tableLayoutPanel2.RowCount = 4;
			this.tableLayoutPanel2.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Percent, 100F));
			this.tableLayoutPanel2.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Absolute, 81F));
			this.tableLayoutPanel2.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Absolute, 81F));
			this.tableLayoutPanel2.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Absolute, 16F));
			this.tableLayoutPanel2.Size = new System.Drawing.Size(771, 234);
			this.tableLayoutPanel2.TabIndex = 10;
			// 
			// tableLayoutPanel6
			// 
			this.tableLayoutPanel6.ColumnCount = 2;
			this.tableLayoutPanel6.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Percent, 92F));
			this.tableLayoutPanel6.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Percent, 8F));
			this.tableLayoutPanel6.Controls.Add(this.tableLayoutPanel4, 0, 0);
			this.tableLayoutPanel6.Dock = System.Windows.Forms.DockStyle.Fill;
			this.tableLayoutPanel6.Location = new System.Drawing.Point(0, 240);
			this.tableLayoutPanel6.Margin = new System.Windows.Forms.Padding(0, 2, 1, 2);
			this.tableLayoutPanel6.Name = "tableLayoutPanel6";
			this.tableLayoutPanel6.RowCount = 1;
			this.tableLayoutPanel6.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Percent, 100F));
			this.tableLayoutPanel6.Size = new System.Drawing.Size(774, 77);
			this.tableLayoutPanel6.TabIndex = 11;
			// 
			// tableLayoutPanel4
			// 
			this.tableLayoutPanel4.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(222)))), ((int)(((byte)(0)))), ((int)(((byte)(0)))));
			this.tableLayoutPanel4.ColumnCount = 1;
			this.tableLayoutPanel4.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Percent, 95F));
			this.tableLayoutPanel4.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Absolute, 15F));
			this.tableLayoutPanel4.Controls.Add(this.pictureBox2, 0, 0);
			this.tableLayoutPanel4.Location = new System.Drawing.Point(0, 0);
			this.tableLayoutPanel4.Margin = new System.Windows.Forms.Padding(0);
			this.tableLayoutPanel4.Name = "tableLayoutPanel4";
			this.tableLayoutPanel4.RowCount = 1;
			this.tableLayoutPanel4.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Percent, 100F));
			this.tableLayoutPanel4.Size = new System.Drawing.Size(704, 72);
			this.tableLayoutPanel4.TabIndex = 2;
			// 
			// SplashScreenForm
			// 
			this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
			this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
			this.BackColor = System.Drawing.Color.Black;
			this.ClientSize = new System.Drawing.Size(775, 403);
			this.Controls.Add(this.tableLayoutPanel1);
			this.FormBorderStyle = System.Windows.Forms.FormBorderStyle.None;
			this.Name = "SplashScreenForm";
			this.StartPosition = System.Windows.Forms.FormStartPosition.CenterScreen;
			this.Text = "SplashScreen";
			((System.ComponentModel.ISupportInitialize)(this.pictureBox2)).EndInit();
			this.tableLayoutPanel1.ResumeLayout(false);
			this.tableLayoutPanel3.ResumeLayout(false);
			this.tableLayoutPanel3.PerformLayout();
			this.tableLayoutPanel5.ResumeLayout(false);
			this.tableLayoutPanel5.PerformLayout();
			this.tableLayoutPanel2.ResumeLayout(false);
			this.tableLayoutPanel2.PerformLayout();
			this.tableLayoutPanel6.ResumeLayout(false);
			this.tableLayoutPanel4.ResumeLayout(false);
			this.ResumeLayout(false);

		}

		private int onlyOnce = 0;
		private bool CloseSplashScreenFlag = false;
		public ProgressBar progressBar1;
		public Label loadingTextLabel;
		public Timer timer1;
		public Label PGA46xSplashLabel;
		private Label label2;
		public Label label3;
		private PictureBox pictureBox2;
		public Label initTextLabel;
		public Label startingTextLabel;
		private TableLayoutPanel tableLayoutPanel1;
		private TableLayoutPanel tableLayoutPanel3;
		private TableLayoutPanel tableLayoutPanel4;
		private TableLayoutPanel tableLayoutPanel2;
		private TableLayoutPanel tableLayoutPanel5;
		private TableLayoutPanel tableLayoutPanel6;
		private delegate void StringParameterDelegate(string Text);
		private delegate void StringParameterWithStatusDelegate(string Text, TypeOfMessage tom);
		private delegate void SplashShowCloseDelegate();
	}
}
