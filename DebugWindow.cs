using System;
using System.ComponentModel;
using System.Drawing;
using System.Windows.Forms;

namespace TI.eLAB.EVM
{
	public partial class DebugWindow : Form
	{
		public DebugWindow()
		{
			InitializeComponent();
		}

		public void InitializeComponent()
		{
            this.groupBox46 = new System.Windows.Forms.GroupBox();
            this.txtSCRC = new System.Windows.Forms.TextBox();
            this.label270 = new System.Windows.Forms.Label();
            this.txtSTAT = new System.Windows.Forms.TextBox();
            this.label278 = new System.Windows.Forms.Label();
            this.txtDATA_LSDO = new System.Windows.Forms.TextBox();
            this.label277 = new System.Windows.Forms.Label();
            this.txtDATA_HSDO = new System.Windows.Forms.TextBox();
            this.txtECBK = new System.Windows.Forms.TextBox();
            this.label1 = new System.Windows.Forms.Label();
            this.label275 = new System.Windows.Forms.Label();
            this.label276 = new System.Windows.Forms.Label();
            this.groupBox45 = new System.Windows.Forms.GroupBox();
            this.txtMCRC = new System.Windows.Forms.TextBox();
            this.txtRSVD = new System.Windows.Forms.TextBox();
            this.txtDATA_LSDI = new System.Windows.Forms.TextBox();
            this.txtDATA_HSDI = new System.Windows.Forms.TextBox();
            this.label279 = new System.Windows.Forms.Label();
            this.txtADDR = new System.Windows.Forms.TextBox();
            this.label274 = new System.Windows.Forms.Label();
            this.label273 = new System.Windows.Forms.Label();
            this.label272 = new System.Windows.Forms.Label();
            this.label271 = new System.Windows.Forms.Label();
            this.groupBox46.SuspendLayout();
            this.groupBox45.SuspendLayout();
            this.SuspendLayout();
            // 
            // groupBox46
            // 
            this.groupBox46.Controls.Add(this.txtSCRC);
            this.groupBox46.Controls.Add(this.label270);
            this.groupBox46.Controls.Add(this.txtSTAT);
            this.groupBox46.Controls.Add(this.label278);
            this.groupBox46.Controls.Add(this.txtDATA_LSDO);
            this.groupBox46.Controls.Add(this.label277);
            this.groupBox46.Controls.Add(this.txtDATA_HSDO);
            this.groupBox46.Controls.Add(this.txtECBK);
            this.groupBox46.Controls.Add(this.label1);
            this.groupBox46.Controls.Add(this.label275);
            this.groupBox46.Location = new System.Drawing.Point(184, 7);
            this.groupBox46.Name = "groupBox46";
            this.groupBox46.Size = new System.Drawing.Size(175, 221);
            this.groupBox46.TabIndex = 151;
            this.groupBox46.TabStop = false;
            this.groupBox46.Text = "SDO";
            // 
            // txtSCRC
            // 
            this.txtSCRC.Font = new System.Drawing.Font("Arial", 9F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.txtSCRC.Location = new System.Drawing.Point(88, 173);
            this.txtSCRC.Name = "txtSCRC";
            this.txtSCRC.ReadOnly = true;
            this.txtSCRC.Size = new System.Drawing.Size(62, 21);
            this.txtSCRC.TabIndex = 138;
            this.txtSCRC.TabStop = false;
            this.txtSCRC.TextAlign = System.Windows.Forms.HorizontalAlignment.Center;
            // 
            // label270
            // 
            this.label270.AutoSize = true;
            this.label270.Font = new System.Drawing.Font("Arial", 9F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.label270.Location = new System.Drawing.Point(20, 176);
            this.label270.Name = "label270";
            this.label270.Size = new System.Drawing.Size(42, 15);
            this.label270.TabIndex = 137;
            this.label270.Text = "SCRC";
            // 
            // txtSTAT
            // 
            this.txtSTAT.Font = new System.Drawing.Font("Arial", 9F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.txtSTAT.Location = new System.Drawing.Point(88, 140);
            this.txtSTAT.Name = "txtSTAT";
            this.txtSTAT.ReadOnly = true;
            this.txtSTAT.Size = new System.Drawing.Size(62, 21);
            this.txtSTAT.TabIndex = 138;
            this.txtSTAT.TabStop = false;
            this.txtSTAT.TextAlign = System.Windows.Forms.HorizontalAlignment.Center;
            // 
            // label278
            // 
            this.label278.AutoSize = true;
            this.label278.Font = new System.Drawing.Font("Arial", 9F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.label278.Location = new System.Drawing.Point(20, 143);
            this.label278.Name = "label278";
            this.label278.Size = new System.Drawing.Size(34, 15);
            this.label278.TabIndex = 137;
            this.label278.Text = "STAT";
            // 
            // txtDATA_LSDO
            // 
            this.txtDATA_LSDO.Font = new System.Drawing.Font("Arial", 9F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.txtDATA_LSDO.Location = new System.Drawing.Point(88, 105);
            this.txtDATA_LSDO.Name = "txtDATA_LSDO";
            this.txtDATA_LSDO.ReadOnly = true;
            this.txtDATA_LSDO.Size = new System.Drawing.Size(62, 21);
            this.txtDATA_LSDO.TabIndex = 138;
            this.txtDATA_LSDO.TabStop = false;
            this.txtDATA_LSDO.TextAlign = System.Windows.Forms.HorizontalAlignment.Center;
            // 
            // label277
            // 
            this.label277.AutoSize = true;
            this.label277.Font = new System.Drawing.Font("Arial", 9F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.label277.Location = new System.Drawing.Point(20, 108);
            this.label277.Name = "label277";
            this.label277.Size = new System.Drawing.Size(49, 15);
            this.label277.TabIndex = 137;
            this.label277.Text = "DATA_L";
            // 
            // txtDATA_HSDO
            // 
            this.txtDATA_HSDO.Font = new System.Drawing.Font("Arial", 9F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.txtDATA_HSDO.Location = new System.Drawing.Point(88, 69);
            this.txtDATA_HSDO.Name = "txtDATA_HSDO";
            this.txtDATA_HSDO.ReadOnly = true;
            this.txtDATA_HSDO.Size = new System.Drawing.Size(62, 21);
            this.txtDATA_HSDO.TabIndex = 138;
            this.txtDATA_HSDO.TabStop = false;
            this.txtDATA_HSDO.TextAlign = System.Windows.Forms.HorizontalAlignment.Center;
            // 
            // txtECBK
            // 
            this.txtECBK.Font = new System.Drawing.Font("Arial", 9F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.txtECBK.Location = new System.Drawing.Point(88, 34);
            this.txtECBK.Name = "txtECBK";
            this.txtECBK.ReadOnly = true;
            this.txtECBK.Size = new System.Drawing.Size(62, 21);
            this.txtECBK.TabIndex = 138;
            this.txtECBK.TabStop = false;
            this.txtECBK.TextAlign = System.Windows.Forms.HorizontalAlignment.Center;
            // 
            // label1
            // 
            this.label1.AutoSize = true;
            this.label1.Font = new System.Drawing.Font("Arial", 9F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.label1.Location = new System.Drawing.Point(20, 72);
            this.label1.Name = "label1";
            this.label1.Size = new System.Drawing.Size(51, 15);
            this.label1.TabIndex = 137;
            this.label1.Text = "DATA_H";
            // 
            // label275
            // 
            this.label275.AutoSize = true;
            this.label275.Font = new System.Drawing.Font("Arial", 9F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.label275.Location = new System.Drawing.Point(20, 37);
            this.label275.Name = "label275";
            this.label275.Size = new System.Drawing.Size(40, 15);
            this.label275.TabIndex = 137;
            this.label275.Text = "ECBK";
            // 
            // label276
            // 
            this.label276.AutoSize = true;
            this.label276.Font = new System.Drawing.Font("Arial", 9F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.label276.Location = new System.Drawing.Point(51, 231);
            this.label276.Name = "label276";
            this.label276.Size = new System.Drawing.Size(265, 15);
            this.label276.TabIndex = 137;
            this.label276.Text = "*All the above values are in hexadecimal format";
            // 
            // groupBox45
            // 
            this.groupBox45.Controls.Add(this.txtMCRC);
            this.groupBox45.Controls.Add(this.txtRSVD);
            this.groupBox45.Controls.Add(this.txtDATA_LSDI);
            this.groupBox45.Controls.Add(this.txtDATA_HSDI);
            this.groupBox45.Controls.Add(this.label279);
            this.groupBox45.Controls.Add(this.txtADDR);
            this.groupBox45.Controls.Add(this.label274);
            this.groupBox45.Controls.Add(this.label273);
            this.groupBox45.Controls.Add(this.label272);
            this.groupBox45.Controls.Add(this.label271);
            this.groupBox45.Location = new System.Drawing.Point(13, 7);
            this.groupBox45.Name = "groupBox45";
            this.groupBox45.Size = new System.Drawing.Size(165, 221);
            this.groupBox45.TabIndex = 151;
            this.groupBox45.TabStop = false;
            this.groupBox45.Text = "SDI";
            // 
            // txtMCRC
            // 
            this.txtMCRC.Font = new System.Drawing.Font("Arial", 9F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.txtMCRC.Location = new System.Drawing.Point(83, 173);
            this.txtMCRC.Name = "txtMCRC";
            this.txtMCRC.ReadOnly = true;
            this.txtMCRC.Size = new System.Drawing.Size(62, 21);
            this.txtMCRC.TabIndex = 138;
            this.txtMCRC.TabStop = false;
            this.txtMCRC.TextAlign = System.Windows.Forms.HorizontalAlignment.Center;
            // 
            // txtRSVD
            // 
            this.txtRSVD.Font = new System.Drawing.Font("Arial", 9F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.txtRSVD.Location = new System.Drawing.Point(83, 140);
            this.txtRSVD.Name = "txtRSVD";
            this.txtRSVD.ReadOnly = true;
            this.txtRSVD.Size = new System.Drawing.Size(62, 21);
            this.txtRSVD.TabIndex = 138;
            this.txtRSVD.TabStop = false;
            this.txtRSVD.TextAlign = System.Windows.Forms.HorizontalAlignment.Center;
            // 
            // txtDATA_LSDI
            // 
            this.txtDATA_LSDI.Font = new System.Drawing.Font("Arial", 9F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.txtDATA_LSDI.Location = new System.Drawing.Point(83, 105);
            this.txtDATA_LSDI.Name = "txtDATA_LSDI";
            this.txtDATA_LSDI.ReadOnly = true;
            this.txtDATA_LSDI.Size = new System.Drawing.Size(62, 21);
            this.txtDATA_LSDI.TabIndex = 138;
            this.txtDATA_LSDI.TabStop = false;
            this.txtDATA_LSDI.TextAlign = System.Windows.Forms.HorizontalAlignment.Center;
            // 
            // txtDATA_HSDI
            // 
            this.txtDATA_HSDI.Font = new System.Drawing.Font("Arial", 9F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.txtDATA_HSDI.Location = new System.Drawing.Point(83, 69);
            this.txtDATA_HSDI.Name = "txtDATA_HSDI";
            this.txtDATA_HSDI.ReadOnly = true;
            this.txtDATA_HSDI.Size = new System.Drawing.Size(62, 21);
            this.txtDATA_HSDI.TabIndex = 138;
            this.txtDATA_HSDI.TabStop = false;
            this.txtDATA_HSDI.TextAlign = System.Windows.Forms.HorizontalAlignment.Center;
            // 
            // label279
            // 
            this.label279.AutoSize = true;
            this.label279.Font = new System.Drawing.Font("Arial", 9F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.label279.Location = new System.Drawing.Point(18, 176);
            this.label279.Name = "label279";
            this.label279.Size = new System.Drawing.Size(43, 15);
            this.label279.TabIndex = 137;
            this.label279.Text = "MCRC";
            // 
            // txtADDR
            // 
            this.txtADDR.Font = new System.Drawing.Font("Arial", 9F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.txtADDR.Location = new System.Drawing.Point(83, 34);
            this.txtADDR.Name = "txtADDR";
            this.txtADDR.ReadOnly = true;
            this.txtADDR.Size = new System.Drawing.Size(62, 21);
            this.txtADDR.TabIndex = 138;
            this.txtADDR.TabStop = false;
            this.txtADDR.TextAlign = System.Windows.Forms.HorizontalAlignment.Center;
            // 
            // label274
            // 
            this.label274.AutoSize = true;
            this.label274.Font = new System.Drawing.Font("Arial", 9F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.label274.Location = new System.Drawing.Point(18, 143);
            this.label274.Name = "label274";
            this.label274.Size = new System.Drawing.Size(40, 15);
            this.label274.TabIndex = 137;
            this.label274.Text = "RSVD";
            // 
            // label273
            // 
            this.label273.AutoSize = true;
            this.label273.Font = new System.Drawing.Font("Arial", 9F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.label273.Location = new System.Drawing.Point(18, 108);
            this.label273.Name = "label273";
            this.label273.Size = new System.Drawing.Size(49, 15);
            this.label273.TabIndex = 137;
            this.label273.Text = "DATA_L";
            // 
            // label272
            // 
            this.label272.AutoSize = true;
            this.label272.Font = new System.Drawing.Font("Arial", 9F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.label272.Location = new System.Drawing.Point(18, 72);
            this.label272.Name = "label272";
            this.label272.Size = new System.Drawing.Size(51, 15);
            this.label272.TabIndex = 137;
            this.label272.Text = "DATA_H";
            // 
            // label271
            // 
            this.label271.AutoSize = true;
            this.label271.Font = new System.Drawing.Font("Arial", 9F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.label271.Location = new System.Drawing.Point(18, 37);
            this.label271.Name = "label271";
            this.label271.Size = new System.Drawing.Size(41, 15);
            this.label271.TabIndex = 137;
            this.label271.Text = "ADDR";
            // 
            // DebugWindow
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(375, 252);
            this.ControlBox = false;
            this.Controls.Add(this.groupBox46);
            this.Controls.Add(this.groupBox45);
            this.Controls.Add(this.label276);
            this.FormBorderStyle = System.Windows.Forms.FormBorderStyle.FixedSingle;
            this.Name = "DebugWindow";
            this.Text = "Debug Window";
            this.TopMost = true;
            this.groupBox46.ResumeLayout(false);
            this.groupBox46.PerformLayout();
            this.groupBox45.ResumeLayout(false);
            this.groupBox45.PerformLayout();
            this.ResumeLayout(false);
            this.PerformLayout();

		}

		public GroupBox groupBox46;
		public TextBox txtSCRC;
		public Label label270;
		public TextBox txtSTAT;
		public Label label278;
		public TextBox txtDATA_LSDO;
		public Label label277;
		public TextBox txtDATA_HSDO;
		public Label label276;
		public TextBox txtECBK;
		public Label label275;
		public GroupBox groupBox45;
		public TextBox txtMCRC;
		public TextBox txtRSVD;
		public TextBox txtDATA_LSDI;
		public TextBox txtDATA_HSDI;
		public Label label279;
		public TextBox txtADDR;
		public Label label274;
		public Label label273;
		public Label label272;
		public Label label271;
		public Label label1;
	}
}
