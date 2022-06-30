using System;
using System.Diagnostics;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace IMEIndicator
{
	public partial class Program
	{
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
		/// Clean up code when exiting the application
		/// </summary>
		/// <param name="sender"></param>
		/// <param name="e"></param>
		private static void CurrentDomain_ProcessExit(object? sender, EventArgs e)
		{
			notifyIcon.Dispose();
			keyboardHook.Uninstall();
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
			ProgramHelpers.LaunchWindowSettingApp("ms-settings:keyboard");
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
		/// Event when update status text is clicked (when new update is available)
		/// </summary>
		/// <param name="sender"></param>
		/// <param name="e"></param>
		private static void MenuItemUpdateStatus_Click(object? sender, EventArgs e)
		{
			Process.Start("explorer", UpdateChecker.GitHubReleaseAddress);
		}

		/// <summary>
		/// Event when check update button is clicked
		/// </summary>
		/// <param name="sender"></param>
		/// <param name="e"></param>
		private static void MenuItemCheckUpdate_Click(object? sender, EventArgs e)
		{
			CheckUpdate();
		}

		/// <summary>
		/// Event when foreground window changes
		/// </summary>
		/// <param name="hWinEventHook"></param>
		/// <param name="eventType"></param>
		/// <param name="hwnd"></param>
		/// <param name="idObject"></param>
		/// <param name="idChild"></param>
		/// <param name="dwEventThread"></param>
		/// <param name="dwmsEventTime"></param>
		public static void WinEventProc(IntPtr hWinEventHook, uint eventType, IntPtr hwnd, int idObject, int idChild, uint dwEventThread, uint dwmsEventTime)
		{
			// https://stackoverflow.com/a/10280800
			UpdateIcon();
		}
	}
}
