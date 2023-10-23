using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Drawing;
using System.Linq;
using System.Windows.Forms;

namespace Subro.Controls
{
    public class DataGridSearchBox : SourceSearchBox
    {
        [Description("The DataGridView coupled to this searchBox. When the grid is readonly, keystrokes of the grid itself are handled as well")]
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
                        m_grid.DataSourceChanged -= grid_DataSourceChanged;
                        m_grid.CurrentCellChanged -= grid_CurrentCellChanged;
                        m_grid.ColumnAdded -= grid_ColumnsChanged;
                        m_grid.ColumnRemoved -= grid_ColumnsChanged;
                        m_grid.ColumnStateChanged -= grid_ColumnStateChanged;
                        base.UnRegisterControl(m_grid);
                    }
                    m_grid = value;
                    if (m_grid != null)
                    {
                        m_grid.DataSourceChanged += grid_DataSourceChanged;
                        m_grid.CurrentCellChanged += grid_CurrentCellChanged;
                        m_grid.ColumnAdded += grid_ColumnsChanged;
                        m_grid.ColumnRemoved += grid_ColumnsChanged;
                        m_grid.ColumnStateChanged += grid_ColumnStateChanged;
                        base.RegisterControl(m_grid);
                    }
                    base.SourceChanged();
                }
            }
        }

        protected override bool CheckIsReady()
        {
            bool result;
            if (m_grid == null)
            {
                result = false;
            }
            else if (m_grid.Columns.Count == 0)
            {
                result = false;
            }
            else
            {
                if (base.SearchProperty == null)
                {
                    if (SearchModeColumn != ColumnSearchMode.AllColumns && base.Mode != SearchBoxMode.Filter)
                    {
                        return false;
                    }
                }
                result = true;
            }
            return result;
        }

        private void grid_ColumnStateChanged(object sender, DataGridViewColumnStateChangedEventArgs e)
        {
            if (e.StateChanged == DataGridViewElementStates.Visible)
            {
                base.ResetColumns();
            }
        }

        private void grid_ColumnsChanged(object sender, DataGridViewColumnEventArgs e)
        {
            base.ResetColumns();
            if (!base.IsValid)
            {
                base.NotifyStateChanged(true);
            }
        }

        protected override int RecordCount
        {
            get
            {
                int result;
                if (m_grid == null)
                {
                    result = 0;
                }
                else
                {
                    result = m_grid.Rows.Count;
                }
                return result;
            }
        }

        protected override Point GetCurrentPosition()
        {
            Point result;
            if (m_grid == null)
            {
                result = Point.Empty;
            }
            else
            {
                result = m_grid.CurrentCellAddress;
            }
            return result;
        }

        protected override void SetPosition(int col, int row)
        {
            m_grid.CurrentCell = m_grid[col, row];
        }

        protected override void OnDisposed()
        {
            DataGridView = null;
        }

        protected override IEnumerable<Column> GetColumns()
        {
            IEnumerable<Column> result;
            if (m_grid == null)
            {
                result = null;
            }
            else
            {
                result = from DataGridViewColumn c in m_grid.Columns
                         where c.Visible
                         orderby c.DisplayIndex
                         select new Column(c.DataPropertyName, (int i) => m_grid[c.Index, i].Value)
                         {
                             Header = c.HeaderText,
                             Index = new int?(c.Index)
                         };
            }
            return result;
        }

        private void grid_DataSourceChanged(object sender, EventArgs e)
        {
            base.SourceChanged();
        }

        private void grid_CurrentCellChanged(object sender, EventArgs e)
        {
            if (!base.IsBusy)
            {
                setcurrentprop();
            }
        }

        private IBindingListView BindingListView
        {
            get
            {
                IBindingListView result;
                if (m_grid == null)
                {
                    result = null;
                }
                else
                {
                    result = (m_grid.DataSource as IBindingListView);
                }
                return result;
            }
        }

        protected override bool CanFilter
        {
            get
            {
                return BindingListView != null && BindingListView.SupportsFiltering;
            }
        }

        protected override void filter(StringSearchMatcher search)
        {
            base.filter(BindingListView, search);
        }

        [DefaultValue(ColumnSearchMode.ActiveColumn)]
        public ColumnSearchMode SearchModeColumn
        {
            get
            {
                return m_colsearchmode;
            }
            set
            {
                if (m_colsearchmode != value)
                {
                    m_colsearchmode = value;
                    setcurrentprop();
                    base.NotifyStateChanged(false);
                }
            }
        }

        private void setcurrentprop()
        {
            if (!base.IsBusy)
            {
                DataGridViewCell currentCell = m_grid.CurrentCell;
                if (currentCell != m_lastcell)
                {
                    m_lastcell = currentCell;
                    int i = (currentCell == null) ? -1 : currentCell.ColumnIndex;
                    Column column;
                    if (i == -1)
                    {
                        column = null;
                        HandleRegisteredKeyDowns = false;
                    }
                    else
                    {
                        DataGridViewColumn dataGridViewColumn = m_grid.Columns[i];
                        Column column2;
                        if (m_colsearchmode != ColumnSearchMode.ActiveColumn && base.Mode != SearchBoxMode.Filter)
                        {
                            column2 = null;
                        }
                        else
                        {
                            column2 = base.Columns.First((Column c) => c.Index == i);
                        }
                        column = column2;
                        HandleRegisteredKeyDowns = dataGridViewColumn.ReadOnly;
                    }
                    if (base.SearchProperty == column)
                    {
                        Text = null;
                    }
                    else
                    {
                        base.SearchProperty = column;
                    }
                }
            }
        }

        protected override void OnOpeningContextMenu(ContextMenuStrip menu, bool FirstTime)
        {
            base.OnOpeningContextMenu(menu, FirstTime);
            if (FirstTime)
            {
                m_searchitems = (from ColumnSearchMode g in Enum.GetValues(typeof(ColumnSearchMode))
                                 select new GridSearchModeItem(this, g)).ToArray();
                menu.Items.Add(new ToolStripSeparator());
                menu.Items.AddRange(m_searchitems);
            }
            else
            {
                foreach (GridSearchModeItem gridSearchModeItem in m_searchitems)
                {
                    gridSearchModeItem.Check();
                }
            }
        }

        private DataGridView m_grid;
        private ColumnSearchMode m_colsearchmode = ColumnSearchMode.ActiveColumn;
        private DataGridViewCell m_lastcell;
        private GridSearchModeItem[] m_searchitems;

        public enum ColumnSearchMode
        {
            ActiveColumn,
            AllColumns
        }

        private class GridSearchModeItem : SearchBoxItem
        {
            public GridSearchModeItem(DataGridSearchBox sb, ColumnSearchMode mode) : base(sb)
            {
                m_mode = mode;
                m_sb = sb;
                Text = mode.ToString();
                Check();
            }

            public void Check()
            {
                base.Checked = (m_mode == m_sb.m_colsearchmode);
                Enabled = (m_sb.Mode != SearchBoxMode.Filter);
            }

            protected override void OnClick(EventArgs e)
            {
                m_sb.SearchModeColumn = m_mode;
            }

            private ColumnSearchMode m_mode;
            private DataGridSearchBox m_sb;
        }
    }
}
