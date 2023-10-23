using System;
using System.IO;
using System.Runtime.InteropServices;
using System.Text;

namespace TI.eLAB.EVM
{
    public class common
	{
		public static U2A u2a = new U2A();
	}

	#region Clock
	public enum ClockDivider1
	{
		_1,
		_2,
		_4,
		_8,
		_16,
		_32
	}
	public enum ClockDivider2
	{
		_1,
		_2,
		_4,
		_8,
		_16,
		_32
	}
	#endregion

	#region ADC/DAC
	public enum ADC_PinFunction
	{
		No_Change,
		Analog_In
	}
	public enum ADC_VREF
	{
		_1V5,
		_2V5,
		_3V3,
		External
	}
	public enum DACs_OperatingMode
	{
		Normal,
		PWD_1k,
		PWD_100k,
		PWD_HiZ
	}
	public enum DACs_WhichDAC
	{
		DAC0,
		DAC1
	}
	#endregion

	#region GPIO
	public enum GPIO_InPinState
	{
		Low,
		High
	}
	public enum GPIO_OutPinState
	{
		No_Change,
		Low,
		High
	}
	public enum GPIO_PinFunction
	{
		No_Change,
		Output,
		Input_No_Resistor,
		Input_Pull_Up,
		Input_Pull_Down
	}
	#endregion

	#region I2C
	public enum I2C_AddressLength
	{
		_7Bits,
		_10Bits
	}
	public enum I2C_PullUps
	{
		OFF,
		ON
	}
	public enum I2C_Speed
	{
		_100kHz,
		_400kHz,
		_10kHz
	}
	#endregion

	#region Power and LED
	public enum LED
	{
		OFF,
		ON,
		TOGGLE
	}

	public enum Power_3V3
	{
		OFF,
		ON
	}
	public enum Power_5V0
	{
		OFF,
		ON
	}
	#endregion

	#region PWM
	public enum PWM_InputDivider
	{
		_1,
		_2,
		_4,
		_8
	}
	public enum PWM_InputDividerEX
	{
		_1,
		_2,
		_3,
		_4,
		_5,
		_6,
		_7,
		_8
	}
	public enum PWM_ModeControl
	{
		Stop,
		Up,
		Continuous,
		UP_Down
	}
	public enum PWM_OutputMode
	{
		Out_bit_value,
		Set,
		Toggle_Reset,
		Set_Reset,
		Toggle,
		Reset,
		Toggle_Set,
		Reset_Set
	}
	public enum PWM_WhichPWM
	{
		PWM0,
		PWM1,
		PWM2,
		PWM3
	}
	#endregion

	#region SPI
	public enum SPI_BitDirection
	{
		LSB_First,
		MSB_First
	}
	public enum SPI_CharacterLength
	{
		_8_Bit,
		_7_Bit
	}
	public enum SPI_ChipSelectType
	{
		With_Every_Word,
		With_Every_Packet
	}
	public enum SPI_ClockPhase
	{
		Change_On_First_Edge,
		Change_On_Following_Edge
	}
	public enum SPI_ClockPolarity
	{
		Inactive_State_Low,
		Inactive_State_High
	}
	public enum SPI_LatchPolarity
	{
		Low_To_High,
		High_To_Low
	}

	#endregion

	#region UART
	public enum UART_BaudRate
	{
		_9600_bps,
		_19200_bps,
		_38400_bps,
		_57600_bps,
		_115200_bps,
		_230400_bps
	}
	public enum UART_BitDirection
	{
		LSB_First,
		MSB_First
	}
	public enum UART_CharacterLength
	{
		_8_Bit,
		_7_Bit
	}
	public enum UART_Parity
	{
		None,
		Even,
		Odd
	}
	public enum UART_StopBits
	{
		One,
		Two
	}
	#endregion

	public class U2A
	{
		[DllImport("USB2ANY.dll")]
		public static extern int u2aFindControllers();
		public int FindControllers()
		{
			return u2aFindControllers();
		}

		[DllImport("USB2ANY.dll")]
		public static extern int u2aGetSerialNumber(int index, StringBuilder SerialNumber);
		public int GetSerialNumber(int index, ref string SerialNumber)
		{
			if (index == -1)
				index = m_u2aHandle;

			StringBuilder stringBuilder = new StringBuilder(40);
			int result = u2aGetSerialNumber(index, stringBuilder);
			SerialNumber = stringBuilder.ToString();
			return result;
		}

		[DllImport("USB2ANY.dll")]
		public static extern int u2aOpenW([MarshalAs(UnmanagedType.LPWStr)] string SerialNumber);

		[DllImport("USB2ANY.dll")]
		public static extern int u2aLogComment([MarshalAs(UnmanagedType.LPStr)] string Comment);

		public int Open(string SerialNumber)
		{
			if (m_u2aHandle != 65535)
			{
				u2aClose(m_u2aHandle);
				m_u2aHandle = 65535;
			}
			u2aLogComment("Using USB2ANY_CS.DLL");
			int handle = u2aOpenW(SerialNumber);
			if (handle > 0)
			{
				m_u2aHandle = handle;
				SetReceiveTimeout(20);
                handle = 0;
            }
            return handle;
		}

		[DllImport("USB2ANY.dll")]
		public static extern int u2aSetReceiveTimeout(int milliseconds);

		public int SetReceiveTimeout(int milliseconds)
		{
			return u2aSetReceiveTimeout(milliseconds);
		}

		[DllImport("USB2ANY.dll")]
		public static extern int u2aClose(int handle);

		public int Close()
		{
			int result = u2aClose(m_u2aHandle);
			m_u2aHandle = 65535;
			return result;
		}

		[DllImport("USB2ANY.dll")]
		public static extern int u2aRestart(int handle);

		public int Restart()
		{
			return u2aRestart(m_u2aHandle);
		}

		[DllImport("USB2ANY.dll")]
		public static extern int u2aSendCommand(int handle, byte command, [MarshalAs(UnmanagedType.LPArray)] byte[] Data, byte nBytes);

		public int SendCommand(byte command, byte[] Data, byte nBytes)
		{
			return u2aSendCommand(m_u2aHandle, command, Data, nBytes);
		}

		[DllImport("USB2ANY.dll")]
		public static extern int u2aGetCommandResponse(int handle, byte command, [MarshalAs(UnmanagedType.LPArray)] byte[] Data, byte nBytes);

		public int GetCommandResponse(byte command, byte[] Data, byte nBytes)
		{
			return u2aGetCommandResponse(m_u2aHandle, command, Data, nBytes);
		}

		[DllImport("USB2ANY.dll")]
		public static extern int u2aI2C_Control(int handle, I2C_Speed Speed, I2C_AddressLength AddressLength, I2C_PullUps PullUps);

		public int I2C_Control(I2C_Speed Speed, I2C_AddressLength AddressLength, I2C_PullUps PullUps)
		{
			return u2aI2C_Control(m_u2aHandle, Speed, AddressLength, PullUps);
		}

		[DllImport("USB2ANY.dll")]
		public static extern int u2aI2C_RegisterWrite(int handle, ushort I2C_Address, byte RegisterAddress, byte Value);

		public int I2C_RegisterWrite(ushort I2C_Address, byte RegisterAddress, byte Value)
		{
			return u2aI2C_RegisterWrite(m_u2aHandle, I2C_Address, RegisterAddress, Value);
		}

		[DllImport("USB2ANY.dll")]
		public static extern int u2aGPIO_SetPort(int handle, byte GPIO_Port, byte function);
		public int GPIO_SetPort(byte GPIO_Port, byte function)
		{
			return u2aGPIO_SetPort(m_u2aHandle, GPIO_Port, function);
		}

		[DllImport("USB2ANY.dll")]
		public static extern int u2aGPIO_WritePort(int handle, byte GPIO_Port, byte state);

		public int GPIO_WritePort(byte GPIO_Port, byte state)
		{
			return u2aGPIO_WritePort(m_u2aHandle, GPIO_Port, state);
		}

		[DllImport("USB2ANY.dll")]
		public static extern int u2aGPIO_ReadPort(int handle, byte GPIO_Port);

		public int GPIO_ReadPort(byte GPIO_Port)
		{
			return u2aGPIO_ReadPort(m_u2aHandle, GPIO_Port);
		}

		[DllImport("USB2ANY.dll")]
		public static extern int u2aUART_Control(int handle, UART_BaudRate _UART_BaudRate, UART_Parity _UART_Parity, UART_BitDirection _UART_BitDirection, UART_CharacterLength _UART_CharacterLength, UART_StopBits _UART_StopBits);
		public int UART_Control(UART_BaudRate _UART_BaudRate, UART_Parity _UART_Parity, UART_BitDirection _UART_BitDirection, UART_CharacterLength _UART_CharacterLength, UART_StopBits _UART_StopBits)
		{
			return u2aUART_Control(m_u2aHandle, _UART_BaudRate, _UART_Parity, _UART_BitDirection, _UART_CharacterLength, _UART_StopBits);
		}

		[DllImport("USB2ANY.dll")]
		public static extern int u2aUART_Write(int handle, byte nBytes, [MarshalAs(UnmanagedType.LPArray)] byte[] Data);
		public int UART_Write(byte nBytes, byte[] Data)
		{
			return u2aUART_Write(m_u2aHandle, nBytes, Data);
		}

		[DllImport("USB2ANY.dll")]
		public static extern int u2aUART_Read(int handle, byte nBytes, [MarshalAs(UnmanagedType.LPArray)] byte[] Data);
		public int UART_Read(byte nBytes, byte[] Data)
		{
			return u2aUART_Read(m_u2aHandle, nBytes, Data);
		}

		[DllImport("USB2ANY.dll")]
		public static extern int u2aFirmwareVersion_Read(int handle, [MarshalAs(UnmanagedType.LPArray)] byte[] szVersion, int bufsize);
		public int FirmwareVersion_Read(ref byte[] buffer, int bufsize)
		{
			return u2aFirmwareVersion_Read(m_u2aHandle, buffer, bufsize);
		}

		[DllImport("USB2ANY.dll")]
		public static extern int u2aSuppressSplash(int suppress);

		public int SuppressSplash(int suppress)
		{
			return u2aSuppressSplash(suppress);
		}

		[DllImport("USB2ANY.dll")]
		public static extern int u2aSuppressFirmwareCheck(int state);
		public int SuppressFirmwareCheck(int state)
		{
			return u2aSuppressFirmwareCheck(state);
		}

		private static int m_u2aHandle = 65535;
		public static string addr_ecbk;
		public static string data_h;
		public static string data_l;
		public static string stat_rsvd;
		public static string crc;
		public static bool fileexists = File.Exists(AppDomain.CurrentDomain.BaseDirectory + "Debug.txt");
		public static bool dodebug = false;
	}
}
