using System;
using System.ComponentModel;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using System.Linq;
using System.Text;
using System.Windows.Forms;
using System.Windows.Forms.DataVisualization.Charting;

namespace TI.eLAB.EVM
{
	public partial class DataPlotter : Form
	{
		public DataPlotter()
		{
			InitializeComponent();
		}

		private void InitializeOpenFileDialog()
		{
			openFileDialog1.Filter = "Text Files|*.txt";
			openFileDialog1.Multiselect = true;
			openFileDialog1.Title = "My Text Browser";
		}

		private void toolStripLabel1_Click(object sender, EventArgs e)
		{
			chart1.Legends[0].Enabled = true;
			InitializeOpenFileDialog();
			DialogResult dialogResult = openFileDialog1.ShowDialog();
			if (dialogResult == DialogResult.OK)
			{
				foreach (string path in openFileDialog1.FileNames)
				{
					try
					{
						string text = string.Empty;
						double num = 0.0;
						StreamReader streamReader = new StreamReader(path);
						toolStripStatusLabel2.Text = "Loaded successfully.";
						if (seriesCount > 0)
						{
							chart1.Series.Add(Convert.ToString(seriesCount));
							chart1.Series[seriesCount].ChartType = displayType;
							chart1.Series[seriesCount].BorderWidth = 3;
							numplots.Text = Convert.ToString(seriesCount);
							chart1.Series[seriesCount].Name = Path.GetFileNameWithoutExtension(path);
						}
						if (seriesCount == 0)
						{
							chart1.Series[0].Name = Path.GetFileNameWithoutExtension(path);
						}
						while ((text = streamReader.ReadLine()) != null)
						{
							string[] array = text.Split(new char[]
							{
								','
							});
							for (int j = 0; j < array.Length; j++)
							{
								array[j] = array[j].Trim();
							}
							if (array.Count<string>() == 4)
							{
								if (num > 0.0 && double.Parse(array[1]) == 0.0)
								{
									chart1.Series[seriesCount].Points.AddXY(double.Parse(array[1]) / 1000.0 * 343.0 / 2.0, -1.0);
									chart1.Series[seriesCount].Points.AddXY(0.0, -1.0);
									numplots.Text = Convert.ToString(double.Parse(numplots.Text) + 1.0);
								}
								chart1.Series[seriesCount].Points.AddXY(double.Parse(array[1]) / 1000.0 * 343.0 / 2.0, double.Parse(array[2]));
								num = double.Parse(array[2]);
							}
						}
						numplots.Text = Convert.ToString(double.Parse(numplots.Text) + 1.0);
						seriesCount++;
					}
					catch
					{
						toolStripStatusLabel2.Text = "Failed to load.";
					}
				}
				if (chart1.Series[0].Name.Contains("ADC") || chart1.Series[0].Name.Contains("DSP"))
				{
					toolStripLabel8_Click(null, null);
				}
				toolStripLabel5_Click(null, null);
				toolStripLabel5_Click(null, null);
			}
		}

		private void toolStripLabel3_Click(object sender, EventArgs e)
		{
			for (int i = 0; i < chart1.Series.Count; i++)
			{
				chart1.Series[i].Points.Clear();
			}
			int count = chart1.Series.Count;
			for (int i = count - 1; i > 0; i--)
			{
				chart1.Series.Remove(chart1.Series[i]);
			}
			seriesCount = 0;
			numplots.Text = "0";
			toolStripStatusLabel2.Text = "Cleared plot.";
			chart1.Legends[0].Enabled = false;
			averageTrue = false;
		}

		private void toolStripLabel2_Click(object sender, EventArgs e)
		{
			chart1.SaveImage(Environment.GetFolderPath(Environment.SpecialFolder.Personal) + "\\BOOSTXL-PGA460\\EDDPChartImage-" + DateTime.Now.ToString("MMddyyyyhhmmss") + ".png", ImageFormat.Png);
			toolStripStatusLabel2.Text = "Saved image to My Documents/BOOSTXL-PGA460.";
		}

		private void toolStripLabel5_Click(object sender, EventArgs e)
		{
			if (chart1.Series[0].IsVisibleInLegend)
				chart1.Series[0].IsVisibleInLegend = false;
			else
				chart1.Series[0].IsVisibleInLegend = true;

			for (int i = 0; i < chart1.Series.Count; i++)
				if (i > 0)
					chart1.Series[i].IsVisibleInLegend = chart1.Series[0].IsVisibleInLegend;

			toolStripStatusLabel2.Text = "Legend toggled.";
		}

		private void toolStripLabel6_Click(object sender, EventArgs e)
		{
			int count = chart1.Series.Count;
			int count2 = chart1.Series[0].Points.Count;
			double[,] array = new double[count + 1, count2 + 1];
			for (int i = 0; i < count; i++)
				for (int j = 0; j < count2; j++)
					array[i, j] = chart1.Series[i].Points[j].YValues[0];

			for (int i = 0; i < count; i++)
				for (int j = 0; j < chart1.Series[0].Points.Count; j++)
					array[count, j] += array[i, j];

			for (int j = 0; j < chart1.Series[0].Points.Count; j++)
				array[count, j] /= (double)count;

			chart1.Series.Add(Convert.ToString(seriesCount));
			chart1.Series[seriesCount].BorderWidth = 6;
			numplots.Text = Convert.ToString(seriesCount);
			chart1.Series[seriesCount].Name = "Average " + seriesCount;
			for (int i = 0; i < count2; i++)
				chart1.Series[seriesCount].Points.AddXY(chart1.Series[0].Points[i].XValue, array[count, i]);

			averageSeries = seriesCount;
			seriesCount++;
			numplots.Text = Convert.ToString(double.Parse(numplots.Text) + 1.0);
			averageTrue = true;
			toolStripStatusLabel2.Text = "Average generated and plotted (bold).";
		}

		private void toolStripLabel7_Click(object sender, EventArgs e)
		{
			if (averageTrue)
			{
				string[] array = new string[130];
				array[0] = ";EDDAVG";
				for (int i = 0; i < 127; i++)
				{
					array[i + 1] = string.Concat(
						i.ToString(),
						",",
						chart1.Series[averageSeries].Points[i].XValue.ToString(),
						",",
						chart1.Series[averageSeries].Points[i].YValues[0].ToString(),
						","
					);
				}
				array[128] = "";
				array[129] = "EOF";
				string str = "PGA460-EDD-AVG " + DateTime.Now.ToString("yyyy-MM-dd-HHmmss") + ".txt";
				string path = Environment.GetFolderPath(Environment.SpecialFolder.Personal) + "\\BOOSTXL-PGA460\\" + str;
				if (!File.Exists(path))
					File.WriteAllLines(path, array, Encoding.UTF8);
				toolStripStatusLabel2.Text = "Saved average data to My Documents/BOOSTXL-PGA460.";
			}
		}

		private void toolStripLabel8_Click(object sender, EventArgs e)
		{
			chart1.ChartAreas[0].AxisY.Maximum = double.NaN;
			chart1.ChartAreas[0].AxisY.Minimum = double.NaN;
			chart1.ChartAreas[0].AxisX.Maximum = double.NaN;
			chart1.ChartAreas[0].AxisX.Minimum = double.NaN;
			chart1.ChartAreas[0].RecalculateAxesScale();
		}

		private void toolStripLabel9_Click(object sender, EventArgs e)
		{
			if (toolStripLabel9.Text.Contains("Line"))
			{
				toolStripLabel3_Click(null, null);
				displayType = SeriesChartType.Column;
				chart1.Series[0].ChartType = displayType;
				toolStripLabel9.Text = "Display Style = Column";
			}
			else if (toolStripLabel9.Text.Contains("Column"))
			{
				toolStripLabel3_Click(null, null);
				displayType = SeriesChartType.Line;
				chart1.Series[0].ChartType = displayType;
				toolStripLabel9.Text = "Display Style = Line";
			}
		}

		private int seriesCount = 0;
		private bool averageTrue = false;
		private int averageSeries = 0;
		private SeriesChartType displayType = SeriesChartType.Line;
	}
}
