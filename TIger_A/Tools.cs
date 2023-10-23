using System;
using System.Drawing;
using System.Text.RegularExpressions;
using System.Windows.Forms;

namespace TIger_A
{
	public static class Tools
	{
		public static byte[] convert_byte_to_ArrayOfBits(byte InputValue)
		{
			return new byte[]
			{
				(byte)((InputValue & 1) >> 0),
				(byte)((InputValue & 2) >> 1),
				(byte)((InputValue & 4) >> 2),
				(byte)((InputValue & 8) >> 3),
				(byte)((InputValue & 16) >> 4),
				(byte)((InputValue & 32) >> 5),
				(byte)((InputValue & 64) >> 6),
				(byte)((InputValue & 128) >> 7)
			};
		}

		public static string bitwiseNOT(string input, int Base, int numberOfBits)
		{
			string text = "";
			if (Base == 2)
				text = input;
			else if (Base == 10)
				text = Tools.StringBase10_Into_StringBase2(input, numberOfBits, true);
			else if (Base == 16)
				text = Tools.StringBase16_Into_StringBase2(input, numberOfBits, true);

			string text2 = "";
			int length = text.Length;
			for (int i = 0; i < length; i++)
			{
				string a = text.Substring(i, 1);
				if (a == "0")
					text2 += "1";
				if (a == "1")
					text2 += "0";
			}
			return text2;
		}

		public static byte StringBase16IntoByte(string input_b16)
		{
			byte result = 0;
			bool flag = true;
			try
			{
				int value = Tools.TestForValidINT32(input_b16, 16, ref flag);
				result = Convert.ToByte(value);
			}
			catch
			{
				MessageBox.Show("Base16 value is to large for conversion to byte.");
				result = 0;
			}
			return result;
		}

		public static byte StringBase10_Into_Byte(string input_b10)
		{
			byte result = 0;
			bool flag = true;
			try
			{
				int value = Tools.TestForValidINT32(input_b10, 10, ref flag);
				result = Convert.ToByte(value);
			}
			catch
			{
				MessageBox.Show("Base10 value can not convert to a byte.");
				result = 0;
			}
			return result;
		}

		public static string StringBase10_Into_StringBase2(string WordBase10, int numberOfBits, ref bool IsValid)
		{
			int num = 0;
			try
			{
				num = Convert.ToInt32(WordBase10);
				IsValid = true;
			}
			catch
			{
				num = 0;
				IsValid = false;
				return "";
			}
			string result;
			if (num >= 0)
			{
				string text = Tools.int32_Into_stringBase2(num);
				text = Tools.addZeros(text, numberOfBits, true);
				result = text;
			}
			else
			{
				num *= -1;
				num--;
				string text2 = Tools.int32_Into_stringBase2(num);
				int length = text2.Length;
				string text = Tools.bitwiseNOT(text2, 2, length);
				text = Tools.addOnes(text, numberOfBits, true);
				result = text;
			}
			return result;
		}

		public static string StringBase10_Into_StringBase2(string WordBase10, int NumberOfBits, bool addZerosToLeft)
		{
			bool flag = false;
			string text = Tools.StringBase10_Into_StringBase2(WordBase10, NumberOfBits, ref flag);
			if (text.Length < NumberOfBits)
			{
				text = Tools.addZeros(text, NumberOfBits, addZerosToLeft);
			}
			return text;
		}

		public static string[] StringBase16_Into_ArrayOfStringBase16(string StringBase16, int NumberOfBitsInWord, bool LSBOfBitStreamGoesToIndex0)
		{
			string[] result = new string[0];
			try
			{
				Convert.ToInt32(StringBase16.Length);
			}
			catch
			{
				return result;
			}
			try
			{
				Convert.ToInt32(NumberOfBitsInWord);
			}
			catch
			{
				return result;
			}
			string[] array = new string[StringBase16.Length / (NumberOfBitsInWord / 4)];
			string[] result2;
			if (NumberOfBitsInWord % 4 != 0)
			{
				result2 = array;
			}
			else
			{
				while (StringBase16.Length % NumberOfBitsInWord / 8 != 0)
					StringBase16 = "0" + StringBase16;

				char[] array2 = StringBase16.ToCharArray();
				int num = NumberOfBitsInWord / 4;
				int num2;
				if (LSBOfBitStreamGoesToIndex0)
					num2 = array.Length - 1;
				else
					num2 = 0;

				for (int i = 0; i < StringBase16.Length; i += num)
				{
					string text = "";
					for (int j = 0; j < num; j++)
					{
						string value = Convert.ToString(array2[i + j]);
						int value2 = Convert.ToInt32(value, 16);
						text += Convert.ToString(value2, 16);
					}
					array[num2] = text.ToUpper();
					if (LSBOfBitStreamGoesToIndex0)
						num2--;
					else
						num2++;
				}
				result2 = array;
			}
			return result2;
		}

		public static string StringBase16_Into_StringBase2(string WordBase16, int lengthOfStringBase2, bool AddZerosToTheLeft)
		{
			string result;
			if (WordBase16 == "")
			{
				result = "";
			}
			else
			{
				string text = Tools.StringBase16_Into_StringBase2(WordBase16, true);
				int length = text.Length;
				if (length > lengthOfStringBase2)
				{
					MessageBox.Show("ERROR - StringBase16_Into_StringBase2().  Length of input string exceeds desired output length.");
					result = text;
				}
				else
				{
					string text2 = Tools.addZeros(text, lengthOfStringBase2, AddZerosToTheLeft);
					result = text2;
				}
			}
			return result;
		}

		public static string StringBase16_Into_StringBase2(string WordBase16, bool RemoveAnyLeadingZerosFromBitStream)
		{
			string text = "";
			string[] array = Tools.StringBase16_Into_ArrayOfStringBase16(WordBase16, 4, false);
			int num = array.Length;
			for (int i = 0; i < num; i++)
			{
				long value = Convert.ToInt64(array[i], 16);
				string text2 = Convert.ToString(value, 2);
				int num2 = 4 - text2.Length;
				for (int j = 0; j < num2; j++)
				{
					text2 = "0" + text2;
				}
				text += text2;
			}
			if (RemoveAnyLeadingZerosFromBitStream)
			{
				bool[] array2 = new bool[text.Length];
				array2 = Tools.StringBase2_Into_ArrayOf_Bool(text);
				int num3 = 0;
				for (int i = array2.Length - 1; i >= 0; i--)
				{
					if (array2[i])
					{
						break;
					}
					num3++;
				}
				bool[] array3 = new bool[text.Length - num3];
				for (int k = 0; k < text.Length - num3; k++)
				{
					array3[k] = array2[k];
				}
				text = Tools.ArrayOf_Bool_Into_StringBase2(array3);
			}
			if (text == "")
			{
				text = "0";
			}
			return text;
		}
		public static string Byte_into_StringBase2(byte input)
		{
			string input2 = Convert.ToString(input, 2);
			return Tools.addZeros(input2, 8, true);
		}

		public static byte StringBase2_into_Byte(string input_b2)
		{
			return (byte)(
				(Convert.ToByte(input_b2.Substring(0, 1)) << 7) +
				(Convert.ToByte(input_b2.Substring(1, 1)) << 6) +
				(Convert.ToByte(input_b2.Substring(2, 1)) << 5) +
				(Convert.ToByte(input_b2.Substring(3, 1)) << 4) +
				(Convert.ToByte(input_b2.Substring(4, 1)) << 3) +
				(Convert.ToByte(input_b2.Substring(5, 1)) << 2) +
				(Convert.ToByte(input_b2.Substring(6, 1)) << 1) +
				(Convert.ToByte(input_b2.Substring(7, 1)) << 0)
				);
		}

		public static string ArrayOf_Bool_Into_StringBase2(bool[] ArrayOfBool)
		{
			string text = "";
			for (int i = 0; i < ArrayOfBool.Length; i++)
			{
				if (ArrayOfBool[i])
				{
					text = "1" + text;
				}
				else
				{
					text = "0" + text;
				}
			}
			return text;
		}
		public static string StringBase2_Into_StringBase16(string StringBase2)
		{
			string result = "";
			int num = StringBase2.Length;
			int num2 = num % 4;
			int num3 = 0;
			while (num2 != 0)
			{
				num3++;
				num++;
				num2 = num % 4;
			}
			int numOfChars = StringBase2.Length + num3;
			StringBase2 = Tools.addZeros(StringBase2, numOfChars, true);
			try
			{
				num = StringBase2.Length;
				for (int i = 0; i < num; i++)
				{
					char[] array = StringBase2.ToCharArray();
					if (array[i] != '0' && array[i] != '1')
					{
						Convert.ToInt32("sdkfj", 10);
					}
				}
			}
			catch
			{
				return result;
			}
			int num4 = 0;
			char[] array2 = new char[StringBase2.Length + num4];
			char[] array3 = StringBase2.ToCharArray();
			int num5 = array3.Length - 1;
			char[] array4 = new char[array3.Length];
			for (int j = 0; j < array3.Length; j++)
			{
				array4[j] = array3[num5];
				num5--;
			}
			for (int k = 0; k < array3.Length; k++)
			{
				array2[k] = array4[k];
			}
			for (int l = array2.Length - 1; l > array2.Length - 1 - num4; l--)
			{
				array2[l] = '0';
			}
			string text = "";
			for (int m = 0; m < array2.Length; m += 4)
			{
				string text2 = "";
				for (int n = 0; n < 4; n++)
				{
					if (array2[m + n] == '0')
					{
						text2 = "0" + text2;
					}
					else
					{
						text2 = "1" + text2;
					}
				}
				int value = Convert.ToInt32(text2, 2);
				text = Convert.ToString(value, 16) + text;
			}
			return text.ToUpper();
		}

		public static string StringBase2_Into_StringBase16(string StringBase2, int numbits)
		{
			bool flag = false;
			numbits = Tools.TestForValidINT32(numbits, ref flag);
			string result;
			if (!flag)
			{
				MessageBox.Show(" Number of bits is not valid in: 'StringBase2_Into_StringBase16'.");
				result = "";
			}
			else
			{
				string text = Tools.StringBase2_Into_StringBase16(StringBase2);
				int num = numbits % 4;
				if (num != 0)
				{
					num = 1;
				}
				int num2 = numbits / 4;
				int numOfChars = num2 + num;
				text = Tools.addZeros(text, numOfChars, true);
				result = text;
			}
			return result;
		}

		public static bool[] StringBase2_Into_ArrayOf_Bool(string StringBase2)
		{
			bool[] array = new bool[StringBase2.Length];
			char[] array2 = new char[StringBase2.Length];
			array2 = StringBase2.ToCharArray();
			int num = StringBase2.Length - 1;
			for (int i = 0; i < StringBase2.Length; i++)
			{
				if (array2[i] == '1')
					array[num] = true;
				else
					array[num] = false;
				num--;
			}
			return array;
		}

		public static string int32_Into_stringBase16(int Int32_Input, int numberOfBits)
		{
			string result;
			if (Int32_Input >= 0)
			{
				string text = Tools.int32_Into_stringBase16(Int32_Input);
				string stringBase = Tools.StringBase16_Into_StringBase2(text, numberOfBits, true);
				string text2 = Tools.StringBase2_Into_StringBase16(stringBase, numberOfBits);
				result = text2;
			}
			else
			{
				int num;
				if (numberOfBits % 4 == 0)
				{
					num = numberOfBits / 4;
				}
				else
				{
					num = numberOfBits / 4 + 1;
				}
				string text = Tools.int32_Into_stringBase16(Int32_Input);
				int length = text.Length;
				int num2 = length - num;
				if (num2 < 0)
				{
					MessageBox.Show("ERROR in: int32_Into_stringBase16(). To few bits for a negative number.");
					result = "00";
				}
				else
				{
					string text3 = text.Substring(num2, num);
					result = text3;
				}
			}
			return result;
		}

		public static string int32_Into_stringBase16(int Int32_Input)
		{
			string text = Int32_Input.ToString("X");
			return text.ToUpper();
		}

		public static byte int32_Into_Byte(int Int32_Input)
		{
			byte b = 0;
			byte result;
			if (Int32_Input > 255)
			{
				MessageBox.Show("Integer is too large to be a byte");
				result = 0;
			}
			else if (Int32_Input < 0)
			{
				MessageBox.Show("Integer must be greater than 0 to be a byte");
				result = 0;
			}
			else
			{
				try
				{
					b = Convert.ToByte(Int32_Input);
				}
				catch
				{
					MessageBox.Show("Integer could not be converted to a byte");
				}
				result = b;
			}
			return result;
		}

		public static string int32_Into_stringBase2(int Int32_Input, int numberOfBits)
		{
			return addZeros(int32_Into_stringBase2(Int32_Input), numberOfBits, true);
		}

		public static string int32_Into_stringBase2(int Int32_Input)
		{
			string text = Int32_Input.ToString("X");
			text = Tools.StringBase16_Into_StringBase2(text, true);
			return text.ToUpper();
		}

		public static string Double_to_string(double input_d, int NumberOfDecimalPlaces)
		{
			string result = "";
			if (NumberOfDecimalPlaces == 0)
				result = input_d.ToString("0.");
			if (NumberOfDecimalPlaces == 1)
				result = input_d.ToString("0.0");
			if (NumberOfDecimalPlaces == 2)
				result = input_d.ToString("0.00");
			if (NumberOfDecimalPlaces == 3)
				result = input_d.ToString("0.000");
			if (NumberOfDecimalPlaces == 4)
				result = input_d.ToString("0.0000");
			if (NumberOfDecimalPlaces == 5)
				result = input_d.ToString("0.00000");
			if (NumberOfDecimalPlaces == 6)
				result = input_d.ToString("0.000000");
			if (NumberOfDecimalPlaces == 7)
				result = input_d.ToString("0.0000000");
			if (NumberOfDecimalPlaces == 8)
				result = input_d.ToString("0.00000000");
			if (NumberOfDecimalPlaces == 9)
				result = input_d.ToString("0.000000000");
			if (NumberOfDecimalPlaces == 10)
				result = input_d.ToString("0.0000000000");
			if (NumberOfDecimalPlaces == 11)
				result = input_d.ToString("0.00000000000");
			if (NumberOfDecimalPlaces == 12)
				result = input_d.ToString("0.000000000000");
			if (NumberOfDecimalPlaces == 13)
				result = input_d.ToString("0.0000000000000");
			if (NumberOfDecimalPlaces == 14)
				result = input_d.ToString("0.00000000000000");
			if (NumberOfDecimalPlaces == 15)
				result = input_d.ToString("0.000000000000000");
			if (NumberOfDecimalPlaces == 16)
				result = input_d.ToString("0.0000000000000000");
			return result;
		}

		public static void timeDelay(double delayTime, string time)
		{
			time = time.ToUpper();
			if (time != "S" && time != "MS" && time != "M")
			{
				string text = "error in timedelay method";
				MessageBox.Show(text);
			}
			DateTime t = DateTime.Now.AddSeconds(delayTime);
			if (time == "S")
			{
				t = DateTime.Now.AddSeconds(delayTime);
			}
			else if (time == "MS")
			{
				t = DateTime.Now.AddMilliseconds(delayTime);
			}
			else if (time == "M")
			{
				t = DateTime.Now.AddMinutes(delayTime);
			}
			while (t > DateTime.Now)
			{
				Application.DoEvents();
			}
		}

		public static void timeDelay(int delayTime, string time)
		{
			double delayTime2 = Tools.convert_Int32_to_double(delayTime);
			Tools.timeDelay(delayTime2, time);
		}

		public static string RemoveZerosFromString(string input, bool RemoveZerosFromLeft)
		{
			string result = "";
			bool flag = false;
			char[] array = new char[input.Length];
			try
			{
				array = input.ToCharArray();
			}
			catch
			{
				return result;
			}
			string text = "";
			if (RemoveZerosFromLeft)
			{
				for (int i = 0; i < array.Length; i++)
				{
					if (array[i] != '0' || flag)
					{
						string text2 = array[i].ToString();
						text += text2;
						flag = true;
					}
				}
				input = text;
			}
			else
			{
				for (int i = array.Length - 1; i >= 0; i--)
				{
					if (array[i] != '0' || flag)
					{
						string text2 = array[i].ToString();
						text = text2 + text;
						flag = true;
					}
				}
				input = text;
			}
			return input;
		}

		public static string addZeros(string input, int NumOfChars, bool AddZerosToLeft)
		{
			string text = "";
			int length;
			try
			{
				length = input.Length;
				int num = Convert.ToInt32(NumOfChars);
			}
			catch
			{
				return text;
			}
			input = Tools.RemoveZerosFromString(input, AddZerosToLeft);
			length = input.Length;
			int num2 = NumOfChars - length;
			string result;
			if (num2 < 0)
			{
				string text2 = "ERROR in \"AddZeros\".  The number of zeros to add is: " + num2.ToString() + " .";
				MessageBox.Show(text2);
				result = text;
			}
			else
			{
				if (AddZerosToLeft)
				{
					for (int i = 0; i < num2; i++)
					{
						input = "0" + input;
					}
				}
				else
				{
					for (int i = 0; i < num2; i++)
					{
						input += "0";
					}
				}
				text = input;
				result = text;
			}
			return result;
		}

		public static string RemoveOnesFromString(string input, bool RemoveOnessFromLeft)
		{
			string result = "";
			bool flag = false;
			char[] array = new char[input.Length];
			try
			{
				array = input.ToCharArray();
			}
			catch
			{
				return result;
			}
			string text = "";
			if (RemoveOnessFromLeft)
			{
				for (int i = 0; i < array.Length; i++)
				{
					if (array[i] != '1' || flag)
					{
						string text2 = array[i].ToString();
						text += text2;
						flag = true;
					}
				}
				input = text;
			}
			else
			{
				for (int i = array.Length - 1; i >= 0; i--)
				{
					if (array[i] != '1' || flag)
					{
						string text2 = array[i].ToString();
						text = text2 + text;
						flag = true;
					}
				}
				input = text;
			}
			return input;
		}

		public static string addOnes(string input, int NumOfChars, bool AddOnesToLeft)
		{
			string text = "";
			int length;
			try
			{
				length = input.Length;
				int num = Convert.ToInt32(NumOfChars);
			}
			catch
			{
				return text;
			}
			input = Tools.RemoveOnesFromString(input, AddOnesToLeft);
			length = input.Length;
			int num2 = NumOfChars - length;
			string result;
			if (num2 < 0)
			{
				string text2 = "ERROR in \"AddOnes\".  The number of ones to add is: " + num2.ToString() + " .";
				MessageBox.Show(text2);
				result = text;
			}
			else
			{
				if (AddOnesToLeft)
				{
					for (int i = 0; i < num2; i++)
					{
						input = "1" + input;
					}
				}
				else
				{
					for (int i = 0; i < num2; i++)
					{
						input += "1";
					}
				}
				text = input;
				result = text;
			}
			return result;
		}

		public static int TestForValidINT32(string Input_string, int Base_INT32, ref bool IsValid)
		{
			int result = 0;
			if (Base_INT32 == 16)
			{
				IsValid = Tools.TestForValidINT32_B16(Input_string, ref result);
				if (!IsValid)
				{
					return result;
				}
			}
			else if (Base_INT32 == 10)
			{
				IsValid = Tools.TestForValidINT32_B10(Input_string, ref result);
				if (!IsValid)
				{
					return result;
				}
			}
			else if (Base_INT32 == 2)
			{
				IsValid = Tools.TestForValidINT32_B2(Input_string, ref result);
				if (!IsValid)
				{
					return result;
				}
			}
			else
			{
				MessageBox.Show("Only base 2,10,&16 are supported in 'TestForValidINT32()'");
			}
			return result;
		}

		public static int TestForValidINT32(int Input_INT32, ref bool IsValid)
		{
			int result;
			try
			{
				Input_INT32 = Convert.ToInt32(Input_INT32);
				IsValid = true;
				result = Input_INT32;
			}
			catch
			{
				IsValid = false;
				result = 0;
			}
			return result;
		}

		public static bool TestForValidINT32(string Input_string, int Base_INT32, int MinNumber_INT32, int MaxNumber_INT32, string errorMessage)
		{
			int num = 0;
			if (errorMessage == null)
			{
				errorMessage = "";
			}
			if (Base_INT32 == 16)
			{
				if (!Tools.TestForValidINT32_B16(Input_string, ref num))
				{
					if (errorMessage != "")
					{
						MessageBox.Show(errorMessage);
					}
					return false;
				}
			}
			else if (Base_INT32 == 10)
			{
				if (!Tools.TestForValidINT32_B10(Input_string, ref num))
				{
					if (errorMessage != "")
					{
						MessageBox.Show(errorMessage);
					}
					return false;
				}
			}
			else if (Base_INT32 == 2)
			{
				if (!Tools.TestForValidINT32_B2(Input_string, ref num))
				{
					if (errorMessage != "")
					{
						MessageBox.Show(errorMessage);
					}
					return false;
				}
			}
			else
			{
				MessageBox.Show("Only base 2,10,&16 are supported in 'TestForValidINT32()'");
			}
			bool result;
			if (num < MinNumber_INT32)
			{
				if (errorMessage != "")
				{
					MessageBox.Show(errorMessage);
				}
				result = false;
			}
			else if (num > MaxNumber_INT32)
			{
				if (errorMessage != "")
				{
					MessageBox.Show(errorMessage);
				}
				result = false;
			}
			else
			{
				result = true;
			}
			return result;
		}

		public static bool TestForValidINT32(int Input_INT32, int MinNumber_INT32, int MaxNumber_INT32, string errorMessage)
		{
			if (errorMessage == null)
			{
				errorMessage = "";
			}
			bool result;
			if (!Tools.TestForValidINT32(Input_INT32))
			{
				if (errorMessage != "")
				{
					MessageBox.Show(errorMessage);
				}
				result = false;
			}
			else if (Input_INT32 < MinNumber_INT32)
			{
				if (errorMessage != "")
				{
					MessageBox.Show(errorMessage);
				}
				result = false;
			}
			else if (Input_INT32 > MaxNumber_INT32)
			{
				if (errorMessage != "")
				{
					MessageBox.Show(errorMessage);
				}
				result = false;
			}
			else
			{
				result = true;
			}
			return result;
		}

		public static bool TestForValidINT32(int Input_INT32)
		{
			bool result;
			try
			{
				Input_INT32 = Convert.ToInt32(Input_INT32);
				result = true;
			}
			catch
			{
				result = false;
			}
			return result;
		}

		public static bool TestForValidINT32_B10(string Input_string_B10, ref int Input_INT32)
		{
			bool result;
			try
			{
				Input_INT32 = Convert.ToInt32(Input_string_B10, 10);
				result = true;
			}
			catch
			{
				result = false;
			}
			return result;
		}

		public static bool TestForValidINT32_B16(string Input_string_B16, ref int Input_INT32)
		{
			bool result;
			try
			{
				Input_INT32 = Convert.ToInt32(Input_string_B16, 16);
				result = true;
			}
			catch
			{
				result = false;
			}
			return result;
		}

		public static bool TestForValidINT32_B2(string Input_string_B2, ref int Input_INT32)
		{
			bool result;
			try
			{
				Input_INT32 = Convert.ToInt32(Input_string_B2, 2);
				result = true;
			}
			catch
			{
				result = false;
			}
			return result;
		}

		public static double convert_Int32_to_double(int input)
		{
			double result = 0.0;
			try
			{
				result = (double)Convert.ToInt32(input);
			}
			catch
			{
				result = 0.0;
			}
			return result;
		}

		public static Color GoodColorsToUse(int number)
		{
			Color[] array = new Color[20];
			if (number >= 20)
			{
				MessageBox.Show("Invalid number for 'GoodColorsToUse()'");
				number = 0;
			}
			int num = 0;
			array[num++] = Color.DarkBlue;
			array[num++] = Color.DarkCyan;
			array[num++] = Color.DarkGoldenrod;
			array[num++] = Color.DarkGreen;
			array[num++] = Color.DarkMagenta;
			array[num++] = Color.DarkOliveGreen;
			array[num++] = Color.DarkOrange;
			array[num++] = Color.DarkRed;
			array[num++] = Color.DarkSlateBlue;
			array[num++] = Color.DarkSlateGray;
			array[num++] = Color.LimeGreen;
			array[num++] = Color.DeepPink;
			array[num++] = Color.ForestGreen;
			array[num++] = Color.Gold;
			array[num++] = Color.GreenYellow;
			array[num++] = Color.Khaki;
			array[num++] = Color.LightSeaGreen;
			array[num++] = Color.LightSkyBlue;
			array[num++] = Color.LightSteelBlue;
			array[num++] = Color.MediumSlateBlue;
			return array[number];
		}

		public static string[] loadRegDefinitionFromConfigFile(string xmlFile, int rowClicked, int columnClicked)
		{
			string[] result = null;
			if (columnClicked == -1 && rowClicked != -1)
			{
				string start = "<Reg" + rowClicked.ToString() + ">";
				string end = "</Reg" + rowClicked.ToString() + ">";
				string[] regs = Regex.Split(xmlFile, start);
				result = Regex.Split(regs[1], end);
			}
			return result;
		}

		private const int row = 0;
		private const int A = 1;
		private const int D = 2;
		private const int O = 3;
		private const int R = 4;
	}
}
