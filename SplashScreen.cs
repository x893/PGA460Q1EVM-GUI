using System;
using System.Drawing;

namespace TI.eLAB.EVM
{
	internal class SplashScreen
	{
		public static void ShowSplashScreen()
		{
			if (m_sf == null)
			{
				m_sf = new SplashScreenForm();
				UpdateLoadingText(1);
				UpdateLoadingText(2);
				m_sf.ShowSplashScreen();
			}
		}

		public static void UpdateProgressBar(int percent)
		{
			m_sf.progressBar1.Value = percent;
		}

		public static void UpdateLoadingText(byte index)
		{
			switch (index)
			{
			case 1:
				m_sf.initTextLabel.ForeColor = ColorTranslator.FromHtml("#FFFFFF");
				m_sf.loadingTextLabel.ForeColor = ColorTranslator.FromHtml("#AAAAAA");
				m_sf.startingTextLabel.ForeColor = ColorTranslator.FromHtml("#AAAAAA");
				break;
			case 2:
				m_sf.initTextLabel.ForeColor = ColorTranslator.FromHtml("#AAAAAA");
				m_sf.loadingTextLabel.ForeColor = ColorTranslator.FromHtml("#FFFFFF");
				m_sf.startingTextLabel.ForeColor = ColorTranslator.FromHtml("#AAAAAA");
				break;
			case 3:
				m_sf.initTextLabel.ForeColor = ColorTranslator.FromHtml("#AAAAAA");
				m_sf.loadingTextLabel.ForeColor = ColorTranslator.FromHtml("#AAAAAA");
				m_sf.startingTextLabel.ForeColor = ColorTranslator.FromHtml("#FFFFFF");
				break;
			default:
				m_sf.initTextLabel.ForeColor = ColorTranslator.FromHtml("#AAAAAA");
				m_sf.loadingTextLabel.ForeColor = ColorTranslator.FromHtml("#AAAAAA");
				m_sf.startingTextLabel.ForeColor = ColorTranslator.FromHtml("#AAAAAA");
				break;
			}
		}

		public static void CloseSplashScreen()
		{
			if (m_sf != null)
			{
				m_sf.CloseSplashScreen();
				m_sf.Close();
				m_sf = null;
			}
		}

		public static void UdpateStatusText(string Text)
		{
			if (m_sf != null)
				m_sf.UdpateStatusText(Text);
		}

		public static void UdpateStatusTextWithStatus(string Text, TypeOfMessage tom)
		{
			if (m_sf != null)
				m_sf.UdpateStatusTextWithStatus(Text, tom);
		}

		private static SplashScreenForm m_sf = null;
	}
}
