using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Drawing.Text;
using System.Globalization;
using System.Runtime.InteropServices;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace IMEIndicator
{
	public class Program
	{
		private static readonly NotifyIcon notifyIcon = new();
		private static readonly KeyboardHook keyboardHook = new();	
		private static readonly StringFormat iconStringFormat = new()
		{
			Alignment = StringAlignment.Center,
			LineAlignment = StringAlignment.Center,
		};
		private static readonly Dictionary<string, Icon> iconMap = new();
		private static Font? iconFont;
		private static WinEventDelegate? winEventDelegate;
		private const string notifyIconText =
			"{0} Keyboard\n" +
			"\n" +
			"Right click to open keyboard setting\n" +
			"Middle click to exit";

		/// <summary>
		/// Main entry point of the program
		/// </summary>
		public static void Main()
		{
			Setup();
			Application.Run();
		}

		/// <summary>
		/// Setup code
		/// </summary>
		private static void Setup()
		{
			SetupProcessExitHook();
			SetupNotifyIcon();
			SetupKeyboardHook();
			SetupWindowEventHook();
			LoadFont();
			UpdateIcon();
		}

		/// <summary>
		/// Initialize process exit hook
		/// </summary>
		private static void SetupProcessExitHook()
		{
			AppDomain.CurrentDomain.ProcessExit += CurrentDomain_ProcessExit;
		}

		/// <summary>
		/// Initialize notify icon
		/// </summary>
		private static void SetupNotifyIcon()
		{
			notifyIcon.Visible = true;
			notifyIcon.MouseClick += NotifyIcon_MouseClick;
		}

		/// <summary>
		/// Initialize keyboard hook
		/// </summary>
		private static void SetupKeyboardHook()
		{
			keyboardHook.KeyUp += KeyboardHook_KeyUp;
			keyboardHook.Install();
		}

		/// <summary>
		/// Initialize foreground window event hook
		/// </summary>
		private static void SetupWindowEventHook()
		{
			// https://stackoverflow.com/a/10280800
			winEventDelegate = new WinEventDelegate(WinEventProc);
			IntPtr m_hhook = SetWinEventHook(EVENT_SYSTEM_FOREGROUND, EVENT_SYSTEM_FOREGROUND, IntPtr.Zero, winEventDelegate, 0, 0, WINEVENT_OUTOFCONTEXT);
		}

		/// <summary>
		/// Update notify icon
		/// </summary>
		private static void UpdateIcon()
		{
			CultureInfo? cultureInfo = GetCurrentKeyboardLayout();

			string text = (cultureInfo?.TwoLetterISOLanguageName.ToUpper()) ?? "XX";

			if (!iconMap.ContainsKey(text))
			{
				Icon newIcon = MakeIcon(text);
				iconMap.Add(text, newIcon);
			}

			notifyIcon.Icon = iconMap[text];
			notifyIcon.Text = string.Format(notifyIconText, (cultureInfo?.DisplayName) ?? "ERROR");
		}

		/// <summary>
		/// Make icon with two letter language code
		/// </summary>
		/// <param name="text">Two letter code</param>
		/// <returns>Generated icon</returns>
		private static Icon MakeIcon(string text)
		{
			Bitmap bitmap = new(128, 128);

			Graphics g = Graphics.FromImage(bitmap);
			g.SmoothingMode = SmoothingMode.AntiAlias;
			g.InterpolationMode = InterpolationMode.HighQualityBicubic;
			g.PixelOffsetMode = PixelOffsetMode.HighQuality;

			// for some reason, iosevka font does not appears to be vertically centered, 72 looks fine
			g.DrawString(text, iconFont!, Brushes.White, 64, 72, iconStringFormat);
			g.Flush();

			Icon newIcon = Icon.FromHandle(bitmap.GetHicon());
			return newIcon;
		}
		
		/// <summary>
		/// Load font from resources
		/// </summary>
		public static void LoadFont()
		{
			// https://stackoverflow.com/a/23519499
			byte[] fontData = Resources.iosevka_fixed_regular_capital;
			IntPtr fontPtr = Marshal.AllocCoTaskMem(fontData.Length);
			Marshal.Copy(fontData, 0, fontPtr, fontData.Length);
			uint dummy = 0;
			PrivateFontCollection iconFonts = new();
			iconFonts.AddMemoryFont(fontPtr, Resources.iosevka_fixed_regular_capital.Length);
			AddFontMemResourceEx(fontPtr, (uint)Resources.iosevka_fixed_regular_capital.Length, IntPtr.Zero, ref dummy);
			Marshal.FreeCoTaskMem(fontPtr);

			iconFont = new Font(iconFonts.Families[0], 84.0f);
		}

		/// <summary>
		/// Use low level win32 API to get keyboard layout info for current foreground window
		/// </summary>
		/// <returns></returns>
		public static CultureInfo? GetCurrentKeyboardLayout()
		{
			// https://yal.cc/csharp-get-current-keyboard-layout/
			try
			{
				IntPtr foregroundWindow = GetForegroundWindow();
				uint foregroundProcess = GetWindowThreadProcessId(foregroundWindow, IntPtr.Zero);
				int keyboardLayout = GetKeyboardLayout(foregroundProcess).ToInt32() & 0xFFFF;
				return new CultureInfo(keyboardLayout);
			}
			catch
			{
				return null;
			}
		}

		/// <summary>
		/// Launch windows setting APP
		/// </summary>
		/// <param name="url">
		///		URL of the setting page
		///		<see href="https://docs.microsoft.com/en-us/windows/uwp/launch-resume/launch-settings-app">
		///			MS docs
		///		</see>
		///	</param>
		private static void LaunchWindowSettingApp(string url)
		{
			ProcessStartInfo processStartInfo = new(url);
			processStartInfo.UseShellExecute = true;
			Process.Start(processStartInfo);
		}

		#region Events

		/// <summary>
		/// Global keyboard key up event
		/// </summary>
		/// <param name="key"></param>
		private static async void KeyboardHook_KeyUp(KeyboardHook.VKeys key)
		{
			// detect key up for any win button so hold win and tap space will be supported
			if (key == KeyboardHook.VKeys.LWIN || key == KeyboardHook.VKeys.RWIN)
			{
				// delay a small time so Windows has time to react
				await Task.Delay(10).ContinueWith(t => UpdateIcon());
			}
		}

		/// <summary>
		/// Event when click notify icon
		/// </summary>
		/// <param name="sender"></param>
		/// <param name="e"></param>
		private static void NotifyIcon_MouseClick(object? sender, MouseEventArgs e)
		{
			switch (e.Button)
			{
				case MouseButtons.Right:
					LaunchWindowSettingApp("ms-settings:keyboard");
					break;
				case MouseButtons.Middle:
					Environment.Exit(0);
					break;
			}
		}

		/// <summary>
		/// Clean up code when exiting the application
		/// </summary>
		/// <param name="sender"></param>
		/// <param name="e"></param>
		private static void CurrentDomain_ProcessExit(object? sender, EventArgs e)
		{
			notifyIcon.Dispose();
			keyboardHook.Uninstall();
		}

		#endregion

		#region Win Event

		// https://stackoverflow.com/a/10280800
		delegate void WinEventDelegate(IntPtr hWinEventHook, uint eventType, IntPtr hwnd, int idObject, int idChild, uint dwEventThread, uint dwmsEventTime);

		public static void WinEventProc(IntPtr hWinEventHook, uint eventType, IntPtr hwnd, int idObject, int idChild, uint dwEventThread, uint dwmsEventTime)
		{
			// https://stackoverflow.com/a/10280800
			UpdateIcon();
		}

		#endregion
	
		#region Win32 API

		private const uint WINEVENT_OUTOFCONTEXT = 0;
		private const uint EVENT_SYSTEM_FOREGROUND = 3;

		[DllImport("user32.dll")] static extern IntPtr GetForegroundWindow();
		[DllImport("user32.dll")] static extern uint GetWindowThreadProcessId(IntPtr hwnd, IntPtr proccess);
		[DllImport("user32.dll")] static extern IntPtr GetKeyboardLayout(uint thread);
		[DllImport("user32.dll")] static extern bool DestroyIcon(IntPtr handle);
		[DllImport("gdi32.dll")] static extern IntPtr AddFontMemResourceEx(IntPtr pbFont, uint cbFont, IntPtr pdv, [In] ref uint pcFonts);
		[DllImport("user32.dll")] static extern IntPtr SetWinEventHook(uint eventMin, uint eventMax, IntPtr hmodWinEventProc, WinEventDelegate lpfnWinEventProc, uint idProcess, uint idThread, uint dwFlags);

		#endregion
	}
}