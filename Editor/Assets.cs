using System;
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEngine;
using UnityEngine.UIElements;

namespace ExpressionUtility.UI
{
	internal class Assets : ScriptableObject
	{
		[SerializeField]
		private Texture2D _githubIconDark;

		[SerializeField]
		private Texture2D _githubIconLight;

		
		[SerializeField]
		private List<Message> _messages;
		
		[SerializeField]
		private VisualTreeAsset _miniAvatar;

		[SerializeField]
		private VisualTreeAsset _avatarSelectorButton;

		[SerializeField]
		private VisualTreeAsset _expressionDefinitionPreviewButton;

		[SerializeField]
		private VisualTreeAsset _infoBox;

		[SerializeField]
		private VisualTreeAsset _avatarParameterDataRow;

		public VisualTreeAsset AvatarParameterDataRow => _avatarParameterDataRow; 
		public VisualTreeAsset MiniAvatar => _miniAvatar;
		public VisualTreeAsset InfoBox => _infoBox;
		public VisualTreeAsset AvatarSelectorButton => _avatarSelectorButton;
		public VisualTreeAsset ExpressionDefinitionPreviewButton => _expressionDefinitionPreviewButton;
		public Dictionary<Type, List<ExpressionUI>> UIAssets {get; private set; }
		public List<Message> Messages => _messages;

		public Texture2D InfoIcon => EditorGUIUtility.IconContent("console.infoicon@2x").image as Texture2D;
		public Texture2D WarningIcon => EditorGUIUtility.IconContent("console.warnicon@2x").image as Texture2D;
		public Texture2D ErrorIcon => EditorGUIUtility.IconContent("console.erroricon@2x").image as Texture2D;

		public Texture2D GithubDark => _githubIconDark;
		public Texture2D GithubLight => _githubIconLight;

		public void Initialize()
		{
			UIAssets = new Dictionary<Type, List<ExpressionUI>>();

			var assets = AssetDatabase.FindAssets($"t:{nameof(ExpressionUI)}").ToList();
			
			foreach (string guid in assets)
			{
				var path = AssetDatabase.GUIDToAssetPath(guid);
				var asset = AssetDatabase.LoadAssetAtPath<ExpressionUI>(path);
				var type = asset.GetType();
				
				var instance = EditorUtility.InstanceIDToObject(asset.GetInstanceID()) as ExpressionUI;
				if (instance == null || instance == null)
				{
					continue;
				}

				if (!UIAssets.TryGetValue(type, out List<ExpressionUI> value))
				{
					value = new List<ExpressionUI>();
				}
				value.Add(instance);
				UIAssets[type] = value;
			}
		}
		
	}
}