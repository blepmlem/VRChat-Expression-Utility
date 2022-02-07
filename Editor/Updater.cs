using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Unity.EditorCoroutines.Editor;
using UnityEditor.PackageManager;
using UnityEngine.Networking;
using PackageInfo = UnityEditor.PackageManager.PackageInfo;
using Version = System.Version;

namespace ExpressionUtility
{
	internal class Updater
	{
		private const string PACKAGE_NAME = "com.blep.vrc-expression-utility";
		
		private const string TAGS_URL = "https://api.github.com/repos/blepmlem/VRChat-Expression-Utility/tags";

		private const string GIT_URL = "https://github.com/blepmlem/VRChat-Expression-Utility.git";

		private TaskCompletionSource<Version> _latestVersionTcs;
		
		public Version LatestOnlineVersion { get; private set; }

		public Version CurrentVersion { get; private set; }
		
		public PackageInfo PackageInfo { get; private set; }

		private Updater()
		{
			
		}

		public static async Task<Updater> GetUpdater()
		{
			var instance = new Updater();
			await instance.CheckForUpdates();
			return instance;
		}

		public bool HasNewerVersion
		{
			get
			{
				if (CurrentVersion == null || LatestOnlineVersion == null)
				{
					return false;
				}

				return LatestOnlineVersion > CurrentVersion;
			}
		}

		public async Task InstallUpdate(Action OnComplete = null)
		{
			var gitString = $"{GIT_URL}#{LatestOnlineVersion}";
			await SetPackage(gitString);
			OnComplete?.Invoke();
		}

		private async Task CheckForUpdates()
		{
			var latestOnlineVersionTask = GetLatestOnlineVersion();
			var packageTask = GetPackage();
			await Task.WhenAll(packageTask, latestOnlineVersionTask);
			LatestOnlineVersion = latestOnlineVersionTask.Result;
			PackageInfo = packageTask.Result;
			CurrentVersion = new Version(PackageInfo.version);
		}

		private Task<PackageInfo> GetPackage()
		{
			var tcs = new TaskCompletionSource<PackageInfo>();
			var req = Client.List();
			IEnumerator Waiter()
			{
				while (!req.IsCompleted)
				{
					yield return null;
				}

				if (req.Error != null)
				{
					req.Error.message.LogError();
				}
				tcs.SetResult(req.Result?.FirstOrDefault(p => p.name == PACKAGE_NAME));
			}

			EditorCoroutineUtility.StartCoroutineOwnerless(Waiter());
			return tcs.Task;
		}

		private Task<bool> SetPackage(string packageId)
		{
			var tcs = new TaskCompletionSource<bool>();
			var req = Client.Add(packageId);
			IEnumerator Waiter()
			{
				while (!req.IsCompleted)
				{
					yield return null;
				}

				if (req.Error != null)
				{
					req.Error.message.LogError();
				}
				tcs.SetResult(req.Status == StatusCode.Success);
			}

			EditorCoroutineUtility.StartCoroutineOwnerless(Waiter());
			return tcs.Task;
		}

		private Task<Version> GetLatestOnlineVersion()
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

									var result = o.First();
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