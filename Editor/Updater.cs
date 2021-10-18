using System;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using ICSharpCodeRenamedToAvoidConflictWithOldSDKs.SharpZipLib.Zip;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using UnityEditor;
using UnityEngine;
using UnityEngine.Networking;
using Version = System.Version;

namespace ExpresionUtility
{
	internal class Updater
	{
		private const string URL = "https://api.github.com/repos/blepmlem/VRChat-Expression-Utility/releases/latest";

		private const string GITHUB = "https://github.com/blepmlem/VRChat-Expression-Utility";

		private const string GITHUB_RELEASES = GITHUB + "/releases";
	
		private const string PACKAGE_PATH = "Packages/com.uwu.vrc-expression-utility/package.json";
	
		private string _latestVersionPath;

		public Version LatestVersion { get; private set; }

		public Version CurrentVersion { get; private set; }

		public bool IsUpdating { get; private set; }

		public bool HasNewerVersion
		{
			get
			{
				if (CurrentVersion == null || LatestVersion == null)
				{
					return false;
				}

				return LatestVersion > CurrentVersion;
			}
		}

		public void OpenGitHub()
		{
			Application.OpenURL(GITHUB_RELEASES);
		}

		public static async Task<Updater> Create()
		{
			var updater = new Updater();
			updater._latestVersionPath = await updater.GetLatestVersionPath();

			if (string.IsNullOrEmpty(updater._latestVersionPath))
			{
				return null;
			}
		
			var version = Regex.Match(updater._latestVersionPath,"(?<=download/)(.*)(?=/)");
			updater.LatestVersion = new Version(version.Value);
		
			dynamic obj = JObject.Parse(File.ReadAllText(PACKAGE_PATH));
			var v = obj["version"];
			updater.CurrentVersion = new Version(v.ToString());

			return updater;
		}
	
		public async Task Update(Action OnComplete = null)
		{
			if (!HasNewerVersion || string.IsNullOrEmpty(_latestVersionPath))
			{
				return;
			}

			IsUpdating = true;
			var tcs = new TaskCompletionSource<bool>();
			var http = UnityWebRequest.Get(_latestVersionPath);
			var req = http.SendWebRequest();
			req.completed += operation =>
			{
				if (http.isHttpError || http.isNetworkError)
				{
					tcs.TrySetResult(false);
				}
				else
				{
					try
					{
						var data = req.webRequest.downloadHandler.data;

						if (data != null)
						{
							var stream = new MemoryStream(data);
							var file = new FastZip();
							file.ExtractZip(stream, "Packages", FastZip.Overwrite.Always, null, null, null, true, true);
							AssetDatabase.Refresh();
							tcs.TrySetResult(true);
						}
					}
					catch (Exception)
					{
						tcs.TrySetResult(false);
					}
				}
				http.Dispose();
			};

			await tcs.Task;
			OnComplete?.Invoke();
		}

		private async Task<string> GetLatestVersionPath()
		{
			var tcs = new TaskCompletionSource<string>();
        
			var http = UnityWebRequest.Get(URL);
			var req = http.SendWebRequest();
			req.completed += operation =>
			{
				if (http.isHttpError || http.isNetworkError)
				{
					tcs.SetResult(null);
				}
				else
				{
					string output = null;
					try
					{
						var txt = req.webRequest.downloadHandler.text;
						dynamic obj = JsonConvert.DeserializeObject(txt);
						if (obj?.assets is JArray assets)
						{
							foreach (JToken jToken in assets)
							{
								var o = jToken.FirstOrDefault(j => (j as JProperty)?.Name == "browser_download_url");
								if (o == null)
								{
									continue;
								}
							
								var result = o.Children().FirstOrDefault();
								output = result?.Value<string>();
							}
						}
					}
					catch (Exception)
					{
						// ignored
					}

					tcs.TrySetResult(output);
				}
				http.Dispose();
			};

			return await tcs.Task;
		}
	}
}