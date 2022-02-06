using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using ICSharpCodeRenamedToAvoidConflictWithOldSDKs.SharpZipLib.Zip;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using UnityEditor;
using UnityEditor.PackageManager.UI;
using UnityEngine;
using UnityEngine.Networking;
using Version = System.Version;

namespace ExpressionUtility
{
	internal class Updater
	{
		private const string TAGS_URL = "https://api.github.com/repos/blepmlem/VRChat-Expression-Utility/tags";

		private const string GIT_URL = "https://github.com/blepmlem/VRChat-Expression-Utility.git";
		
		private const string PACKAGE_PATH = "Packages/com.uwu.vrc-expression-utility/package.json";

		private TaskCompletionSource<Version> _latestVersionTcs;

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

		public void OpenGitHub() => Application.OpenURL(GIT_URL);

		[MenuItem("Expression Utility/Check for updates")]
		private static void CheckForUpdates()
		{
			Create();
		}
		
		public static async Task<Updater> Create()
		{
			var updater = new Updater();
			var newest = await updater.GetLatestVersion();

			updater.LatestVersion = newest;
		
			dynamic obj = JObject.Parse(File.ReadAllText(PACKAGE_PATH));
			var v = obj["version"];
			updater.CurrentVersion = new Version(v.ToString());

			return updater;
		}
	
		// UnityEditor.PackageManager.Client.
		
		public async Task Update(Action OnComplete = null)
		{
			if (!HasNewerVersion)
			{
				return;
			}

			var gitString = $"{GIT_URL}#{LatestVersion}";
			//
			//
			//
			// IsUpdating = true;
			var tcs = new TaskCompletionSource<bool>();
			// var http = UnityWebRequest.Get(_latestVersionPath);
			// var req = http.SendWebRequest();
			// req.completed += operation =>
			// {
			// 	if (http.isHttpError || http.isNetworkError)
			// 	{
			// 		tcs.TrySetResult(false);
			// 	}
			// 	else
			// 	{
			// 		try
			// 		{
			// 			var data = req.webRequest.downloadHandler.data;
			//
			// 			if (data != null)
			// 			{
			// 				var stream = new MemoryStream(data);
			// 				var file = new FastZip();
			// 				file.ExtractZip(stream, "Packages", FastZip.Overwrite.Always, null, null, null, true, true);
			// 				AssetDatabase.Refresh();
			// 				tcs.TrySetResult(true);
			// 			}
			// 		}
			// 		catch (Exception)
			// 		{
			// 			tcs.TrySetResult(false);
			// 		}
			// 	}
			// 	http.Dispose();
			// };

			await tcs.Task;
			OnComplete?.Invoke();
		}

		private Task<Version> GetLatestVersion()
		{
			if (_latestVersionTcs != null)
			{
				return _latestVersionTcs.Task;
			}

			var versions = new List<Version>();
			_latestVersionTcs = new TaskCompletionSource<Version>();
			var http = UnityWebRequest.Get(TAGS_URL);
			var req = http.SendWebRequest();
			req.completed += operation =>
			{
				if (http.isHttpError || http.isNetworkError)
				{
					_latestVersionTcs.SetResult(null);
				}
				else
				{
					try
					{
						var txt = req.webRequest.downloadHandler.text;
						dynamic obj = JsonConvert.DeserializeObject(txt);
						if (obj is JArray assets)
						{
							foreach (JToken jToken in assets)
							{
								try
								{
									var o = jToken.FirstOrDefault(j => (j as JProperty)?.Name == "name");
									if (o == null)
									{
										continue;
									}

									var result = o.FirstOrDefault();
									var versionString = result.Value<string>();
									var version = new Version(versionString);
									versions.Add(version);
								}
								catch (Exception)
								{
									// ignored
								}
							}
						}
					}
					catch (Exception)
					{
						// ignored
					}
					
					versions.Sort();
					versions.Reverse();
					var newest = versions.FirstOrDefault();
					_latestVersionTcs.TrySetResult(newest);
				}
				http.Dispose();
			};

			return _latestVersionTcs.Task;
		}
	}
}