using System;
using System.Drawing;
using System.IO;
using System.Windows.Forms;

namespace TIger_A
{
	public class RegisterValueGridEditor
	{
		public delegate void RegisterClickedHadler();
		public delegate void MouseEventHandler(object sender, MouseEventArgs e);
		public delegate void AutomatiCellUpdate();
		public RegisterClickedHadler registerClickHandler;
		public MouseEventHandler mouseEventHandlerGRID;
		public AutomatiCellUpdate automaticUpdateHandler;

		protected virtual void OnRegisterClickHandle()
		{
			if (registerClickHandler != null)
				registerClickHandler();
		}

		protected virtual void OnMouseHandleGRID(object sender, MouseEventArgs e)
		{
			if (registerClickHandler != null)
				mouseEventHandlerGRID(sender, e);
		}

		protected virtual void OnautoautoUpdateHandle()
		{
			if (registerClickHandler != null)
				automaticUpdateHandler();
		}

		public RegisterValueGridEditor(Form arg, int _regSize, int _numRows, string _tableRegSet, int _posX, int _posY, int _Height, int _Width, string[] _OptionalColHeaderLabels, string[] _OptionalRowHeaderLabels, string _topLeftCellText, string _ColZeroText, bool _UseColors, bool _GridIsReadOnly)
		{
			gridRegValEditor = new DataGridView();
			gridRegValEditor.SortCompare += gridRegValEditor_SortCompare;
			constructorIsDone = false;
			REG_BIT_SIZE = _regSize;
			NUM_COLS = _regSize + 1;
			NUM_ROWS = _numRows;
			TABLE_DATA_SET = _tableRegSet;
			MAX_HEX_VAL = 0UL;
			GRID_DATA_NUM_ROWS = _numRows;
			GRID_DATA_NUM_COLS = _regSize;
			MAX_HEX_VAL = getMaxRegVal(REG_BIT_SIZE);
			GridDataArray = new string[NUM_ROWS, NUM_COLS, 4];
			GridDataArray_Last = new string[NUM_ROWS, NUM_COLS, 4];
			Grid = new string[NUM_ROWS, NUM_COLS, 4];
			Point location = new Point(_posX, _posY);
			gridRegValEditor.Location = location;
			InitializeTable(_Height, _Width, _OptionalColHeaderLabels, _OptionalRowHeaderLabels, _UseColors, _topLeftCellText, _ColZeroText, _GridIsReadOnly);
			arg.Controls.Add(gridRegValEditor);
			gridRegValEditor.CellValueChanged += dataGridView_CellValueChanged;
			gridRegValEditor.CellMouseClick += dataGridView_CellMouseClick;
			gridRegValEditor.Scroll += gridRegValEditor_Scroll;
			zeroOutRegisters();
			saveAllValues();
			constructorIsDone = true;
			for (int i = 0; i < NUM_ROWS; i++)
				for (int j = 0; j < NUM_COLS; j++)
				{
					GridDataArray[i, j, 0] = gridRegValEditor.Rows[i].Cells[j].Value.ToString();
					GridDataArray_Last[i, j, 0] = GridDataArray[i, j, 0];
					Grid[i, j, 0] = GridDataArray[i, j, 0];
				}

			for (int i = 0; i < NUM_ROWS; i++)
				for (int j = 0; j < NUM_COLS; j++)
					for (int k = 0; k < 4; k++)
					{
						GridDataArray_Last[i, j, k] = GridDataArray[i, j, k];
						Grid[i, j, k] = GridDataArray[i, j, k];
					}

			Exists = true;
			gridName = _tableRegSet;
		}

		private void gridRegValEditor_SortCompare(object sender, DataGridViewSortCompareEventArgs e)
		{
			string strA = (e.CellValue1 != null) ? e.CellValue1.ToString() : string.Empty;
			string strB = (e.CellValue2 != null) ? e.CellValue2.ToString() : string.Empty;
			e.SortResult = string.Compare(strA, strB);
			e.Handled = true;
		}

		public RegisterValueGridEditor(TabPage arg, int _regSize, int _numRows, string _tableRegSet, int _posX, int _posY, int _Height, int _Width, string[] _OptionalColHeaderLabels, string[] _OptionalRowHeaderLabels, string _topLeftCellText, string _ColZeroText, bool _UseColors, bool _GridIsReadOnly)
		{
			gridRegValEditor = new DataGridView();
			gridRegValEditor.SortCompare += gridRegValEditor_SortCompare;
			constructorIsDone = false;
			REG_BIT_SIZE = _regSize;
			NUM_COLS = _regSize + 1;
			NUM_ROWS = _numRows;
			TABLE_DATA_SET = _tableRegSet;
			MAX_HEX_VAL = 0UL;
			GRID_DATA_NUM_ROWS = _numRows;
			GRID_DATA_NUM_COLS = _regSize;
			MAX_HEX_VAL = getMaxRegVal(REG_BIT_SIZE);
			GridDataArray = new string[NUM_ROWS, NUM_COLS, 4];
			GridDataArray_Last = new string[NUM_ROWS, NUM_COLS, 4];
			Grid = new string[NUM_ROWS, NUM_COLS, 4];
			Point location = new Point(_posX, _posY);
			gridRegValEditor.Location = location;
			InitializeTable(_Height, _Width, _OptionalColHeaderLabels, _OptionalRowHeaderLabels, _UseColors, _topLeftCellText, _ColZeroText, _GridIsReadOnly);
			arg.Controls.Add(gridRegValEditor);
			gridRegValEditor.CellValueChanged += dataGridView_CellValueChanged;
			gridRegValEditor.CellMouseClick += dataGridView_CellMouseClick;
			gridRegValEditor.Scroll += gridRegValEditor_Scroll;
			zeroOutRegisters();
			saveAllValues();
			constructorIsDone = true;
			for (int i = 0; i < NUM_ROWS; i++)
				for (int j = 0; j < NUM_COLS; j++)
				{
					GridDataArray[i, j, 0] = gridRegValEditor.Rows[i].Cells[j].Value.ToString();
					GridDataArray_Last[i, j, 0] = GridDataArray[i, j, 0];
					Grid[i, j, 0] = GridDataArray[i, j, 0];
				}

			for (int i = 0; i < NUM_ROWS; i++)
				for (int j = 0; j < NUM_COLS; j++)
					for (int k = 0; k < 4; k++)
					{
						GridDataArray_Last[i, j, k] = GridDataArray[i, j, k];
						Grid[i, j, k] = GridDataArray[i, j, k];
					}

			Exists = true;
			gridName = _tableRegSet;
		}

		public RegisterValueGridEditor(GroupBox arg, int _regSize, int _numRows, string _tableRegSet, int _posX, int _posY, int _Height, int _Width, string[] _OptionalColHeaderLabels, string[] _OptionalRowHeaderLabels, string _topLeftCellText, string _ColZeroText, bool _UseColors, bool _GridIsReadOnly)
		{
			gridRegValEditor = new DataGridView();
			gridRegValEditor.SortCompare += gridRegValEditor_SortCompare;
			constructorIsDone = false;
			REG_BIT_SIZE = _regSize;
			NUM_COLS = _regSize + 1;
			NUM_ROWS = _numRows;
			TABLE_DATA_SET = _tableRegSet;
			MAX_HEX_VAL = 0UL;
			GRID_DATA_NUM_ROWS = _numRows;
			GRID_DATA_NUM_COLS = _regSize;
			MAX_HEX_VAL = getMaxRegVal(REG_BIT_SIZE);
			GridDataArray = new string[NUM_ROWS, NUM_COLS, 4];
			GridDataArray_Last = new string[NUM_ROWS, NUM_COLS, 4];
			Grid = new string[NUM_ROWS, NUM_COLS, 4];
			Point location = new Point(_posX, _posY);
			gridRegValEditor.Location = location;
			InitializeTable(_Height, _Width, _OptionalColHeaderLabels, _OptionalRowHeaderLabels, _UseColors, _topLeftCellText, _ColZeroText, _GridIsReadOnly);
			arg.Controls.Add(gridRegValEditor);
			gridRegValEditor.CellValueChanged += dataGridView_CellValueChanged;
			gridRegValEditor.CellMouseClick += dataGridView_CellMouseClick;
			gridRegValEditor.Scroll += gridRegValEditor_Scroll;
			zeroOutRegisters();
			saveAllValues();
			constructorIsDone = true;
			for (int i = 0; i < NUM_ROWS; i++)
				for (int j = 0; j < NUM_COLS; j++)
				{
					GridDataArray[i, j, 0] = gridRegValEditor.Rows[i].Cells[j].Value.ToString();
					GridDataArray_Last[i, j, 0] = GridDataArray[i, j, 0];
					Grid[i, j, 0] = GridDataArray[i, j, 0];
				}

			for (int i = 0; i < NUM_ROWS; i++)
				for (int j = 0; j < NUM_COLS; j++)
					for (int k = 0; k < 4; k++)
					{
						GridDataArray_Last[i, j, k] = GridDataArray[i, j, k];
						Grid[i, j, k] = GridDataArray[i, j, k];
					}

			Exists = true;
			gridName = _tableRegSet;
		}

		public RegisterValueGridEditor(Panel arg, int _regSize, int _numRows, string _tableRegSet, int _posX, int _posY, int _Height, int _Width, string[] _OptionalColHeaderLabels, string[] _OptionalRowHeaderLabels, string _topLeftCellText, string _ColZeroText, bool _UseColors, bool _GridIsReadOnly)
		{
			gridRegValEditor = new DataGridView();
			gridRegValEditor.SortCompare += gridRegValEditor_SortCompare;
			constructorIsDone = false;
			REG_BIT_SIZE = _regSize;
			NUM_COLS = _regSize + 1;
			NUM_ROWS = _numRows;
			TABLE_DATA_SET = _tableRegSet;
			MAX_HEX_VAL = 0UL;
			GRID_DATA_NUM_ROWS = _numRows;
			GRID_DATA_NUM_COLS = _regSize;
			MAX_HEX_VAL = getMaxRegVal(REG_BIT_SIZE);
			GridDataArray = new string[NUM_ROWS, NUM_COLS, 4];
			GridDataArray_Last = new string[NUM_ROWS, NUM_COLS, 4];
			Grid = new string[NUM_ROWS, NUM_COLS, 4];
			Point location = new Point(_posX, _posY);
			gridRegValEditor.Location = location;
			InitializeTable(_Height, _Width, _OptionalColHeaderLabels, _OptionalRowHeaderLabels, _UseColors, _topLeftCellText, _ColZeroText, _GridIsReadOnly);
			arg.Controls.Add(gridRegValEditor);
			gridRegValEditor.CellValueChanged += dataGridView_CellValueChanged;
			gridRegValEditor.CellMouseClick += dataGridView_CellMouseClick;
			gridRegValEditor.Scroll += gridRegValEditor_Scroll;
			zeroOutRegisters();
			saveAllValues();
			constructorIsDone = true;
			for (int i = 0; i < NUM_ROWS; i++)
				for (int j = 0; j < NUM_COLS; j++)
				{
					GridDataArray[i, j, 0] = gridRegValEditor.Rows[i].Cells[j].Value.ToString();
					GridDataArray_Last[i, j, 0] = GridDataArray[i, j, 0];
					Grid[i, j, 0] = GridDataArray[i, j, 0];
				}

			for (int i = 0; i < NUM_ROWS; i++)
				for (int j = 0; j < NUM_COLS; j++)
					for (int k = 0; k < 4; k++)
					{
						GridDataArray_Last[i, j, k] = GridDataArray[i, j, k];
						Grid[i, j, k] = GridDataArray[i, j, k];
					}

			Exists = true;
			gridName = _tableRegSet;
		}

		public RegisterValueGridEditor(SplitContainer arg, int _regSize, int _numRows, string _tableRegSet, int _posX, int _posY, int _Height, int _Width, string[] _OptionalColHeaderLabels, string[] _OptionalRowHeaderLabels, string _topLeftCellText, string _ColZeroText, bool _UseColors, bool _GridIsReadOnly)
		{
			gridRegValEditor = new DataGridView();
			gridRegValEditor.SortCompare += gridRegValEditor_SortCompare;
			REG_BIT_SIZE = _regSize;
			NUM_COLS = _regSize + 2;
			NUM_ROWS = _numRows + 1;
			TABLE_DATA_SET = _tableRegSet;
			MAX_HEX_VAL = 0UL;
			MAX_HEX_VAL = getMaxRegVal(REG_BIT_SIZE);
			Point location = new Point(_posX, _posY);
			gridRegValEditor.Location = location;
			InitializeTable(_Height, _Width, _OptionalColHeaderLabels, _OptionalRowHeaderLabels, _UseColors, _topLeftCellText, _ColZeroText, _GridIsReadOnly);
			arg.Controls.Add(gridRegValEditor);
			gridRegValEditor.CellValueChanged += dataGridView_CellValueChanged;
			gridRegValEditor.CellMouseClick += dataGridView_CellMouseClick;
			gridRegValEditor.Scroll += gridRegValEditor_Scroll;
			zeroOutRegisters();
			saveAllValues();
			gridName = _tableRegSet;
		}

		public RegisterValueGridEditor(FlowLayoutPanel arg, int _regSize, int _numRows, string _tableRegSet, int _posX, int _posY, int _Height, int _Width, string[] _OptionalColHeaderLabels, string[] _OptionalRowHeaderLabels, string _topLeftCellText, string _ColZeroText, bool _UseColors, bool _GridIsReadOnly)
		{
			gridRegValEditor = new DataGridView();
			gridRegValEditor.SortCompare += gridRegValEditor_SortCompare;
			REG_BIT_SIZE = _regSize;
			NUM_COLS = _regSize + 2;
			NUM_ROWS = _numRows + 1;
			TABLE_DATA_SET = _tableRegSet;
			MAX_HEX_VAL = 0UL;
			MAX_HEX_VAL = getMaxRegVal(REG_BIT_SIZE);
			Point location = new Point(_posX, _posY);
			gridRegValEditor.Location = location;
			InitializeTable(_Height, _Width, _OptionalColHeaderLabels, _OptionalRowHeaderLabels, _UseColors, _topLeftCellText, _ColZeroText, _GridIsReadOnly);
			arg.Controls.Add(gridRegValEditor);
			gridRegValEditor.CellValueChanged += dataGridView_CellValueChanged;
			gridRegValEditor.CellMouseClick += dataGridView_CellMouseClick;
			gridRegValEditor.Scroll += gridRegValEditor_Scroll;
			zeroOutRegisters();
			saveAllValues();
			gridName = _tableRegSet;
		}

		public RegisterValueGridEditor(TableLayoutPanel arg, int _regSize, int _numRows, string _tableRegSet, int _posX, int _posY, int _Height, int _Width, string[] _OptionalColHeaderLabels, string[] _OptionalRowHeaderLabels, string _topLeftCellText, string _ColZeroText, bool _UseColors, bool _GridIsReadOnly)
		{
			gridRegValEditor = new DataGridView();
			gridRegValEditor.SortCompare += gridRegValEditor_SortCompare;
			REG_BIT_SIZE = _regSize;
			NUM_COLS = _regSize + 2;
			NUM_ROWS = _numRows + 1;
			TABLE_DATA_SET = _tableRegSet;
			MAX_HEX_VAL = 0UL;
			MAX_HEX_VAL = getMaxRegVal(REG_BIT_SIZE);
			Point location = new Point(_posX, _posY);
			gridRegValEditor.Location = location;
			InitializeTable(_Height, _Width, _OptionalColHeaderLabels, _OptionalRowHeaderLabels, _UseColors, _topLeftCellText, _ColZeroText, _GridIsReadOnly);
			arg.Controls.Add(gridRegValEditor);
			gridRegValEditor.CellValueChanged += dataGridView_CellValueChanged;
			gridRegValEditor.CellMouseClick += dataGridView_CellMouseClick;
			gridRegValEditor.Scroll += gridRegValEditor_Scroll;
			zeroOutRegisters();
			saveAllValues();
			gridName = _tableRegSet;
		}

		public RegisterValueGridEditor(Form arg, int _regSize, int _numRows, string _tableRegSet, int _posX, int _posY, int _Height, int _Width, string[] _OptionalColHeaderLabels, string[] _OptionalRowHeaderLabels, string _topLeftCellText, string _ColZeroText, bool _UseColors, bool _GridIsReadOnly, Color _flashColor, double _flashTime_ms)
		{
			gridRegValEditor = new DataGridView();
			gridRegValEditor.SortCompare += gridRegValEditor_SortCompare;
			constructorIsDone = false;
			REG_BIT_SIZE = _regSize;
			NUM_COLS = _regSize + 1;
			NUM_ROWS = _numRows;
			TABLE_DATA_SET = _tableRegSet;
			MAX_HEX_VAL = 0UL;
			GRID_DATA_NUM_ROWS = _numRows;
			GRID_DATA_NUM_COLS = _regSize;
			MAX_HEX_VAL = getMaxRegVal(REG_BIT_SIZE);
			GridDataArray = new string[NUM_ROWS, NUM_COLS, 4];
			GridDataArray_Last = new string[NUM_ROWS, NUM_COLS, 4];
			Grid = new string[NUM_ROWS, NUM_COLS, 4];
			Point location = new Point(_posX, _posY);
			gridRegValEditor.Location = location;
			InitializeTable(_Height, _Width, _OptionalColHeaderLabels, _OptionalRowHeaderLabels, _UseColors, _topLeftCellText, _ColZeroText, _GridIsReadOnly);
			arg.Controls.Add(gridRegValEditor);
			gridRegValEditor.CellValueChanged += dataGridView_CellValueChanged;
			gridRegValEditor.CellMouseClick += dataGridView_CellMouseClick;
			gridRegValEditor.Scroll += gridRegValEditor_Scroll;
			zeroOutRegisters();
			saveAllValues();
			constructorIsDone = true;
			for (int i = 0; i < NUM_ROWS; i++)
				for (int j = 0; j < NUM_COLS; j++)
				{
					GridDataArray[i, j, 0] = gridRegValEditor.Rows[i].Cells[j].Value.ToString();
					GridDataArray_Last[i, j, 0] = GridDataArray[i, j, 0];
					Grid[i, j, 0] = GridDataArray[i, j, 0];
				}

			for (int i = 0; i < NUM_ROWS; i++)
				for (int j = 0; j < NUM_COLS; j++)
					for (int k = 0; k < 4; k++)
					{
						GridDataArray_Last[i, j, k] = GridDataArray[i, j, k];
						Grid[i, j, k] = GridDataArray[i, j, k];
					}

			Exists = true;
			flashColor = _flashColor;
			flashTime_ms = _flashTime_ms;
			gridName = _tableRegSet;
		}

		public RegisterValueGridEditor(TabPage arg, int _regSize, int _numRows, string _tableRegSet, int _posX, int _posY, int _Height, int _Width, string[] _OptionalColHeaderLabels, string[] _OptionalRowHeaderLabels, string _topLeftCellText, string _ColZeroText, bool _UseColors, bool _GridIsReadOnly, Color _flashColor, double _flashTime_ms)
		{
			gridRegValEditor = new DataGridView();
			gridRegValEditor.SortCompare += gridRegValEditor_SortCompare;
			constructorIsDone = false;
			REG_BIT_SIZE = _regSize;
			NUM_COLS = _regSize + 1;
			NUM_ROWS = _numRows;
			TABLE_DATA_SET = _tableRegSet;
			MAX_HEX_VAL = 0UL;
			GRID_DATA_NUM_ROWS = _numRows;
			GRID_DATA_NUM_COLS = _regSize;
			MAX_HEX_VAL = getMaxRegVal(REG_BIT_SIZE);
			GridDataArray = new string[NUM_ROWS, NUM_COLS, 4];
			GridDataArray_Last = new string[NUM_ROWS, NUM_COLS, 4];
			Grid = new string[NUM_ROWS, NUM_COLS, 4];
			Point location = new Point(_posX, _posY);
			gridRegValEditor.Location = location;
			InitializeTable(_Height, _Width, _OptionalColHeaderLabels, _OptionalRowHeaderLabels, _UseColors, _topLeftCellText, _ColZeroText, _GridIsReadOnly);
			arg.Controls.Add(gridRegValEditor);
			gridRegValEditor.CellValueChanged += dataGridView_CellValueChanged;
			gridRegValEditor.CellMouseClick += dataGridView_CellMouseClick;
			gridRegValEditor.Scroll += gridRegValEditor_Scroll;
			zeroOutRegisters();
			saveAllValues();
			constructorIsDone = true;
			for (int i = 0; i < NUM_ROWS; i++)
				for (int j = 0; j < NUM_COLS; j++)
				{
					GridDataArray[i, j, 0] = gridRegValEditor.Rows[i].Cells[j].Value.ToString();
					GridDataArray_Last[i, j, 0] = GridDataArray[i, j, 0];
					Grid[i, j, 0] = GridDataArray[i, j, 0];
				}

			for (int i = 0; i < NUM_ROWS; i++)
				for (int j = 0; j < NUM_COLS; j++)
					for (int k = 0; k < 4; k++)
					{
						GridDataArray_Last[i, j, k] = GridDataArray[i, j, k];
						Grid[i, j, k] = GridDataArray[i, j, k];
					}

			Exists = true;
			flashColor = _flashColor;
			flashTime_ms = _flashTime_ms;
			gridName = _tableRegSet;
		}

		public RegisterValueGridEditor(GroupBox arg, int _regSize, int _numRows, string _tableRegSet, int _posX, int _posY, int _Height, int _Width, string[] _OptionalColHeaderLabels, string[] _OptionalRowHeaderLabels, string _topLeftCellText, string _ColZeroText, bool _UseColors, bool _GridIsReadOnly, Color _flashColor, double _flashTime_ms)
		{
			gridRegValEditor = new DataGridView();
			gridRegValEditor.SortCompare += gridRegValEditor_SortCompare;
			constructorIsDone = false;
			REG_BIT_SIZE = _regSize;
			NUM_COLS = _regSize + 1;
			NUM_ROWS = _numRows;
			TABLE_DATA_SET = _tableRegSet;
			MAX_HEX_VAL = 0UL;
			GRID_DATA_NUM_ROWS = _numRows;
			GRID_DATA_NUM_COLS = _regSize;
			MAX_HEX_VAL = getMaxRegVal(REG_BIT_SIZE);
			GridDataArray = new string[NUM_ROWS, NUM_COLS, 4];
			GridDataArray_Last = new string[NUM_ROWS, NUM_COLS, 4];
			Grid = new string[NUM_ROWS, NUM_COLS, 4];
			Point location = new Point(_posX, _posY);
			gridRegValEditor.Location = location;
			InitializeTable(_Height, _Width, _OptionalColHeaderLabels, _OptionalRowHeaderLabels, _UseColors, _topLeftCellText, _ColZeroText, _GridIsReadOnly);
			arg.Controls.Add(gridRegValEditor);
			gridRegValEditor.CellValueChanged += dataGridView_CellValueChanged;
			gridRegValEditor.CellMouseClick += dataGridView_CellMouseClick;
			gridRegValEditor.Scroll += gridRegValEditor_Scroll;
			zeroOutRegisters();
			saveAllValues();
			constructorIsDone = true;
			for (int i = 0; i < NUM_ROWS; i++)
				for (int j = 0; j < NUM_COLS; j++)
				{
					GridDataArray[i, j, 0] = gridRegValEditor.Rows[i].Cells[j].Value.ToString();
					GridDataArray_Last[i, j, 0] = GridDataArray[i, j, 0];
					Grid[i, j, 0] = GridDataArray[i, j, 0];
				}

			for (int i = 0; i < NUM_ROWS; i++)
				for (int j = 0; j < NUM_COLS; j++)
					for (int k = 0; k < 4; k++)
					{
						GridDataArray_Last[i, j, k] = GridDataArray[i, j, k];
						Grid[i, j, k] = GridDataArray[i, j, k];
					}

			Exists = true;
			flashColor = _flashColor;
			flashTime_ms = _flashTime_ms;
			gridName = _tableRegSet;
		}

		public RegisterValueGridEditor(Panel arg, int _regSize, int _numRows, string _tableRegSet, int _posX, int _posY, int _Height, int _Width, string[] _OptionalColHeaderLabels, string[] _OptionalRowHeaderLabels, string _topLeftCellText, string _ColZeroText, bool _UseColors, bool _GridIsReadOnly, Color _flashColor, double _flashTime_ms)
		{
			gridRegValEditor = new DataGridView();
			gridRegValEditor.DefaultCellStyle.Font = new Font("Courier New", 10f);
			gridRegValEditor.RowHeadersDefaultCellStyle.Font = new Font("Courier New", 10f);
			gridRegValEditor.ColumnHeadersDefaultCellStyle.Font = new Font("Courier New", 9f);
			gridRegValEditor.SortCompare += gridRegValEditor_SortCompare;
			gridRegValEditor.Cursor = Cursors.Hand;
			constructorIsDone = false;
			REG_BIT_SIZE = _regSize;
			NUM_COLS = _regSize + 1;
			NUM_ROWS = _numRows;
			TABLE_DATA_SET = _tableRegSet;
			MAX_HEX_VAL = 0UL;
			GRID_DATA_NUM_ROWS = _numRows;
			GRID_DATA_NUM_COLS = _regSize;
			MAX_HEX_VAL = getMaxRegVal(REG_BIT_SIZE);
			GridDataArray = new string[NUM_ROWS, NUM_COLS, 4];
			GridDataArray_Last = new string[NUM_ROWS, NUM_COLS, 4];
			Grid = new string[NUM_ROWS, NUM_COLS, 4];
			Point location = new Point(_posX, _posY);
			gridRegValEditor.Location = location;
			InitializeTable(_Height, _Width, _OptionalColHeaderLabels, _OptionalRowHeaderLabels, _UseColors, _topLeftCellText, _ColZeroText, _GridIsReadOnly);
			arg.Controls.Add(gridRegValEditor);
			gridRegValEditor.CellValueChanged += dataGridView_CellValueChanged;
			gridRegValEditor.CellMouseClick += dataGridView_CellMouseClick;
			gridRegValEditor.Scroll += gridRegValEditor_Scroll;
			gridRegValEditor.KeyUp += dataGridView_KeyUp;
			gridRegValEditor.CellContentDoubleClick += dataGridView_CellDoubleClick;
			gridRegValEditor.CellEndEdit += dataGridView_CellEndEdit;
			gridRegValEditor.DataBindingComplete += dataGridView_DataBindingComplete;
			zeroOutRegisters();
			saveAllValues();
			constructorIsDone = true;
			for (int i = 0; i < NUM_ROWS; i++)
				for (int j = 0; j < NUM_COLS; j++)
				{
					GridDataArray[i, j, 0] = gridRegValEditor.Rows[i].Cells[j].Value.ToString();
					GridDataArray_Last[i, j, 0] = GridDataArray[i, j, 0];
					Grid[i, j, 0] = GridDataArray[i, j, 0];
				}

			for (int i = 0; i < NUM_ROWS; i++)
				for (int j = 0; j < NUM_COLS; j++)
					for (int k = 0; k < 4; k++)
					{
						GridDataArray_Last[i, j, k] = GridDataArray[i, j, k];
						Grid[i, j, k] = GridDataArray[i, j, k];
					}

			Exists = true;
			flashColor = _flashColor;
			flashTime_ms = _flashTime_ms;
			gridName = _tableRegSet;
			gridRegValEditor.Columns[0].SortMode = DataGridViewColumnSortMode.NotSortable;
			gridRegValEditor.Columns[1].SortMode = DataGridViewColumnSortMode.NotSortable;
			gridRegValEditor.Columns[2].SortMode = DataGridViewColumnSortMode.NotSortable;
			gridRegValEditor.Columns[3].SortMode = DataGridViewColumnSortMode.NotSortable;
			gridRegValEditor.Columns[4].SortMode = DataGridViewColumnSortMode.NotSortable;
			gridRegValEditor.Columns[5].SortMode = DataGridViewColumnSortMode.NotSortable;
			gridRegValEditor.Columns[6].SortMode = DataGridViewColumnSortMode.NotSortable;
			gridRegValEditor.Columns[7].SortMode = DataGridViewColumnSortMode.NotSortable;
			gridRegValEditor.Columns[8].SortMode = DataGridViewColumnSortMode.NotSortable;
		}

		public RegisterValueGridEditor(SplitContainer arg, int _regSize, int _numRows, string _tableRegSet, int _posX, int _posY, int _Height, int _Width, string[] _OptionalColHeaderLabels, string[] _OptionalRowHeaderLabels, string _topLeftCellText, string _ColZeroText, bool _UseColors, bool _GridIsReadOnly, Color _flashColor, double _flashTime_ms)
		{
			gridRegValEditor = new DataGridView();
			gridRegValEditor.SortCompare += gridRegValEditor_SortCompare;
			REG_BIT_SIZE = _regSize;
			NUM_COLS = _regSize + 2;
			NUM_ROWS = _numRows + 1;
			TABLE_DATA_SET = _tableRegSet;
			MAX_HEX_VAL = 0UL;
			MAX_HEX_VAL = getMaxRegVal(REG_BIT_SIZE);
			Point location = new Point(_posX, _posY);
			gridRegValEditor.Location = location;
			InitializeTable(_Height, _Width, _OptionalColHeaderLabels, _OptionalRowHeaderLabels, _UseColors, _topLeftCellText, _ColZeroText, _GridIsReadOnly);
			arg.Controls.Add(gridRegValEditor);
			gridRegValEditor.CellValueChanged += dataGridView_CellValueChanged;
			gridRegValEditor.CellMouseClick += dataGridView_CellMouseClick;
			gridRegValEditor.Scroll += gridRegValEditor_Scroll;
			zeroOutRegisters();
			saveAllValues();
			flashColor = _flashColor;
			flashTime_ms = _flashTime_ms;
			gridName = _tableRegSet;
		}

		public RegisterValueGridEditor(FlowLayoutPanel arg, int _regSize, int _numRows, string _tableRegSet, int _posX, int _posY, int _Height, int _Width, string[] _OptionalColHeaderLabels, string[] _OptionalRowHeaderLabels, string _topLeftCellText, string _ColZeroText, bool _UseColors, bool _GridIsReadOnly, Color _flashColor, double _flashTime_ms)
		{
			gridRegValEditor = new DataGridView();
			gridRegValEditor.SortCompare += gridRegValEditor_SortCompare;
			REG_BIT_SIZE = _regSize;
			NUM_COLS = _regSize + 2;
			NUM_ROWS = _numRows + 1;
			TABLE_DATA_SET = _tableRegSet;
			MAX_HEX_VAL = 0UL;
			MAX_HEX_VAL = getMaxRegVal(REG_BIT_SIZE);
			Point location = new Point(_posX, _posY);
			gridRegValEditor.Location = location;
			InitializeTable(_Height, _Width, _OptionalColHeaderLabels, _OptionalRowHeaderLabels, _UseColors, _topLeftCellText, _ColZeroText, _GridIsReadOnly);
			arg.Controls.Add(gridRegValEditor);
			gridRegValEditor.CellValueChanged += dataGridView_CellValueChanged;
			gridRegValEditor.CellMouseClick += dataGridView_CellMouseClick;
			gridRegValEditor.Scroll += gridRegValEditor_Scroll;
			zeroOutRegisters();
			saveAllValues();
			flashColor = _flashColor;
			flashTime_ms = _flashTime_ms;
			gridName = _tableRegSet;
		}

		public RegisterValueGridEditor(TableLayoutPanel arg, int _regSize, int _numRows, string _tableRegSet, int _posX, int _posY, int _Height, int _Width, string[] _OptionalColHeaderLabels, string[] _OptionalRowHeaderLabels, string _topLeftCellText, string _ColZeroText, bool _UseColors, bool _GridIsReadOnly, Color _flashColor, double _flashTime_ms)
		{
			gridRegValEditor = new DataGridView();
			gridRegValEditor.SortCompare += gridRegValEditor_SortCompare;
			REG_BIT_SIZE = _regSize;
			NUM_COLS = _regSize + 2;
			NUM_ROWS = _numRows + 1;
			TABLE_DATA_SET = _tableRegSet;
			MAX_HEX_VAL = 0UL;
			MAX_HEX_VAL = getMaxRegVal(REG_BIT_SIZE);
			Point location = new Point(_posX, _posY);
			gridRegValEditor.Location = location;
			InitializeTable(_Height, _Width, _OptionalColHeaderLabels, _OptionalRowHeaderLabels, _UseColors, _topLeftCellText, _ColZeroText, _GridIsReadOnly);
			arg.Controls.Add(gridRegValEditor);
			gridRegValEditor.CellValueChanged += dataGridView_CellValueChanged;
			gridRegValEditor.CellMouseClick += dataGridView_CellMouseClick;
			gridRegValEditor.Scroll += gridRegValEditor_Scroll;
			zeroOutRegisters();
			saveAllValues();
			flashColor = _flashColor;
			flashTime_ms = _flashTime_ms;
			gridName = _tableRegSet;
		}

		public string getGridName()
		{
			gridName = gridName.ToUpper();
			return gridName;
		}

		public int getNumberOf_Addresses_in_DUT()
		{
			return NUM_ROWS;
		}

		public void Dispose()
		{
			gridRegValEditor.Dispose();
		}

		public int getNumberOf_bits_in_DataWord()
		{
			return REG_BIT_SIZE;
		}

		public int[] WhatIsArrayLength(string[,] RowValues_ROW_A_D_O_R_in)
		{
			int[] array = new int[3];
			int num = 0;
			int num2;
			int num3;
			try
			{
				num2 = RowValues_ROW_A_D_O_R_in.Length / RowValues_ROW_A_D_O_R_in.GetLength(1);
				num3 = RowValues_ROW_A_D_O_R_in.GetLength(1);
				num = RowValues_ROW_A_D_O_R_in[0, 1].Length / 2 + RowValues_ROW_A_D_O_R_in[0, 2].Length / 2;
			}
			catch
			{
				num2 = 0;
				num3 = 0;
				num = 0;
			}
			array[0] = num2;
			array[1] = num3;
			array[2] = num;
			return array;
		}

		public int[] IsAddressAndDataValid(string address, string data)
		{
			int[] array = new int[3];
			int num = 0;
			int num2 = 0;
			int num3 = 0;
			if (address == "" || address == "address" || address == "ADDRESS" || data == "" || data == "data" || data == "DATA")
			{
				num = 0;
				num2 = 0;
				num3 = 0;
			}
			else
			{
				try
				{
					if (address.Length != 0)
					{
						num = 1;
						num2 = 5;
						num3 = 0;
					}
					else
						Convert.ToInt64("junk", 16);
				}
				catch
				{
					MessageBox.Show("The input Address is: " + address + " is not valid.");
					num = 0;
					num2 = 0;
					num3 = 0;
				}
				try
				{
					Convert.ToInt64(data, 16);
					num3 = data.Length / 2;
				}
				catch
				{
					MessageBox.Show("The input Data is: " + data + " is not valid.");
					num = 0;
					num2 = 0;
					num3 = 0;
				}
			}
			array[0] = num;
			array[1] = num2;
			array[2] = num3;
			return array;
		}

		public int getNumberOfHighlighedRows()
		{
			return NumberOfHighlightedRows;
		}

		public DateTime GetLastSelectedTime()
		{
			DateTime result;
			if (anyCellHighlighted())
				result = timeStamp;
			else
				result = default(DateTime);
			return result;
		}

		public bool setDataIntoGridDataArray(string[,] RowValues_ROW_A_D_O_Rin, string address, string data)
		{
			bool flag = true;
			int[] array = WhatIsArrayLength(RowValues_ROW_A_D_O_Rin);
			int[] array2 = IsAddressAndDataValid(address, data);
			int num = 0;
			int num2 = 0;
			if (address == "" || address == "address" || address == "ADDRESS" || data == "" || data == "data" || data == "DATA")
			{
				if (array[0] > 0 && array[1] > 0)
				{
					flag = true;
					num = array[0];
					num2 = array[1];
				}
			}
			else
			{
				if (array2[0] <= 0 || array2[1] <= 0)
				{
					string text = "Error in 'setDataIntoGridDataArray'.  Neither address/data string inputs or string[,] RowValues_ROW_A_D_O_Rin contain data.";
					MessageBox.Show(text);
					return false;
				}
				flag = false;
				num = array2[0];
				num2 = array2[1];
			}
			string[,] array3 = new string[num, num2];
			if (flag)
			{
				array3 = RowValues_ROW_A_D_O_Rin;
			}
			else
			{
				array3[0, 0] = FindRowIndexThatMatchsAddress(address, false).ToString();
				array3[0, 1] = address;
				array3[0, 4] = data;
			}
			setDataIntoGridDataArray(array3, false);
			return true;
		}

		public bool setDataIntoGridDataArray(string[,] RowValues_ROW_A_D_O_R_in, bool selectedTrue)
		{
			int num = RowValues_ROW_A_D_O_R_in.Length / RowValues_ROW_A_D_O_R_in.GetLength(1);
			int num2 = RowValues_ROW_A_D_O_R_in.Length / RowValues_ROW_A_D_O_R_in.GetLength(0);
			bool result = false;
			for (int i = 0; i < num; i++)
				if (RowValues_ROW_A_D_O_R_in[i, 1] != null && RowValues_ROW_A_D_O_R_in[i, 4] != null)
				{
					int index = FindRowIndexThatMatchsAddress(RowValues_ROW_A_D_O_R_in[i, 1], selectedTrue);
					gridRegValEditor.Rows[index].Cells[0].Value = RowValues_ROW_A_D_O_R_in[i, 4];
					result = false;
				}

			return result;
		}

		public string[,] Get_HIGHLIGHTED_Grid_Values__R_D_A_O(bool SingleRW)
		{
			string[,] array;
			if (SingleRW)
				array = GetHighlightedGridAddresses(DisplayErrors, true);
			else
				array = GetHighlightedGridAddresses(DisplayErrors, false);

			array = GetSelectedGridData(array);
			array = GetOriginalGridValuesBeforeChange(array);
			string[,] result;
			if (array.Length == 0)
			{
				string[,] array2 = new string[1, 5];
				result = array2;
			}
			else if (array[0, 2] == null)
			{
				string[,] array2 = new string[1, 5];
				result = array2;
			}
			else
			{
				try
				{
					NumberOfHighlightedRows = array.GetLength(0);
				}
				catch
				{
					NumberOfHighlightedRows = 0;
				}
				result = array;
			}
			return result;
		}

		public string[,] GetSelectedGridData(string[,] InputAddresses)
		{
			string[,] array = new string[InputAddresses.Length / InputAddresses.GetLength(1), InputAddresses.GetLength(1)];
			long num = (long)NUM_ROWS;
			if (InputAddresses.Length == 0)
			{
			}
			string[,] array2 = new string[InputAddresses.Length / InputAddresses.GetLength(1), InputAddresses.GetLength(1)];
			for (int i = 0; i < InputAddresses.Length / InputAddresses.GetLength(1); i++)
			{
				string value = InputAddresses[i, 0];
				int rowIndex = Convert.ToInt32(value, 10);
				string text = FindDataThatIsInGridRowIndex(rowIndex);
				InputAddresses[i, 2] = text;
			}
			string[,] result;
			if (InputAddresses.Length == 0)
				result = new string[1, 5];
			else
				result = InputAddresses;

			return result;
		}

		public int FindRowIndexThatMatchsAddress(string Address, bool selectedTrue)
		{
			int num = -1;
			for (int i = 0; i < NUM_ROWS; i++)
			{
				string a = gridRegValEditor.Rows[i].HeaderCell.Value.ToString();
				if (a == Address)
					num = i;
			}
			if (num == -1)
			{
				string text = "ERROR - FindRowIndexThatMatchsAddress(string Address)  No Input Address matched a grid address.";
				MessageBox.Show(text);
			}
			return num;
		}

		public string FindDataThatIsInGridRowIndex(int rowIndex)
		{
			string text = "";
			string result;
			if (rowIndex < 0 || rowIndex > NUM_ROWS)
			{
				string text2 = "ERROR - FindDataThatIsInGridRowIndex(int rowIndex)  No Input RowIndex: " + rowIndex.ToString() + " is included in the grid.";
				MessageBox.Show(text2);
				result = text;
			}
			else
			{
				text = gridRegValEditor.Rows[rowIndex].Cells[0].Value.ToString();
				result = text;
			}
			return result;
		}

		public string[,] GetOriginalGridValuesBeforeChange(string[,] RowValues_ROW_A_D_O_R)
		{
			int num = RowValues_ROW_A_D_O_R.Length / RowValues_ROW_A_D_O_R.GetLength(1);
			for (int i = 0; i < num; i++)
			{
				string value = RowValues_ROW_A_D_O_R[i, 0];
				int num2 = Convert.ToInt32(value, 10);
				RowValues_ROW_A_D_O_R[i, 3] = GridDataArray_Last[num2, 0, 0];
			}
			return RowValues_ROW_A_D_O_R;
		}

		public string[,] GetHighlightedGridAddresses(bool DisplayError, bool SingleRW)
		{
			long num = 0L;
			num = (long)NUM_ROWS;
			int num2 = 5;
			string[] array = new string[NUM_ROWS];
			string[] array2 = new string[num];
			string[] array3 = new string[20];
			DataGridViewRow[] array4 = new DataGridViewRow[num];
			gridRegValEditor.SelectedRows.CopyTo(array4, 0);
			int num3 = 0;
			int num4 = 0;
			int i = 0;
			while ((long)i < num)
			{
				if (array4[i] != null)
				{
					string text = array4[i].ToString();
					char[] array5 = new char[text.Length];
					array5 = text.ToCharArray();
					for (int j = 0; j < text.Length; j++)
					{
						if (array5[j] == '=')
							num3 = j;
						if (array5[j] == ' ')
							num4 = j;
					}
					int num5 = 0;
					string[] array6 = new string[100];
					for (int k = num3 + 1; k < num4; k++)
					{
						char value = array5[k];
						if (!SingleRW)
							value = '0';
						array6[num5] = Convert.ToString(value);
						num5++;
					}
					for (int l = 0; l < num5; l++)
						array[i] += array6[l];
				}
				i++;
			}
			int num6 = 0;
			for (i = 0; i < array.Length; i++)
			{
				if (array[i] != null)
					num6++;
			}
			string[,] array7 = new string[num6, num2];
			for (i = 0; i < num6; i++)
				if (array[i] != null)
				{
					int index = Convert.ToInt32(array[i], 10);
					array7[i, 1] = gridRegValEditor.Rows[index].HeaderCell.Value.ToString();
					array7[i, 0] = index.ToString();
				}

			int num7 = 0;
			try
			{
				int length = array7.GetLength(1);
				num7 = array7.Length / length;
			}
			catch
			{
				num7 = 0;
			}
			string[] array8 = new string[num];
			int num8 = 0;
			i = 0;
			while ((long)i < num)
			{
				if (gridRegValEditor.Rows[i].Cells[1].Style.BackColor == Color.Yellow)
				{
					array8[num8] = Convert.ToString(i, 10);
					array8[num8] = array8[num8].ToUpper();
					num8++;
				}
				i++;
			}
			num6 = 0;
			for (i = 0; i < array8.Length; i++)
				if (array8[i] != null)
					num6++;

			string[,] array9 = new string[num6, num2];
			for (i = 0; i < num6; i++)
				if (array8[i] != null)
				{
					int index = Convert.ToInt32(array8[i], 10);
					array9[i, 1] = gridRegValEditor.Rows[index].HeaderCell.Value.ToString();
					array9[i, 0] = index.ToString();
				}

			int num9 = 0;
			try
			{
				int length2 = array9.GetLength(1);
				num9 = array9.Length / length2;
			}
			catch
			{
				num9 = 0;
			}
			string[,] array10 = new string[num9 + num7, num2];
			int m;
			for (m = 0; m < num7; m++)
			{
				array10[m, 1] = array7[m, 1];
				array10[m, 0] = array7[m, 0];
			}
			for (int n = m; n < num9 + m; n++)
			{
				array10[n, 1] = array9[n - m, 1];
				array10[n, 0] = array9[n - m, 0];
			}
			int num10 = array10.Length / array10.GetLength(1);
			for (int num11 = 0; num11 < array10.Length / array10.GetLength(1); num11++)
				for (i = 0; i < num10; i++)
					if (num11 != i)
						if (array10[num11, 1] == array10[i, 1])
						{
							array10[i, 1] = null;
							array10[i, 0] = null;
						}

			num6 = 0;
			for (int num11 = 0; num11 < num10; num11++)
				if (array10[num11, 1] != null)
					num6++;

			string[,] array11 = new string[num6, num2];
			int num12 = 0;
			for (int num11 = 0; num11 < num10; num11++)
				if (array10[num11, 1] != null)
				{
					array11[num12, 1] = array10[num11, 1];
					array11[num12, 0] = array10[num11, 0];
					num12++;
				}

			int num13 = array11.Length / array11.GetLength(1);
			bool flag = true;
			try
			{
				for (i = 0; i < num13; i++)
					Convert.ToInt32(array11[i, 1], 16);
			}
			catch
			{
				flag = false;
			}
			string[,] array12 = new string[num13, num2];
			for (i = 0; i < num13; i++)
			{
				array12[i, 1] = array11[i, 1];
				array12[i, 0] = array11[i, 0];
			}
			if (flag)
			{
				bool flag2 = true;
				while (flag2)
				{
					for (i = 0; i < num13 - 1; i++)
					{
						int num14 = Convert.ToInt32(array11[i, 1], 16);
						int num15 = Convert.ToInt32(array11[i + 1, 1], 16);
						if (num14 > num15)
						{
							string text2 = array11[i, 1];
							string text3 = array11[i, 0];
							array11[i, 1] = array11[i + 1, 1];
							array11[i, 0] = array11[i + 1, 0];
							array11[i + 1, 1] = text2;
							array11[i + 1, 0] = text3;
						}
					}
					flag2 = false;
					for (int num11 = 0; num11 < num13 - 1; num11++)
						if (Convert.ToInt32(array11[num11, 1], 16) > Convert.ToInt32(array11[num11 + 1, 1], 16))
							flag2 = true;
				}
			}
			if (array11.Length == 0 && DisplayError)
				MessageBox.Show("Please select one or more addresses.  Rows highlighted in yellow, blue and/or any changed value in the 'HEX' column will be read/written.");

			string[,] result;
			if (array11.Length == 0)
			{
				NumberOfHighlightedRows = 0;
				result = new string[0, 1];
			}
			else
				result = array11;

			return result;
		}

		public string[] getDataFromGridDataArray(string address)
		{
			string[] array = new string[6];
			for (long num = 0L; num < (long)GRID_DATA_NUM_ROWS; num += 1L)
			{
				checked
				{
					if (GridDataArray[(int)((IntPtr)num), (int)((IntPtr)0L), (int)((IntPtr)0L)] == address)
					{
						array[0] = GridDataArray[(int)((IntPtr)num), (int)((IntPtr)0L), (int)((IntPtr)0L)];
						array[1] = GridDataArray[(int)((IntPtr)num), (int)((IntPtr)1L), (int)((IntPtr)0L)];
						array[2] = GridDataArray[(int)((IntPtr)num), (int)((IntPtr)2L), (int)((IntPtr)0L)];
						array[3] = GridDataArray[(int)((IntPtr)num), (int)((IntPtr)3L), (int)((IntPtr)0L)];
						array[4] = GridDataArray[(int)((IntPtr)num), (int)((IntPtr)4L), (int)((IntPtr)0L)];
						array[5] = GridDataArray[(int)((IntPtr)num), (int)((IntPtr)5L), (int)((IntPtr)0L)];
						break;
					}
				}
			}
			return array;
		}

		public int getNumberOfRegisters()
		{
			return NUM_ROWS - 1;
		}

		public int getBitSize()
		{
			return REG_BIT_SIZE;
		}

		private void gridRegValEditor_Enter(object sender, ScrollEventArgs e)
		{
		}

		private void gridRegValEditor_Scroll(object sender, ScrollEventArgs e)
		{
		}

		public string[] getYellowRegisters()
		{
			Color right = default(Color);
			bool flag = false;
			int num = 1;
			right = Color.Yellow;
			string[] array = new string[NUM_ROWS];
			for (int i = 0; i < NUM_ROWS; i++)
				array[i] = "";
			do
			{
				if (gridRegValEditor.Rows[num].Cells[2].Style.BackColor == right)
				{
					right = Color.Yellow;
					array[num] = gridRegValEditor.Rows[num].Cells[0].Value.ToString();
				}
				num++;
			}
			while (!flag && num < NUM_ROWS);
			int num2 = 0;
			for (int i = 0; i < array.Length; i++)
			{
				if (array[i] != "")
				{
					num2++;
				}
			}
			int num3 = 0;
			string[] array2 = new string[num2];
			for (int i = 0; i < array.Length; i++)
			{
				if (array[i] != "")
				{
					array2[num3] = array[i];
					num3++;
				}
			}
			return array2;
		}

		public string getSelectedRegister()
		{
			int row_index = 1;
			for (;;)
			{
				for (int i = 0; i < NUM_COLS - 1; i++)
				{
					if (gridRegValEditor.Rows[row_index].Cells[i].Selected)
                        return gridRegValEditor.Rows[row_index].Cells[0].Value.ToString();
                }
				row_index++;
				if (row_index >= NUM_ROWS)
                    return "";
            }
		}

		public bool setRegisterValue(ulong reg, string data)
		{
			bool flag = false;
			int row_index = 0;
			do
			{
				row_index++;
				ulong num2 = Convert.ToUInt64(getRegAddressFromTableIndex(row_index), 16);
				if (reg == num2)
				{
					gridRegValEditor.Rows[row_index].Cells[1].Value = data;
					flag = true;
				}
			}
			while (!flag && row_index < NUM_ROWS);
			return flag;
		}

		public bool forceRegisterValue(int rowIndex, string data)
		{
			bool result;
			try
			{
				getRegAddressFromTableIndex(rowIndex);
				gridRegValEditor.Rows[rowIndex].Cells[0].Value = data;
				result = true;
			}
			catch
			{
				result = false;
			}
			return result;
		}

		public bool selectSingleRow(int rowIndex)
		{
			bool result;
			try
			{
				getRegAddressFromTableIndex(rowIndex);
				gridRegValEditor.Rows[rowIndex].Selected = true;
				result = true;
			}
			catch
			{
				result = false;
			}
			return result;
		}

		public bool deselectRows(int rowIndex, bool all)
		{
			bool result;
			try
			{
				if (!all)
				{
					getRegAddressFromTableIndex(rowIndex);
					gridRegValEditor.Rows[rowIndex].Selected = false;
				}
				else
				{
					gridRegValEditor.ClearSelection();
				}
				result = true;
			}
			catch
			{
				result = false;
			}
			return result;
		}

		public bool setRegisterValue(string regHex, string data)
		{
			bool result;
			try
			{
				bool flag = false;
				int num = 0;
				do
				{
					num++;
					if (regHex == getRegAddressFromTableIndex(num))
					{
						gridRegValEditor.Rows[num].Cells[1].Value = data;
						flag = true;
					}
				}
				while (!flag && num < NUM_ROWS);
				result = flag;
			}
			catch
			{
				result = false;
			}
			return result;
		}

		public string getRegAddressFromTableIndex(int tablePosition)
		{
			return gridRegValEditor.Rows[tablePosition].Cells[0].Value.ToString();
		}

		public string[] getAddressGridList()
		{
			string[] array = new string[NUM_ROWS];
			for (int i = 0; i < NUM_ROWS; i++)
			{
				array[i] = gridRegValEditor.Rows[i].HeaderCell.Value.ToString();
			}
			return array;
		}

		public void saveAllValues()
		{
			Color color;
			ColorConverter colorConverter = new ColorConverter();
			for (int i = 0; i < NUM_ROWS; i++)
			{
				for (int j = 0; j < NUM_COLS; j++)
				{
					if (!constructorIsDone)
					{
						color = (Color)colorConverter.ConvertFromString(GridDataArray[i, j, 1]);
						gridRegValEditor.Rows[i].Cells[j].Style.BackColor = color;
					}
					else if (gridRegValEditor.Rows[i].Cells[j].Style.BackColor == Color.Yellow)
					{
						color = (Color)colorConverter.ConvertFromString(GridDataArray_Last[i, j, 1]);
						gridRegValEditor.Rows[i].Cells[j].Style.BackColor = color;
					}
				}
			}
			for (int i = 0; i < NUM_ROWS; i++)
				for (int j = 0; j < NUM_COLS; j++)
				{
					GridDataArray[i, j, 0] = gridRegValEditor.Rows[i].Cells[j].Value.ToString();
					color = gridRegValEditor.Rows[i].Cells[j].Style.BackColor;
					GridDataArray[i, j, 1] = colorConverter.ConvertToString(color);
				}

			for (int i = 0; i < NUM_ROWS; i++)
				for (int j = 0; j < NUM_COLS; j++)
					for (int k = 0; k < 4; k++)
						GridDataArray_Last[i, j, k] = GridDataArray[i, j, k];

			flashGridColor(flashColor, flashTime_ms);
		}

		public void resetAllGridValues()
		{
			for (int i = 0; i < NUM_ROWS; i++)
				for (int j = 0; j < NUM_COLS; j++)
				{
					gridRegValEditor.Rows[i].Cells[j].Value = GridDataArray_Last[i, j, 0];
					GridDataArray[i, j, 0] = gridRegValEditor.Rows[i].Cells[j].Value.ToString();
				}
			saveAllValues();
		}

		public bool Test_AnyHighlightedRows(string[] AddressesOfRowsToHighlight)
		{
			bool flag;
			string[,] highlightedGridAddresses = GetHighlightedGridAddresses(false, false);
			try
			{
				flag = (highlightedGridAddresses[0, 0] != null);
			}
			catch
			{
				flag = false;
			}

			bool flag2 = false;
			if (!flag)
			{
				try
				{
					flag2 = (AddressesOfRowsToHighlight != null && AddressesOfRowsToHighlight[0] != null && !(AddressesOfRowsToHighlight[0] == ""));
				}
				catch
				{
					flag2 = false;
				}
			}
			if (flag2)
				HighlightRows(AddressesOfRowsToHighlight);
			return flag;
		}

		public void HighlightRows(string[] AddressList)
		{
			string[] addressGridList = getAddressGridList();
			for (int i = 0; i < addressGridList.Length; i++)
			{
				for (int j = 0; j < AddressList.Length; j++)
				{
					if (AddressList[j] == addressGridList[i].Substring(0, 2) || AddressList[j] == addressGridList[i])
					{
						int registerRow = getRegisterRow(AddressList[j]);
						for (int k = 0; k < NUM_COLS; k++)
						{
							GridDataArray[registerRow, k, 1] = "Yellow";
							gridRegValEditor.Rows[registerRow].Cells[k].Style.BackColor = Color.Yellow;
						}
					}
				}
			}
		}

		public void zeroOutRegisters()
		{
			string text = "";
			for (int i = 0; i < REG_BIT_SIZE / 4; i++)
				text += "0";
			if (REG_BIT_SIZE % 4 != 0)
				text += "0";

			for (int i = 0; i < NUM_ROWS; i++)
				gridRegValEditor.Rows[i].Cells[0].Value = text;

			for (int i = 0; i < NUM_ROWS; i++)
				for (int j = 1; j < NUM_COLS; j++)
					gridRegValEditor.Rows[i].Cells[j].Value = "0";
		}

		public void zeroOutGridValues()
		{
			zeroOutRegisters();
			saveAllValues();
		}

		public string getGridFromFile(string FileToOpen)
		{
			string text = "";
			OpenFileDialog openFileDialog = new OpenFileDialog();
			openFileDialog.Title = "Open File";
			openFileDialog.Filter = "Text Files|*.txt";
			openFileDialog.FilterIndex = 3;
			openFileDialog.RestoreDirectory = true;
			if (FileToOpen == "")
			{
				openFileDialog.InitialDirectory = "c:\\";
				text = openFileDialog.InitialDirectory;
			}
			FileToOpen = text;
			if (openFileDialog.ShowDialog() == DialogResult.OK)
			{
				string initialDirectory = openFileDialog.InitialDirectory;
				text = openFileDialog.FileName;
				FileToOpen = text;
			}
			char[] separator = new char[]
			{
				'\t'
			};
			string[] array = new string[64];
			int num = 0;
			try
			{
				TextReader textReader = new StreamReader(FileToOpen);
				num = 0;
				if (array.Length == 0)
				{
					string text2 = "ERROR - You have opened a blank file.";
					MessageBox.Show(text2);
					textReader.Close();
					return "";
				}
				while (array[0] != "EOF")
				{
					string text3 = textReader.ReadLine();
					if (text3 == "")
					{
						string text2 = "ERROR - The patern file has a blank row.";
						textReader.Close();
						MessageBox.Show(text2);
						return "";
					}
					text3 = text3.ToUpper();
					array = text3.Split(separator);
					if (array.Length != 0)
					{
						for (int i = 0; i < array.Length; i++)
						{
							array[i] = array[i].Trim();
						}
					}
					if (array[0] != "EOF" && array[0] != "/")
					{
						num++;
						try
						{
							Convert.ToInt64(array[1], 16);
						}
						catch
						{
							string text2 = "ERROR - The address on line " + num + " is not reading in as a HEX value";
							textReader.Close();
							MessageBox.Show(text2);
							return "";
						}
						try
						{
							Convert.ToInt64(array[2], 16);
						}
						catch
						{
							string text2 = "ERROR - The data on line " + num + " is not reading in as a HEX value";
							textReader.Close();
							MessageBox.Show(text2);
							return "";
						}
						int num2 = Convert.ToInt32(array[1], 10);
						if (Convert.ToInt32(array[1], 10) >= NUM_ROWS)
						{
							string text2 = "ERROR - The address on line " + num + " is larger than maximum address of DUT.";
							textReader.Close();
							MessageBox.Show(text2);
							return "";
						}
						double value = Math.Pow(2.0, (double)getBitSize());
						long num3 = Convert.ToInt64(value);
						if (Convert.ToInt64(array[2], 16) > num3)
						{
							string text2 = "ERROR - The data on line " + num + " is larger than maximum value for DUT.";
							textReader.Close();
							MessageBox.Show(text2);
							return "";
						}
					}
				}
				textReader.Close();
				if (num != NUM_ROWS)
				{
					string text2 = string.Concat(new string[]
					{
						"ERROR - DUT requires ",
						NUM_ROWS.ToString(),
						" registers, but file contains ",
						num.ToString(),
						" lines of Addresses"
					});
					MessageBox.Show(text2);
					return "";
				}
			}
			catch
			{
				string text2 = "ERROR - Could not open " + text;
				MessageBox.Show(text2);
				return "";
			}
			TextReader textReader2 = new StreamReader(FileToOpen);
			try
			{
				array[0] = "";
				while (array[0] != "EOF")
				{
					string text3 = textReader2.ReadLine();
					text3 = text3.ToUpper();
					array = text3.Split(separator);
					for (int j = 0; j < array.Length; j++)
					{
						array[j] = array[j].Trim();
					}
					if (array[0] != "/" && array[0] != "EOF")
					{
						string text4 = array[2].ToString();
						string text5 = array[3].ToString();
						string[,] array2 = new string[1, 5];
						array2[0, 1] = text4;
						array2[0, 4] = text5;
						setDataIntoGridDataArray(array2, false);
					}
				}
				saveAllValues();
				textReader2.Close();
			}
			catch
			{
				try
				{
					textReader2.Close();
				}
				catch
				{
				}
			}
			string[] addressGridList = getAddressGridList();
			HighlightRows(addressGridList);
			return text;
		}

		public void Highlight_All_Rows()
		{
			string[] addressGridList = getAddressGridList();
			HighlightRows(addressGridList);
		}

		public string SaveGridToFile(string filename)
		{
			string result = "";
			saveAllValues();
			SaveFileDialog saveFileDialog = new SaveFileDialog();
			saveFileDialog.Filter = "Text Files|*.txt";
			saveFileDialog.FilterIndex = 1;
			string text = "";
			char[] array = filename.ToCharArray();
			if (array.Length > 5)
			{
				if (array[array.Length - 4] != '.')
				{
					filename = "";
				}
			}

			if (filename == "")
				saveFileDialog.InitialDirectory = "c:\\";
			else
				saveFileDialog.InitialDirectory = filename;

			saveFileDialog.RestoreDirectory = true;
			if (saveFileDialog.ShowDialog() == DialogResult.OK)
			{
				filename = saveFileDialog.FileName;
				text = filename;
			}
			try
			{
				TextWriter textWriter = new StreamWriter(text);
				result = text;
				string str = DateTime.Now.ToString();
				textWriter.WriteLine("/\t" + str);
				textWriter.WriteLine("/\tRow\tAddress\tData");
				int num_ROWS = NUM_ROWS;
				for (int i = 0; i < NUM_ROWS; i++)
				{
					string text2 = i.ToString();
					string text3 = GridDataArray[i, 0, 0];
					string text4 = gridRegValEditor.Rows[i].HeaderCell.Value.ToString();
					string value = string.Concat(new string[]
					{
						"\t",
						text2,
						"\t",
						text4,
						"\t",
						text3
					});
					textWriter.WriteLine(value);
				}
				textWriter.WriteLine("EOF");
				textWriter.Close();
			}
			catch
			{
				string text5 = "ERROR - Unable to write file";
				MessageBox.Show(text5);
			}
			return result;
		}

		public Color getFlashColor()
		{
			return flashColor;
		}

		public int getRegisterRow(string reg)
		{
			bool flag = false;
			int num = 0;
			while (!flag && num < NUM_ROWS)
			{
				string text = gridRegValEditor.Rows[num].HeaderCell.Value.ToString();
				if (reg == text.Substring(0, 2) || reg == text)
				{
					flag = true;
				}
				num++;
			}
			num--;
			int result;
			if (flag)
				result = num;
			else
				result = -1;
			return result;
		}

		public void updateBits(int row, string hexValue)
		{
			int reg_BIT_SIZE = REG_BIT_SIZE;
			ulong value = Convert.ToUInt64(hexValue, 16);
			string text = Convert.ToString((long)value, 2);
			int length = text.Length;
			for (int i = 0; i < length; i++)
			{
				GridDataArray[row, reg_BIT_SIZE - i, 0] = text[length - 1 - i].ToString();
				gridRegValEditor.Rows[row].Cells[reg_BIT_SIZE - i].Value = text[length - 1 - i];
			}
			if (length < REG_BIT_SIZE)
			{
				for (int i = length; i < REG_BIT_SIZE; i++)
				{
					gridRegValEditor.Rows[row].Cells[reg_BIT_SIZE - i].Value = "0";
					GridDataArray[row, reg_BIT_SIZE - i, 0] = "0";
				}
			}
		}

		public byte[] hexToByteArray(string hexVal)
		{
			int length = hexVal.Length;
			byte[] array = new byte[length / 2 + length % 2];
			if (length % 2 == 1)
			{
				array[0] = Convert.ToByte(hexVal[0].ToString(), 16);
				for (int i = 1; i < length; i += 2)
					array[i / 2 + 1] = Convert.ToByte(hexVal.Substring(i, 2), 16);
			}
			else
			{
				for (int i = 0; i < length; i += 2)
					array[i / 2] = Convert.ToByte(hexVal.Substring(i, 2), 16);
			}
			return array;
		}

		public bool validateCellData(int row, int col)
		{
			string text = gridRegValEditor.Rows[row].Cells[col].Value.ToString();
			if (text != null && text != "")
			{
				ulong num;
				try
				{
					hexToByteArray(gridRegValEditor.Rows[row].Cells[col].Value.ToString());
					num = Convert.ToUInt64(GridDataArray[row, col, 0], 16);
				}
				catch
				{
					MessageBox.Show("Invalid Data: Cannot convert to HEX value");
					GridDataArray[row, col, 0] = "";
					return true;
				}
				if (num > MAX_HEX_VAL)
				{
					MessageBox.Show("Invalid Data: Entered value is too big for " + REG_BIT_SIZE + " byte register");
					GridDataArray[row, col, 0] = "";
					return true;
				}
				updateBits(row, GridDataArray[row, col, 0]);
			}
			return false;
		}

		public string formatData(string data)
		{
			data = data.ToUpper();
			long num = 0L;
			num = 0L;
			try
			{
				num = Convert.ToInt64(data, 16);
			}
			catch
			{
				num = 1L;
			}
			if (num == 0L)
			{
				if ((float)data.Length > (float)REG_BIT_SIZE / 4f)
				{
					data = "0";
				}
			}
			if ((float)data.Length < (float)REG_BIT_SIZE / 4f)
			{
				for (int i = 0; i < REG_BIT_SIZE / 4 - data.Length; i++)
				{
					data = "0" + data;
				}
				if (REG_BIT_SIZE % 4 != 0)
				{
					data = "0" + data;
				}
			}
			return data;
		}

		public void InitializeTable(int _Height, int _Width, string[] OptionalColHeaderLabels, string[] OptionalRowHeaderLabels, bool UseColors, string topLeftCellText, string ColZeroText, bool GridIsReadOnly)
		{
			gridRegValEditor.RowHeadersVisible = false;
			gridRegValEditor.ColumnHeadersVisible = false;
			gridRegValEditor.AllowUserToAddRows = false;
			gridRegValEditor.AllowUserToDeleteRows = false;
			gridRegValEditor.AllowUserToResizeColumns = false;
			gridRegValEditor.AllowUserToResizeRows = false;
			gridRegValEditor.BorderStyle = BorderStyle.Fixed3D;
			gridRegValEditor.ClipboardCopyMode = DataGridViewClipboardCopyMode.EnableWithoutHeaderText;
			gridRegValEditor.ColumnHeadersHeightSizeMode = DataGridViewColumnHeadersHeightSizeMode.DisableResizing;
			gridRegValEditor.Dock = DockStyle.Fill;
			gridRegValEditor.BackgroundColor = Color.White;
			gridRegValEditor.ScrollBars = ScrollBars.Both;
			for (int i = 0; i < NUM_COLS; i++)
			{
				gridRegValEditor.Columns.Add("", "");
			}
			for (int i = 0; i < NUM_ROWS; i++)
			{
				gridRegValEditor.Rows.Add(new object[]
				{
					"",
					""
				});
			}
			if (GridIsReadOnly)
				for (int i = 0; i < NUM_ROWS; i++)
					for (int j = 0; j < NUM_COLS; j++)
						gridRegValEditor.Rows[i].Cells[j].ReadOnly = true;

			gridRegValEditor.ColumnHeadersVisible = true;
			gridRegValEditor.TopLeftHeaderCell.Value = topLeftCellText;
			gridRegValEditor.Columns[0].HeaderCell.Value = ColZeroText;
			bool flag = true;
			try
			{
				if (OptionalColHeaderLabels.Length == 0)
				{
					flag = false;
				}
				else if (OptionalColHeaderLabels[0] == null)
				{
					flag = false;
				}
			}
			catch
			{
				flag = false;
			}
			if (!flag)
				for (int i = 1; i < REG_BIT_SIZE + 1; i++)
					gridRegValEditor.Columns[i].HeaderCell.Value = ((REG_BIT_SIZE - i).ToString() ?? "");
			else
				for (int i = 0; i < NUM_COLS - 1; i++)
					gridRegValEditor.Columns[i + 1].HeaderCell.Value = OptionalColHeaderLabels[i];

			gridRegValEditor.RowHeadersVisible = true;
			bool flag2 = true;
			try
			{
				if (OptionalRowHeaderLabels.Length == 0)
					flag2 = false;
				else if (OptionalRowHeaderLabels[0] == null)
					flag2 = false;
			}
			catch
			{
				flag2 = false;
			}
			if (!flag2)
			{
				int num = 0;
				for (int i = 0; i < NUM_ROWS; i++)
				{
					string text = Convert.ToString(num, 16);
					text = text.ToUpper();
					gridRegValEditor.Rows[i].HeaderCell.Value = text;
					num++;
				}
			}
			else
			{
				for (int i = 0; i < NUM_ROWS; i++)
				{
					gridRegValEditor.Rows[i].HeaderCell.Value = OptionalRowHeaderLabels[i];
				}
			}
			int num2 = 0;
			int num3 = 0;
			if (flag2)
			{
				for (int j = 0; j < OptionalRowHeaderLabels.Length; j++)
				{
					int length = OptionalRowHeaderLabels[j].Length;
					if (length > num2)
					{
						num2 = length;
					}
				}
			}
			else
			{
				for (int k = 1; k <= NUM_ROWS; k++)
				{
					double value = Math.Pow(2.0, (double)k);
					int num4 = Convert.ToInt32(value);
					if (num4 >= NUM_ROWS)
					{
						int num5 = k % 4;
						if (num5 == 0)
						{
							num3 = k / 4;
						}
						else
						{
							num3 = k / 4 + 1;
						}
						break;
					}
				}
			}
			int num6 = topLeftCellText.Length + 12;
			int num7 = REG_BIT_SIZE % 4;
			int num8;
			if (num7 == 0)
			{
				num8 = REG_BIT_SIZE / 4;
			}
			else
			{
				num8 = REG_BIT_SIZE / 4 + 1;
			}
			int length2 = ColZeroText.Length;
			int num9 = 0;
			if (flag)
			{
				for (int j = 0; j < OptionalColHeaderLabels.Length; j++)
				{
					int length = OptionalColHeaderLabels[j].Length;
					if (length > num9)
					{
						num9 = length;
					}
				}
			}
			string text2 = Convert.ToString(REG_BIT_SIZE, 10);
			int num10 = text2.Length + 1;
			int num11;
			if (num10 > num9)
			{
				num11 = num10;
			}
			else
			{
				num11 = num9;
			}
			for (int i = 0; i < NUM_ROWS; i++)
			{
				gridRegValEditor.Rows[i].Height = 26;
			}
			gridRegValEditor.ColumnHeadersHeight = 26;
			int num12 = 7;
			int num13 = 14;
			int num14 = num13 + num12 * num6;
			num12 = 7;
			int num15 = 18;
			num13 = 14 + num15;
			int num16 = num13 + num12 * num3;
			if (num14 > num16)
			{
				gridRegValEditor.RowHeadersWidth = num14;
			}
			else
			{
				gridRegValEditor.RowHeadersWidth = num16;
			}
			num12 = 7;
			num13 = 15;
			num14 = num13 + num12 * length2;
			num12 = 7;
			num13 = 15;
			num16 = num13 + num12 * num8;
			if (num14 > num16)
			{
				gridRegValEditor.Columns[0].Width = num14 + 5;
			}
			else
			{
				gridRegValEditor.Columns[0].Width = num16 + 5;
			}
			num12 = 8;
			num13 = 6;
			int num17 = num13 + num12 * num11;
			for (int l = 1; l < NUM_COLS; l++)
			{
				gridRegValEditor.Columns[l].Width = num17 + 2;
			}
			Size size = new Size(_Width, _Height);
			gridRegValEditor.Size = size;
			zeroOutRegisters();
			int num18;
			if (REG_BIT_SIZE % 2 == 0)
			{
				if (REG_BIT_SIZE == 2)
				{
					num18 = 1;
				}
				else if (REG_BIT_SIZE == 4)
				{
					num18 = 2;
				}
				else if (REG_BIT_SIZE == 6)
				{
					num18 = 3;
				}
				else if (REG_BIT_SIZE == 8)
				{
					num18 = 4;
				}
				else if (REG_BIT_SIZE % 4 == 0)
				{
					num18 = REG_BIT_SIZE / (REG_BIT_SIZE / 4);
				}
				else if (REG_BIT_SIZE % 4 == 2)
				{
					num18 = REG_BIT_SIZE / (REG_BIT_SIZE / 2);
				}
				else
				{
					num18 = 2;
					MessageBox.Show("Error in 'Grid Initialize()'.  Number of colums that are the same color is un initialized.");
				}
			}
			else
			{
				num18 = 0;
			}
			ColorConverter colorConverter = new ColorConverter();
			Color color = default(Color);
			for (int i = 0; i < NUM_ROWS; i++)
			{
				color = Color.White;
				GridDataArray[i, 0, 1] = colorConverter.ConvertToString(color);
				Grid[i, 0, 1] = colorConverter.ConvertToString(color);
				GridDataArray_Last[i, 0, 1] = colorConverter.ConvertToString(color);
			}
			for (int i = 0; i < NUM_ROWS; i++)
			{
				int num19 = 0;
				for (int j = 1; j < NUM_COLS; j++)
				{
					if (num19 >= num18 * 2)
						num19 = 0;
					if (num19 < num18)
					{
						if (UseColors)
						{
							if (20 <= i && i < 44)
								gridRegValEditor.Rows[i].Cells[j].Style.BackColor = Color.White;
							else if (44 <= i && i < 58)
								gridRegValEditor.Rows[i].Cells[j].Style.BackColor = Color.White;
							else if (58 <= i && i < 91)
								gridRegValEditor.Rows[i].Cells[j].Style.BackColor = Color.White;
							else
								gridRegValEditor.Rows[i].Cells[j].Style.BackColor = Color.White;
							color = gridRegValEditor.Rows[i].Cells[j].Style.BackColor;
						}
						else
							gridRegValEditor.Rows[i].Cells[j].Style.BackColor = Color.White;

						GridDataArray[i, j, 1] = colorConverter.ConvertToString(color);
						Grid[i, j, 1] = colorConverter.ConvertToString(color);
						GridDataArray_Last[i, j, 1] = colorConverter.ConvertToString(color);
					}
					else
					{
						if (UseColors)
							gridRegValEditor.Rows[i].Cells[j].Style.BackColor = Color.White;
						else
							gridRegValEditor.Rows[i].Cells[j].Style.BackColor = Color.White;

						color = gridRegValEditor.Rows[i].Cells[j].Style.BackColor;
						GridDataArray[i, j, 1] = colorConverter.ConvertToString(color);
						Grid[i, j, 1] = colorConverter.ConvertToString(color);
						GridDataArray_Last[i, j, 1] = colorConverter.ConvertToString(color);
					}
					num19++;
				}
			}
			timeStamp = default(DateTime);
			timeStamp = DateTime.Now;
		}

		public void dataGridView_CellValueChanged(object sender, DataGridViewCellEventArgs e)
		{
			int firstDisplayedScrollingRowIndex = gridRegValEditor.FirstDisplayedScrollingRowIndex;
			gridRegValEditor.FirstDisplayedScrollingRowIndex = firstDisplayedScrollingRowIndex;
			int rowIndex = e.RowIndex;
			int columnIndex = e.ColumnIndex;
			Color color = default(Color);
			ColorConverter colorConverter = new ColorConverter();
			if (rowIndex >= 0 && columnIndex >= 0)
			{
				string text = gridRegValEditor.Rows[rowIndex].Cells[columnIndex].Value.ToString();
				GridDataArray[rowIndex, columnIndex, 0] = text;
				if (columnIndex == 0)
				{
					bool flag = validateCellData(rowIndex, columnIndex);
					if (flag)
					{
						text = Grid[rowIndex, columnIndex, 0];
					}
					gridRegValEditor.Rows[rowIndex].Cells[columnIndex].Value = formatData(text);
					GridDataArray[rowIndex, columnIndex, 0] = formatData(text);
					string text2 = gridRegValEditor.Rows[rowIndex].Cells[columnIndex].Value.ToString();
					if (gridRegValEditor.Rows[rowIndex].Cells[columnIndex].Value.ToString() == GridDataArray_Last[rowIndex, columnIndex, 0])
					{
						for (int i = 0; i < NUM_COLS; i++)
						{
							color = (Color)colorConverter.ConvertFromString(GridDataArray_Last[rowIndex, i, 1]);
							gridRegValEditor.Rows[rowIndex].Cells[i].Style.BackColor = color;
							GridDataArray_Last[rowIndex, i, 1] = colorConverter.ConvertToString(color);
						}
					}
					else
					{
						for (int i = 0; i < NUM_COLS; i++)
						{
							GridDataArray[rowIndex, i, 1] = "Yellow";
							gridRegValEditor.Rows[rowIndex].Cells[i].Style.BackColor = Color.Yellow;
						}
					}
				}
				if (constructorIsDone)
				{
					for (int j = 0; j < NUM_ROWS; j++)
					{
						for (int k = 0; k < NUM_COLS; k++)
						{
							for (int l = 0; l < 1; l++)
							{
								Grid[j, k, l] = gridRegValEditor.Rows[j].Cells[k].Value.ToString();
							}
						}
					}
				}
			}
		}

		public void dataGridView_KeyUp(object sender, KeyEventArgs e)
		{
			if (e.KeyCode == Keys.Return
				&& gridRegValEditor.CurrentCell.RowIndex != gridRegValEditor.Rows.Count
				&& autoUpdateControl
				)
			{
				SendKeys.Send("{UP}");
				Tools.timeDelay(1, "MS");
				OnautoautoUpdateHandle();
				SendKeys.Send("{DOWN}");
			}
			e.Handled = true;
		}

		public void dataGridView_SelectionChanged(object sender, EventArgs e)
		{
			if (resetRow)
			{
				resetRow = false;
				gridRegValEditor.CurrentCell = gridRegValEditor.Rows[currentRow].Cells[0];
			}
		}

		public void dataGridView_CellEndEdit(object sender, DataGridViewCellEventArgs e)
		{
			resetRow = true;
			currentRow = e.RowIndex;
		}

		public void dataGridView_DataBindingComplete(object sender, DataGridViewBindingCompleteEventArgs e)
		{
			gridRegValEditor.SelectionChanged += dataGridView_SelectionChanged;
		}

		public void dataGridView_CellDoubleClick(object sender, DataGridViewCellEventArgs e)
		{
			int firstDisplayedScrollingRowIndex = gridRegValEditor.FirstDisplayedScrollingRowIndex;
			gridRegValEditor.FirstDisplayedScrollingRowIndex = firstDisplayedScrollingRowIndex;
		}

		public void dataGridView_CellMouseClick(object sender, DataGridViewCellMouseEventArgs e)
		{
			timeStamp = DateTime.Now;
			int rowIndex = e.RowIndex;
			int columnIndex = e.ColumnIndex;
			mRow = rowIndex;
			mColumn = columnIndex;
			OnRegisterClickHandle();
			gridRegValEditor.ClearSelection();
			gridRegValEditor.SelectionMode = DataGridViewSelectionMode.FullRowSelect;
			gridRegValEditor.MultiSelect = false;
			int firstDisplayedScrollingRowIndex = gridRegValEditor.FirstDisplayedScrollingRowIndex;
			gridRegValEditor.FirstDisplayedScrollingRowIndex = firstDisplayedScrollingRowIndex;
			try
			{
				gridRegValEditor.Rows[mRow].Cells[0].Selected = true;
			}
			catch
			{
				return;
			}
			if (rowIndex >= 0 && columnIndex >= 0)
			{
				if (columnIndex != 0)
				{
					string text = gridRegValEditor.Rows[rowIndex].Cells[0].Value.ToString();
					if (gridRegValEditor.Rows[rowIndex].Cells[columnIndex].Value.ToString() == "0")
					{
						ulong num = pow(2, NUM_COLS - 1 - columnIndex);
						string text2 = (Convert.ToUInt64(gridRegValEditor.Rows[rowIndex].Cells[0].Value.ToString(), 16) + num).ToString("X");
						GridDataArray[rowIndex, 1, 0] = text2;
						gridRegValEditor.Rows[rowIndex].Cells[0].Value = text2;
					}
					else
					{
						ulong num2 = pow(2, NUM_COLS - 1 - columnIndex);
						string text2 = (Convert.ToUInt64(gridRegValEditor.Rows[rowIndex].Cells[0].Value.ToString(), 16) - num2).ToString("X");
						GridDataArray[rowIndex, 1, 0] = text2;
						gridRegValEditor.Rows[rowIndex].Cells[0].Value = text2;
					}
					if (constructorIsDone)
					{
						for (int i = 0; i < NUM_ROWS; i++)
						{
							for (int j = 0; j < NUM_COLS; j++)
							{
								for (int k = 0; k < 1; k++)
								{
									Grid[i, j, k] = gridRegValEditor.Rows[i].Cells[j].Value.ToString();
								}
							}
						}
					}
				}
				if (autoUpdateControl && columnIndex != 0)
				{
					OnautoautoUpdateHandle();
				}
			}
		}

		private void temp_updateDeviceUnlockStatus(object sender, EventArgs e)
		{
			throw new NotImplementedException();
		}

		public ulong pow(int baseVal, int expVal)
		{
			ulong result;
			if (expVal == 0)
				result = 1UL;
			else if (expVal == 1)
				result = (ulong)((long)baseVal);
			else
			{
				ulong num = (ulong)((long)baseVal);
				for (int i = 2; i <= expVal; i++)
				{
					num *= (ulong)((long)baseVal);
				}
				result = num;
			}
			return result;
		}

		public void redrawGrid()
		{
			gridRegValEditor.Refresh();
		}

		public void flashGridColor(Color inputColor, double flashTime_ms)
		{
			Color[,] array = new Color[NUM_ROWS, NUM_COLS];
			for (int i = 0; i < NUM_ROWS; i++)
			{
				for (int j = 0; j < NUM_COLS; j++)
					array[i, j] = gridRegValEditor.Rows[i].Cells[j].Style.BackColor;
			}
			for (int i = 0; i < NUM_ROWS; i++)
			{
				for (int j = 0; j < NUM_COLS; j++)
					gridRegValEditor.Rows[i].Cells[j].Style.BackColor = inputColor;
			}
			gridRegValEditor.Refresh();
			Tools.timeDelay(flashTime_ms, "ms");
			for (int i = 0; i < NUM_ROWS; i++)
			{
				for (int j = 0; j < NUM_COLS; j++)
					gridRegValEditor.Rows[i].Cells[j].Style.BackColor = array[i, j];
			}
			gridRegValEditor.Refresh();
		}

		public ulong getMaxRegVal(int regSize)
		{
			ulong num = 0UL;
			for (int i = 0; i < regSize; i++)
				num += pow(2, i);
			return num;
		}

		public bool anyCellHighlighted()
		{
			bool result = false;
			string[,] highlightedGridAddresses = GetHighlightedGridAddresses(false, false);
			try
			{
				result = (highlightedGridAddresses[0, 0] != null);
			}
			catch
			{
				result = false;
			}
			return result;
		}

		private DataGridView gridRegValEditor;
		public int REG_BIT_SIZE;
		public int NUM_COLS;
		public int NUM_ROWS;
		public string TABLE_DATA_SET;

		public ulong MAX_HEX_VAL;
		public string[,,] GridDataArray;
		public string[,,] GridDataArray_Last;
		public string[,,] Grid;
		public int GRID_DATA_NUM_ROWS;
		public int GRID_DATA_NUM_COLS;
		public bool DisplayErrors = true;
		public bool constructorIsDone;
		public bool Exists = false;
		public int NumberOfHighlightedRows = 0;
		public DateTime timeStamp;
		public Color flashColor;
		public double flashTime_ms;
		public string gridName;
		public bool autoUpdateControl = true;
		private int currentRow;
		private bool resetRow = false;
		public bool immidiateUpdate = false;
		public int prevReload = 0;
		public int mRow;
		public int mColumn;
	}
}
