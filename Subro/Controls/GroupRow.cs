using System;
using System.Collections;
using System.Collections.Generic;
using System.ComponentModel;
using System.Windows.Forms;

namespace Subro.Controls
{
	public class GroupRow : IEnumerable
	{
		internal GroupRow(GroupList owner)
		{
			Owner = owner;
		}

		public int Index { get; internal set; }

		public int LastIndex
		{
			get
			{
				int result;
				if (m_collapsed)
				{
					result = Index;
				}
				else
				{
					result = Index + Rows.Count;
				}
				return result;
			}
		}

		public int GroupIndex { get; internal set; }

		public object Value
		{
			get
			{
				return value;
			}
		}

		public int Count
		{
			get
			{
				return Rows.Count;
			}
		}

		public bool Collapsed
		{
			get
			{
				return m_collapsed;
			}
			set
			{
				SetCollapsed(value, true);
			}
		}

		internal void SetCollapsed(bool collapse, bool Perform)
		{
			if (m_collapsed != collapse)
			{
				if (!collapse || AllowCollapse)
				{
					m_collapsed = collapse;
					if (Perform)
					{
						int index = Index + 1;
						if (collapse)
						{
							Owner.Rows.RemoveRange(index, Rows.Count);
						}
						else
						{
							Owner.Rows.InsertRange(index, Rows);
						}
						Owner.ReIndex(Owner.IndexOf(this));
						try
						{
							if (Rows.Count > 1)
							{
								Owner.Source.FireBaseReset(true);
							}
							else
							{
								Owner.Source.FireBaseChanged(m_collapsed ? ListChangedType.ItemDeleted : ListChangedType.ItemAdded, index, true);
							}
						}
						catch
						{
						}
					}
				}
			}
		}

		protected virtual bool AllowCollapse
		{
			get
			{
				return true;
			}
		}

		public object this[int Index]
		{
			get
			{
				return Rows[Index];
			}
		}

		public object FirstRow
		{
			get
			{
				object result;
				if (Rows.Count == 0)
				{
					result = null;
				}
				else
				{
					result = Rows[0];
				}
				return result;
			}
		}

		public GroupDisplayEventArgs GetDisplayInfo(bool selected)
		{
			GroupDisplayEventArgs groupDisplayEventArgs = new GroupDisplayEventArgs(this, Owner.Source.GroupOn);
			groupDisplayEventArgs.Selected = selected;
			SetDisplayInfo(groupDisplayEventArgs);
			GroupDisplayEventArgs result;
			if (groupDisplayEventArgs.Cancel)
			{
				result = null;
			}
			else
			{
				Owner.Source.FireDisplayGroup(groupDisplayEventArgs);
				result = groupDisplayEventArgs;
			}
			return result;
		}

		protected virtual void SetDisplayInfo(GroupDisplayEventArgs e)
		{
			DataGridView grid = Owner.Source.Grid;
			if (grid != null)
			{
				e.BackColor = (e.Selected ? grid.DefaultCellStyle.SelectionBackColor : grid.DefaultCellStyle.BackColor);
				e.ForeColor = (e.Selected ? grid.DefaultCellStyle.SelectionForeColor : grid.DefaultCellStyle.ForeColor);
			}
			GroupingOptions options = Owner.Source.Options;
			if (options.ShowCount)
			{
				e.Summary = "(" + Count + ")";
			}
			if (options.ShowGroupName)
			{
				e.Header = e.GroupingInfo.ToString();
			}
			e.GroupingInfo.SetDisplayValues(e);
		}

		public virtual void Remove(object rec)
		{
			if (Rows.Remove(rec))
			{
				bool flag = Count == 0 && AllowRemove;
				int num = Owner.List.IndexOf(this);
				if (flag)
				{
					Owner.Rows.RemoveAt(Index);
					Owner.List.RemoveAt(num);
				}
				Owner.ReIndex(num);
				Owner.Source.FireBaseChanged(flag ? ListChangedType.ItemDeleted : ListChangedType.ItemChanged, Index, true);
			}
		}

		public virtual int Add(object rec)
		{
			int num = Owner.Rows.Add(rec);
			Owner.Source.FireBaseChanged(ListChangedType.ItemAdded, num, false);
			Rows.Add(rec);
			Owner.Source.FireBaseChanged(ListChangedType.ItemChanged, Index, false);
			return num;
		}

		protected virtual bool AllowRemove
		{
			get
			{
				return true;
			}
		}

		IEnumerator IEnumerable.GetEnumerator()
		{
			return Rows.GetEnumerator();
		}

		public readonly GroupList Owner;
		internal object value;
		private bool m_collapsed;
		internal List<object> Rows = new List<object>();
		internal int HashCode;
	}
}
