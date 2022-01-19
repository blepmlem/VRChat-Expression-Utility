using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;
using UnityEditor;
using UnityEngine;
using UnityEngine.UIElements;
using VRC.Core;
using VRC.SDK3.Avatars.Components;

namespace ExpressionUtility
{
	internal static class Utility
	{
		private static APIUser _user;
		private static IEnumerable<ApiAvatar> _avatars;
		private static TaskCompletionSource<IEnumerable<ApiAvatar>> _avatarTSC;
		private static TaskCompletionSource<APIUser> _loginTSC;
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



		private static async Task<APIUser> Login()
		{
			if (_loginTSC != null)
			{
				return await _loginTSC.Task;
			}
			
			_loginTSC = new TaskCompletionSource<APIUser>();
	
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
					_loginTSC.TrySetResult(c.Model as APIUser);
				}

				void Error(ApiModelContainer<APIUser> c)
				{
					_loginTSC.TrySetResult(c.Model as APIUser);
					_loginTSC = null;
				}
				
				APIUser.InitialFetchCurrentUser(Success, Error);
			}
			else
			{
				_loginTSC.TrySetResult(APIUser.CurrentUser);
			}


			return await _loginTSC.Task;
		}

		public static void BorderColor(this VisualElement e, Color color)
		{
			e.style.borderBottomColor = color;
			e.style.borderLeftColor = color;
			e.style.borderRightColor = color;
			e.style.borderTopColor = color;
		}
		
		public static void BorderWidth(this VisualElement e, float width)
		{
			e.style.borderBottomWidth = width;
			e.style.borderLeftWidth = width;
			e.style.borderRightWidth = width;
			e.style.borderTopWidth = width;
		}
		
		public static void BorderRadius(this VisualElement e, float radius)
		{
			e.style.borderBottomLeftRadius = radius;
			e.style.borderBottomRightRadius = radius;
			e.style.borderTopLeftRadius = radius;
			e.style.borderTopRightRadius = radius;
		}
		
		public static async Task<IEnumerable<ApiAvatar>> GetAvatars()
		{
			if (_avatarTSC != null)
			{
				return await _avatarTSC.Task;
			}

			await Login();
			_avatarTSC = new TaskCompletionSource<IEnumerable<ApiAvatar>>();
			void OnSuccess(IEnumerable<ApiAvatar> avs)
			{
				_avatarTSC.TrySetResult(avs);
			}

			ApiAvatar.FetchList(OnSuccess, s => _avatarTSC.SetResult(new List<ApiAvatar>()), 
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

		public static bool OwnsAnimator(this VRCAvatarDescriptor descriptor, RuntimeAnimatorController animator)
		{
			foreach (var layer in descriptor.baseAnimationLayers)
			{
				if (!layer.isDefault && layer.animatorController == animator)
				{
					return true;
				}
			}

			return false;
		}

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