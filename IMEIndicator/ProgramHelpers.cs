using Microsoft.Win32;
using System;
using System.Diagnostics;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Globalization;

namespace IMEIndicator
{
	internal static class ProgramHelpers
	{

		/// <summary>
		/// Use low level win32 API to get keyboard layout info for current foreground window
		/// </summary>
		/// <returns></returns>
		public static CultureInfo? GetCurrentKeyboardLayout()
		{
			// https://yal.cc/csharp-get-current-keyboard-layout/
			try
			{
				IntPtr foregroundWindow = WinAPI.GetForegroundWindow();
				uint foregroundProcess = WinAPI.GetWindowThreadProcessId(foregroundWindow, IntPtr.Zero);
				int keyboardLayout = WinAPI.GetKeyboardLayout(foregroundProcess).ToInt32() & 0xFFFF;
				return new CultureInfo(keyboardLayout);
			}
			catch
			{
				return null;
			}
		}

		/// <summary>
		/// Check if Windows theme is dark mode
		/// </summary>
		/// <returns>if Windows is dark mode</returns>
		public static bool IsDarkMode()
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
		/// Launch windows setting APP
		/// </summary>
		/// <param name="url">
		///		URL of the setting page
		///		<see href="https://docs.microsoft.com/en-us/windows/uwp/launch-resume/launch-settings-app">
		///			MS docs
		///		</see>
		///	</param>
		public static void LaunchWindowSettingApp(string url)
		{
			ProcessStartInfo processStartInfo = new(url);
			processStartInfo.UseShellExecute = true;
			Process.Start(processStartInfo);
		}

		/// <summary>
		/// Make icon with two letter language code
		/// </summary>
		/// <param name="text">Two letter code</param>
		/// <returns>Generated icon</returns>
		public static Icon MakeIcon(string text, Font? iconFont, StringFormat iconStringFormat)
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
	}
}