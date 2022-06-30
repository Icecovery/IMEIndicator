using System;
using System.Runtime.InteropServices;

namespace IMEIndicator
{
	internal class WinAPI
	{
		public const uint WINEVENT_OUTOFCONTEXT = 0;
		public const uint EVENT_SYSTEM_FOREGROUND = 3;

		[DllImport("user32.dll")] public static extern IntPtr GetForegroundWindow();
		[DllImport("user32.dll")] public static extern uint GetWindowThreadProcessId(IntPtr hwnd, IntPtr proccess);
		[DllImport("user32.dll")] public static extern IntPtr GetKeyboardLayout(uint thread);
		[DllImport("user32.dll")] public static extern bool DestroyIcon(IntPtr handle);
		[DllImport("gdi32.dll")] public static extern IntPtr AddFontMemResourceEx(IntPtr pbFont, uint cbFont, IntPtr pdv, [In] ref uint pcFonts);
		[DllImport("user32.dll")] public static extern IntPtr SetWinEventHook(uint eventMin, uint eventMax, IntPtr hmodWinEventProc, WinEventDelegate lpfnWinEventProc, uint idProcess, uint idThread, uint dwFlags);

		// https://stackoverflow.com/a/10280800
		public delegate void WinEventDelegate(IntPtr hWinEventHook, uint eventType, IntPtr hwnd, int idObject, int idChild, uint dwEventThread, uint dwmsEventTime);
	}
}
