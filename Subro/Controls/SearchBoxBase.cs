using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Drawing;
using System.Linq;
using System.Windows.Forms;

namespace Subro.Controls
{
    public abstract class SearchBoxBase : UserControl, ISupportInitialize
    {
        public SearchBoxBase()
        {
            searchmatcher = new StringSearchMatcher(GetDefaultMode());
            InitializeComponent();
        }

        protected virtual SearchBoxMode GetDefaultMode()
        {
            return SearchBoxMode.Lookup_Wildcards;
        }

        public SearchBoxMode Mode
        {
            get
            {
                return searchmatcher.Mode;
            }
            set
            {
                if (Mode != value)
                {
                    searchmatcher.Mode = value;
                    btnNext.Visible = (value != SearchBoxMode.Filter);
                    NotifyStateChanged(true);
                }
            }
        }

        private bool ShouldSerializeMode()
        {
            return Mode != GetDefaultMode();
        }

        public void ResetMode()
        {
            Mode = GetDefaultMode();
        }

        public Func<string, bool> SearchDelegate
        {
            get
            {
                return searchmatcher.SearchDelegate;
            }
        }

        [DefaultValue(false)]
        public bool AlwaysSearchInnerText
        {
            get
            {
                return alwayswildcard;
            }
            set
            {
                if (alwayswildcard != value)
                {
                    alwayswildcard = value;
                    searchmatcher.AlwaysSearchInnerText = value;
                    NotifyStateChanged(false);
                }
            }
        }

        protected void NotifyStateChanged(bool resettext)
        {
            if (!IsBusy && !disposed)
            {
                ResetStartPosition(null);
                NullResult = false;
                checkvalid();
                if (txt.TextLength > 0)
                {
                    if (resettext)
                    {
                        txt.Clear();
                    }
                    else
                    {
                        check();
                    }
                }
            }
        }

        protected sealed override void Dispose(bool disposing)
        {
            if (disposing && components != null)
            {
                components.Dispose();
            }
            base.Dispose(disposing);
            while (registeredcontrols.Count > 0)
            {
                UnRegisterControl(registeredcontrols[0]);
            }
            disposed = true;
            OnDisposed();
        }

        protected virtual void OnDisposed()
        {
        }

        public bool IsInitializing
        {
            get
            {
                return initializing > 0;
            }
        }

        public void BeginInit()
        {
            initializing++;
        }

        public void EndInit()
        {
            if (--initializing == 0)
            {
                OnEndInit();
            }
        }

        protected virtual void OnEndInit()
        {
            NotifyStateChanged(false);
        }

        private void checkvalid()
        {
            isvalid = CheckIsReady();
            if (isvalid && !Supports(Mode))
            {
                isvalid = false;
                OnInvalidModeSelected();
            }
            txt.Enabled = isvalid;
        }

        public bool IsValid
        {
            get
            {
                return isvalid;
            }
        }

        protected virtual void OnInvalidModeSelected()
        {
            Exception ex = new Exception("Source does not support " + Mode);
            if (base.DesignMode)
            {
                throw ex;
            }
            ShowException(ex);
        }

        protected virtual bool CheckIsReady()
        {
            return initializing == 0;
        }

        protected virtual bool Supports(SearchBoxMode Mode)
        {
            return true;
        }

        [Category("Label")]
        [DefaultValue(false)]
        public bool ShowLabel
        {
            get
            {
                return lbl != null;
            }
            set
            {
                if (ShowLabel != value)
                {
                    if (value)
                    {
                        lbl = new Label();
                        setlabeltext();
                        lbl.Dock = DockStyle.Left;
                        lbl.AutoSize = true;
                        base.Controls.Add(lbl);
                    }
                    else
                    {
                        lbl.Dispose();
                        lbl = null;
                    }
                }
            }
        }

        private void setlabeltext()
        {
            if (labeltext == null)
            {
                string text = "Search";
                lbl.Text = text;
            }
            else
            {
                lbl.Text = labeltext;
            }
        }

        [Category("Label")]
        [DefaultValue(null)]
        public string CustomLabelText
        {
            get
            {
                return labeltext;
            }
            set
            {
                labeltext = value;
                if (lbl == null)
                {
                    if (base.DesignMode)
                    {
                        ShowLabel = true;
                    }
                }
                else
                {
                    setlabeltext();
                }
            }
        }

        protected bool HandleKey(KeyEventArgs k)
        {
            return HandleKey(k.KeyCode, k.Modifiers);
        }

        protected bool HandleKey(Keys key, Keys mod)
        {
            if (key == Keys.Back)
            {
                if (mod == Keys.Control)
                {
                    Text = null;
                }
                else if (Text.Length > 0)
                {
                    Text = Text.Substring(0, Text.Length - 1);
                }
            }
            else
            {
                if (key != Keys.Escape)
                {
                    return false;
                }
                Text = null;
            }
            return true;
        }

        protected bool HandleKey(KeyPressEventArgs e)
        {
            char keyChar = e.KeyChar;
            bool result;
            if (keyChar < ' ' || keyChar > '\u007f')
            {
                result = false;
            }
            else
            {
                Text += keyChar.ToString();
                result = true;
            }
            return result;
        }

        private void c_KeyPress(object sender, KeyPressEventArgs e)
        {
            if (HandleRegisteredKeyDowns)
            {
                e.Handled = HandleKey(e);
            }
        }

        private void c_KeyDown(object sender, KeyEventArgs e)
        {
            if (HandleRegisteredKeyDowns)
            {
                e.Handled = HandleKey(e);
            }
        }

        protected void RegisterControl(Control c)
        {
            if (!registeredcontrols.Contains(c))
            {
                registeredcontrols.Add(c);
                c.KeyDown += c_KeyDown;
                c.KeyPress += c_KeyPress;
            }
        }

        protected void UnRegisterControl(Control c)
        {
            if (registeredcontrols.Remove(c))
            {
                c.KeyDown -= c_KeyDown;
                c.KeyPress -= c_KeyPress;
            }
        }

        private void txt_TextChanged(object sender, EventArgs e)
        {
            searchmatcher.SearchText = Text;
            check();
            OnTextChanged(e);
        }

        private void btnNext_Click(object sender, EventArgs e)
        {
            SearchNext();
        }

        private void btnClear_Click(object sender, EventArgs e)
        {
            Text = null;
        }

        [DefaultValue(null)]
        public override string Text
        {
            get
            {
                return txt.Text;
            }
            set
            {
                txt.Text = value;
            }
        }

        public int TextLength
        {
            get
            {
                return searchmatcher.TextLength;
            }
        }

        private void check()
        {
            setvisible();
            if (isvalid)
            {
                if (txt.TextLength < prevlen)
                {
                    ResetStartPosition(null);
                }
                prevlen = txt.TextLength;
                if (Mode == SearchBoxMode.Filter)
                {
                    Filter();
                }
                else
                {
                    Search();
                }
                btnNext.Enabled = (prevlen > 0 && !nullresult);
            }
        }

        private void setvisible()
        {
            if (autohide)
            {
                base.Visible = (txt.TextLength > 0);
            }
        }

        [DefaultValue(false)]
        public bool AutoHide
        {
            get
            {
                return autohide;
            }
            set
            {
                if (autohide != value)
                {
                    autohide = value;
                    setvisible();
                }
            }
        }

        protected abstract void ResetStartPosition(object storedpos);

        protected abstract bool IncreasePosition();

        protected abstract void SetFoundPosition();

        [Browsable(false)]
        [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
        public bool NullResult
        {
            get
            {
                return nullresult;
            }
            private set
            {
                if (nullresult != value)
                {
                    nullresult = value;
                    txt.BackColor = (value ? notfoundcol : Color.Empty);
                }
            }
        }

        public Color NotFoundColor
        {
            get
            {
                return notfoundcol;
            }
            set
            {
                notfoundcol = value;
            }
        }

        private bool ShouldSerializeNotFoundColor()
        {
            return notfoundcol != Color.Red;
        }

        public bool Search()
        {
            bool flag = search();
            NullResult = (txt.TextLength > 0 && !flag);
            return flag;
        }

        public bool IsSearching
        {
            get
            {
                return searching;
            }
        }

        public bool IsFiltering
        {
            get
            {
                return filtering;
            }
        }

        public bool IsBusy
        {
            get
            {
                return searching || filtering || initializing > 0;
            }
        }

        public bool SearchNext()
        {
            IncreasePosition();
            return Search();
        }

        private bool search()
        {
            searching = true;
            try
            {
                return txt.TextLength > 0 && search(searchmatcher);
            }
            catch (Exception ex)
            {
                ShowException(ex);
            }
            finally
            {
                searching = false;
            }
            return false;
        }

        protected abstract object GetPosition();

        protected abstract object GetCurrent();

        protected virtual bool search(StringSearchMatcher search)
        {
            object position = GetPosition();
            for (; ; )
            {
                object current = GetCurrent();
                if (current != null && search.Matches(current.ToString()))
                {
                    break;
                }
                if (!IncreasePosition())
                {
                    ResetStartPosition(position);
                    return false;
                }
            }
            SetFoundPosition();
            return true;
        }

        public void Filter()
        {
            filtering = true;
            try
            {
                filter(searchmatcher);
            }
            catch (Exception ex)
            {
                ShowException(ex);
            }
            finally
            {
                filtering = false;
            }
        }

        protected virtual void ShowException(Exception ex)
        {
            if (!base.Disposing)
            {
                MessageBox.Show(ex.Message);
            }
        }

        protected abstract void filter(StringSearchMatcher search);

        public TextBox TextBox
        {
            get
            {
                return txt;
            }
        }

        [DefaultValue(false)]
        public bool ShowOptionsButton
        {
            get
            {
                return btnOptions != null;
            }
            set
            {
                if (ShowOptionsButton != value)
                {
                    if (value)
                    {
                        btnOptions = new ContextMenuStripButton();
                        btnOptions.Dock = DockStyle.Right;
                        btnOptions.ContextMenuStrip = menu;
                        base.Controls.Add(btnOptions);
                    }
                    else
                    {
                        btnOptions.Dispose();
                        btnOptions = null;
                    }
                }
            }
        }

        private void menu_Opening(object sender, CancelEventArgs e)
        {
            if (modeitems == null)
            {
                modeitems = (from SearchBoxMode sm in Enum.GetValues(typeof(SearchBoxMode))
                             select new SearchBoxBase.ModeItem(this, sm)).ToArray<SearchBoxBase.ModeItem>();
                for (int i = 0; i < modeitems.Length; i++)
                {
                    menu.Items.Insert(i, modeitems[i]);
                }
                OnOpeningContextMenu(menu, true);
            }
            else
            {
                foreach (SearchBoxBase.ModeItem modeItem in modeitems)
                {
                    modeItem.Check();
                }
                OnOpeningContextMenu(menu, false);
            }
            tsInner.Checked = alwayswildcard;
            tsClear.Enabled = (txt.TextLength > 0);
        }

        protected virtual void OnOpeningContextMenu(ContextMenuStrip menu, bool FirstTime)
        {
        }

        private void tsClear_Click(object sender, EventArgs e)
        {
            txt.Clear();
        }

        private void tsInner_Click(object sender, EventArgs e)
        {
            AlwaysSearchInnerText = !alwayswildcard;
        }

        private void InitializeComponent()
        {
            components = new Container();
            ComponentResourceManager resources = new ComponentResourceManager(typeof(SearchBoxBase));
            txt = new TextBox();
            btnNext = new Button();
            menu = new ContextMenuStrip(components);
            toolStripSeparator1 = new ToolStripSeparator();
            tsClear = new ToolStripMenuItem();
            tsInner = new ToolStripMenuItem();
            toolStripSeparator2 = new ToolStripSeparator();
            menu.SuspendLayout();
            base.SuspendLayout();
            txt.BorderStyle = BorderStyle.FixedSingle;
            txt.Dock = DockStyle.Fill;
            txt.Location = new Point(0, 0);
            txt.Name = "txt";
            txt.Size = new Size(153, 20);
            txt.TabIndex = 0;
            txt.TextChanged += txt_TextChanged;
            btnNext.Dock = DockStyle.Right;
            btnNext.Image = (Image)resources.GetObject("btnNext.Image");
            btnNext.Location = new Point(153, 0);
            btnNext.Name = "btnNext";
            btnNext.Size = new Size(20, 21);
            btnNext.TabIndex = 0;
            btnNext.UseVisualStyleBackColor = true;
            btnNext.Click += btnNext_Click;
            menu.Items.AddRange(new ToolStripItem[]
            {
                toolStripSeparator1,
                tsInner,
                toolStripSeparator2,
                tsClear
            });
            menu.Name = "menu";
            menu.Size = new Size(162, 60);
            menu.Opening += menu_Opening;
            toolStripSeparator1.Name = "toolStripSeparator1";
            toolStripSeparator1.Size = new Size(177, 6);
            tsClear.Name = "tsClear";
            tsClear.Size = new Size(161, 22);
            tsClear.Text = "Clear";
            tsClear.Click += tsClear_Click;
            tsInner.Name = "tsInner";
            tsInner.Click += tsInner_Click;
            tsInner.Size = new Size(161, 22);
            tsInner.Text = "Search inner text";
            tsInner.ToolTipText = "If checked, the entire text is searched, otherwise only values starting with the searchvalues are searched";
            toolStripSeparator2.Name = "toolStripSeparator2";
            toolStripSeparator2.Size = new Size(177, 6);
            base.AutoScaleDimensions = new SizeF(6f, 13f);
            base.AutoScaleMode = AutoScaleMode.Font;
            ContextMenuStrip = menu;
            base.Controls.Add(txt);
            base.Controls.Add(btnNext);
            base.Name = "SearchBoxBase";
            base.Size = new Size(173, 21);
            menu.ResumeLayout(false);
            base.ResumeLayout(false);
            base.PerformLayout();
        }

        private StringSearchMatcher searchmatcher;

        private bool alwayswildcard = false;

        private bool disposed;

        private int initializing;

        private bool isvalid;

        private Label lbl;

        private string labeltext = null;

        protected bool HandleRegisteredKeyDowns = true;

        private List<Control> registeredcontrols = new List<Control>();

        private int prevlen;

        private bool autohide;

        private bool nullresult;

        private Color notfoundcol = Color.Red;

        private bool searching;

        private bool filtering;

        private ContextMenuStripButton btnOptions;

        private SearchBoxBase.ModeItem[] modeitems;

        private IContainer components = null;

        private TextBox txt;

        private Button btnNext;

        protected ContextMenuStrip menu;

        private ToolStripSeparator toolStripSeparator1;

        private ToolStripMenuItem tsClear;

        private ToolStripMenuItem tsInner;

        private ToolStripSeparator toolStripSeparator2;

        protected class SearchBoxItem : ToolStripMenuItem
        {
            public SearchBoxItem(SearchBoxBase sb)
            {
                SearchBox = sb;
            }

            public readonly SearchBoxBase SearchBox;
        }

        protected class ModeItem : SearchBoxBase.SearchBoxItem
        {
            public ModeItem(SearchBoxBase sb, SearchBoxMode mode) : base(sb)
            {
                Mode = mode;
                Text = Mode.ToString();
                Check();
            }

            public void Check()
            {
                base.Checked = (SearchBox.Mode == Mode);
            }

            protected override void OnClick(EventArgs e)
            {
                SearchBox.Mode = Mode;
            }

            public readonly SearchBoxMode Mode;
        }
    }
}
