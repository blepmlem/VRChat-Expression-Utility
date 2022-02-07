using System;
using System.Linq;
using ExpressionUtility.UI;
using UnityEditor;
using UnityEditor.Animations;
using UnityEngine;
using VRC.SDK3.Avatars.Components;
using VRC.SDK3.Avatars.ScriptableObjects;

namespace ExpressionUtility
{
	[Serializable]
	internal class ExpressionInfo : IDisposable
	{
		private string _expressionName = string.Empty;
		private AnimatorController _controller;
		private AvatarCache.AvatarInfo _avatarInfo;
		private event Action<ExpressionInfo> _avatarWasUpdated;
		
		public ExpressionInfo(Action<ExpressionInfo> avatarWasUpdated) => _avatarWasUpdated = avatarWasUpdated;

		private const string MENU_PREF = "EXPRUTIL_MENU_";
		private const string FOLDER_PREF = "EXPRUTIL_FOLDER_";

		public void Ping() => EditorGUIUtility.PingObject(GameObject);

		public void SetInfo(AvatarCache.AvatarInfo info)
		{
			if (_avatarInfo == info)
			{
				return;
			}

			_avatarInfo = info;
			_controller = info.VrcAvatarDescriptor.baseAnimationLayers.LastOrDefault().animatorController as AnimatorController;
			if (Menu == null && AvatarDescriptor.expressionsMenu != null)
			{
				Menu = AvatarDescriptor.expressionsMenu.GetMenusRecursively().FirstOrDefault();
			}
			_avatarWasUpdated?.Invoke(this);
		}

		public string ExpressionName
		{
			get => _expressionName;
			set => _expressionName = value;
		}

		public AnimatorController Controller
		{
			get => _controller;
			set => _controller = value;
		}

		public VRCExpressionsMenu Menu
		{
			get => AssetDatabase.LoadAssetAtPath(EditorPrefs.GetString($"{MENU_PREF}_{GameObject.name}", null), typeof(VRCExpressionsMenu)) as VRCExpressionsMenu;
			set => EditorPrefs.SetString($"{MENU_PREF}_{GameObject.name}", AssetDatabase.GetAssetPath(value));
		}
		
		public DefaultAsset AnimationsFolder
		{
			get => AssetDatabase.LoadAssetAtPath(EditorPrefs.GetString($"{FOLDER_PREF}_{GameObject.name}", null), typeof(DefaultAsset)) as DefaultAsset;
			set => EditorPrefs.SetString($"{FOLDER_PREF}_{GameObject.name}", AssetDatabase.GetAssetPath(value));
		}

		public GameObject GameObject => _avatarInfo?.IsValid ?? false ? _avatarInfo.VrcAvatarDescriptor.gameObject : null;

		public VRCAvatarDescriptor AvatarDescriptor => AvatarInfo?.VrcAvatarDescriptor;

		public AvatarCache.AvatarInfo AvatarInfo => _avatarInfo;

		public void Dispose()
		{
			_avatarWasUpdated = null;
		}
	}
}