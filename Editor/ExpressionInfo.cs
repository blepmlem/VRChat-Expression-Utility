using System;
using System.Linq;
using ExpressionUtility.UI;
using UnityEditor;
using UnityEditor.Animations;
using UnityEngine;
using VRC.SDK3.Avatars.Components;
using VRC.SDK3.Avatars.ScriptableObjects;
using VRC.SDKBase;

namespace ExpressionUtility
{
	[Serializable]
	internal class ExpressionInfo : IDisposable
	{
		private string _expressionName = string.Empty;
		private AnimatorController _controller;
		private AvatarCache.AvatarInfo _avatarInfo;
		private bool _createAnimations = true;

		private const string MENU_PREF = "EXPRUTIL_MENU_";
		private const string CREATE_ANIMATIONS = "EXPRUTIL_CreateAnimations_";
		private const string FOLDER_PREF = "EXPRUTIL_FOLDER_";
		private const string DEFAULT_ANIMATION_FOLDER_PATH = "Assets/Animations";
		
		public event Action<ExpressionInfo> DataWasUpdated;

		public void Ping() => EditorGUIUtility.PingObject(GameObject);

		public void SetInfo(AvatarCache.AvatarInfo info)
		{
			if (_avatarInfo == info)
			{
				return;
			}

			_avatarInfo = info;
			_controller = info.VrcAvatarDescriptor.baseAnimationLayers.LastOrDefault().animatorController as AnimatorController;
			DataWasUpdated?.Invoke(this);
		}

		public string ExpressionName
		{
			get => _expressionName;
			set
			{
				_expressionName = value;
				DataWasUpdated?.Invoke(this);
			}
		}

		public AnimatorController Controller
		{
			get => _controller;
			set
			{
				_controller = value;
				DataWasUpdated?.Invoke(this);
			}
		}

		public VRCExpressionsMenu Menu
		{
			get => AssetDatabase.LoadAssetAtPath(EditorPrefs.GetString($"{MENU_PREF}_{GameObject.name}", null), typeof(VRCExpressionsMenu)) as VRCExpressionsMenu;
			set
			{
				EditorPrefs.SetString($"{MENU_PREF}_{GameObject.name}", AssetDatabase.GetAssetPath(value));
				DataWasUpdated?.Invoke(this);
			}
		}

		public bool CreateAnimations
		{
			get => EditorPrefs.GetBool($"{CREATE_ANIMATIONS}", true);
			set 
			{ 
				EditorPrefs.SetBool($"{CREATE_ANIMATIONS}", value); 
				DataWasUpdated?.Invoke(this);
			}
		}
		
		public DefaultAsset AnimationsFolder
		{
			get => AssetDatabase.LoadAssetAtPath(EditorPrefs.GetString($"{FOLDER_PREF}_{GameObject.name}", DEFAULT_ANIMATION_FOLDER_PATH), typeof(DefaultAsset)) as DefaultAsset;
			set
			{
				EditorPrefs.SetString($"{FOLDER_PREF}_{GameObject.name}", AssetDatabase.GetAssetPath(value));
				DataWasUpdated?.Invoke(this);
			}
		}

		public GameObject GameObject => _avatarInfo?.IsValid ?? false ? _avatarInfo.VrcAvatarDescriptor.gameObject : null;

		public VRCAvatarDescriptor AvatarDescriptor => AvatarInfo?.VrcAvatarDescriptor;

		public AvatarCache.AvatarInfo AvatarInfo => _avatarInfo;

		public void Dispose()
		{
			DataWasUpdated = null;
		}
	}
}