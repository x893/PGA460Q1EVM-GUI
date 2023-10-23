using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Windows.Forms;

namespace Subro.Controls
{
	public class DataGridViewGrouperControl : UserControl
	{
		public DataGridViewGrouperControl()
		{
			InitializeComponent();
		}

		[DefaultValue(null)]
		public DataGridViewGrouper Grouper
		{
			get
			{
				return m_grouper;
			}
			set
			{
				if (m_grouper != value)
				{
					if (!base.DesignMode)
					{
						m_cmbFields.Enabled = (value != null);
					}
					if (m_grouper != null)
					{
						m_grouper.PropertiesChanged -= GroupingSource_DataSourceChanged;
						m_grouper.GroupingChanged -= grouper_GroupingChanged;
						if (m_grouperowned)
						{
							if (m_grouper != null)
							{
								((IDisposable)m_grouper).Dispose();
							}
							m_grouperowned = false;
						}
					}
					m_grouper = value;
					if (m_grouper != null)
					{
						m_grouper.PropertiesChanged += GroupingSource_DataSourceChanged;
						m_grouper.GroupingChanged += grouper_GroupingChanged;
					}
					setprops();
					if (m_cm != null)
					{
						m_cm.Grouper = value;
					}
				}
			}
		}

		private bool ShouldSerializeGrouper()
		{
			return m_grouper != null && !m_grouperowned;
		}

		protected override void Dispose(bool disposing)
		{
			Grouper = null;
			if (m_cm != null)
				m_cm.Dispose();
			if (disposing && components != null)
				components.Dispose();
			base.Dispose(disposing);
		}

		public override string Text
		{
			get
			{
				return m_chk.Text;
			}
			set
			{
				m_chk.Text = value;
			}
		}

		[DefaultValue(null)]
		public DataGridView DataGridView
		{
			get
			{
				DataGridView result;
				if (m_grouper != null)
					result = m_grouper.DataGridView;
				else
					result = null;
				return result;
			}
			set
			{
				if (DataGridView != value)
				{
					if (DataGridView != null)
						DataGridView.DataSourceChanged -= value_DataSourceChanged;
					if (m_grouperowned || value == null)
						Grouper = null;
					if (value != null)
					{
						if (m_grouper != null)
							m_grouper.DataGridView = value;
						else if (value is IDataGridViewGrouperOwner)
						{
							Grouper = (value as IDataGridViewGrouperOwner).Grouper;
							m_grouper.DataGridView = value;
						}
						else
						{
							Grouper = new DataGridViewGrouper(value);
							m_grouperowned = true;
						}
					}
				}
			}
		}

		private void value_DataSourceChanged(object sender, EventArgs e)
		{
			setprops();
		}

		public GroupingSource GroupingSource
		{
			get
			{
				GroupingSource result;
				if (m_grouper == null)
					result = null;
				else
					result = m_grouper.GroupingSource;
				return result;
			}
		}

		private void setprops()
		{
			if (!m_settingvalues)
			{
				m_settingvalues = true;
				m_cmbFields.BeginUpdate();
				m_cmbFields.Items.Clear();
				try
				{
					if (m_grouper != null)
					{
						IEnumerable<PropertyDescriptor> properties = m_grouper.GetProperties();
						if (properties != null)
						{
							GroupingInfo groupOn = m_grouper.GroupOn;
							foreach (PropertyDescriptor propertyDescriptor in properties)
							{
								m_cmbFields.Items.Add(propertyDescriptor);
								if (groupOn != null && groupOn.IsProperty(propertyDescriptor.Name))
									m_cmbFields.SelectedItem = propertyDescriptor;
							}
						}
					}
					m_chk.Checked = (m_grouper != null && m_grouper.GroupOn != null);
				}
				catch (Exception ex)
				{
					ShowEx(ex);
				}
				m_settingvalues = false;
				m_cmbFields.EndUpdate();
			}
		}

		private void ShowEx(Exception ex)
		{
			MessageBox.Show(ex.Message);
		}

		private void grouper_GroupingChanged(object sender, EventArgs e)
		{
			if (!m_settingvalues)
			{
				m_settingvalues = true;
				try
				{
					GroupingInfo groupOn = m_grouper.GroupOn;
					m_chk.Checked = (groupOn != null);
					if (m_chk.Checked)
					{
						PropertyDescriptor propertyDescriptor = FindProperty(groupOn);
						m_cmbFields.SelectedItem = propertyDescriptor;
						if (propertyDescriptor == null)
							m_cmbFields.Text = groupOn.ToString();
					}
				}
				finally
				{
					m_settingvalues = false;
				}
			}
		}

		public PropertyDescriptor FindProperty(GroupingInfo gr)
		{
			foreach (object obj in m_cmbFields.Items)
			{
				PropertyDescriptor propertyDescriptor = (PropertyDescriptor)obj;
				if (gr.IsProperty(propertyDescriptor.Name))
					return propertyDescriptor;
			}
			return null;
		}

		public bool IsGrouped
		{
			get
			{
				return m_grouper != null && m_grouper.GroupOn != null;
			}
		}

		private void GroupingSource_DataSourceChanged(object sender, EventArgs e)
		{
			setprops();
		}

		private void chk_CheckedChanged(object sender, EventArgs e)
		{
			setgroup();
		}

		private void cmbFields_SelectedIndexChanged(object sender, EventArgs e)
		{
			if (m_chk.Checked)
				setgroup();
		}

		private void setgroup()
		{
			if (!m_settingvalues && m_grouper != null)
			{
				m_settingvalues = true;
				if (!m_chk.Checked || m_cmbFields.SelectedItem == null)
				{
					m_grouper.RemoveGrouping();
				}
				else
				{
					PropertyDescriptor selectedProperty = SelectedProperty;
					try
					{
						m_grouper.SetGroupOn(selectedProperty);
					}
					catch (Exception ex)
					{
						ShowEx(new Exception("Error while grouping on " + selectedProperty.Name + ": " + ex.Message, ex));
					}
				}
				m_settingvalues = false;
			}
		}

		public PropertyDescriptor SelectedProperty
		{
			get
			{
				return (PropertyDescriptor)m_cmbFields.SelectedItem;
			}
		}

		protected override void OnPaint(PaintEventArgs e)
		{
			base.OnPaint(e);
			try
			{
				Rectangle clipRectangle = e.ClipRectangle;
				if (!clipRectangle.IsEmpty)
				{
					ControlPaint.DrawButton(e.Graphics, clipRectangle, m_down ? ButtonState.Pushed : ButtonState.Normal);
					if (m_down)
						clipRectangle.Offset(1, 1);

					e.Graphics.SmoothingMode = SmoothingMode.AntiAlias;
					Point point = new Point(clipRectangle.X + 2, clipRectangle.Y + clipRectangle.Height / 2 - 2);
					for (int i = 0; i < 2; i++)
					{
						int num = clipRectangle.Right - point.X - 4;
						Point[] points = new Point[]
						{
							point,
							new Point(point.X + num / 2, point.Y + 2),
							new Point(point.X + num, point.Y)
						};
						e.Graphics.DrawLines(Pens.Black, points);
						point.Y += 3;
					}
				}
			}
			catch
			{
			}
		}

		protected override void OnMouseDown(MouseEventArgs e)
		{
			base.OnMouseDown(e);
			if (!m_down)
			{
				m_down = true;
				ContextMenuStrip.Show(this, new Point(base.Width, base.Height), ToolStripDropDownDirection.BelowLeft);
				base.Invalidate();
			}
		}

		[DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
		[Browsable(false)]
		public new DataGridViewGrouperContextMenuStrip ContextMenuStrip
		{
			get
			{
				if (m_cm == null)
				{
					m_cm = new DataGridViewGrouperContextMenuStrip(m_grouper);
					m_cm.Closed += cm_Closed;
				}
				return m_cm;
			}
		}

		private void cm_Closed(object sender, ToolStripDropDownClosedEventArgs e)
		{
			m_down = false;
			base.Invalidate();
		}

		private void InitializeComponent()
		{
			this.m_cmbFields = new System.Windows.Forms.ComboBox();
			this.m_chk = new System.Windows.Forms.CheckBox();
			this.SuspendLayout();
			// 
			// m_cmbFields
			// 
			this.m_cmbFields.AutoCompleteMode = System.Windows.Forms.AutoCompleteMode.SuggestAppend;
			this.m_cmbFields.DisplayMember = "Name";
			this.m_cmbFields.Dock = System.Windows.Forms.DockStyle.Fill;
			this.m_cmbFields.DropDownWidth = 120;
			this.m_cmbFields.FormattingEnabled = true;
			this.m_cmbFields.Location = new System.Drawing.Point(70, 0);
			this.m_cmbFields.Name = "m_cmbFields";
			this.m_cmbFields.Size = new System.Drawing.Size(129, 21);
			this.m_cmbFields.Sorted = true;
			this.m_cmbFields.TabIndex = 0;
			// 
			// m_chk
			// 
			this.m_chk.AutoSize = true;
			this.m_chk.Dock = System.Windows.Forms.DockStyle.Left;
			this.m_chk.Location = new System.Drawing.Point(0, 0);
			this.m_chk.Name = "m_chk";
			this.m_chk.Size = new System.Drawing.Size(70, 20);
			this.m_chk.TabIndex = 1;
			this.m_chk.Text = "Group on";
			this.m_chk.UseVisualStyleBackColor = true;
			// 
			// DataGridViewGrouperControl
			// 
			this.AllowDrop = true;
			this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
			this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
			this.BorderStyle = System.Windows.Forms.BorderStyle.FixedSingle;
			this.Controls.Add(this.m_cmbFields);
			this.Controls.Add(this.m_chk);
			this.Name = "DataGridViewGrouperControl";
			this.Padding = new System.Windows.Forms.Padding(0, 0, 10, 0);
			this.Size = new System.Drawing.Size(209, 20);
			this.ResumeLayout(false);
			this.PerformLayout();

		}

		private DataGridViewGrouper m_grouper;
		private bool m_settingvalues;
		private bool m_grouperowned;
		private DataGridViewGrouperContextMenuStrip m_cm;
		private bool m_down;
		private IContainer components = null;
		private ComboBox m_cmbFields;
		private CheckBox m_chk;
	}
}
