using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Text;
using System.Globalization;
using System.Linq;
using System.Runtime.InteropServices;
using System.Windows.Forms;

namespace IMEIndicator
{
	public partial class Program
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
		private static WinAPI.WinEventDelegate? winEventDelegate;
		private const string notifyIconText =
			"{0} Keyboard\n" +
			"\n" +
			"Right click to show all options\n" +
			"Middle click to exit";

		private static bool? needUpdate = null;

		/// <summary>
		/// Main entry point of the program
		/// </summary>
		public static void Main()
		{
			Setup();
			Application.Run();
		}

		/// <summary>
		/// Setup process
		/// </summary>
		private static void Setup()
		{
			SetupProcessExitHook();
			SetupNotifyIcon();
			SetupKeyboardHook();
			SetupWindowEventHook();
			LoadFont();
			UpdateIcon();
			CheckUpdate();
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

			ToolStripMenuItem menuItemUpdateStatus = new("Update Status")
			{
				Name = "UpdateStatus",
				Enabled = false
			};
			menuItemUpdateStatus.Click += MenuItemUpdateStatus_Click;
			ToolStripMenuItem menuItemCheckUpdate = new("Check for updates...");
			menuItemCheckUpdate.Click += MenuItemCheckUpdate_Click;

			ToolStripMenuItem menuItemExit = new("Exit");
			menuItemExit.Click += MenuItemExit_Click;

			ContextMenuStrip contextMenuStrip = new();
			contextMenuStrip.Items.Add(menuItemSetting);
			contextMenuStrip.Items.Add(menuItemStartupFolder);
			contextMenuStrip.Items.Add(new ToolStripSeparator());
			contextMenuStrip.Items.Add(menuItemUpdateStatus);
			contextMenuStrip.Items.Add(menuItemCheckUpdate);
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
			winEventDelegate = new WinAPI.WinEventDelegate(WinEventProc);
			IntPtr m_hhook = WinAPI.SetWinEventHook(WinAPI.EVENT_SYSTEM_FOREGROUND, WinAPI.EVENT_SYSTEM_FOREGROUND, IntPtr.Zero, winEventDelegate, 0, 0, WinAPI.WINEVENT_OUTOFCONTEXT);
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
			WinAPI.AddFontMemResourceEx(fontPtr, (uint)Resources.iosevka_fixed_regular_capital.Length, IntPtr.Zero, ref dummy);
			Marshal.FreeCoTaskMem(fontPtr);

			iconFont = new Font(iconFonts.Families[0], 84.0f);
		}

		/// <summary>
		/// Update notify icon
		/// </summary>
		private static void UpdateIcon()
		{
			bool isDarkMode = ProgramHelpers.IsDarkMode();

			if (isDarkMode != darkModeIcon)
			{
				iconMap.Clear();
				darkModeIcon = isDarkMode;
			}

			CultureInfo? cultureInfo = ProgramHelpers.GetCurrentKeyboardLayout();

			string text = (cultureInfo?.TwoLetterISOLanguageName.ToUpper()) ?? "XX";

			if (!iconMap.ContainsKey(text))
			{
				Icon newIcon = ProgramHelpers.MakeIcon(text, iconFont, iconStringFormat);
				iconMap.Add(text, newIcon);
			}

			notifyIcon.Icon = iconMap[text];
			notifyIcon.Text = string.Format(notifyIconText, (cultureInfo?.DisplayName) ?? "ERROR");
		}

		/// <summary>
		/// Check if the program needs to be updated
		/// </summary>
		private static void CheckUpdate()
		{
			needUpdate = UpdateChecker.Check();

			ToolStripItem status = notifyIcon.ContextMenuStrip.Items.Find("UpdateStatus", true).First();

			if (needUpdate == null)
			{
				status.Text = "Failed to Fetch Updates";
				status.Enabled = false;
			}
			else if (needUpdate.Value)
			{
				status.Text = "New Update Available! (Click to view)";
				status.Enabled = true;
			}
			else
			{
				status.Text = "No Update Available";
				status.Enabled = false;
			}
		}
	}
}