using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;
using UnityEditor;
using UnityEditor.Animations;
using UnityEditor.UIElements;
using UnityEngine;
using UnityEngine.UIElements;
using VRC.Core;
using VRC.SDK3.Avatars.Components;
using VRC.SDK3.Avatars.ScriptableObjects;
using Object = UnityEngine.Object;

namespace ExpressionUtility
{
	internal static class Utility
	{
		private static APIUser _user;
		private static IEnumerable<ApiAvatar> _avatars;
		private static TaskCompletionSource<IEnumerable<ApiAvatar>> _avatarTSC;
		private static TaskCompletionSource<bool> _loginSuccessTSC;
		private static Dictionary<string, TaskCompletionSource<(string url, Texture2D image)>> _cachedTextureDownloads = new Dictionary<string, TaskCompletionSource<(string url, Texture2D image)>>();
		
		public static Task<(string url, Texture2D image)> DownloadImage(string imageUrl)
		{
			if (!_cachedTextureDownloads.TryGetValue(imageUrl, out var tcs))
			{
				tcs = new TaskCompletionSource<(string url, Texture2D image)>();
				_cachedTextureDownloads[imageUrl] = tcs;
				ImageDownloader.DownloadImage(imageUrl, 0, OnSuccess, OnFailure);
			}
			
			void OnSuccess(Texture2D texture2D)
			{
				var temp = new Texture2D(texture2D.width, texture2D.height, texture2D.format, texture2D.mipmapCount, false);
				Graphics.CopyTexture(texture2D, temp);
				var result = new Texture2D(texture2D.width, texture2D.height);
				
				result.SetPixels(temp.GetPixels());
				result.Apply(true);
				result.mipMapBias = -1;
				result.hideFlags = HideFlags.HideAndDontSave;
				tcs.TrySetResult((imageUrl, result));
			}	
			
			void OnFailure()
			{
				_cachedTextureDownloads.Remove(imageUrl);
				tcs.TrySetResult((imageUrl, null));
				$"Failed to download image: {imageUrl}".LogError();
			}

			return tcs.Task;
		}



		private static async Task<bool> Login()
		{
			if (!Settings.AllowConnectToVrcApi)
			{
				return false;
			}
			
			if (_loginSuccessTSC != null)
			{
				return await _loginSuccessTSC.Task;
			}
			
			_loginSuccessTSC = new TaskCompletionSource<bool>();

			bool loaded = ApiCredentials.IsLoaded();
			if (!loaded)
			{
				loaded = ApiCredentials.Load();
			}

			if (!APIUser.IsLoggedIn & loaded)
			{
				API.SetOnlineMode(true);

				void Success(ApiModelContainer<APIUser> c)
				{
					_loginSuccessTSC.TrySetResult(true);
				}

				void Error(ApiModelContainer<APIUser> c)
				{
					_loginSuccessTSC.TrySetResult(false);
					_loginSuccessTSC = null;
				}
				
				APIUser.InitialFetchCurrentUser(Success, Error);
			}
			else
			{
				_loginSuccessTSC.TrySetResult(APIUser.CurrentUser != null);
			}


			return await _loginSuccessTSC.Task;
		}

		public static void BorderColor(this VisualElement e, Color color)
		{
			e.style.borderBottomColor = color;
			e.style.borderLeftColor = color;
			e.style.borderRightColor = color;
			e.style.borderTopColor = color;
		}
		
		public static bool Contains(this string source, string toCheck, StringComparison comp)
		{
			return source?.IndexOf(toCheck, comp) >= 0;
		}

		public static bool DeleteDirectoryRecursive(this DirectoryInfo directoryInfo)
		{
			if (directoryInfo == null)
			{
				return false;
			}

			try
			{
				return DeleteDirectoryRecursive(directoryInfo.FullName);
			}
			catch (Exception e)
			{
				$"{e}".LogError();
				return false;
			}
		}
		
		private static bool DeleteDirectoryRecursive(this string targetDir)
		{
			File.SetAttributes(targetDir, FileAttributes.Normal);

			string[] files = Directory.GetFiles(targetDir);
			string[] dirs = Directory.GetDirectories(targetDir);

			foreach (string file in files)
			{
				File.SetAttributes(file, FileAttributes.Normal);
				File.Delete(file);
			}

			foreach (string dir in dirs)
			{
				DeleteDirectoryRecursive(dir);
			}

			Directory.Delete(targetDir, false);
			return true;
		}
		
		public static async Task<IEnumerable<ApiAvatar>> GetAvatars()
		{
			var emptyList = new List<ApiAvatar>();
			if (!Settings.AllowConnectToVrcApi)
			{
				return emptyList;
			}
			
			if (_avatarTSC != null)
			{
				return await _avatarTSC.Task;
			}
			
			bool loginSuccess = await Login();
			if (!loginSuccess)
			{
				return emptyList;
			}
			
			_avatarTSC = new TaskCompletionSource<IEnumerable<ApiAvatar>>();
			void OnSuccess(IEnumerable<ApiAvatar> avs)
			{
				_avatarTSC.TrySetResult(avs);
			}

			ApiAvatar.FetchList(OnSuccess, s => _avatarTSC.SetResult(emptyList), 
				ApiAvatar.Owner.Mine,
				ApiAvatar.ReleaseStatus.All,
				null,
				20,
				0,
				ApiAvatar.SortHeading.None,
				ApiAvatar.SortOrder.Descending,
				null,
				null, 
				true,
				false,
				null,
				false
			);
			
			return await _avatarTSC.Task;
		}

		public static VisualElement InstantiateTemplate(this VisualTreeAsset template, VisualElement target)
		{
			template.CloneTree(target);
			return target.Children().Last();
		}
		
		public static T InstantiateTemplate<T>(this VisualTreeAsset template, VisualElement target) where T : VisualElement => InstantiateTemplate(template, target) as T;

		public static void SelectAnimatorLayer(this AnimatorController animator, AnimatorControllerLayer layer)
		{
			try
			{
				
				var type = Type.GetType("UnityEditor.Graphs.AnimatorControllerTool,UnityEditor.Graphs");
				EditorApplication.ExecuteMenuItem("Window/Animation/Animator");
				Selection.activeObject = animator;
				EditorApplication.delayCall += DelayCall;
				
				void DelayCall()
				{
					var index = Array.FindIndex(animator.layers, l => l.name == layer.name);
					var window = EditorWindow.GetWindow(type);
					var prop = type?.GetProperties().FirstOrDefault(p => p.Name == "selectedLayerIndex");
					prop?.GetSetMethod()?.Invoke(window, new object[]{index});
				}
			}
			catch (Exception)
			{
				// ignored
			}
		}

	

		public static bool OwnsAnimator(this VRCAvatarDescriptor descriptor, RuntimeAnimatorController animator)
		{
			if (animator == null)
			{
				return false;
			}
				
			foreach (var layer in descriptor.baseAnimationLayers)
			{
				if (!layer.isDefault && layer.animatorController == animator)
				{
					return true;
				}
			}

			return false;
		}

		public static IEnumerable<AnimatorController> GetValidAnimators(this VRCAvatarDescriptor descriptor)
		{
			return descriptor.baseAnimationLayers
				.Where(b => !b.isDefault)
				.Select(b => b.animatorController)
				.Cast<AnimatorController>();
		}
		
		public static void Replace(this VisualElement root, VisualElement oldElement, VisualElement newElement)
		{
			var index = root.IndexOf(oldElement);
			root.Insert(index, newElement);
			root.Remove(oldElement);
		}

		/// <summary>
		/// Is true null for native-null Unity Objects
		/// </summary>
		public static T NotNull<T>(this T obj) where T : Object => obj != null ? obj : null;

		public static void Display(this VisualElement element, bool shouldDisplay)
		{
			if (element == null)
			{
				return;
			}
			element.style.display = shouldDisplay ? DisplayStyle.Flex : DisplayStyle.None;
		}

		public static void Log(this string msg, [CallerFilePath] string filePath = null)
		{
			var source = Path.GetFileNameWithoutExtension(filePath);
			Debug.Log($"[{source}] {msg}");
		}

		public static void LogError(this string msg, [CallerFilePath] string filePath = null)
		{
			var source = Path.GetFileNameWithoutExtension(filePath);
			Debug.LogError($"[{source}] {msg}");
		}
		
		public static void SetDirty(this IEnumerable<Object> objs)
		{
			foreach (var o in objs)
			{
				if (o == null)
				{
					continue;
				}
				EditorUtility.SetDirty(o);
			}
		}

		public static void RemoveLayer(this AnimatorController controller, AnimatorControllerLayer layer)
		{
			if (controller == null)
			{
				return;
			}
			
			var layers = controller.layers.ToList();
			var index = layers.FindIndex(l => l.name == layer.name);
			if(index >= 0)
			{
				controller.RemoveLayer(index);
			}
		}

		public static IEnumerable<AnimatorStateMachine> GetAnimatorStateMachinesRecursively(this AnimatorStateMachine stateMachine)
		{
			yield return stateMachine;
			var subs = stateMachine.stateMachines.SelectMany(sm => GetAnimatorStateMachinesRecursively(sm.stateMachine));
			foreach (AnimatorStateMachine sub in subs)
			{
				yield return sub;
			}
		}

		public static void RemoveObjectSelector(this ObjectField field) => field.AddToClassList("subObject-field--no-selector");
		public static void RemoveIcon(this ObjectField field) => field.AddToClassList("subObject-field--no-icon");

		public static IEnumerable<VRCExpressionsMenu> GetMenusRecursively(this VRCExpressionsMenu menu)
		{
			yield return menu;
			foreach (VRCExpressionsMenu vrcExpressionsMenu in menu.controls
				.Where(mControl => mControl.type == VRCExpressionsMenu.Control.ControlType.SubMenu && mControl.subMenu != null)
				.SelectMany(mControl => GetMenusRecursively(mControl.subMenu)))
			{
				yield return vrcExpressionsMenu;
			}
		}

		public static void AddSubObject(this Object asset, Object subObject)
		{
			var path = AssetDatabase.GetAssetPath(asset);
			if (path == "")
			{
				return;
			}
			
			// subObject.hideFlags = HideFlags.HideInHierarchy;
			EditorUtility.SetDirty(subObject);
			AssetDatabase.AddObjectToAsset(subObject, path);
		}
		
		public static void AddObjectsToAsset(this Object asset, params Object[] objs)
		{
			var path = AssetDatabase.GetAssetPath(asset);
			if (path == "")
			{
				return;
			}
			
			foreach (var o in objs)
			{
				if (o == null)
				{
					continue;
				}

				o.hideFlags = HideFlags.HideInHierarchy;
				EditorUtility.SetDirty(o);
				AssetDatabase.AddObjectToAsset(o, path);
			}
			
			
			AssetDatabase.SaveAssets();
		}
	}
}