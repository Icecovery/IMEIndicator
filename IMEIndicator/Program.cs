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
using Microsoft.Win32;

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
		private static bool darkModeIcon = true;
		private static Font? iconFont;
		private static WinEventDelegate? winEventDelegate;
		private const string notifyIconText =
			"{0} Keyboard\n" +
			"\n" +
			"Right click to show all options\n" +
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

			// setup drop down menu
			ToolStripMenuItem menuItemSetting = new("Settings");
			menuItemSetting.Click += MenuItemSetting_Click;

			ToolStripMenuItem menuItemStartupFolder = new("Open Startup Folder");
			menuItemStartupFolder.Click += MenuItemStartupFolder_Click;
			
			ToolStripMenuItem menuItemExit = new("Exit");
			menuItemExit.Click += MenuItemExit_Click;
			
			ContextMenuStrip contextMenuStrip = new();
			contextMenuStrip.Items.Add(menuItemSetting);
			contextMenuStrip.Items.Add(menuItemStartupFolder);
			contextMenuStrip.Items.Add(new ToolStripSeparator());
			contextMenuStrip.Items.Add(menuItemExit);

			notifyIcon.ContextMenuStrip = contextMenuStrip;
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
			bool isDarkMode = IsDarkMode();

			if (isDarkMode != darkModeIcon)
			{
				iconMap.Clear();
				darkModeIcon = isDarkMode;
			}

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

			g.DrawString(text, iconFont!, IsDarkMode() ? Brushes.White : Brushes.Black, 64, 72, iconStringFormat);
			g.Flush();

			Icon newIcon = Icon.FromHandle(bitmap.GetHicon());
			return newIcon;
		}

		/// <summary>
		/// Check if Windows theme is dark mode
		/// </summary>
		/// <returns>if Windows is dark mode</returns>
		private static bool IsDarkMode()
		{
			try
			{
				if (Registry.GetValue(@"HKEY_CURRENT_USER\Software\Microsoft\Windows\CurrentVersion\Themes\Personalize", "SystemUsesLightTheme", "1") is object v)
				{
					return v.ToString() == "0";
				}
				else return true;
			}
			catch
			{
				return true;
			}
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

				// get conversion test

				IntPtr contextHIMC = ImmGetDefaultIMEWnd(foregroundWindow);

				Console.WriteLine($"context {contextHIMC}");

				uint lpdfwConversion = 0;
				uint lpfdwSentence = 0;
				bool output = ImmGetConversionStatus(contextHIMC, ref lpdfwConversion, ref lpfdwSentence);

				Console.WriteLine($"output {output}, conversion {lpdfwConversion}, sentence {lpfdwSentence}");

				ImmReleaseContext(foregroundWindow, contextHIMC);

				// end of get conversion test

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
				case MouseButtons.Middle:
					Environment.Exit(0);
					break;
			}
		}

		/// <summary>
		/// Event when drop down setting button clicked
		/// </summary>
		/// <param name="sender"></param>
		/// <param name="e"></param>
		private static void MenuItemSetting_Click(object? sender, EventArgs e)
		{
			LaunchWindowSettingApp("ms-settings:keyboard");
		}

		/// <summary>
		/// Event when drop down open startup folder button clicked
		/// </summary>
		/// <param name="sender"></param>
		/// <param name="e"></param>
		private static void MenuItemStartupFolder_Click(object? sender, EventArgs e)
		{
			Process.Start("explorer.exe", "shell:startup");
		}

		/// <summary>
		/// Event when drop down quit button clicked
		/// </summary>
		/// <param name="sender"></param>
		/// <param name="e"></param>
		private static void MenuItemExit_Click(object? sender, EventArgs e)
		{
			Environment.Exit(0);
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
		[DllImport("Imm32.dll")] static extern bool ImmGetConversionStatus(IntPtr HIMC, ref uint lpfdwConversion, ref uint lpfdwSentence);
		[DllImport("Imm32.dll")] static extern IntPtr ImmGetContext(IntPtr hwnd);
		[DllImport("Imm32.dll")] static extern bool ImmReleaseContext(IntPtr hwnd, IntPtr HIMC);
		[DllImport("imm32.dll")] static extern bool ImmGetOpenStatus(IntPtr himc);
		[DllImport("imm32.dll")] static extern bool ImmSetOpenStatus(IntPtr himc, bool b);
		[DllImport("imm32.dll")] static extern IntPtr ImmGetDefaultIMEWnd(IntPtr hwnd);
		#endregion
	}
}