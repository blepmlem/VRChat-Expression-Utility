using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using ICSharpCodeRenamedToAvoidConflictWithOldSDKs.SharpZipLib.Zip;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Unity.EditorCoroutines.Editor;
using UnityEditor;
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
		
		public GitPackage? LatestOnlineVersion { get; private set; }

		public PackageSource? LocalPackageSource => LocalPackage?.source;
		
		public PackageInfo LocalPackage { get; private set; }


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
				return true;
				if (LocalPackage == null || LatestOnlineVersion == null || !Version.TryParse(LocalPackage.version, out var packageVersion))
				{
					return false;
				}
				
				return LatestOnlineVersion?.Version > packageVersion;
			}
		}

		public Task InstallUpdate(Action OnComplete = null)
		{
			if (LocalPackageSource == PackageSource.Git)
			{
				return InstallUpmUpdate(OnComplete);
			}

			if(LocalPackageSource == PackageSource.Embedded)
			{
				return InstallEmbeddedUpdate(OnComplete);
			}

			return Task.CompletedTask;
		}

		private async Task InstallEmbeddedUpdate(Action OnComplete = null)
		{
			var tcs = new TaskCompletionSource<bool>();
			var http = UnityWebRequest.Get(LatestOnlineVersion?.ZipURL ?? string.Empty);
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
							const string PACKAGES = "Packages";
							
							var stream = new MemoryStream(data);
							var file = new FastZip();
							var packagesBefore = Directory.EnumerateDirectories(PACKAGES).ToList();
							
							
							file.ExtractZip(stream, PACKAGES, FastZip.Overwrite.Always, null, null, null, true, true);

							var newFolder = Directory.EnumerateDirectories(PACKAGES).Except(packagesBefore).FirstOrDefault();

							if (newFolder == null)
							{
								tcs.TrySetResult(false);
								return;
							}

							var oldPath = LocalPackage.resolvedPath;
							var oldDir = new DirectoryInfo(oldPath);
							oldDir.Delete(true);
							
							var newDir = new DirectoryInfo(newFolder);
							newDir.MoveTo(oldPath);
							
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
		
		private async Task InstallUpmUpdate(Action OnComplete = null)
		{
			var gitString = $"{GIT_URL}#{LatestOnlineVersion}";
			await SetUpmPackage(gitString);
			OnComplete?.Invoke();
		}

		private async Task CheckForUpdates()
		{
			var latestOnlineVersionTask = GetLatestOnlineVersion();
			var packageTask = GetPackage();
			await Task.WhenAll(packageTask, latestOnlineVersionTask);
			LatestOnlineVersion = latestOnlineVersionTask.Result.FirstOrDefault();
			LocalPackage = packageTask.Result;
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

		private Task<bool> SetUpmPackage(string packageId)
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

		private Task<List<GitPackage>> GetLatestOnlineVersion()
		{
			var versions = new List<GitPackage>();
			var tcs = new TaskCompletionSource<List<GitPackage>>();
			var http = UnityWebRequest.Get(TAGS_URL);
			var req = http.SendWebRequest();
			req.completed += operation =>
			{
				if (http.isHttpError || http.isNetworkError)
				{
					tcs.SetResult(null);
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
								if (GitPackage.TryCreate(jToken, out var package))
								{
									versions.Add(package);
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
					tcs.TrySetResult(versions);
				}
				http.Dispose();
			};

			return tcs.Task;
		}
	}
}