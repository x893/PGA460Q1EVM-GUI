using System;
using System.ComponentModel;
using System.Windows.Forms;

namespace Subro.Controls
{
	public class TreeviewSearchBox : SearchBoxBase
	{
		protected override void ResetStartPosition(object storedpos)
		{
			if (storedpos != null)
				m_lastnode = (TreeNode)storedpos;
			else if (m_tv == null || m_tv.Nodes.Count == 0)
				m_lastnode = null;
			else
				m_lastnode = m_tv.Nodes[0];
		}

		protected override bool IncreasePosition()
		{
			bool result;
			if (m_lastnode == null || m_lastnode.TreeView == null)
			{
				if (m_tv.Nodes.Count == 0)
					result = false;
				else
				{
					m_lastnode = m_tv.Nodes[0];
					result = true;
				}
			}
			else
			{
				m_lastnode = GetNextNode(m_lastnode, false);
				result = (m_lastnode != null);
			}
			return result;
		}

		protected override void SetFoundPosition()
		{
			m_tv.SelectedNode = m_lastnode;
		}

		protected override object GetPosition()
		{
			return m_lastnode;
		}

		private TreeNode GetNextNode(TreeNode n, bool SkipChildren)
		{
			TreeNode result;
			if (n == null)
				result = null;
			else if (!SkipChildren && n.Nodes.Count > 0)
				result = n.Nodes[0];
			else if (n.NextNode != null)
				result = n.NextNode;
			else
				result = GetNextNode(n.Parent, true);
			return result;
		}

		protected override bool CheckIsReady()
		{
			return base.CheckIsReady() && m_tv != null;
		}

		protected override object GetCurrent()
		{
			object result;
			if (m_lastnode == null)
				result = null;
			else
				result = m_lastnode.Text;
			return result;
		}

		protected override void filter(StringSearchMatcher search)
		{
			throw new NotImplementedException();
		}

		protected override bool Supports(SearchBoxMode Mode)
		{
			return base.Supports(Mode) && Mode != SearchBoxMode.Filter;
		}

		[DefaultValue(null)]
		public TreeView TreeView
		{
			get
			{
				return m_tv;
			}
			set
			{
				if (m_tv != value)
				{
					if (m_tv != null)
					{
						m_tv.AfterSelect -= tv_AfterSelect;
						base.UnRegisterControl(m_tv);
					}
					m_tv = value;
					if (m_tv != null)
					{
						m_tv.AfterSelect += tv_AfterSelect;
						base.RegisterControl(m_tv);
					}
					base.NotifyStateChanged(true);
				}
			}
		}

		protected override void OnDisposed()
		{
			TreeView = null;
		}

		private void tv_AfterSelect(object sender, TreeViewEventArgs e)
		{
			if (!base.IsBusy)
			{
				ResetStartPosition(null);
				Text = null;
			}
		}

		private TreeNode m_lastnode;
		private TreeView m_tv;
	}
}
