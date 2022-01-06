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
		private VRCExpressionsMenu _menu;
		private AnimatorController _controller;
		private AvatarCache.AvatarInfo _avatarInfo;
		private DefaultAsset _animationFolder;
		private bool _createAnimations;
		
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

		public VRCExpressionsMenu Menu
		{
			get => _menu;
			set
			{
				_menu = value;
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

		public const string CREATE_ANIMATIONS = "EXPRUTIL_CreateAnimations_";
		public bool CreateAnimations
		{
			get => EditorPrefs.GetBool($"{CREATE_ANIMATIONS}", true);
			set 
			{ 
				EditorPrefs.SetBool($"EXPRUTIL_CreateAnimations", value); 
				DataWasUpdated?.Invoke(this);
			}
		}
		
		public const string FOLDER_PREF = "EXPRUTIL_FOLDER_";
		public const string DEFAULT_ANIMATION_FOLDER_PATH = "Assets/Animations";
		
		public DefaultAsset AnimationsFolder
		{
			get
			{
				if (_controller == null)
				{
					return null;
				}
				
				return AssetDatabase.LoadAssetAtPath(EditorPrefs.GetString($"{FOLDER_PREF}_{_controller.name}", DEFAULT_ANIMATION_FOLDER_PATH), typeof(DefaultAsset)) as DefaultAsset;
			}
			set => EditorPrefs.SetString(FOLDER_PREF, AssetDatabase.GetAssetPath(value));
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