using Octokit;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Reflection;

namespace IMEIndicator
{
	internal static class UpdateChecker
	{
		private static readonly string gitHubUserName = "Icecovery";
		private static readonly string gitHubRepoName = "IMEIndicator";

		public static string GitHubReleaseAddress => $"https://github.com/{gitHubUserName}/{gitHubRepoName}/releases";

		/// <summary>
		/// Check if need update
		/// </summary>
		/// <returns>true if need update, false if not, null if failed</returns>
		public static bool? Check()
		{
			try
			{
				FileVersionInfo fvi = FileVersionInfo.GetVersionInfo(Assembly.GetExecutingAssembly().Location);
				if (fvi.FileVersion == null)
				{
					return null;
				}
				Version currentVersion = new(fvi.FileVersion);

				GitHubClient client = new(new ProductHeaderValue("IMEIndicator", currentVersion.ToString()));
				IReadOnlyList<Release> releases = client.Repository.Release.GetAll(gitHubUserName, gitHubRepoName).Result;
				if (releases is null || releases.Count == 0)
				{
					return null;
				}
				
				Version onlineVersion = new(releases[0].TagName.Replace("v", string.Empty));
				static int NegativeCheck(int input) => input == -1 ? 0 : input;
				onlineVersion = new(NegativeCheck(onlineVersion.Major),
									NegativeCheck(onlineVersion.Minor),
									NegativeCheck(onlineVersion.Build),
									NegativeCheck(onlineVersion.Revision));

				int result = currentVersion.CompareTo(onlineVersion);
				return result < 0;	
			}
			catch
			{
				return null;
			}
		}
	}
}
