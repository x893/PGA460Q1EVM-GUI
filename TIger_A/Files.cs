using System;
using System.IO;
using System.Windows.Forms;

namespace TIger_A
{
	public class Files
	{
		public Files()
		{
			ActiveFile = "";
			OpenForWriting = false;
			OpenForReading = false;
		}

		public void Dispose()
		{
			try
			{
				textRead.Dispose();
			}
			catch { }
			try
			{
				textWrite.Dispose();
			}
			catch { }
		}

		public bool textFileOpenForRead(string fileName, string[] extension, string startPath)
		{
			bool readNOTwrite = true;
			return textFileOpen(ref fileName, extension, startPath, readNOTwrite);
		}

		public bool textFileOpenForWrite(string fileName, string[] extension, string startPath)
		{
			bool readNOTwrite = false;
			return textFileOpen(ref fileName, extension, startPath, readNOTwrite);
		}

		public bool textFileOpen(ref string fileName, string[] extension, string startPath, bool readNOTwrite)
		{
			bool flag = true;
			string text = startPath;
			string suggestedFileName = "";
			UserCanceled = false;
			bool flag2;
			if (readNOTwrite)
			{
				if (fileName == null)
				{
					flag2 = true;
					fileName = "";
				}
				else
					flag2 = (fileName == "");
			}
			else
			{
				flag2 = true;
				suggestedFileName = fileName;
			}
			if (flag2)
			{
				if (readNOTwrite)
				{
					string dialogBoxTitle = "OPEN FILE";
					fileName = GetFileName_FromOpenDialogBox(ref flag, extension, ref text, dialogBoxTitle);
				}
				else
				{
					string dialogBoxTitle = "SAVE FILE";
					fileName = GetFileName_FromSaveDialogBox(ref flag, suggestedFileName, extension, ref text, dialogBoxTitle);
				}
				if (!flag)
					return false;
				if (UserCanceled)
					return false;
			}
			ActiveFile = text + fileName;
			if (!StartStreamWriterReader(ActiveFile, readNOTwrite))
			{
				string dialogBoxTitle;
				if (readNOTwrite)
					dialogBoxTitle = "OPEN FILE";
				else
					dialogBoxTitle = "SAVE FILE";

				fileName = GetFileName_FromOpenDialogBox(ref flag, extension, ref text, dialogBoxTitle);
				if (!flag)
					return false;
			}
			ActiveFile = text + fileName;
			bool result;
			if (text == "" || fileName == "")
				result = false;
			else
			{
				flag = StartStreamWriterReader(ActiveFile, readNOTwrite);
				result = flag;
			}
			return result;
		}

		public bool StartStreamWriterReader(string ActiveFile, bool readNOTwrite)
		{
			bool result;
			if (readNOTwrite)
			{
				if (!OpenForWriting && !OpenForReading)
				{
					try
					{
						OpenForReading = true;
						textRead = new StreamReader(ActiveFile);
						return true;
					}
					catch
					{
						OpenForReading = false;
						return false;
					}
				}
				result = true;
			}
			else
			{
				if (!OpenForWriting && !OpenForReading)
				{
					try
					{
						textWrite = new StreamWriter(ActiveFile);
						OpenForWriting = true;
						return true;
					}
					catch
					{
						OpenForWriting = false;
						return false;
					}
				}
				result = true;
			}
			return result;
		}

		public string GetFileName_FromOpenDialogBox(ref bool IsValid, string[] extension, ref string Path, string DialogBoxTitle)
		{
			IsValid = false;
			string result = "";
			string text = "";
			if (extension != null)
			{
				for (int i = 0; i < extension.Length; i++)
				{
					extension[i] = extension[i].ToUpper();
				}
				string[,] array = new string[3, 2];
				array[0, 0] = "CSV";
				array[0, 1] = "Comma Delimited (*.csv)|*.CSV; *.CSV | ";
				array[1, 0] = "TXT";
				array[1, 1] = "text file (*.txt)|*.TXT; *.txt | ";
				array[2, 0] = "HEX";
				array[2, 1] = "hex file (*.hex)|*.HEX; *.hex | ";
				for (int i = 0; i < extension.Length; i++)
					for (int j = 0; j < array.GetLength(0); j++)
						if (extension[i] == array[j, 0])
							text += array[j, 1];
			}
			else
				text = "";

			text += "All files (*.*)|*.*text file (*.txt)|*.TXT; *.txt | ";
			if (Path != null)
			{
				Path = Path.ToUpper();
				if (Path == "")
					Path = "c:\\";
			}
			else
			{
				Path = "c:\\";
			}
			Path = Path.ToUpper();
			OpenFileDialog openFileDialog = new OpenFileDialog();
			openFileDialog.Title = DialogBoxTitle;
			openFileDialog.InitialDirectory = Path;
			openFileDialog.Filter = text;
			openFileDialog.FilterIndex = 0;
			openFileDialog.RestoreDirectory = true;
			DialogResult dialogResult = openFileDialog.ShowDialog();
			if (dialogResult == DialogResult.OK)
			{
				string initialDirectory = openFileDialog.InitialDirectory;
				result = openFileDialog.FileName;
				IsValid = true;
				UserCanceled = false;
			}
			else if (dialogResult == DialogResult.Cancel)
			{
				UserCanceled = true;
				IsValid = false;
			}
			else
			{
				IsValid = false;
				UserCanceled = false;
			}
			ParseFileName(ref result, ref Path);
			openFileDialog.Dispose();
			return result;
		}

		public string GetFileName_FromSaveDialogBox(ref bool IsValid, string suggestedFileName, string[] extension, ref string Path, string DialogBoxTitle)
		{
			IsValid = false;
			string result = "";
			string str = "";
			if (extension != null)
			{
				for (int i = 0; i < extension.Length; i++)
				{
					extension[i] = extension[i].ToUpper();
				}
				string[,] array = new string[3, 2];
				array[0, 0] = "CSV";
				array[0, 1] = "Comma Delimited (*.csv)|*.CSV; *.CSV | ";
				array[1, 0] = "TXT";
				array[1, 1] = "text file (*.txt)|*.TXT; *.txt | ";
				array[2, 0] = "HEX";
				array[2, 1] = "hex file (*.hex)|*.HEX; *.hex | ";
				for (int i = 0; i < extension.Length; i++)
					for (int j = 0; j < array.GetLength(0); j++)
						if (extension[i] == array[j, 0])
							str += array[j, 1];
			}
			else
				str = "";

			str += "All files (*.*)|*.*text file (*.txt)|*.TXT; *.txt | ";
			if (Path != null)
			{
				Path = Path.ToUpper();
				if (Path == "")
					Path = "c:\\";
			}
			else
				Path = "c:\\";

			Path = Path.ToUpper();
			SaveFileDialog saveFileDialog = new SaveFileDialog();
			saveFileDialog.Title = DialogBoxTitle;
			saveFileDialog.InitialDirectory = Path;
			saveFileDialog.Filter = "Text File | *.txt";
			saveFileDialog.FilterIndex = 0;
			saveFileDialog.RestoreDirectory = true;
			saveFileDialog.FileName = suggestedFileName;
			bool flag = false;
			try
			{
				DialogResult dialogResult = saveFileDialog.ShowDialog();
				if (dialogResult == DialogResult.OK)
				{
					string initialDirectory = saveFileDialog.InitialDirectory;
					result = saveFileDialog.FileName;
					IsValid = true;
					UserCanceled = false;
				}
				else if (dialogResult == DialogResult.Cancel)
				{
					IsValid = false;
					flag = false;
					UserCanceled = true;
				}
				else
				{
					IsValid = false;
					UserCanceled = false;
				}
				flag = false;
			}
			catch
			{
				MessageBox.Show("'suggestedFileName' is not a valid file name.");
				suggestedFileName = "";
				flag = true;
			}
			if (flag)
			{
				try
				{
					DialogResult dialogResult = saveFileDialog.ShowDialog();
					if (dialogResult == DialogResult.OK)
					{
						string initialDirectory = saveFileDialog.InitialDirectory;
						result = saveFileDialog.FileName;
						IsValid = true;
						UserCanceled = false;
					}
					else if (dialogResult == DialogResult.Cancel)
					{
						IsValid = false;
						UserCanceled = true;
					}
					else
					{
						IsValid = false;
						UserCanceled = false;
					}
					flag = false;
				}
				catch
				{
					MessageBox.Show("'suggestedFileName' is not a valid file name.");
					suggestedFileName = "";
					flag = true;
				}
			}
			ParseFileName(ref result, ref Path);
			saveFileDialog.Dispose();
			return result;
		}

		public void textFileClose()
		{
			if (OpenForWriting)
				textWrite.Close();
			else if (OpenForReading)
				textRead.Close();
			else
			{
				try
				{
					textWrite.Close();
				}
				catch { }
				try
				{
					textRead.Close();
				}
				catch { }
			}
			OpenForWriting = false;
			OpenForReading = false;
		}

		public string textFileReadLine(ref bool sucess)
		{
			string result = "";
			sucess = true;
			if (textRead == null)
				textRead = new StreamReader(ActiveFile);
			try
			{
				result = textRead.ReadLine();
			}
			catch
			{
				sucess = false;
			}
			return result;
		}

		public string[] TextFileReadWholeFile(ref bool sucess, ref int NumLines, string OptionalEOFText)
		{
			sucess = false;
			if (NumLines == 0)
			{
				NumLines = HowManyLinesInTextFile(OptionalEOFText);
			}
			string[] array = new string[NumLines];
			for (int i = 0; i < NumLines; i++)
			{
				array[i] = textFileReadLine(ref sucess);
				if (!sucess)
				{
					i = NumLines + 1;
				}
			}
			return array;
		}

		public string[,] TextFileReadWholeFileAndParseAddressData(int NumBitsInAWord, int NumBitsInAddress)
		{
			bool flag = false;
			int num = 0;
			string optionalEOFText = "";
			string[] inputLines = TextFileReadWholeFile(ref flag, ref num, optionalEOFText);
			return ParseStandardHEXFile(inputLines, NumBitsInAWord, NumBitsInAddress);
		}

		public bool ReadGridFromFile(RegisterValueGridEditor grid, string gridName, string startPath, string fileNameDOTextenstion)
		{
			int num = grid.getNumberOfRegisters();
			string[] array = new string[num + 2];
			string[,] array2 = new string[num, 2];
			bool flag = true;
			bool flag2 = textFileOpenForRead(fileNameDOTextenstion, new string[] { "TXT" }, startPath);

			bool result;
			if (!flag2 && !UserCanceled)
			{
				MessageBox.Show("The file could not be opened for reading.");
				result = false;
			}
			else if (!flag2 && UserCanceled)
				result = false;
			else
			{
				string optionalEOFText = "EOF";
				num = 0;
				string[] array3 = TextFileReadWholeFile(ref flag, ref num, optionalEOFText);
				int length = array3.GetLength(0);
				textFileClose();
				string delimiter = ",";
				int num2 = 0;
				string text = "";
				string[,] array4 = ParseGridFile(array3, ref text, ref num2, delimiter);
				string[,] array5 = new string[num2, 5];
				for (int i = 0; i < num2; i++)
				{
					array5[i, 1] = array4[i, 0];
					array5[i, 2] = array4[i, 1];
				}
				if (gridName != text)
				{
					MessageBox.Show(string.Concat(new string[]
					{
						"File, line 0: ",
						text,
						"\ndoes not match requested grid name : ",
						gridName,
						"\n to be populated from file."
					}));
					result = false;
				}
				else
				{
					string[,] array6 = new string[num2, 5];
					grid.Highlight_All_Rows();
					array6 = grid.Get_HIGHLIGHTED_Grid_Values__R_D_A_O(false);
					grid.saveAllValues();
					if (num2 != array6.GetLength(0))
					{
						MessageBox.Show(string.Concat(new string[]
						{
							"The Grid has: ",
							(num + 1).ToString(),
							"\nBut the file has: ",
							num2.ToString(),
							"\nThe total number of addresses does not match."
						}));
						result = false;
					}
					else
					{
						int num3 = 0;
						for (int i = 0; i < array6.GetLength(0); i++)
						{
							if (array6[i, 1] != array5[i, 1])
							{
								num3++;
							}
						}
						if (num3 > 0)
						{
							MessageBox.Show("Register Addresses from imported file do not match addresses in grid.");
							result = false;
						}
						else
						{
							for (int i = 0; i < array6.GetLength(0); i++)
							{
								array5[i, 0] = array6[i, 0];
								array5[i, 4] = array5[i, 2];
							}
							grid.setDataIntoGridDataArray(array5, false);
							grid.saveAllValues();
							result = true;
						}
					}
				}
			}
			return result;
		}

		public bool TextFileWriteWholeFile(string[] Write, string OptionalEOFText)
		{
			bool flag = false;
			int length = Write.GetLength(0);
			bool result;
			if (length <= 0)
				result = false;
			else
			{
				if (OpenForReading)
					textFileClose();
				if (!OpenForWriting)
				{
					string text = "";
					if (Write[0] == ";EDDBG")
					{
						flag = true;
						ActiveFile = globalBgEDDPath;
					}
					else
						flag = textFileOpen(ref text, null, "", false);

					if (!flag)
						return false;
				}
				for (int i = 0; i < length; i++)
				{
					flag = textFileWriteLine(Write[i]);
					if (!flag)
					{
						i = length + 1;
						return flag;
					}
				}
				if (!(OptionalEOFText == "") && OptionalEOFText != null)
					flag = textFileWriteLine(OptionalEOFText);
				result = flag;
			}
			return result;
		}

		public bool textFileWriteLine(string write)
		{
			bool result = true;
			if (textWrite == null)
				textWrite = new StreamWriter(ActiveFile);
			try
			{
				textWrite.WriteLine(write);
			}
			catch
			{
				result = false;
			}
			return result;
		}

		public bool WriteGridToFile(RegisterValueGridEditor grid, string gridName, string startPath, string fileNameDOTextenstion)
		{
			int numberOfRegisters = grid.getNumberOfRegisters();
			string[] array = new string[numberOfRegisters + 2];
			grid.saveAllValues();
			grid.Highlight_All_Rows();
			string[,] array2 = grid.Get_HIGHLIGHTED_Grid_Values__R_D_A_O(false);
			grid.saveAllValues();
			array[0] = ";" + gridName.ToUpper();
			for (int i = 0; i <= numberOfRegisters; i++)
			{
				array[i + 1] = array2[i, 1] + "," + array2[i, 2];
			}
			bool flag = textFileOpenForWrite(fileNameDOTextenstion, null, startPath);
			bool result;
			if (!flag && !UserCanceled)
			{
				MessageBox.Show("The file could not be opened for writting.");
				result = false;
			}
			else if (!flag && UserCanceled)
			{
				result = false;
			}
			else if (!TextFileWriteWholeFile(array, "EOF"))
			{
				MessageBox.Show("The File could not be written.");
				result = false;
			}
			else
			{
				textFileClose();
				result = true;
			}
			return result;
		}

		public bool WriteArrayToFile(string ArrayName, string[] Array, string startPath, string fileNameDOTextenstion)
		{
			int num = Array.Length;
			string[] array = new string[num + 2];
			globalBgEDDPath = startPath + fileNameDOTextenstion;
			array[0] = ";" + ArrayName.ToUpper();
			for (int i = 0; i < num; i++)
			{
				array[i + 1] = string.Concat(new object[]
				{
					i,
					",",
					Array[i],
					","
				});
			}
			bool flag = ArrayName == "EDDBG" || textFileOpenForWrite(fileNameDOTextenstion, null, startPath);
			bool result;
			if (!flag && !UserCanceled)
			{
				MessageBox.Show("The file could not be opened for writing.");
				result = false;
			}
			else if (!flag && UserCanceled)
			{
				result = false;
			}
			else if (!TextFileWriteWholeFile(array, "EOF"))
			{
				MessageBox.Show("The File could not be written.");
				result = false;
			}
			else
			{
				textFileClose();
				result = true;
			}
			return result;
		}

		public void ParseFileName(ref string FileName, ref string Path)
		{
			string text = FileName;
			int length = text.Length;
			string[] array = text.Split(new string[] { "\\" }, StringSplitOptions.None);
			int length2 = array.GetLength(0);
			FileName = array[length2 - 1];
			Path = "";
			for (int i = 0; i < length2 - 1; i++)
				Path = Path + array[i] + "\\";
		}

		public int HowManyLinesInTextFile(string EOFText)
		{
			bool openForReading = OpenForReading;
			bool openForWriting = OpenForWriting;
			bool flag = true;
			int num = 0;
			int result;
			if (ActiveFile == "" || ActiveFile == null)
			{
				result = 0;
			}
			else
			{
				if (OpenForReading)
				{
					textFileClose();
					textFileOpenForRead(ActiveFile, null, "");
					while (flag)
					{
						if (EOFText != null)
						{
							string text = textFileReadLine(ref flag);
							if (text == null || text == "EOF" || text == EOFText)
								break;
							num++;
						}
						else
						{
							string text = textFileReadLine(ref flag);
							if (text == null || text == "EOF")
								break;
							num++;
						}
					}
					textFileClose();
					if (openForReading)
						textFileOpenForRead(ActiveFile, null, "");
					else if (openForWriting)
						textFileOpenForWrite(ActiveFile, null, "");
				}
				else if (OpenForWriting)
				{
					textFileClose();
					textFileOpenForRead(ActiveFile, null, "");
					while (flag)
					{
						if (EOFText != null)
						{
							string text = textFileReadLine(ref flag);
							if (text == null || text == "EOF" || text == EOFText)
								break;
							num++;
						}
						else
						{
							string text = textFileReadLine(ref flag);
							if (text == null || text == "EOF")
								break;
							num++;
						}
					}
					textFileClose();
					textFileOpenForWrite(ActiveFile, null, "");
				}
				else
					num = 0;
				result = num;
			}
			return result;
		}

		public string GetDateTime(bool addDate, bool addDay, bool addTime)
		{
			string text = "";
			DateTime now = DateTime.Now;
			string text2 = now.Minute.ToString();
			string text3 = now.Hour.ToString();
			string text4 = now.Month.ToString();
			string text5 = now.Day.ToString();
			string text6 = now.Year.ToString();
			if (text2.Length == 1)
			{
				text2 = "0" + text2;
			}
			string text7 = now.TimeOfDay.ToString();
			string str = now.DayOfWeek.ToString();
			if (addDay)
				text += str;
			if (addDate)
				text = string.Concat(text, ", ", text4, "/", text5, "/", text6);
			if (addTime)
				text = string.Concat(text, ", ", text3, ":", text2);
			return text;
		}

		public string CreateFileName(string prefix, string suffix, string extension, bool addDate, bool addTime, bool addDay)
		{
			DateTime now = DateTime.Now;
			string text = now.Millisecond.ToString();
			string text2 = now.Second.ToString();
			string text3 = now.Minute.ToString();
			string text4 = now.Hour.ToString();
			string text5 = now.Month.ToString();
			string text6 = now.Day.ToString();
			string text7 = now.Year.ToString();
			string str = now.DayOfWeek.ToString();
			if (text3.Length == 1)
				text3 = "0" + text3;
			string text8 = prefix;
			if (addDay)
				text8 = text8 + " " + str;
			if (addDate)
				text8 = string.Concat(text8, " ", text5, "-", text6, "-", text7);
			if (addTime)
				text8 = string.Concat(text8, " ", text4, "_", text3, "_", text2, "_", text);
			text8 += suffix;
			if (extension != "")
				text8 = text8 + "." + extension;
			return text8;
		}

		public string[] addLineToStringArray(bool putInIndex0, string[] Array, string line)
		{
			if (Array == null)
				Array = new string[0];

			int num = Array.Length;
			string[] array = new string[num + 1];
			int num2 = array.Length;
			if (putInIndex0)
			{
				array[0] = line;
				for (int i = 0; i < num; i++)
					array[i + 1] = Array[i];
			}
			else
			{
				for (int i = 0; i < num; i++)
					array[i] = Array[i];
				array[num] = line;
			}
			return array;
		}

		public string[] add_StringArray_To_StringArray(string[] ArrayWhole, string[] ArrayToBeAdded, bool AddToIndexZero)
		{
			int num = 0;
			int num2 = 0;
			string[] array;
			if (ArrayWhole == null)
			{
				if (ArrayToBeAdded != null)
				{
					num = 0;
					array = new string[ArrayToBeAdded.Length];
				}
			}
			else if (ArrayToBeAdded == null)
			{
				num = ArrayWhole.Length;
				num2 = 0;
			}
			int num3 = num + num2;
			array = new string[num3];
			if (AddToIndexZero)
			{
				int num4 = 0;
				for (int i = 0; i < num; i++)
				{
					array[num4] = ArrayWhole[i];
					num4++;
				}
				for (int i = 0; i < num2; i++)
				{
					array[num4] = ArrayToBeAdded[i];
					num4++;
				}
			}
			else
			{
				int num4 = 0;
				for (int i = 0; i < num2; i++)
				{
					array[num4] = ArrayToBeAdded[i];
					num4++;
				}
				for (int i = 0; i < num; i++)
				{
					array[num4] = ArrayWhole[i];
					num4++;
				}
			}
			return array;
		}

		public string[,] ParseStandardHEXFile(string[] inputLines, int NumBitsInAWord, int NumHexCharsInAddress)
		{
			int length = inputLines.GetLength(0);
			int num = 0;
			for (int i = 0; i < length; i++)
			{
				string[,] array = ParseStandardHEXFile(inputLines[i], NumBitsInAWord, NumHexCharsInAddress);
				num += array.GetLength(0);
			}
			string[,] array2 = new string[num, 2];
			int num2 = 0;
			for (int i = 0; i < length; i++)
			{
				string[,] array = ParseStandardHEXFile(inputLines[i], NumBitsInAWord, NumHexCharsInAddress);
				int length2 = array.GetLength(0);
				for (int j = 0; j < length2; j++)
				{
					array2[num2, 0] = array[j, 0];
					array2[num2, 1] = array[j, 1];
					num2++;
				}
			}
			return array2;
		}

		public string[,] ParseStandardHEXFile(string inputLine, int NumBitsInAWord, int NumHexCharsInAddress)
		{
			string text = "";
			string input_string = "";
			string input_string2 = "";
			string text2 = "";
			string text3 = "";
			string[] array = ParseStandardHEXFile(inputLine, NumBitsInAWord, ref text, ref input_string, ref input_string2, ref text2, ref text3, NumHexCharsInAddress);
			int num = array.Length;
			int num2 = Tools.TestForValidINT32(input_string, 16, ref IsValid);
			if (num != num2)
			{
				string text4 = "ERROR - 'ParseStandardHEXFile()'  \nThe number of data records does not match number of data records stated.";
				MessageBox.Show(text4);
			}
			string[,] array2 = new string[num, 2];
			for (int i = 0; i < num; i++)
				array2[i, 1] = array[i];

			int num3 = Tools.TestForValidINT32(input_string2, 16, ref IsValid);
			for (int i = 0; i < num; i++)
			{
				array2[i, 0] = (num3 + i).ToString("X");
				array2[i, 0] = Tools.addZeros(array2[i, 0], NumHexCharsInAddress, true);
			}
			return array2;
		}

		public string[] ParseStandardHEXFile(string inputLine, int NumBitsInAWord, ref string SOR, ref string nn, ref string aaaa, ref string tt, ref string cc, int NumHexCharsInAddress)
		{
			string text = ParseStandardHEXFile(inputLine, ref SOR, ref nn, ref aaaa, ref tt, ref cc, NumHexCharsInAddress);
			int length = text.Length;
			string[] array = new string[length / 2];
			string[] result;
			if (length % 2 != 0)
			{
				string text2 = "ERROR -  'ParseStandardHEXFile()' \nNumber of Bits in a Word is " + NumBitsInAWord.ToString() + " .\nBut the number of HEX characters does not allow for this word size.";
				MessageBox.Show(text2);
				result = array;
			}
			else
			{
				int num = 0;
				for (int i = 0; i < length / 2; i++)
				{
					array[i] = text.Substring(num, 2);
					num += 2;
				}
				result = array;
			}
			return result;
		}

		public string ParseStandardHEXFile(string inputLine, ref string SOR, ref string nn, ref string aaaa, ref string tt, ref string cc, int NumHexCharsInAddress)
		{
			string text = "";
			string result;
			if (inputLine == null)
				result = text;
			else if (inputLine == "")
				result = text;
			else
			{
				inputLine = inputLine.Trim();
				int length = inputLine.Length;
				int num = 0;
				SOR = inputLine.Substring(0, 1);
				num += SOR.Length;
				nn = inputLine.Substring(1, 2);
				num += nn.Length;
				aaaa = inputLine.Substring(3, NumHexCharsInAddress);
				num += aaaa.Length;
				tt = inputLine.Substring(3 + NumHexCharsInAddress, 2);
				num += tt.Length;
				cc = inputLine.Substring(length - 2, 2);
				num += cc.Length;
				text = inputLine.Substring(9, length - num);
				result = text;
			}
			return result;
		}

		public string[,] ParseGridFile(string[] input, ref string GridFileTitle, ref int NumFileAddressLines, string delimiter)
		{
			string[,] result;
			try
			{
				if (input == null)
					return null;

				int num = input.Length;
				if (num == 0)
					return null;

				string[,] array = new string[num, 2];
				string[,] array2 = new string[num, 2];
				for (int i = 0; i < num; i++)
					input[i] = input[i].Trim();

				GridFileTitle = input[0].Replace(';', ' ');
				GridFileTitle = GridFileTitle.Trim();
				int num2 = 0;
				for (int i = 0; i < num; i++)
					if (!input[i].StartsWith(";"))
					{
						array[num2, 0] = input[i];
						num2++;
					}

				num2 = 0;
				for (int i = 0; i < num; i++)
					if (!input[i].StartsWith("EOF"))
					{
						array[num2, 0] = array[i, 0];
						num2++;
					}

				string[] array3 = new string[2];
				num2 = 0;
				for (int i = 0; i < num; i++)
					if (array[i, 0] != null)
					{
						int num3 = array[i, 0].LastIndexOfAny(new char[] { ';' });
						if (num3 >= 0)
							array[i, 0] = array[i, 0].Remove(num3);
						array[i, 0] = array[i, 0].Trim();
						array3 = array[i, 0].Split(new string[] { delimiter }, StringSplitOptions.RemoveEmptyEntries);
						array2[num2, 0] = array3[0];
						array2[num2, 1] = array3[1];
						num2++;
					}

				int num4 = 0;
				for (int i = 0; i < array2.GetLength(0); i++)
					if (array2[i, 0] != null)
						num4++;

				NumFileAddressLines = num4;
				string[,] array4 = new string[num4, 2];
				for (int i = 0; i < num4; i++)
				{
					array4[i, 0] = array2[i, 0];
					array4[i, 1] = array2[i, 1];
				}
				result = array4;
			}
			catch
			{
				MessageBox.Show("Grid input File was not able to be parsed. The file contains errors.");
				result = null;
			}
			return result;
		}

		private const int row = 0;
		private const int A = 1;
		private const int D = 2;
		private const int O = 3;
		private const int R = 4;
		private TextReader textRead;
		private TextWriter textWrite;
		private string ActiveFile = "";
		private bool OpenForWriting = false;
		private bool OpenForReading = false;
		private bool IsValid = false;
		private bool UserCanceled = false;
		private string globalBgEDDPath = "";
	}
}
