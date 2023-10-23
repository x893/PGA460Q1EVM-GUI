using System;
using System.Collections;
using System.Collections.Generic;
using System.ComponentModel;
using System.Windows.Forms;

namespace Subro.Controls
{
	[DefaultEvent("GroupingChanged")]
	public class GroupingSource : BindingSource, ICancelAddNew
	{
		public GroupingSource()
		{
		}

		public GroupingSource(object DataSource) : this()
		{
			base.DataSource = DataSource;
		}

		public GroupingSource(object DataSource, string GroupOn) : this(DataSource)
		{
		}

		[DefaultValue(null)]
		public GroupingInfo GroupOn
		{
			get
			{
				return m_groupon;
			}
			set
			{
				if (m_groupon != value)
				{
					if (value == null)
						RemoveGrouping();
					else if (!value.Equals(m_groupon))
						setgroupon(value, Options.AlwaysGroupOnText);
				}
			}
		}

		private void setgroupon(GroupingInfo value, bool forcetext)
		{
			m_info = null;
			if (forcetext && value.GroupValueType != typeof(string))
				value = new StringGroupWrapper(value);
			m_groupon = value;
			OnListChanged(new ListChangedEventArgs(ListChangedType.Reset, -1));
			OnGroupingChanged();
		}

		public bool IsGrouped
		{
			get
			{
				return m_groupon != null;
			}
		}

		public void RemoveGrouping()
		{
			if (m_groupon != null)
			{
				m_groupon = null;
				ResetGroups();
				OnGroupingChanged();
			}
		}

		public bool SetGroupOn(string Property)
		{
			return SetGroupOn(GetProperty(Property));
		}

		public PropertyDescriptor GetProperty(string Name)
		{
			PropertyDescriptor propertyDescriptor = GetItemProperties(null)[Name];
			if (propertyDescriptor == null)
				throw new Exception(Name + " is not a valid property");
			return propertyDescriptor;
		}

		public bool SetGroupOn(PropertyDescriptor p)
		{
			if (p == null)
				throw new ArgumentNullException();
			bool result;
			if (m_groupon == null || !m_groupon.IsProperty(p))
			{
				GroupOn = new PropertyGrouper(p);
				result = true;
			}
			else
				result = false;
			return result;
		}

		public void SetCustomGroup<T>(Func<T, object> GroupValueProvider, string Description = null)
		{
			if (GroupValueProvider == null)
				throw new ArgumentNullException();
			GroupOn = new DelegateGrouper<T>(GroupValueProvider, Description);
		}

		public void SetGroupOnStartLetters(GroupingInfo g, int Letters)
		{
			GroupOn = new StartLetterGrouper(g, Letters);
		}

		public void SetGroupOnStartLetters(string Property, int Letters)
		{
			SetGroupOnStartLetters(GetProperty(Property), Letters);
		}

		public bool IsGroupRow(int Index)
		{
			return m_info != null && Index >= 0 && Index < Count && m_info.Rows[Index] is GroupRow;
		}

		public void CollapseExpandAll(bool collapse)
		{
			if (Groups != null)
			{
				GroupRow currentGroup = CurrentGroup;
				Groups.CollapseExpandAll(collapse);
				if (currentGroup != null)
				{
					try
					{
						CurrentGroup = currentGroup;
					}
					catch { }
				}
			}
		}

		[DefaultValue(SortOrder.Ascending)]
		public SortOrder GroupSortOrder
		{
			get
			{
				SortOrder result;
				if (m_options == null)
					result = SortOrder.Ascending;
				else
					result = m_options.GroupSortOrder;
				return result;
			}
			set
			{
				Options.GroupSortOrder = value;
			}
		}

		[DefaultValue(null)]
		public GroupingOptions Options
		{
			get
			{
				if (m_options == null && !base.DesignMode)
				{
					Options = new GroupingOptions();
				}
				return m_options;
			}
			set
			{
				if (m_options != value)
				{
					SortOrder groupSortOrder = GroupSortOrder;
					if (m_options != null)
					{
						m_options.OptionChanged -= options_OptionChanged;
						groupSortOrder = m_options.GroupSortOrder;
					}
					m_options = value;
					if (m_options != null)
						m_options.OptionChanged += options_OptionChanged;
					if (GroupSortOrder != groupSortOrder)
						sort();
				}
			}
		}

		private void options_OptionChanged(object sender, GroupingOptionChangedEventArgs e)
		{
			if (shouldreset)
			{
				if (e.Option == GroupingOption.GroupSortOrder)
					sort();
				else if (e.Option == GroupingOption.AlwaysGroupOnText)
					setgroupontext();
				else if (e.Option == GroupingOption.StartCollapsed)
					CollapseExpandAll(m_options.StartCollapsed);
				else if (e.Option == GroupingOption.ShowCount || e.Option == GroupingOption.ShowGroupName)
				{
					if (Grid != null)
						InvalidateGridGroupRows();
				}
			}
		}

		private void InvalidateGridGroupRows()
		{
			DataGridView grid = Grid;
			foreach (GroupRow groupRow in ((IEnumerable<GroupRow>)m_info.Groups))
				grid.InvalidateRow(groupRow.Index);
		}

		private void setgroupontext()
		{
			bool flag = m_groupon.GroupValueType == typeof(string);
			if (flag != m_options.AlwaysGroupOnText)
			{
				if (flag)
				{
					if (m_groupon is StringGroupWrapper)
						GroupOn = ((StringGroupWrapper)m_groupon).Grouper;
				}
				else
					setgroupon(m_groupon, true);
			}
		}

		private void sort()
		{
			if (m_info != null)
			{
				if (GroupSortOrder == SortOrder.None)
					reset(false);
				else
					m_info.Sort();
			}
		}

		[DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
		[DefaultValue(null)]
		public GroupRow CurrentGroup
		{
			get
			{
				return GetGroup(base.Position);
			}
			set
			{
				if (value == null)
					throw new ArgumentNullException();
				base.Position = value.Index;
				if (!value.Collapsed)
					base.Position++;
			}
		}

		public GroupRow GetGroup(int RowIndex)
		{
			GroupRow result;
			if (RowIndex == -1 || Groups == null)
				result = null;
			else
				result = Groups.GetByRow(RowIndex);
			return result;
		}

		public GroupList Groups
		{
			get
			{
				return Info.Groups;
			}
		}

		internal void CheckNewRow()
		{
			if (shouldreset)
			{
				m_info.Groups.CheckNewRow(true);
			}
		}

		private GroupingSource.GroupInfo Info
		{
			get
			{
				if (m_info == null)
				{
					m_info = new GroupingSource.GroupInfo(this);
					if (NeedSync)
						SyncCurrencyManagers();
				}
				return m_info;
			}
		}

		private void OnGroupingChanged()
		{
			if (GroupingChanged != null)
				GroupingChanged(this, EventArgs.Empty);
		}

		public event EventHandler GroupingChanged;

		internal DataGridView Grid
		{
			get
			{
				DataGridView result;
				if (GrouperEx == null)
					result = null;
				else
					result = GrouperEx.DataGridView;
				return result;
			}
		}

		public void ResetGroups()
		{
			reset(false);
		}

		private void reset(bool fromlistchange)
		{
			if (m_info != null && !m_resetting)
			{
				int position = base.Position;
				object obj = base.Current;
				DataGridView grid = Grid;
				int? num = (grid == null) ? null : new int?(grid.FirstDisplayedScrollingRowIndex);
				m_resetting = true;
				try
				{
					if (fromlistchange)
					{
						m_info.ReBuild();
					}
					else
					{
						m_info = null;
					}
					base.OnListChanged(new ListChangedEventArgs(ListChangedType.Reset, -1));
					if (position != -1)
					{
						int num2 = (obj is GroupRow) ? position : IndexOf(obj);
						if (num2 == -1)
							num2 = position;
						if (base.Position == num2)
							OnPositionChanged(EventArgs.Empty);
						else
							base.Position = num2;
						if (num != null)
						{
							try
							{
								if (grid.Rows.Count > num.Value && num.Value > -1)
									grid.FirstDisplayedScrollingRowIndex = num.Value;
							}
							catch { }
						}
					}
				}
				finally
				{
					m_resetting = false;
					if (NeedSync)
					{
						SyncCurrencyManagers();
					}
				}
			}
		}

		internal void FireBaseReset(bool PreserveScrollPosition)
		{
			FireBaseChanged(new ListChangedEventArgs(ListChangedType.Reset, -1), PreserveScrollPosition);
		}

		internal void FireBaseChanged(ListChangedType type, int index, bool PreserveScrollPosition)
		{
			FireBaseChanged(new ListChangedEventArgs(type, index), PreserveScrollPosition);
		}

		internal void FireBaseChanged(ListChangedEventArgs e, bool PreserveScrollPosition)
		{
			int num = -1;
			PreserveScrollPosition &= (Grid != null);
			if (PreserveScrollPosition)
				num = Grid.FirstDisplayedScrollingRowIndex;
			base.OnListChanged(e);
			if (num > 0)
			{
				try
				{
					Grid.FirstDisplayedScrollingRowIndex = num;
				}
				catch { }
			}
		}

		public event EventHandler<GroupDisplayEventArgs> DisplayGroup;

		internal void FireDisplayGroup(GroupDisplayEventArgs e)
		{
			if (DisplayGroup != null)
				DisplayGroup(this, e);
		}

		private void UnwireCurMan()
		{
			if (m_cm != null)
				m_cm.CurrentChanged -= bsource_CurrentChanged;
		}

		protected override void Dispose(bool disposing)
		{
			UnwireCurMan();
			m_groupon = null;
			base.Dispose(disposing);
		}

		protected override void OnDataSourceChanged(EventArgs e)
		{
			UnwireCurMan();
			ResetGroups();
			object dataSource = base.DataSource;
			if (dataSource is ICurrencyManagerProvider)
				m_cm = ((ICurrencyManagerProvider)dataSource).CurrencyManager;
			if (m_cm != null)
			{
				m_cm.CurrentChanged += bsource_CurrentChanged;
				if (NeedSync)
					SyncCurrencyManagers();
			}
			base.OnDataSourceChanged(e);
		}

		protected override void OnListChanged(ListChangedEventArgs e)
		{
			if (m_suspendlistchange <= 0 && !m_resetting)
			{
				if (shouldreset)
				{
					switch (e.ListChangedType)
					{
					case ListChangedType.Reset:
						reset(true);
						return;
					case ListChangedType.ItemAdded:
						if (m_info.Groups.HasNewRow)
							m_info.Groups.AddNew(base.List[e.NewIndex], true);
						else
							reset(true);
						return;
					case ListChangedType.ItemDeleted:
						reset(true);
						return;
					case ListChangedType.ItemMoved:
						reset(true);
						return;
					case ListChangedType.ItemChanged:
						if (m_groupon.IsProperty(e.PropertyDescriptor) && !m_info.Groups.IsNewRow(e.NewIndex))
							reset(true);
						else
							FireBaseChanged(new ListChangedEventArgs(ListChangedType.ItemChanged, IndexOf(base.List[e.NewIndex]), e.PropertyDescriptor), false);
						return;
					}
				}
				switch (e.ListChangedType)
				{
				case ListChangedType.PropertyDescriptorAdded:
				case ListChangedType.PropertyDescriptorDeleted:
				case ListChangedType.PropertyDescriptorChanged:
					m_props = null;
					break;
				}
				base.OnListChanged(e);
			}
		}

		private bool shouldreset
		{
			get
			{
				return m_info != null && m_info.Groups != null;
			}
		}

		public override object AddNew()
		{
			object result;
			if (!shouldreset)
				result = base.AddNew();
			else
			{
				m_suspendlistchange++;
				object obj;
				int position;
				try
				{
					obj = base.AddNew();
					position = m_info.Groups.AddNew(obj, false);
				}
				finally
				{
					m_suspendlistchange--;
				}
				base.Position = position;
				result = obj;
			}
			return result;
		}

		public override void ApplySort(PropertyDescriptor property, ListSortDirection sort)
		{
			if (property is GroupingSource.PropertyWrapper)
				property = (property as GroupingSource.PropertyWrapper).Property;
			base.ApplySort(property, sort);
		}

		public override void ApplySort(ListSortDescriptionCollection sorts)
		{
			base.ApplySort(sorts);
		}

		public override void RemoveAt(int index)
		{
			if (!shouldreset)
			{
				base.RemoveAt(index);
			}
			else if (!IsGroupRow(index))
			{
				m_suspendlistchange++;
				try
				{
					object obj = this[index];
					int num = base.List.IndexOf(obj);
					if (num != -1)
						base.List.RemoveAt(num);
					m_info.Rows.RemoveAt(index);
					base.OnListChanged(new ListChangedEventArgs(ListChangedType.ItemDeleted, index));
					GroupRow group = GetGroup(index);
					if (group != null)
						group.Remove(obj);
				}
				finally
				{
					m_suspendlistchange--;
				}
			}
		}

		public override void Remove(object value)
		{
			if (!(value is GroupRow))
			{
				int num = IndexOf(value);
				if (num != -1)
				{
					RemoveAt(num);
				}
			}
		}

		void ICancelAddNew.CancelNew(int pos)
		{
			if (shouldreset && m_info.Groups.IsNewRow(pos))
			{
				ICancelAddNew cancelAddNew = base.List as ICancelAddNew;
				if (cancelAddNew != null)
				{
					m_suspendlistchange++;
					try
					{
						int itemIndex = base.List.IndexOf(this[pos]);
						cancelAddNew.CancelNew(itemIndex);
					}
					finally
					{
						m_suspendlistchange--;
					}
				}
				RemoveAt(pos);
			}
		}

		protected override void OnCurrentChanged(EventArgs e)
		{
			base.OnCurrentChanged(e);
			if (NeedSync)
			{
				object obj = base.Current;
				while (obj is GroupRow)
				{
					obj = ((GroupRow)obj).FirstRow;
					if (obj == m_cm.Current)
						return;
				}
				m_suspendsync = true;
				try
				{
					m_cm.Position = m_cm.List.IndexOf(obj);
				}
				finally
				{
					m_suspendsync = false;
				}
			}
		}

		private void bsource_CurrentChanged(object sender, EventArgs e)
		{
			if (NeedSync)
			{
				SyncCurrencyManagers();
			}
		}

		private bool NeedSync
		{
			get
			{
				bool result;
				if (m_cm == null || m_suspendlistchange > 0 || m_suspendsync || m_cm.Count == 0)
					result = false;
				else
				{
					int position = base.Position;
					result = (position != m_cm.Position || (position != -1 && base.Current != m_cm.Current));
				}
				return result;
			}
		}

		private void SyncCurrencyManagers()
		{
			m_suspendsync = true;
			try
			{
				if (m_cm.Count > 0)
				{
					base.Position = IndexOf(m_cm.Current);
				}
			}
			finally
			{
				m_suspendsync = false;
			}
		}

		public override int IndexOf(object value)
		{
			return Info.Rows.IndexOf(value);
		}

		public override bool Contains(object value)
		{
			return Info.Rows.Contains(value);
		}

		public override void Clear()
		{
			m_suspendlistchange++;
			try
			{
				base.Clear();
				if (shouldreset)
				{
					m_info.Groups.Fill();
				}
				FireBaseReset(false);
			}
			finally
			{
				m_suspendlistchange--;
			}
		}

		public override int Add(object value)
		{
			return base.Add(value);
		}

		public override void Insert(int index, object value)
		{
			base.Insert(index, value);
		}

		public override PropertyDescriptorCollection GetItemProperties(PropertyDescriptor[] listAccessors)
		{
			PropertyDescriptorCollection itemProperties;
			if (listAccessors == null)
			{
				if (m_props == null)
				{
					m_props = base.GetItemProperties(null);
					if (m_props == null)
						return null;

					PropertyDescriptor[] array = new PropertyDescriptor[m_props.Count];
					for (int i = 0; i < m_props.Count; i++)
						array[i] = new GroupingSource.PropertyWrapper(m_props[i], this);
					m_props = new PropertyDescriptorCollection(array);
				}
				itemProperties = m_props;
			}
			else
			{
				itemProperties = base.GetItemProperties(listAccessors);
			}
			return itemProperties;
		}

		public int BaseCount
		{
			get
			{
				return base.List.Count;
			}
		}

		public object GetBaseRow(int Index)
		{
			return base.List[Index];
		}

		public override int Count
		{
			get
			{
				return Info.Rows.Count;
			}
		}

		public override object this[int index]
		{
			get
			{
				return Info.Rows[index];
			}
			set
			{
				Info.Rows[index] = value;
			}
		}

		private GroupingInfo m_groupon;
		internal DataGridViewGrouper GrouperEx;
		private GroupingOptions m_options;
		private GroupingSource.GroupInfo m_info;
		private bool m_resetting;
		private CurrencyManager m_cm;
		private int m_suspendlistchange;
		private bool m_suspendsync;
		private PropertyDescriptorCollection m_props;

		private class NullValue
		{
			public override string ToString()
			{
				return "<Null>";
			}

			public static readonly GroupingSource.NullValue Instance = new GroupingSource.NullValue();
		}

		private class GroupInfo
		{
			public GroupInfo(GroupingSource owner)
			{
				Owner = owner;
				set();
			}

			private void set()
			{
				Groups = null;
				if (Owner.m_groupon == null)
				{
					Rows = Owner.List;
				}
				else
				{
					Groups = new GroupList(Owner);
					Rows = Groups.Fill();
				}
			}

			public void ReBuild()
			{
				if (Groups == null)
					set();
				else
					Groups.Fill();
			}

			public void Sort()
			{
				if (Groups != null)
					Groups.Sort(Owner.GroupSortOrder);
			}

			public readonly GroupingSource Owner;
			public IList Rows;
			public GroupList Groups;
		}

		public class PropertyWrapper : PropertyDescriptor
		{
			public PropertyWrapper(PropertyDescriptor property, GroupingSource owner) : base(property)
			{
				Property = property;
				Owner = owner;
			}

			public override bool CanResetValue(object component)
			{
				return Property.CanResetValue(component);
			}

			public override Type ComponentType
			{
				get
				{
					return Property.ComponentType;
				}
			}

			public override object GetValue(object component)
			{
				object result;
				if (component is GroupRow)
				{
					if (Owner.m_groupon.IsProperty(Property))
						result = (component as GroupRow).Value;
					else
						result = null;
				}
				else
				{
					result = Property.GetValue(component);
				}
				return result;
			}

			public override bool IsReadOnly
			{
				get
				{
					return Property.IsReadOnly;
				}
			}

			public override Type PropertyType
			{
				get
				{
					return Property.PropertyType;
				}
			}

			public override string Category
			{
				get
				{
					return Property.Category;
				}
			}

			public override string Description
			{
				get
				{
					return Property.Description;
				}
			}

			public override string DisplayName
			{
				get
				{
					return Property.DisplayName;
				}
			}

			public override bool DesignTimeOnly
			{
				get
				{
					return Property.DesignTimeOnly;
				}
			}

			public override AttributeCollection Attributes
			{
				get
				{
					return Property.Attributes;
				}
			}

			public override string Name
			{
				get
				{
					return Property.Name;
				}
			}

			public override void ResetValue(object component)
			{
				Property.ResetValue(component);
			}

			public override void SetValue(object component, object value)
			{
				Property.SetValue(component, value);
			}

			public override bool ShouldSerializeValue(object component)
			{
				return Property.ShouldSerializeValue(component);
			}

			public readonly PropertyDescriptor Property;
			public readonly GroupingSource Owner;
		}
	}
}
