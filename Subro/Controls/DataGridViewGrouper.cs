using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Drawing;
using System.Linq.Expressions;
using System.Windows.Forms;

namespace Subro.Controls
{
	[DefaultEvent("DisplayGroup")]
	public class DataGridViewGrouper : Component
	{
		public DataGridViewGrouper()
		{
			m_source.DataSourceChanged += source_DataSourceChanged;
			m_source.GrouperEx = this;
		}

		public DataGridViewGrouper(DataGridView Grid) : this()
		{
			DataGridView = Grid;
		}

		public DataGridViewGrouper(IContainer Container) : this()
		{
			Container.Add(this);
		}

		[DefaultValue(null)]
		public DataGridView DataGridView
		{
			get
			{
				return m_grid;
			}
			set
			{
				if (m_grid != value)
				{
					if (m_grid != null)
					{
						m_grid.RowPrePaint -= grid_RowPrePaint;
						m_grid.RowPostPaint -= grid_RowPostPaint;
						m_grid.CellBeginEdit -= grid_CellBeginEdit;
						m_grid.CellDoubleClick -= grid_CellDoubleClick;
						m_grid.CellClick -= grid_CellClick;
						m_grid.MouseMove -= grid_MouseMove;
						m_grid.SelectionChanged -= grid_SelectionChanged;
						m_grid.DataSourceChanged -= grid_DataSourceChanged;
						m_grid.AllowUserToAddRowsChanged -= grid_AllowUserToAddRowsChanged;
					}
					RemoveGrouping();
					m_selectedGroups.Clear();
					m_grid = value;
					if (m_grid != null)
					{
						m_grid.RowPrePaint += grid_RowPrePaint;
						m_grid.RowPostPaint += grid_RowPostPaint;
						m_grid.CellBeginEdit += grid_CellBeginEdit;
						m_grid.CellDoubleClick += grid_CellDoubleClick;
						m_grid.CellClick += grid_CellClick;
						m_grid.MouseMove += grid_MouseMove;
						m_grid.SelectionChanged += grid_SelectionChanged;
						m_grid.DataSourceChanged += grid_DataSourceChanged;
						m_grid.AllowUserToAddRowsChanged += grid_AllowUserToAddRowsChanged;
					}
				}
			}
		}

		private void grid_AllowUserToAddRowsChanged(object sender, EventArgs e)
		{
			m_source.CheckNewRow();
		}

		private void grid_MouseMove(object sender, MouseEventArgs e)
		{
			if (e.X < HeaderOffset && e.X >= HeaderOffset - 10)
			{
				DataGridView.HitTestInfo hitTestInfo = m_grid.HitTest(e.X, e.Y);
				if (IsGroupRow(hitTestInfo.RowIndex))
				{
					int num = e.Y - hitTestInfo.RowY;
					if (num >= 5 && num <= 15)
					{
						checkcollapsedfocused(hitTestInfo.ColumnIndex, hitTestInfo.RowIndex);
						return;
					}
				}
			}
			checkcollapsedfocused(-1, -1);
		}

		private void InvalidateCapturedBox()
		{
			if (m_capturedcollapsebox.Y != -1)
			{
				try
				{
					m_grid.InvalidateCell(m_capturedcollapsebox.X, m_capturedcollapsebox.Y);
				}
				catch
				{
					m_capturedcollapsebox = new Point(-1, -1);
				}
			}
		}

		private void checkcollapsedfocused(int col, int row)
		{
			if (m_capturedcollapsebox.X != col || m_capturedcollapsebox.Y != row)
			{
				InvalidateCapturedBox();
				m_capturedcollapsebox = new Point(col, row);
				InvalidateCapturedBox();
			}
		}

		private void grid_CellClick(object sender, DataGridViewCellEventArgs e)
		{
			if (e.RowIndex != -1)
			{
				if (e.RowIndex == m_capturedcollapsebox.Y)
				{
					GroupRow groupRow = GetGroupRow(e.RowIndex);
					groupRow.Collapsed = !groupRow.Collapsed;
				}
			}
		}

		private void grid_SelectionChanged(object sender, EventArgs e)
		{
			if (m_selectionset)
			{
				m_selectionset = false;
				invalidateselected();
			}
		}

		private void setselection()
		{
			m_selectionset = true;
			m_selectedGroups.Clear();
			foreach (object obj in m_grid.SelectedCells)
			{
				DataGridViewCell dataGridViewCell = (DataGridViewCell)obj;
				if (IsGroupRow(dataGridViewCell.RowIndex) && !m_selectedGroups.Contains(dataGridViewCell.RowIndex))
				{
					m_selectedGroups.Add(dataGridViewCell.RowIndex);
				}
			}
			invalidateselected();
		}

		private void invalidateselected()
		{
			if (m_selectedGroups.Count != 0 && m_grid.SelectionMode != DataGridViewSelectionMode.FullRowSelect)
			{
				int count = m_grid.Rows.Count;
				foreach (int num in m_selectedGroups)
				{
					if (num < count)
					{
						m_grid.InvalidateRow(num);
					}
				}
			}
		}

		public void ExpandAll()
		{
			m_source.CollapseExpandAll(false);
		}

		public void CollapseAll()
		{
			m_source.CollapseExpandAll(true);
		}

		private void grid_CellDoubleClick(object sender, DataGridViewCellEventArgs e)
		{
			if (IsGroupRow(e.RowIndex) && m_capturedcollapsebox.Y != e.RowIndex && Options.SelectRowsOnDoubleClick)
			{
				GroupRow groupRow = GetGroupRow(e.RowIndex);
				groupRow.Collapsed = false;
				m_grid.SuspendLayout();
				m_grid.CurrentCell = m_grid[1, e.RowIndex + 1];
				m_grid.Rows[e.RowIndex].Selected = false;
				SelectGroup(e.RowIndex);
				m_grid.ResumeLayout();
			}
		}

		private GroupRow GetGroupRow(int RowIndex)
		{
			return (GroupRow)m_source.Groups.Rows[RowIndex];
		}

		private IEnumerable<DataGridViewRow> GetRows(int index)
		{
			GroupRow gr = GetGroupRow(index);
			for (int i = 0; i < gr.Count; i++)
			{
				yield return m_grid.Rows[++index];
			}
			yield break;
		}

		private void SelectGroup(int offset)
		{
			foreach (DataGridViewRow dataGridViewRow in GetRows(offset))
			{
				dataGridViewRow.Selected = true;
			}
		}

		public GroupList Groups
		{
			get
			{
				return m_source.Groups;
			}
		}

		public bool IsGroupRow(int RowIndex)
		{
			return m_source.IsGroupRow(RowIndex);
		}

		private void source_DataSourceChanged(object sender, EventArgs e)
		{
			OnPropertiesChanged();
		}

		private void OnPropertiesChanged()
		{
			if (PropertiesChanged != null)
			{
				PropertiesChanged(this, EventArgs.Empty);
			}
		}

		public event EventHandler PropertiesChanged;

		public IEnumerable<PropertyDescriptor> GetProperties()
		{
			foreach (object obj in m_source.GetItemProperties(null))
			{
				PropertyDescriptor pd = (PropertyDescriptor)obj;
				yield return pd;
			}
			yield break;
		}

		private void grid_CellBeginEdit(object sender, DataGridViewCellCancelEventArgs e)
		{
			if (IsGroupRow(e.RowIndex))
			{
				e.Cancel = true;
			}
		}

		protected override void Dispose(bool disposing)
		{
			DataGridView = null;
			m_source.Dispose();
			base.Dispose(disposing);
		}

		public GroupingSource GroupingSource
		{
			get
			{
				return m_source;
			}
		}

		private void grid_DataSourceChanged(object sender, EventArgs e)
		{
			if (!GridUsesGroupSource)
			{
				try
				{
					m_source.DataSource = m_grid.DataSource;
				}
				catch
				{
					m_source.RemoveGrouping();
				}
			}
		}

		public bool RemoveGrouping()
		{
			if (GridUsesGroupSource)
			{
				try
				{
					m_grid.DataSource = m_source.DataSource;
					m_grid.DataMember = m_source.DataMember;
					m_source.RemoveGrouping();
					return true;
				}
				catch
				{
				}
			}
			return false;
		}

		public event EventHandler GroupingChanged
		{
			add
			{
				m_source.GroupingChanged += value;
			}
			remove
			{
				m_source.GroupingChanged -= value;
			}
		}

		private bool GridUsesGroupSource
		{
			get
			{
				return m_grid != null && m_grid.DataSource == m_source;
			}
		}

		public void ResetGrouping()
		{
			if (GridUsesGroupSource)
			{
				m_capturedcollapsebox = new Point(-1, -1);
				m_source.ResetGroups();
			}
		}

		[DefaultValue(null)]
		public GroupingInfo GroupOn
		{
			get
			{
				return m_source.GroupOn;
			}
			set
			{
				if (GroupOn != value)
				{
					if (value == null)
					{
						RemoveGrouping();
					}
					else
					{
						CheckSource().GroupOn = value;
					}
				}
			}
		}

		public bool IsGrouped
		{
			get
			{
				return m_source.IsGrouped;
			}
		}

		[DefaultValue(SortOrder.Ascending)]
		public SortOrder GroupSortOrder
		{
			get
			{
				return m_source.GroupSortOrder;
			}
			set
			{
				m_source.GroupSortOrder = value;
			}
		}

		[DefaultValue(null)]
		public GroupingOptions Options
		{
			get
			{
				return m_source.Options;
			}
			set
			{
				m_source.Options = value;
			}
		}

		public bool SetGroupOn(DataGridViewColumn col)
		{
			return SetGroupOn((col == null) ? null : col.DataPropertyName);
		}

		public bool SetGroupOn(PropertyDescriptor Property)
		{
			return CheckSource().SetGroupOn(Property);
		}

		public void SetCustomGroup<T>(Func<T, object> GroupValueProvider, string Description = null)
		{
			CheckSource().SetCustomGroup<T>(GroupValueProvider, Description);
		}

		public void SetGroupOnStartLetters(GroupingInfo g, int Letters)
		{
			CheckSource().SetGroupOnStartLetters(g, Letters);
		}

		public void SetGroupOnStartLetters(string Property, int Letters)
		{
			CheckSource().SetGroupOnStartLetters(Property, Letters);
		}

		public bool SetGroupOn(string Name)
		{
			bool result;
			if (string.IsNullOrEmpty(Name))
			{
				result = RemoveGrouping();
			}
			else
			{
				result = CheckSource().SetGroupOn(Name);
			}
			return result;
		}

		public bool SetGroupOn<T>(Expression<Func<T, object>> Property)
		{
			bool result;
			if (Property == null)
			{
				result = RemoveGrouping();
			}
			else
			{
				result = CheckSource().SetGroupOn(Parser.GetFieldName<T, object>(Property));
			}
			return result;
		}

		public PropertyDescriptor GetProperty(string Name)
		{
			return CheckSource().GetProperty(Name);
		}

		private GroupingSource CheckSource()
		{
			if (m_grid == null)
			{
				throw new Exception("No target datagridview set");
			}
			if (!GridUsesGroupSource)
			{
				m_source.DataSource = m_grid.DataSource;
				m_source.DataMember = m_grid.DataMember;
				m_grid.DataSource = m_source;
			}
			return m_source;
		}

		private void grid_RowPrePaint(object sender, DataGridViewRowPrePaintEventArgs e)
		{
			if (IsGroupRow(e.RowIndex))
			{
				e.Handled = true;
				PaintGroupRow(e);
			}
		}

		private int HeaderOffset
		{
			get
			{
				int result;
				if (m_grid.RowHeadersVisible)
				{
					result = m_grid.RowHeadersWidth;
				}
				else
				{
					result = 20;
				}
				return result;
			}
		}

		private bool DrawExpandCollapseLines
		{
			get
			{
				return m_grid.RowHeadersVisible;
			}
		}

		private void grid_RowPostPaint(object sender, DataGridViewRowPostPaintEventArgs e)
		{
			if (DrawExpandCollapseLines && e.RowIndex < m_source.Count && m_source.GroupOn != null)
			{
				int num = e.RowIndex + 1;
				int rowHeadersWidth = m_grid.RowHeadersWidth;
				int num2 = HeaderOffset - 5;
				int num3 = e.RowBounds.Top + e.RowBounds.Height / 2;
				e.Graphics.DrawLine(m_linepen, num2, num3, rowHeadersWidth, num3);
				if (num < m_source.Count && !IsGroupRow(num))
				{
					num3 = e.RowBounds.Bottom;
				}
				e.Graphics.DrawLine(m_linepen, num2, e.RowBounds.Top, num2, num3);
			}
		}

		public event EventHandler<GroupDisplayEventArgs> DisplayGroup
		{
			add
			{
				m_source.DisplayGroup += value;
			}
			remove
			{
				m_source.DisplayGroup -= value;
			}
		}

		public DataGridViewGrouper this[int GroupIndex]
		{
			get
			{
				return (DataGridViewGrouper)m_source[GroupIndex];
			}
		}

		private void PaintGroupRow(DataGridViewRowPrePaintEventArgs e)
		{
			GroupRow groupRow = (GroupRow)m_source[e.RowIndex];
			if (!m_selectionset)
			{
				setselection();
			}
			GroupDisplayEventArgs displayInfo = groupRow.GetDisplayInfo(m_selectedGroups.Contains(e.RowIndex));
			if (displayInfo != null && !displayInfo.Cancel)
			{
				if (displayInfo.Font == null)
				{
					displayInfo.Font = e.InheritedRowStyle.Font;
				}
				Rectangle rowBounds = e.RowBounds;
				rowBounds.Height--;
				using (SolidBrush solidBrush = new SolidBrush(displayInfo.BackColor))
				{
					e.Graphics.DrawLine(Pens.SteelBlue, rowBounds.Left, rowBounds.Bottom, rowBounds.Right, rowBounds.Bottom);
					int num = HeaderOffset + 1;
					rowBounds.X = num - m_grid.HorizontalScrollingOffset;
					e.Graphics.FillRectangle(solidBrush, rowBounds);
					using (SolidBrush solidBrush2 = new SolidBrush(displayInfo.ForeColor))
					{
						StringFormat format = new StringFormat
						{
							LineAlignment = StringAlignment.Center
						};
						if (displayInfo.Header != null)
						{
							SizeF sizeF = e.Graphics.MeasureString(displayInfo.Header, displayInfo.Font);
							e.Graphics.DrawString(displayInfo.Header, displayInfo.Font, solidBrush2, rowBounds, format);
							rowBounds.X += (int)sizeF.Width + 5;
						}
						if (displayInfo.DisplayValue != null)
						{
							using (Font font = new Font(displayInfo.Font.FontFamily, displayInfo.Font.Size + 2f, FontStyle.Bold))
							{
								SizeF sizeF = e.Graphics.MeasureString(displayInfo.DisplayValue, font);
								e.Graphics.DrawString(displayInfo.DisplayValue, font, solidBrush2, rowBounds, format);
								rowBounds.X += (int)sizeF.Width + 10;
							}
						}
						if (displayInfo.Summary != null)
						{
							e.Graphics.DrawString(displayInfo.Summary, displayInfo.Font, solidBrush2, rowBounds, format);
						}
					}
					e.Graphics.FillRectangle(solidBrush, 0, rowBounds.Top, num, rowBounds.Height);
				}
				Rectangle collapseBoxBounds = GetCollapseBoxBounds(e.RowBounds.Y);
				if (m_capturedcollapsebox.Y == e.RowIndex)
				{
					e.Graphics.FillEllipse(Brushes.Yellow, collapseBoxBounds);
				}
				e.Graphics.DrawEllipse(m_linepen, collapseBoxBounds);
				bool collapsed = groupRow.Collapsed;
				if (DrawExpandCollapseLines && !collapsed)
				{
					int num2 = HeaderOffset - 5;
					e.Graphics.DrawLine(m_linepen, num2, collapseBoxBounds.Bottom, num2, rowBounds.Bottom);
				}
				collapseBoxBounds.Inflate(-2, -2);
				int num3 = collapseBoxBounds.Y + collapseBoxBounds.Height / 2;
				e.Graphics.DrawLine(m_linepen, collapseBoxBounds.X, num3, collapseBoxBounds.Right, num3);
				if (collapsed)
				{
					int num2 = collapseBoxBounds.X + collapseBoxBounds.Width / 2;
					e.Graphics.DrawLine(m_linepen, num2, collapseBoxBounds.Top, num2, collapseBoxBounds.Bottom);
				}
			}
		}

		private Rectangle GetCollapseBoxBounds(int Y_Offset)
		{
			return new Rectangle(HeaderOffset - 10, Y_Offset + 5, 10, 10);
		}

		public bool CurrentRowIsGroupRow
		{
			get
			{
				return m_grid != null && IsGroupRow(m_grid.CurrentCellAddress.Y);
			}
		}

		[DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
		[DefaultValue(null)]
		public GroupRow CurrentGroup
		{
			get
			{
				return m_source.CurrentGroup;
			}
			set
			{
				m_source.CurrentGroup = value;
			}
		}

		private const int m_collapseboxwidth = 10;
		private const int m_lineoffset = 5;
		private const int m_CollapseBox_Y_Offset = 5;
		private DataGridView m_grid;
		private Point m_capturedcollapsebox = new Point(-1, -1);
		private List<int> m_selectedGroups = new List<int>();
		private bool m_selectionset;
		private readonly GroupingSource m_source = new GroupingSource();
		private Pen m_linepen = Pens.SteelBlue;
	}
}
