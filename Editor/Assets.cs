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
		[Serializable]
		public class ExpressionDefinitionData
		{
			public string Name;
			[TextArea]
			public string Description;
			[HideInInspector]
			public Texture2D Icon;

			public MonoScript Asset;
			public IExpressionDefinition Instance { get; set; }
		}

		[SerializeField]
		private List<ExpressionDefinitionData> _definitionData;
		
		[SerializeField]
		private List<Message> _messages;
		
		[SerializeField]
		private VisualTreeAsset _miniAvatar;

		[SerializeField]
		private VisualTreeAsset _avatarSelectorButton;

		[SerializeField]
		private VisualTreeAsset _expressionDefinitionPreviewButton;

		[SerializeField]
		private DefaultAsset _uiAssetsFolder;

		[SerializeField]
		private VisualTreeAsset _mainWindow;
		
		[SerializeField]
		private VisualTreeAsset _infoBox;
		
		[SerializeField]
		private Texture2D _infoIcon;
		[SerializeField]
		private Texture2D _warningIcon;
		[SerializeField]
		private Texture2D _errorIcon;
		public VisualTreeAsset MiniAvatar => _miniAvatar;
		public VisualTreeAsset InfoBox => _infoBox;
		public VisualTreeAsset AvatarSelectorButton => _avatarSelectorButton;
		public VisualTreeAsset ExpressionDefinitionPreviewButton => _expressionDefinitionPreviewButton;
		public Dictionary<Type, (IExpressionUI ui, VisualTreeAsset treeAsset)> UIAssets {get; private set; }
		public Dictionary<Type, ExpressionDefinitionData> ExpressionDefinitionAssets { get; private set; }
		public List<Message> Messages => _messages;
		public VisualTreeAsset MainWindow => _mainWindow;

		public Texture2D InfoIcon => _infoIcon != null ? _infoIcon :  EditorGUIUtility.IconContent("console.infoicon@2x").image as Texture2D;
		public Texture2D WarningIcon => _warningIcon != null ? _warningIcon :  EditorGUIUtility.IconContent("console.warnicon@2x").image as Texture2D;
		public Texture2D ErrorIcon => _errorIcon != null ? _errorIcon :  EditorGUIUtility.IconContent("console.erroricon@2x").image as Texture2D;


		public void Initialize()
		{
			UIAssets = new Dictionary<Type, (IExpressionUI ui, VisualTreeAsset treeAsset)>();
			ExpressionDefinitionAssets = new Dictionary<Type, ExpressionDefinitionData>();

			string uiPath = AssetDatabase.GetAssetPath(_uiAssetsFolder);
			
			foreach (Type type in TypeCache.GetTypesDerivedFrom<IExpressionUI>())
			{
				var asset = AssetDatabase.LoadAssetAtPath<VisualTreeAsset>($"{uiPath}/{type.Name}.uxml");
				var instance = Activator.CreateInstance(type) as IExpressionUI;
				if (asset == null || instance == null)
				{
					continue;
				}

				UIAssets.Add(type, (instance, asset));

				if (instance is IExpressionDefinition expressionDefinition)
				{
					var exprAsset = _definitionData.FirstOrDefault(d => d.Asset != null && d.Asset.GetClass() == type);
					if (exprAsset == null)
					{
						continue;
					}
					
					exprAsset.Icon = AssetDatabase.GetCachedIcon(AssetDatabase.GetAssetPath(exprAsset.Asset)) as Texture2D;
					exprAsset.Instance = expressionDefinition;
					ExpressionDefinitionAssets[type] = exprAsset;
				}
			}
		}
	}
}