using System;
using System.Collections.Generic;
using System.IO;
using System.Runtime.InteropServices;
using Microsoft.Win32.SafeHandles;

namespace CashinoSDK.UsbPrint;

internal static class UsbPrintHelper
{
    // {28D78FAD-5A12-11D1-AE5B-0000F803A8C2} — интерфейс, который регистрирует usbprint.sys
    private static Guid GUID_DEVINTERFACE_USBPRINT =
        new Guid(0x28D78FAD, 0x5A12, 0x11D1, 0xAE, 0x5B, 0x00, 0x00, 0xF8, 0x03, 0xA8, 0xC2);

    private const uint DIGCF_PRESENT = 0x02, DIGCF_DEVICEINTERFACE = 0x10;
    private const uint GENERIC_READ = 0x80000000, GENERIC_WRITE = 0x40000000;
    private const uint FILE_SHARE_READ = 1, FILE_SHARE_WRITE = 2, OPEN_EXISTING = 3;

    [StructLayout(LayoutKind.Sequential)]
    private struct SP_DEVICE_INTERFACE_DATA
    {
        public int cbSize;
        public Guid InterfaceClassGuid;
        public uint Flags;
        public IntPtr Reserved;
    }

    [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Unicode)]
    private struct SP_DEVICE_INTERFACE_DETAIL_DATA
    {
        public int cbSize;
        [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 260)]
        public string DevicePath;
    }

    [DllImport("setupapi.dll", CharSet = CharSet.Unicode, SetLastError = true)]
    private static extern IntPtr SetupDiGetClassDevs(ref Guid g, IntPtr e, IntPtr p, uint f);

    [DllImport("setupapi.dll", SetLastError = true)]
    private static extern bool SetupDiEnumDeviceInterfaces(IntPtr set, IntPtr devInfo,
        ref Guid g, uint index, ref SP_DEVICE_INTERFACE_DATA data);

    [DllImport("setupapi.dll", CharSet = CharSet.Unicode, SetLastError = true)]
    private static extern bool SetupDiGetDeviceInterfaceDetail(IntPtr set,
        ref SP_DEVICE_INTERFACE_DATA data, ref SP_DEVICE_INTERFACE_DETAIL_DATA detail,
        int detailSize, out int required, IntPtr devInfo);

    [DllImport("setupapi.dll", SetLastError = true)]
    private static extern bool SetupDiDestroyDeviceInfoList(IntPtr set);

    [DllImport("kernel32.dll", CharSet = CharSet.Unicode, SetLastError = true)]
    private static extern SafeFileHandle CreateFile(string path, uint access, uint share,
        IntPtr sa, uint disposition, uint flags, IntPtr template);

    /// <summary>Все пути устройств USB-принтеров, которыми управляет usbprint.sys.</summary>
    public static List<string> EnumeratePrinterDevicePaths()
    {
        var result = new List<string>();
        IntPtr set = SetupDiGetClassDevs(ref GUID_DEVINTERFACE_USBPRINT,
            IntPtr.Zero, IntPtr.Zero, DIGCF_PRESENT | DIGCF_DEVICEINTERFACE);
        if (set == IntPtr.Zero || set == new IntPtr(-1))
            throw new System.ComponentModel.Win32Exception(Marshal.GetLastWin32Error(), "SetupDiGetClassDevs");

        try
        {
            var did = new SP_DEVICE_INTERFACE_DATA { cbSize = Marshal.SizeOf<SP_DEVICE_INTERFACE_DATA>() };
            for (uint i = 0; SetupDiEnumDeviceInterfaces(set, IntPtr.Zero,
                     ref GUID_DEVINTERFACE_USBPRINT, i, ref did); i++)
            {
                var detail = new SP_DEVICE_INTERFACE_DETAIL_DATA
                {
                    // печально известный gotcha: 8 на x64, 6 на x86 (Unicode)
                    cbSize = (IntPtr.Size == 8) ? 8 : 6
                };
                if (SetupDiGetDeviceInterfaceDetail(set, ref did, ref detail,
                        Marshal.SizeOf<SP_DEVICE_INTERFACE_DETAIL_DATA>(), out _, IntPtr.Zero))
                    result.Add(detail.DevicePath);
            }
        }
        finally { SetupDiDestroyDeviceInfoList(set); }
        return result;
    }

    /// <summary>Открыть устройство по пути из EnumeratePrinterDevicePaths().</summary>
    public static FileStream Open(string devicePath)
    {
        SafeFileHandle h = CreateFile(devicePath, GENERIC_READ | GENERIC_WRITE,
            FILE_SHARE_READ | FILE_SHARE_WRITE, IntPtr.Zero, OPEN_EXISTING, 0, IntPtr.Zero);
        if (h.IsInvalid)
            throw new System.ComponentModel.Win32Exception(Marshal.GetLastWin32Error(),
                $"CreateFile('{devicePath}')");
        return new FileStream(h, FileAccess.ReadWrite);
    }
}