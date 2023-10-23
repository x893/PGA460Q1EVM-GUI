using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Drawing;
using System.Windows.Forms;

namespace Subro.Controls
{
	public class DataGridViewGrouperContextMenuStrip : ContextMenuStrip
	{
		public DataGridViewGrouperContextMenuStrip(DataGridViewGrouper grouper) : this()
		{
			Grouper = grouper;
		}

		public DataGridViewGrouperContextMenuStrip()
		{
			CollapseAllItem = Add("CollapseAll", "Collapse all", new EventHandler(collapse));
			ExpandAllItem = Add("ExpandAll", "Expand all", new EventHandler(expand));
			GroupOnMenuItem = Add("Grouping style", null);
			GroupOnMenuItem.DropDown.ItemClicked += GroupOnDropDown_ItemClicked;
			AddGroupOnItem<StringGroupWrapper>("Force as text", () => new StringGroupWrapper(m_grouper.GroupOn));
			AddGroupOnItem<StartLetterGrouper>("First letter", () => new StartLetterGrouper(m_grouper.GroupOn));
			AddGroupOnItem<FirstWordGrouper>("First word", () => new FirstWordGrouper(m_grouper.GroupOn));
			AddGroupOnItem<LastWordGrouper>("Last word", () => new LastWordGrouper(m_grouper.GroupOn));
			SortMenuItem = Add("Sort groups", null);
			foreach (object obj in Enum.GetValues(typeof(SortOrder)))
			{
				SortOrder s = (SortOrder)obj;
				AddSortItem(s);
			}
			SortMenuItem.DropDown.ItemClicked += SortDropDown_ItemClicked;
			OptionsMenuItem = Add("OtherOptions", "Other Options", null);
			AddOption("Start collapsed", GroupingOption.StartCollapsed);
			AddOption("Always group on text value", GroupingOption.AlwaysGroupOnText);
			OptionsMenuItem.DropDownItems.Add(new ToolStripSeparator());
			AddOption("Show row counts", GroupingOption.ShowCount);
			AddOption("Show group field names", GroupingOption.ShowGroupName);
			AddOption("Select rows if double clicked on header", GroupingOption.SelectRowsOnDoubleClick);
			Items.Add(new ToolStripSeparator());
			OverViewMenuItem = Add("JumpGroup", "Jump to ...", new EventHandler(jumptogroup));
			Items.Add(new ToolStripSeparator());
			RemoveGroupingItem = Add("RemoveGroup", "Remove grouping", new EventHandler(RemoveGrouping));
		}

		private void SortDropDown_ItemClicked(object sender, ToolStripItemClickedEventArgs e)
		{
			DataGridViewGrouperContextMenuStrip.SortItem sortItem = e.ClickedItem as DataGridViewGrouperContextMenuStrip.SortItem;
			if (sortItem != null)
			{
				m_grouper.Options.GroupSortOrder = sortItem.SortOrder;
			}
		}

		private void GroupOnDropDown_ItemClicked(object sender, ToolStripItemClickedEventArgs e)
		{
			DataGridViewGrouperContextMenuStrip.GroupOnItem groupOnItem = e.ClickedItem as DataGridViewGrouperContextMenuStrip.GroupOnItem;
			if (groupOnItem != null)
			{
				GroupingInfo groupOn = m_grouper.GroupOn;
				if (groupOnItem.EqualsInfo(groupOn))
				{
					if (groupOn is GroupWrapper)
					{
						m_grouper.GroupOn = ((GroupWrapper)groupOn).Grouper;
					}
					groupOnItem.Checked = false;
				}
				else
				{
					m_grouper.GroupOn = groupOnItem.CreateInfo();
					groupOnItem.Checked = true;
				}
			}
		}

		private void jumptogroup(object sender, EventArgs e)
		{
			DataGridViewGrouperContextMenuStrip.FormJumpTo formJumpTo = new DataGridViewGrouperContextMenuStrip.FormJumpTo(Grouper);
			formJumpTo.Show(this);
		}

		public DataGridViewGrouper Grouper
		{
			get
			{
				return m_grouper;
			}
			set
			{
				m_grouper = value;
			}
		}

		protected override void OnOpening(CancelEventArgs e)
		{
			Initialize();
			base.OnOpening(e);
		}

		public void Initialize()
		{
			bool flag = m_grouper != null;
			bool flag2 = flag && m_grouper.IsGrouped;
			CollapseAllItem.Enabled = flag2;
			ExpandAllItem.Enabled = flag2;
			RemoveGroupingItem.Enabled = flag2;
			GroupOnMenuItem.Enabled = flag2;
			OverViewMenuItem.Enabled = flag2;
			SortMenuItem.Enabled = flag;
			OptionsMenuItem.Enabled = flag;
			if (flag)
			{
				SortOrder groupSortOrder = m_grouper.Options.GroupSortOrder;
				foreach (DataGridViewGrouperContextMenuStrip.SortItem sortItem in GetSortItems())
				{
					sortItem.Checked = (sortItem.SortOrder == groupSortOrder);
				}
				foreach (object obj in OptionsMenuItem.DropDownItems)
				{
					if (obj is DataGridViewGrouperContextMenuStrip.booloption)
					{
						((DataGridViewGrouperContextMenuStrip.booloption)obj).Init();
					}
				}
			}
			if (flag2)
			{
				GroupingInfo groupOn = m_grouper.GroupOn;
				foreach (DataGridViewGrouperContextMenuStrip.GroupOnItem groupOnItem in GetGroupOnItems())
				{
					groupOnItem.Checked = groupOnItem.EqualsInfo(groupOn);
				}
			}
		}

		private ToolStripMenuItem Add(string txt)
		{
			return Add(txt, null);
		}

		private ToolStripMenuItem Add(string txt, EventHandler onClick)
		{
			return Add(null, txt, onClick);
		}

		private ToolStripMenuItem Add(string kw, string txt, EventHandler onClick)
		{
			if (kw != null)
			{
			}
			return Add(txt, onClick, Items);
		}

		private ToolStripMenuItem Add(string txt, EventHandler onClick, ToolStripItemCollection Items)
		{
			ToolStripMenuItem toolStripMenuItem = new ToolStripMenuItem(txt, null, onClick);
			Items.Add(toolStripMenuItem);
			return toolStripMenuItem;
		}

		private DataGridViewGrouperContextMenuStrip.booloption AddOption(string txt, GroupingOption o)
		{
			DataGridViewGrouperContextMenuStrip.booloption booloption = new DataGridViewGrouperContextMenuStrip.booloption(o);
			booloption.Text = txt;
			booloption.Strip = this;
			OptionsMenuItem.DropDownItems.Add(booloption);
			return booloption;
		}

		private void expand(object sender, EventArgs e)
		{
			Grouper.GroupingSource.CollapseExpandAll(false);
		}

		private void collapse(object sender, EventArgs e)
		{
			Grouper.GroupingSource.CollapseExpandAll(true);
		}

		private void RemoveGrouping(object sender, EventArgs e)
		{
			Grouper.GroupOn = null;
		}

		public ToolStripMenuItem AddGroupOnItem<T>(string Text, Func<T> Creator) where T : GroupingInfo
		{
			DataGridViewGrouperContextMenuStrip.GroupOnItem<T> groupOnItem = new DataGridViewGrouperContextMenuStrip.GroupOnItem<T>();
			groupOnItem.Text = Text;
			groupOnItem.CreateInfoDelegate = Creator;
			GroupOnMenuItem.DropDownItems.Add(groupOnItem);
			return groupOnItem;
		}

		public IEnumerable<DataGridViewGrouperContextMenuStrip.GroupOnItem> GetGroupOnItems()
		{
			foreach (object item in GroupOnMenuItem.DropDownItems)
			{
				if (item is DataGridViewGrouperContextMenuStrip.GroupOnItem)
				{
					yield return (DataGridViewGrouperContextMenuStrip.GroupOnItem)item;
				}
			}
			yield break;
		}

		public IEnumerable<DataGridViewGrouperContextMenuStrip.SortItem> GetSortItems()
		{
			foreach (object item in SortMenuItem.DropDownItems)
			{
				if (item is DataGridViewGrouperContextMenuStrip.SortItem)
				{
					yield return (DataGridViewGrouperContextMenuStrip.SortItem)item;
				}
			}
			yield break;
		}

		private DataGridViewGrouperContextMenuStrip.SortItem AddSortItem(SortOrder s)
		{
			DataGridViewGrouperContextMenuStrip.SortItem sortItem = new DataGridViewGrouperContextMenuStrip.SortItem(s);
			SortMenuItem.DropDownItems.Add(sortItem);
			return sortItem;
		}

		public readonly ToolStripMenuItem CollapseAllItem;
		public readonly ToolStripMenuItem ExpandAllItem;
		public readonly ToolStripMenuItem RemoveGroupingItem;
		public readonly ToolStripMenuItem GroupOnMenuItem;
		public readonly ToolStripMenuItem OverViewMenuItem;
		public readonly ToolStripMenuItem SortMenuItem;
		public readonly ToolStripMenuItem OptionsMenuItem;
		private DataGridViewGrouper m_grouper;
		private class FormJumpTo : Form
		{
			public FormJumpTo(DataGridViewGrouper grouper)
			{
				Grouper = grouper;
				m_grouperGrid = Grouper.DataGridView;
				m_dg.AutoGenerateColumns = false;
				m_dg.Columns.Add(new DataGridViewTextBoxColumn
				{
					DataPropertyName = "Value",
					HeaderText = "Group",
					AutoSizeMode = DataGridViewAutoSizeColumnMode.Fill,
					ReadOnly = true
				});
				m_dg.Columns.Add(new DataGridViewTextBoxColumn
				{
					DataPropertyName = "Count",
					Width = 60,
					ReadOnly = true
				});
				m_dg.Columns.Add(new DataGridViewCheckBoxColumn
				{
					DataPropertyName = "Collapsed",
					HeaderText = "Collapse",
					Width = 60
				});
				m_dg.AllowUserToAddRows = false;
				m_dg.AllowUserToDeleteRows = false;
				base.ClientSize = new Size(400, 400);
				m_dg.Dock = DockStyle.Fill;
				base.Controls.Add(m_dg);
				base.Controls.Add(new DataGridSearchBox
				{
					DataGridView = m_dg,
					Dock = DockStyle.Top,
					ShowOptionsButton = true
				});
				m_dg.CreateControl();
			}

			private void setdata()
			{
				GroupList groups = Grouper.GroupingSource.Groups;
				GroupRow[] array = (groups == null) ? null : groups.ToArray();
				m_settingcur = true;
				try
				{
					m_dg.DataSource = array;
				}
				finally
				{
					m_settingcur = false;
				}
				m_dg.Enabled = (array != null);
				syncwithdg();
			}

			protected override void OnClosing(CancelEventArgs e)
			{
				Grouper.GroupingChanged -= Grouper_GroupingChanged;
				m_grouperGrid.CurrentCellChanged -= GrouperGrid_CurrentCellChanged;
				base.OnClosing(e);
			}

			protected override void OnLoad(EventArgs e)
			{
				base.OnLoad(e);
				m_dg.CurrentCellChanged += dg_CurrentCellChanged;
				m_dg.CellDoubleClick += dg_CellDoubleClick;
				m_grouperGrid.CurrentCellChanged += GrouperGrid_CurrentCellChanged;
				Grouper.GroupingChanged += Grouper_GroupingChanged;
				setdata();
			}

			private void Grouper_GroupingChanged(object sender, EventArgs e)
			{
				setdata();
			}

			private void GrouperGrid_CurrentCellChanged(object sender, EventArgs e)
			{
				syncwithdg();
			}

			private void dg_CellDoubleClick(object sender, DataGridViewCellEventArgs e)
			{
				SelectCurrent();
				base.DialogResult = DialogResult.OK;
				base.Close();
			}

			private void syncwithdg()
			{
				if (!m_settingcur)
				{
					GroupRow currentGroup = Grouper.GroupingSource.CurrentGroup;
					if (currentGroup != null)
					{
						int num = Grouper.GroupingSource.Groups.IndexOf(currentGroup);
						if (m_dg.CurrentCellAddress.Y != num)
						{
							m_settingcur = true;
							try
							{
								m_dg.CurrentCell = m_dg[0, num];
							}
							catch
							{
							}
							finally
							{
								m_settingcur = false;
							}
						}
					}
				}
			}

			private void dg_CurrentCellChanged(object sender, EventArgs e)
			{
				SelectCurrent();
			}
			
			public void SelectCurrent()
			{
				if (!m_settingcur)
				{
					m_settingcur = true;
					try
					{
						Grouper.GroupingSource.CurrentGroup = Current;
					}
					finally
					{
						m_settingcur = false;
					}
				}
			}

			public GroupRow Current
			{
				get
				{
					return (GroupRow)m_dg.CurrentRow.DataBoundItem;
				}
			}

			public readonly DataGridViewGrouper Grouper;
			private DataGridView m_grouperGrid;
			private DataGridView m_dg = new DataGridView();
			private bool m_settingcur;
		}

		public abstract class GroupOnItem : ToolStripMenuItem
		{
			public virtual GroupingInfo CreateInfo()
			{
				return (GroupingInfo)Activator.CreateInstance(GroupInfoType);
			}

			public abstract Type GroupInfoType { get; }

			public virtual bool EqualsInfo(GroupingInfo g)
			{
				return GroupInfoType.IsAssignableFrom(g.GetType());
			}
		}

		public class GroupOnItem<T> : DataGridViewGrouperContextMenuStrip.GroupOnItem where T : GroupingInfo
		{
			public override Type GroupInfoType
			{
				get
				{
					return typeof(T);
				}
			}

			public override GroupingInfo CreateInfo()
			{
				GroupingInfo result;
				if (CreateInfoDelegate == null)
				{
					result = base.CreateInfo();
				}
				else
				{
					result = CreateInfoDelegate();
				}
				return result;
			}

			public override bool EqualsInfo(GroupingInfo g)
			{
				return g is T;
			}

			public Func<T> CreateInfoDelegate;
		}

		public class SortItem : ToolStripMenuItem
		{
			public SortItem(SortOrder sortOrder)
			{
				SortOrder = sortOrder;
				Text = SortOrder.ToString();
			}

			public readonly SortOrder SortOrder;
		}

		private class booloption : ToolStripMenuItem
		{
			public booloption(GroupingOption option)
			{
				Option = option;
			}

			private GroupingOptionValue<bool> GetOption()
			{
				return (GroupingOptionValue<bool>)Strip.m_grouper.Options[Option];
			}

			protected override void OnClick(EventArgs e)
			{
				base.OnClick(e);
				GroupingOptionValue<bool> option = GetOption();
				base.Checked = !base.Checked;
				option.Value = base.Checked;
			}

			public void Init()
			{
				base.Checked = GetOption().Value;
			}

			public readonly GroupingOption Option;
			internal DataGridViewGrouperContextMenuStrip Strip;
		}
	}
}
