using System;
using System.Collections;
using System.Collections.Generic;
using System.ComponentModel;
using System.Windows.Forms;

namespace Subro.Controls
{
	public class GroupList : IEnumerable<GroupRow>, IEnumerable
	{
		public GroupList(GroupingSource source)
		{
			Source = source;
			m_gi = source.GroupOn;
			GroupValueType = m_gi.GroupValueType;
		}

		internal IList Fill()
		{
			GroupingOptions options = Source.Options;
			bool startCollapsed = options.StartCollapsed;
			bool flag = m_allgroups.Count > 0;
			if (flag)
				foreach (GroupRow groupRow in m_allgroups)
					groupRow.Rows.Clear();

			List.Clear();
			if (m_newrows != null)
				m_newrows.Rows.Clear();

			foreach (object obj in Source.List)
			{
				object groupValue = m_gi.GetGroupValue(obj);
				int num = (groupValue == null) ? 0 : groupValue.GetHashCode();
				GroupRow groupRow2 = null;
				for (int i = 0; i < m_allgroups.Count; i++)
				{
					if (m_allgroups[i].HashCode == num && (groupValue == null || groupValue.Equals(m_allgroups[i].value)))
					{
						groupRow2 = m_allgroups[i];
						break;
					}
				}
				if (groupRow2 == null)
				{
					groupRow2 = new GroupRow(this);
					groupRow2.value = groupValue;
					groupRow2.HashCode = num;
					if (startCollapsed)
						groupRow2.SetCollapsed(true, false);
					m_allgroups.Add(groupRow2);
				}
				groupRow2.Rows.Add(obj);
			}
			if (flag)
				foreach (GroupRow groupRow in m_allgroups)
					if (groupRow.Count > 0)
						List.Add(groupRow);
			else
				List.AddRange(m_allgroups);

			sort(Source.GroupSortOrder, false);
			if (Rows == null)
				Rows = new ArrayList(List.Count + Source.BaseCount);
			else
				Rows.Clear();

			if (startCollapsed && !flag)
				AddGroupsOnly();
			else
				RebuildRows();

			CheckNewRow(false);
			return Rows;
		}

		private int compare(GroupRow g1, GroupRow g2)
		{
			return m_comparer.Compare(g1.value, g2.value);
		}

		internal void RebuildRows()
		{
			int num = 0;
			foreach (GroupRow groupRow in List)
			{
				groupRow.Index = Rows.Count;
				groupRow.GroupIndex = num++;
				Rows.Add(groupRow);
				if (!groupRow.Collapsed)
					foreach (object value in groupRow.Rows)
						Rows.Add(value);
			}
		}

		internal void AddGroupsOnly()
		{
			Rows.AddRange(List);
			ReIndex(0);
		}

		internal void ReIndex(int From)
		{
			int index = (From == 0) ? 0 : (List[From - 1].LastIndex + 1);
			for (int i = From; i < List.Count; i++)
			{
				List[i].Index = index;
				List[i].GroupIndex = i;
				index = List[i].LastIndex + 1;
			}
		}

		public GroupRow[] ToArray()
		{
			return List.ToArray();
		}

		public int Count
		{
			get
			{
				return List.Count;
			}
		}

		public GroupRow this[int Index]
		{
			get
			{
				return List[Index];
			}
		}

		public int IndexOf(GroupRow row)
		{
			return List.IndexOf(row);
		}

		IEnumerator IEnumerable.GetEnumerator()
		{
			return List.GetEnumerator();
		}

		IEnumerator<GroupRow> IEnumerable<GroupRow>.GetEnumerator()
		{
			return List.GetEnumerator();
		}

		public GroupRow GetByRow(int RowIndex)
		{
			GroupRow result = null;
			foreach (GroupRow groupRow in List)
			{
				if (groupRow.Index > RowIndex)
					break;
				result = groupRow;
			}
			return result;
		}

		public void CollapseExpandAll(bool collapse)
		{
			foreach (GroupRow groupRow in List)
				groupRow.SetCollapsed(collapse, false);

			Rows.Clear();
			if (collapse)
				AddGroupsOnly();
			else
				RebuildRows();

			Source.FireBaseReset(false);
		}

		private void sort(SortOrder order, bool Rebuild)
		{
			if (order != SortOrder.None)
			{
				GroupRow groupRow = Rebuild ? Source.GetGroup(Source.Position) : null;
				if (m_comparer == null)
					m_comparer = new GenericComparer(GroupValueType);

				m_comparer.Descending = (order == SortOrder.Descending);
				List.Sort(new Comparison<GroupRow>(compare));
				if (Rebuild)
				{
					Rows.Clear();
					RebuildRows();
					Source.FireBaseReset(false);
					if (groupRow != null)
					{
						Source.Position = groupRow.Index;
						if (!groupRow.Collapsed)
							Source.Position++;
					}
				}
			}
		}

		public void Sort(SortOrder sortOrder)
		{
			sort(sortOrder, true);
		}

		internal int AddNew(object res, bool GroupOnly)
		{
			int result;
			if (m_newrows == null)
			{
				int num = Rows.Add(res);
				Source.FireBaseChanged(ListChangedType.ItemAdded, num, false);
				result = num;
			}
			else
				result = m_newrows.Add(res);

			return result;
		}

		internal bool HasNewRow
		{
			get
			{
				return m_newrows != null;
			}
		}

		internal bool IsNewRow(int pos)
		{
			return m_newrows != null && pos > m_newrows.Index;
		}

		internal void CheckNewRow(bool FireChanged)
		{
			DataGridView grid = Source.Grid;
			bool flag = grid != null && grid.AllowUserToAddRows;
			int num = (FireChanged && m_newrows != null) ? Rows.IndexOf(m_newrows) : -1;
			if (flag)
			{
				if (num == -1)
				{
					if (m_newrows == null)
						m_newrows = new GroupList.NewRowsGroup(this);

					m_newrows.Index = Rows.Count;
					List.Add(m_newrows);
					Rows.Add(m_newrows);
					if (FireChanged)
						Source.FireBaseChanged(ListChangedType.ItemAdded, m_newrows.Index, true);
				}
			}
			else if (num != -1)
			{
				Rows.RemoveAt(num);
				if (m_newrows.Count == 0)
					Source.FireBaseChanged(ListChangedType.ItemDeleted, num, true);
				else
				{
					m_newrows.Rows.Clear();
					Fill();
					Source.FireBaseReset(true);
				}
			}
		}

		private GroupingInfo m_gi;
		public readonly GroupingSource Source;
		public readonly Type GroupValueType;
		private GenericComparer m_comparer;
		internal List<GroupRow> List = new List<GroupRow>();
		private List<GroupRow> m_allgroups = new List<GroupRow>();
		internal ArrayList Rows;
		private GroupList.NewRowsGroup m_newrows;

		private class NewRowsGroup : GroupRow
		{
			public NewRowsGroup(GroupList list) : base(list)
			{
			}

			protected override void SetDisplayInfo(GroupDisplayEventArgs e)
			{
				base.SetDisplayInfo(e);
				e.Header = "New Rows";
				e.DisplayValue = null;
			}

			protected override bool AllowRemove
			{
				get
				{
					return false;
				}
			}

			protected override bool AllowCollapse
			{
				get
				{
					return false;
				}
			}
		}
	}
}
