using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using System.Text;

namespace USBClassLibrary
{
	public class USBClass
	{
		public delegate void USBDeviceEventHandler(object sender, USBDeviceEventArgs e);

		public event USBDeviceEventHandler USBDeviceAttached;
		public event USBDeviceEventHandler USBDeviceRemoved;
		public event USBDeviceEventHandler USBDeviceQueryRemove;

		public bool IsQueryHooked
		{
			get
			{
				return !(deviceEventHandle == IntPtr.Zero);
			}
		}

		public bool RegisterForDeviceChange(bool Register, IntPtr WindowsHandle)
		{
			bool flag = false;
			if (Register)
			{
				Win32Wrapper.DEV_BROADCAST_DEVICEINTERFACE dev_BROADCAST_DEVICEINTERFACE = default(Win32Wrapper.DEV_BROADCAST_DEVICEINTERFACE);
				int num = Marshal.SizeOf(dev_BROADCAST_DEVICEINTERFACE);
				dev_BROADCAST_DEVICEINTERFACE.dbcc_size = num;
				dev_BROADCAST_DEVICEINTERFACE.dbcc_devicetype = 5;
				IntPtr intPtr = IntPtr.Zero;
				intPtr = Marshal.AllocHGlobal(num);
				Marshal.StructureToPtr(dev_BROADCAST_DEVICEINTERFACE, intPtr, true);
				deviceEventHandle = Win32Wrapper.RegisterDeviceNotification(WindowsHandle, intPtr, 4);
				flag = (deviceEventHandle != IntPtr.Zero);
				if (!flag)
					Marshal.GetLastWin32Error();
				Marshal.FreeHGlobal(intPtr);
			}
			else
			{
				if (deviceEventHandle != IntPtr.Zero)
					flag = Win32Wrapper.UnregisterDeviceNotification(deviceEventHandle);
				deviceEventHandle = IntPtr.Zero;
			}
			return flag;
		}

		public void ProcessWindowsMessage(int Msg, IntPtr WParam, IntPtr LParam, ref bool handled)
		{
			if (Msg == 537)
			{
				switch (WParam.ToInt32())
				{
					case 32768:
						{
							Win32Wrapper.DBTDEVTYP dbtdevtyp = (Win32Wrapper.DBTDEVTYP)Marshal.ReadInt32(LParam, 4);
							if (dbtdevtyp == Win32Wrapper.DBTDEVTYP.DBT_DEVTYP_DEVICEINTERFACE)
							{
								handled = true;
								USBDeviceAttached(this, new USBDeviceEventArgs());
							}
							break;
						}
					case 32769:
						{
							Win32Wrapper.DBTDEVTYP dbtdevtyp = (Win32Wrapper.DBTDEVTYP)Marshal.ReadInt32(LParam, 4);
							if (dbtdevtyp == Win32Wrapper.DBTDEVTYP.DBT_DEVTYP_DEVICEINTERFACE)
							{
								handled = true;
								USBDeviceQueryRemove(this, new USBDeviceEventArgs());
							}
							break;
						}
					case 32772:
						{
							handled = true;
							Win32Wrapper.DBTDEVTYP dbtdevtyp = (Win32Wrapper.DBTDEVTYP)Marshal.ReadInt32(LParam, 4);
							if (dbtdevtyp == Win32Wrapper.DBTDEVTYP.DBT_DEVTYP_DEVICEINTERFACE)
								USBDeviceRemoved(this, new USBDeviceEventArgs());
							break;
						}
				}
			}
		}

		public static bool GetUSBDevice(uint VID, uint PID, ref List<DeviceProperties> ListOfDP, bool GetCOMPort, uint? MI = null)
		{
			IntPtr intPtr = Marshal.AllocHGlobal(1024);
			IntPtr intPtr2 = IntPtr.Zero;
			DeviceProperties item = default(DeviceProperties);
			ListOfDP.Clear();
			bool result;
			try
			{
				string enumerator = "USB";
				string text = string.Empty;
				string text2 = string.Empty;
				text = "VID_" + VID.ToString("X4") + "&PID_" + PID.ToString("X4");
				text = text.ToLowerInvariant();
				if (MI != null)
				{
					text2 = "MI_" + MI.Value.ToString("X2");
					text2 = text2.ToLowerInvariant();
				}
				intPtr2 = Win32Wrapper.SetupDiGetClassDevs(IntPtr.Zero, enumerator, IntPtr.Zero, 6);
				if (intPtr2.ToInt32() != -1)
				{
					bool flag = true;
					uint num = 0u;
					while (flag)
					{
						if (flag)
						{
							uint num2 = 0u;
							uint num3 = 0u;
							IntPtr zero = IntPtr.Zero;
							Win32Wrapper.SP_DEVINFO_DATA sp_DEVINFO_DATA = default(Win32Wrapper.SP_DEVINFO_DATA);
							sp_DEVINFO_DATA.cbSize = (uint)Marshal.SizeOf(sp_DEVINFO_DATA);
							flag = Win32Wrapper.SetupDiEnumDeviceInfo(intPtr2, num, ref sp_DEVINFO_DATA);
							if (flag)
							{
								Win32Wrapper.SetupDiGetDeviceRegistryProperty(intPtr2, ref sp_DEVINFO_DATA, 1u, ref num3, IntPtr.Zero, 0u, ref num2);
								Win32Wrapper.WinErrors winErrors = (Win32Wrapper.WinErrors)Marshal.GetLastWin32Error();
								if (winErrors == Win32Wrapper.WinErrors.ERROR_INSUFFICIENT_BUFFER)
								{
									if (num2 <= 1024u)
									{
										if (Win32Wrapper.SetupDiGetDeviceRegistryProperty(intPtr2, ref sp_DEVINFO_DATA, 1u, ref num3, intPtr, 1024u, ref num2))
										{
											string text3 = Marshal.PtrToStringAuto(intPtr);
											text3 = text3.ToLowerInvariant();
											if (text3.Contains(text) && ((MI != null && text3.Contains(text2)) || (MI == null && !text3.Contains("mi"))))
											{
												item.FriendlyName = string.Empty;
												if (Win32Wrapper.SetupDiGetDeviceRegistryProperty(intPtr2, ref sp_DEVINFO_DATA, 12u, ref num3, intPtr, 1024u, ref num2))
													item.FriendlyName = Marshal.PtrToStringAuto(intPtr);
												item.DeviceType = string.Empty;
												if (Win32Wrapper.SetupDiGetDeviceRegistryProperty(intPtr2, ref sp_DEVINFO_DATA, 25u, ref num3, intPtr, 1024u, ref num2))
													item.DeviceType = Marshal.PtrToStringAuto(intPtr);
												item.DeviceClass = string.Empty;
												if (Win32Wrapper.SetupDiGetDeviceRegistryProperty(intPtr2, ref sp_DEVINFO_DATA, 7u, ref num3, intPtr, 1024u, ref num2))
													item.DeviceClass = Marshal.PtrToStringAuto(intPtr);
												item.DeviceManufacturer = string.Empty;
												if (Win32Wrapper.SetupDiGetDeviceRegistryProperty(intPtr2, ref sp_DEVINFO_DATA, 11u, ref num3, intPtr, 1024u, ref num2))
													item.DeviceManufacturer = Marshal.PtrToStringAuto(intPtr);
												item.DeviceLocation = string.Empty;
												if (Win32Wrapper.SetupDiGetDeviceRegistryProperty(intPtr2, ref sp_DEVINFO_DATA, 13u, ref num3, intPtr, 1024u, ref num2))
													item.DeviceLocation = Marshal.PtrToStringAuto(intPtr);
												item.DevicePath = string.Empty;
												if (Win32Wrapper.SetupDiGetDeviceRegistryProperty(intPtr2, ref sp_DEVINFO_DATA, 35u, ref num3, intPtr, 1024u, ref num2))
													item.DevicePath = Marshal.PtrToStringAuto(intPtr);
												item.DevicePhysicalObjectName = string.Empty;
												if (Win32Wrapper.SetupDiGetDeviceRegistryProperty(intPtr2, ref sp_DEVINFO_DATA, 14u, ref num3, intPtr, 1024u, ref num2))
													item.DevicePhysicalObjectName = Marshal.PtrToStringAuto(intPtr);
												item.DeviceDescription = string.Empty;
												if (Win32Wrapper.SetupDiGetDeviceRegistryProperty(intPtr2, ref sp_DEVINFO_DATA, 0u, ref num3, intPtr, 1024u, ref num2))
													item.DeviceDescription = Marshal.PtrToStringAuto(intPtr);
												item.COMPort = string.Empty;
												if (GetCOMPort)
												{
													IntPtr hKey = Win32Wrapper.SetupDiOpenDevRegKey(intPtr2, ref sp_DEVINFO_DATA, 1u, 0u, 1u, 131097u);
													if ((long)hKey.ToInt32() == -1L)
													{
														winErrors = (Win32Wrapper.WinErrors)Marshal.GetLastWin32Error();
														break;
													}
													uint num4 = 0u;
													StringBuilder stringBuilder = new StringBuilder(1024);
													uint capacity = (uint)stringBuilder.Capacity;
													int num5 = Win32Wrapper.RegQueryValueEx(hKey, "PortName", 0u, out num4, stringBuilder, ref capacity);
													if (num5 == 0)
														item.COMPort = stringBuilder.ToString();
													Win32Wrapper.RegCloseKey(hKey);
												}
												ListOfDP.Add(item);
											}
										}
									}
								}
							}
						}
						else
						{
							Win32Wrapper.WinErrors winErrors = (Win32Wrapper.WinErrors)Marshal.GetLastWin32Error();
						}
						num += 1u;
					}
				}
				result = (ListOfDP.Count > 0);
			}
			catch (Exception ex)
			{
				throw ex;
			}
			finally
			{
				Win32Wrapper.SetupDiDestroyDeviceInfoList(intPtr2);
				Marshal.FreeHGlobal(intPtr);
			}
			return result;
		}

		~USBClass()
		{
			RegisterForDeviceChange(false, IntPtr.Zero);
		}

		private const long INVALID_HANDLE_VALUE = -1L;
		private const int BUFFER_SIZE = 1024;
		private IntPtr deviceEventHandle;

		private class Win32Wrapper
		{
			[DllImport("user32.dll", SetLastError = true)]
			public static extern IntPtr RegisterDeviceNotification(IntPtr hRecipient, IntPtr NotificationFilter, int Flags);

			[DllImport("user32.dll", SetLastError = true)]
			public static extern bool UnregisterDeviceNotification(IntPtr hHandle);

			[DllImport("setupapi.dll", SetLastError = true)]
			public static extern bool SetupDiEnumDeviceInfo(IntPtr DeviceInfoSet, uint MemberIndex, ref Win32Wrapper.SP_DEVINFO_DATA DeviceInfoData);

			[DllImport("setupapi.dll", CharSet = CharSet.Auto, SetLastError = true)]
			public static extern bool SetupDiDestroyDeviceInfoList(IntPtr hDevInfo);

			[DllImport("setupapi.dll", CharSet = CharSet.Auto)]
			public static extern IntPtr SetupDiGetClassDevs(IntPtr ClassGuid, string Enumerator, IntPtr hwndParent, int Flags);

			[DllImport("setupapi.dll", CharSet = CharSet.Auto, SetLastError = true)]
			public static extern bool SetupDiGetDeviceRegistryProperty(IntPtr DeviceInfoSet, ref Win32Wrapper.SP_DEVINFO_DATA DeviceInfoData, uint Property, ref uint PropertyRegDataType, IntPtr PropertyBuffer, uint PropertyBufferSize, ref uint RequiredSize);

			[DllImport("Setupapi", CharSet = CharSet.Auto, SetLastError = true)]
			public static extern IntPtr SetupDiOpenDevRegKey(IntPtr hDeviceInfoSet, ref Win32Wrapper.SP_DEVINFO_DATA DeviceInfoData, uint Scope, uint HwProfile, uint KeyType, uint samDesired);

			[DllImport("advapi32.dll", CharSet = CharSet.Unicode, EntryPoint = "RegQueryValueExW", SetLastError = true)]
			public static extern int RegQueryValueEx(IntPtr hKey, string lpValueName, uint lpReserved, out uint lpType, StringBuilder lpData, ref uint lpcbData);

			[DllImport("advapi32.dll", SetLastError = true)]
			public static extern int RegCloseKey(IntPtr hKey);

			public const int WM_DEVICECHANGE = 537;

			public enum DBTDEVTYP : uint
			{
				DBT_DEVTYP_OEM,
				DBT_DEVTYP_DEVNODE,
				DBT_DEVTYP_VOLUME,
				DBT_DEVTYP_PORT,
				DBT_DEVTYP_NET,
				DBT_DEVTYP_DEVICEINTERFACE,
				DBT_DEVTYP_HANDLE
			}

			public enum WinErrors : long
			{
				ERROR_SUCCESS,
				ERROR_INVALID_FUNCTION,
				ERROR_FILE_NOT_FOUND,
				ERROR_PATH_NOT_FOUND,
				ERROR_TOO_MANY_OPEN_FILES,
				ERROR_ACCESS_DENIED,
				ERROR_INVALID_HANDLE,
				ERROR_ARENA_TRASHED,
				ERROR_NOT_ENOUGH_MEMORY,
				ERROR_INVALID_BLOCK,
				ERROR_BAD_ENVIRONMENT,
				ERROR_BAD_FORMAT,
				ERROR_INVALID_ACCESS,
				ERROR_INVALID_DATA,
				ERROR_OUTOFMEMORY,
				ERROR_INSUFFICIENT_BUFFER = 122L,
				ERROR_MORE_DATA = 234L,
				ERROR_NO_MORE_ITEMS = 259L,
				ERROR_SERVICE_SPECIFIC_ERROR = 1066L,
				ERROR_INVALID_USER_BUFFER = 1784L
			}


			[StructLayout(LayoutKind.Sequential, Pack = 1)]
			public struct SP_DEVINFO_DATA
			{
				public uint cbSize;
				public Guid ClassGuid;
				public uint DevInst;
				public IntPtr Reserved;
			}

			[StructLayout(LayoutKind.Sequential, Pack = 1)]
			public struct SP_DEVICE_INTERFACE_DATA
			{
				public uint cbSize;
				public Guid interfaceClassGuid;
				public uint flags;
				private IntPtr reserved;
			}

			[StructLayout(LayoutKind.Sequential, Pack = 1)]
			public struct SP_DEVICE_INTERFACE_DETAIL_DATA
			{
				public uint cbSize;
				[MarshalAs(UnmanagedType.ByValTStr, SizeConst = 256)]
				public string devicePath;
			}

			[StructLayout(LayoutKind.Explicit)]
			private struct DevBroadcastDeviceInterfaceBuffer
			{
				public DevBroadcastDeviceInterfaceBuffer(int deviceType)
				{
					dbch_size = Marshal.SizeOf(typeof(Win32Wrapper.DevBroadcastDeviceInterfaceBuffer));
					dbch_devicetype = deviceType;
					dbch_reserved = 0;
				}

				[FieldOffset(0)]
				public int dbch_size;

				[FieldOffset(4)]
				public int dbch_devicetype;

				[FieldOffset(8)]
				public int dbch_reserved;
			}

			[StructLayout(LayoutKind.Sequential, CharSet = CharSet.Unicode)]
			public struct DEV_BROADCAST_DEVICEINTERFACE
			{
				public int dbcc_size;
				public int dbcc_devicetype;
				public int dbcc_reserved;
				[MarshalAs(UnmanagedType.ByValArray, SizeConst = 16, ArraySubType = UnmanagedType.U1)]
				public byte[] dbcc_classguid;
				[MarshalAs(UnmanagedType.ByValArray, SizeConst = 128)]
				public char[] dbcc_name;
			}
		}

		public class USBDeviceEventArgs : EventArgs
		{
			public USBDeviceEventArgs()
			{
				Cancel = false;
				HookQueryRemove = false;
			}

			public bool Cancel;
			public bool HookQueryRemove;
		}

		public struct DeviceProperties
		{
			public string FriendlyName;
			public string DeviceDescription;
			public string DeviceType;
			public string DeviceManufacturer;
			public string DeviceClass;
			public string DeviceLocation;
			public string DevicePath;
			public string DevicePhysicalObjectName;
			public string COMPort;
		}
	}
}
