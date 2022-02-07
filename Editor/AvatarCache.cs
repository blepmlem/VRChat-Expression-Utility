using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using VRC.Core;
using VRC.SDK3.Avatars.Components;
using Object = UnityEngine.Object;
#pragma warning disable 4014

namespace ExpressionUtility.UI
{
	internal class AvatarCache : IDisposable
	{
		private static readonly List<AvatarInfo> _cachedAvatarInfo = new  List<AvatarInfo>();

		public event Action<AvatarInfo> AvatarWasUpdated;
		
		public AvatarCache()
		{
			foreach (AvatarInfo info in _cachedAvatarInfo.Where(a => !a.IsValid).ToList())
			{
				_cachedAvatarInfo.Remove(info);
			}
			SetupAvatars(GetAvatarDescriptors());
			EditorApplication.hierarchyChanged += OnHierarchyChanged;
		}

		private List<VRCAvatarDescriptor> GetAvatarDescriptors() => StageUtility.GetCurrentStageHandle().FindComponentsOfType<VRCAvatarDescriptor>().Where(d => d.gameObject.activeInHierarchy).ToList();

		private void OnHierarchyChanged()
		{
			var descriptors = GetAvatarDescriptors();

			if (_cachedAvatarInfo.Count == descriptors.Count)
			{
				return;
			}
			
			foreach (AvatarInfo info in _cachedAvatarInfo.Where(a => !a.IsValid).ToList())
			{
				_cachedAvatarInfo.Remove(info);
				AvatarWasUpdated?.Invoke(info);
			}
			SetupAvatars(descriptors);
		}

		public class AvatarInfo
		{
			public Texture2D Thumbnail { get; set; }
			public VRCAvatarDescriptor VrcAvatarDescriptor { get; set; }
			public ApiAvatar ApiAvatar { get; set; }

			public string Name => ApiAvatar?.name ?? VrcAvatarDescriptor.name;
			public string Description => ApiAvatar?.description ?? VrcAvatarDescriptor.gameObject.name;

			public bool IsValid => VrcAvatarDescriptor != null;
		}

		public int AvatarCount => _cachedAvatarInfo.Count;
		
		public List<AvatarInfo> GetAllAvatarInfo() => _cachedAvatarInfo;

		public AvatarInfo GetAvatarInfo(VRCAvatarDescriptor vrcAvatarDescriptor) => _cachedAvatarInfo.FirstOrDefault(a => a.VrcAvatarDescriptor == vrcAvatarDescriptor);

		private async Task SetupAvatars(List<VRCAvatarDescriptor> descriptors)
		{
			var avatarsTask = Utility.GetAvatars();

			foreach (VRCAvatarDescriptor descriptor in descriptors)
			{
				var info = GetAvatarInfo(descriptor);
				if (info != null)
				{
					continue;
				}
				
				var go = new GameObject
				{
					hideFlags = HideFlags.HideAndDontSave,
				};
				var cam = go.AddComponent<Camera>();
				cam.fieldOfView = 50f;
				cam.depth = -100;
				descriptor.PositionPortraitCamera(go.transform);
				var tex = new RenderTexture(256, 256, 24);
				cam.targetTexture = tex;
				var oldActive = RenderTexture.active;
				RenderTexture.active = tex;
				cam.Render();
				var tex2d = new Texture2D(256, 256);
				tex2d.ReadPixels(new Rect(0,0,256, 256), 0,0);
				tex2d.Apply();
				RenderTexture.active = oldActive;
				cam.targetTexture = null;
				tex.Release();
				Object.DestroyImmediate(go);

				info = new AvatarInfo
				{
					Thumbnail = tex2d,
					VrcAvatarDescriptor = descriptor,
				};
				_cachedAvatarInfo.Add(info);
				
				AvatarWasUpdated?.Invoke(info);
			}


			var avatars = await avatarsTask;

			var tasks = new List<Task<(string url, Texture2D image)>>();
			foreach (ApiAvatar avatar in avatars)
			{
				foreach (VRCAvatarDescriptor descriptor in descriptors)
				{
					if (descriptor.TryGetComponent<PipelineManager>(out var pipelineManager) && pipelineManager.blueprintId == avatar.id)
					{
						var info = GetAvatarInfo(descriptor);
						if(info != null && info.ApiAvatar == null)
						{
							info.ApiAvatar = avatar;
							AvatarWasUpdated?.Invoke(info);
							tasks.Add(Utility.DownloadImage(avatar.imageUrl));
						}
					}
				}
			}

			await Task.WhenAll(tasks);
			
			foreach (var task in tasks)
			{
				var result = task.Result;
				if (result.image == null)
				{
					continue;
				}
				
				foreach (var avatar in _cachedAvatarInfo)
				{
					if (avatar.ApiAvatar?.imageUrl == result.url)
					{
						avatar.Thumbnail = result.image;
						AvatarWasUpdated?.Invoke(avatar);
					}
				}
			}
		}

		public void Dispose()
		{
			AvatarWasUpdated = null;
			EditorApplication.hierarchyChanged -= OnHierarchyChanged;
		}
	}
}