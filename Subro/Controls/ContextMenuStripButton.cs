using System;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Windows.Forms;

namespace Subro.Controls
{
	public class ContextMenuStripButton : Control
	{
		protected override void OnMouseDown(MouseEventArgs e)
		{
			base.OnMouseDown(e);
			if (!m_down)
			{
				m_down = true;
				ContextMenuStrip contextMenuStrip = ContextMenuStrip;
				if (contextMenuStrip != null)
				{
					contextMenuStrip.Show(this, new Point(base.Width, base.Height), ToolStripDropDownDirection.BelowLeft);
					contextMenuStrip.Closed += cm_Closed;
				}
				base.Invalidate();
			}
		}

		protected override void OnMouseUp(MouseEventArgs e)
		{
			base.OnMouseUp(e);
			m_down = false;
			base.Invalidate();
		}

		protected override Size DefaultSize
		{
			get
			{
				return new Size(10, 24);
			}
		}

		private void cm_Closed(object sender, ToolStripDropDownClosedEventArgs e)
		{
			((ContextMenuStrip)sender).Closed -= cm_Closed;
			m_down = false;
			base.Invalidate();
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
					{
						clipRectangle.Offset(1, 1);
					}
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
			catch { }
		}

		private bool m_down;
	}
}