using System;
using System.ComponentModel;
using System.Drawing;
using System.Windows.Forms;

namespace TI.eLAB.EVM
{
	public partial class About : Form
	{
		public About()
		{
			InitializeComponent();
		}

		public void button1_Click(object sender, EventArgs e)
		{
			base.Close();
		}

		public string AboutTextBox
		{
			get
			{
				return this.aboutTextBox.Text;
			}
			set
			{
				this.aboutTextBox.Text = value;
			}
		}

		public void InitializeComponent()
		{
            System.ComponentModel.ComponentResourceManager resources = new System.ComponentModel.ComponentResourceManager(typeof(About));
            this.pictureBox1 = new System.Windows.Forms.PictureBox();
            this.button1 = new System.Windows.Forms.Button();
            this.aboutTextBox = new System.Windows.Forms.TextBox();
            this.revHistoryTextBox = new System.Windows.Forms.TextBox();
            this.label1 = new System.Windows.Forms.Label();
            ((System.ComponentModel.ISupportInitialize)(this.pictureBox1)).BeginInit();
            this.SuspendLayout();
            // 
            // pictureBox1
            // 
            this.pictureBox1.Image = ((System.Drawing.Image)(resources.GetObject("pictureBox1.Image")));
            this.pictureBox1.InitialImage = null;
            this.pictureBox1.Location = new System.Drawing.Point(14, 12);
            this.pictureBox1.Name = "pictureBox1";
            this.pictureBox1.Size = new System.Drawing.Size(293, 107);
            this.pictureBox1.TabIndex = 0;
            this.pictureBox1.TabStop = false;
            // 
            // button1
            // 
            this.button1.Location = new System.Drawing.Point(111, 431);
            this.button1.Name = "button1";
            this.button1.Size = new System.Drawing.Size(87, 27);
            this.button1.TabIndex = 1;
            this.button1.Text = "Close";
            this.button1.UseVisualStyleBackColor = true;
            // 
            // aboutTextBox
            // 
            this.aboutTextBox.BackColor = System.Drawing.SystemColors.Control;
            this.aboutTextBox.Location = new System.Drawing.Point(13, 125);
            this.aboutTextBox.Multiline = true;
            this.aboutTextBox.Name = "aboutTextBox";
            this.aboutTextBox.ReadOnly = true;
            this.aboutTextBox.Size = new System.Drawing.Size(294, 96);
            this.aboutTextBox.TabIndex = 2;
            // 
            // revHistoryTextBox
            // 
            this.revHistoryTextBox.BackColor = System.Drawing.SystemColors.Control;
            this.revHistoryTextBox.Font = new System.Drawing.Font("Arial", 6.75F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.revHistoryTextBox.Location = new System.Drawing.Point(14, 246);
            this.revHistoryTextBox.Multiline = true;
            this.revHistoryTextBox.Name = "revHistoryTextBox";
            this.revHistoryTextBox.ReadOnly = true;
            this.revHistoryTextBox.ScrollBars = System.Windows.Forms.ScrollBars.Vertical;
            this.revHistoryTextBox.Size = new System.Drawing.Size(294, 179);
            this.revHistoryTextBox.TabIndex = 3;
            // 
            // label1
            // 
            this.label1.AutoSize = true;
            this.label1.Location = new System.Drawing.Point(13, 228);
            this.label1.Name = "label1";
            this.label1.Size = new System.Drawing.Size(96, 15);
            this.label1.TabIndex = 4;
            this.label1.Text = "Revision History";
            // 
            // About
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(96F, 96F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Dpi;
            this.ClientSize = new System.Drawing.Size(320, 470);
            this.Controls.Add(this.label1);
            this.Controls.Add(this.revHistoryTextBox);
            this.Controls.Add(this.aboutTextBox);
            this.Controls.Add(this.button1);
            this.Controls.Add(this.pictureBox1);
            this.Font = new System.Drawing.Font("Arial", 9F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.Icon = ((System.Drawing.Icon)(resources.GetObject("$this.Icon")));
            this.Name = "About";
            this.StartPosition = System.Windows.Forms.FormStartPosition.CenterScreen;
            this.Text = "About";
            ((System.ComponentModel.ISupportInitialize)(this.pictureBox1)).EndInit();
            this.ResumeLayout(false);
            this.PerformLayout();

		}

		public PictureBox pictureBox1;
		public Button button1;
		public TextBox aboutTextBox;
		public TextBox revHistoryTextBox;
		private Label label1;
	}
}
