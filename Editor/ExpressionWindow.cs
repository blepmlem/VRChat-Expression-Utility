using System;
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEditor.Animations;
using UnityEngine;
using VRC.Core;
using VRC.SDK3.Avatars.Components;
using VRC.SDK3.Avatars.ScriptableObjects;
using Object = UnityEngine.Object;
#pragma warning disable 4014

namespace ExpresionUtility
{
	internal class ExpressionWindow : EditorWindow
	{
		public static VRCAvatarDescriptor AvatarDescriptor { get; private set; }
		public static IEnumerable<VRCExpressionsMenu> Menus => AssetDatabase.FindAssets("t:VRCExpressionsMenu").Select(AssetDatabase.GUIDToAssetPath).Select(AssetDatabase.LoadAssetAtPath<VRCExpressionsMenu>);

		public static DefaultAsset AnimationsFolder
		{
			get => AssetDatabase.LoadAssetAtPath(EditorPrefs.GetString(FOLDER_PREF, DEFAULT_ANIMATION_FOLDER_PATH), typeof(DefaultAsset)) as DefaultAsset;
			set => EditorPrefs.SetString(FOLDER_PREF, AssetDatabase.GetAssetPath(value));
		}

		[SerializeField]
		private Vector2 _scroll;

		[SerializeField]
		private ExpressionDefinition _expressionDefinition;
	
		[SerializeField]
		private GUIStyle _helpStyle;
		
		[SerializeField]
		private GUIStyle _buttonDown;

		[SerializeField]
		private bool _experimentalFeatures;
		
		
		private Updater _updater;
		

		private async void OnEnable()
		{
			AvatarDescriptor = Object.FindObjectOfType<VRCAvatarDescriptor>();
			this.minSize = new Vector2(600, 600);
			_updater = await Updater.Create();
		}

		private void OnGUI()
		{
			DrawUpdate();
			_scroll = GUILayout.BeginScrollView(_scroll);
			GUILayout.BeginHorizontal();
			GUILayout.BeginVertical();
			if (DrawInit())
			{
				DrawLayers();
				DrawAnimationBuilder();
			}
			GUILayout.FlexibleSpace();
			_experimentalFeatures = EditorGUILayout.ToggleLeft("Experimental Features", _experimentalFeatures);
			GUILayout.EndVertical();
			DrawHelp();
			GUILayout.EndHorizontal();
			GUILayout.EndScrollView();
		}

		private void DrawUpdate()
		{
			if (_updater != null && _updater.HasNewerVersion)
			{
				var originalColor = GUI.color;
				var color = Color.green + Color.white * 0.5f;
			
				var txtUpdating = $"Updating...\nThis window will close when complete!";
				var txtUpdate = $"Update available!\n<b>New version: {_updater.LatestVersion}</b>";
			
				GUILayout.BeginHorizontal("box", GUILayout.Height(30));
				EditorGUILayout.LabelField(_updater.IsUpdating ? txtUpdating : txtUpdate, _helpStyle, GUILayout.ExpandHeight(true));

				GUI.color = originalColor;
				if (!_updater.IsUpdating)
				{
					if (GUILayout.Button("Github", GUILayout.ExpandHeight(true)))
					{
						_updater.OpenGitHub();
					}
			
					GUI.color = color;
					if (GUILayout.Button("Install", GUILayout.ExpandHeight(true)))
					{
						_updater.Update(Close);
					}	
				}

				GUI.color = originalColor;
				GUILayout.EndHorizontal();

			}
		}

		private void DrawHelp()
		{
			string txt;
			var version = _updater != null ? $"Version {_updater.CurrentVersion}\n\n" : "";
			if (AvatarDescriptor == null)
			{
				txt = $"{version}Please open this window in a scene containing an avatar with a {nameof(VRCAvatarDescriptor)} component on it! You can also manually drag your avatar prefab into this slot instead.";
			}
			else if (_expressionDefinition == null || _expressionDefinition.Controller == null)
			{
				txt = $"{version}Welcome! \n\nTo create a new Expression, you should select one of the Animators to the right that you want to put a new Expression on. To do so, click the <b>Select</b> button for that Animator. \n\nMost of the time you'll want to use your FX Animator. If the layer you want to use is empty, you will need to select your avatar and set up the Playable Layer (Animator) you want in your Avatar Descriptor in the inspector.\n\n";
			}
			else
			{
				txt = $"{version}You're ready to make an Expression! \n\nNow you can see which Layers this Animator has. \n\nTo add a new Expression you simply put in a new Expression name under <b>Create New Expression</b> and select which menu you want to put it in. This name will be used to create new Layers, Transitions, Menu Controls, And a new empty animation if wanted! \n\nAfter this is done, you will need to take the animation associated with this expression and set up what it will actually do. \n\nThe system will select your avatar and put in the Animator you used so you quickly can set up an animation! \n\nAs soon as that is done, you can upload your avatar and test it! \n\nHave fun <3";
			}
		
			GUILayout.BeginVertical("box", GUILayout.ExpandHeight(true), GUILayout.Width(200));
			EditorGUILayout.LabelField(txt, _helpStyle);
			GUILayout.EndVertical();
		}

		private bool DrawInit()
		{
			if (_helpStyle == null)
			{
				_helpStyle = new GUIStyle("Label") {richText = true, wordWrap = true, stretchHeight = true, stretchWidth = true};
			}

			if (_buttonDown == null)
			{
				_buttonDown = new GUIStyle(EditorStyles.toolbarButton) {richText = true, wordWrap = true, stretchHeight = true, stretchWidth = true};
				_buttonDown.normal = _buttonDown.active;
			}
			
			AvatarDescriptor = EditorGUILayout.ObjectField("Active Avatar", AvatarDescriptor, typeof(VRCAvatarDescriptor), true) as VRCAvatarDescriptor;

			var folder = EditorGUILayout.ObjectField("Animations Folder", AnimationsFolder, typeof(DefaultAsset), false) as DefaultAsset;
			if (folder != AnimationsFolder)
			{
				AnimationsFolder = folder;
			}
		
			if (AvatarDescriptor == null)
			{
				return false;
			}

			EditorGUILayout.Space();
			return true;
		}

		private void DrawLayers()
		{
			foreach (VRCAvatarDescriptor.CustomAnimLayer layer in AvatarDescriptor.baseAnimationLayers)
			{
				if (DrawCreateAnimButton(layer))
				{
					_expressionDefinition = new ExpressionDefinition(layer);
				}
			}
		}

		private void DrawAnimationBuilder()
		{
			if (_expressionDefinition == null || _expressionDefinition.Controller == null)
			{
				return;
			}
		
			EditorGUILayout.Space();
			EditorGUILayout.BeginVertical("box");
			EditorGUILayout.LabelField(_expressionDefinition.Controller.name,EditorStyles.boldLabel);
		
			EditorGUILayout.Space();
			Expression toDelete = null;

			EditorGUILayout.LabelField(" -Create new Expression- ", EditorStyles.boldLabel);
			
			EditorGUILayout.BeginVertical("box");
			EditorGUILayout.BeginHorizontal();
			
			EditorGUI.BeginDisabledGroup(!_experimentalFeatures);
			
			var types = Enum.GetNames(typeof(ExpressionDefinition.ExpressionType));
			for (int i = 0; i < types.Length; i++)
			{
				bool isClicked = _expressionDefinition.Type == (ExpressionDefinition.ExpressionType)i;
				if (GUILayout.Button(types[i], isClicked ? _buttonDown : EditorStyles.toolbarButton, GUILayout.ExpandWidth(true)))
				{
					_expressionDefinition.Type = (ExpressionDefinition.ExpressionType) i;
				}	
			}
			EditorGUILayout.EndHorizontal();
			
			_expressionDefinition.ParameterType = (VRCExpressionParameters.ValueType) EditorGUILayout.EnumPopup("Type",  _expressionDefinition.ParameterType);
			EditorGUI.EndDisabledGroup();

			if (_expressionDefinition.Type == ExpressionDefinition.ExpressionType.Toggle)
			{
				_expressionDefinition.ParameterName = EditorGUILayout.TextField("Name", _expressionDefinition.ParameterName);
				_expressionDefinition.Menu = EditorGUILayout.ObjectField("Menu", _expressionDefinition.Menu, typeof(VRCExpressionsMenu), false) as VRCExpressionsMenu;
				_expressionDefinition.CreateAnimation = EditorGUILayout.Toggle("Create Animation", _expressionDefinition.CreateAnimation);
				
				EditorGUI.BeginDisabledGroup(string.IsNullOrEmpty(_expressionDefinition.ParameterName) || Expression.Exists(_expressionDefinition.ParameterName));
				if (GUILayout.Button("CREATE", EditorStyles.miniButton, GUILayout.Width(70)))
				{
					var expression = Expression.Create(_expressionDefinition);
					if(_expressionDefinition.CreateAnimation)
					{
						var anim = AvatarDescriptor.gameObject.GetComponent<Animator>();
						if (anim != null)
						{
							anim.runtimeAnimatorController = _expressionDefinition.Controller;
							Selection.activeGameObject = AvatarDescriptor.gameObject;
						}
						else
						{
							AssetDatabase.OpenAsset(expression.AnimationClip);
						}
					}
					_expressionDefinition = new ExpressionDefinition(_expressionDefinition.Controller);
				}
				EditorGUI.EndDisabledGroup();
			}
			else
			{
				EditorGUILayout.LabelField("Broken. Go back.", EditorStyles.boldLabel);
			}

			EditorGUILayout.EndVertical();
			
			EditorGUILayout.Space();
			EditorGUILayout.LabelField(" -Current Animation Layers- ", EditorStyles.boldLabel);
			foreach (AnimatorControllerLayer animatorControllerLayer in _expressionDefinition.Controller.layers)
			{
				var expression = new Expression(_expressionDefinition.Controller, animatorControllerLayer.name);

				using (new GUILayout.HorizontalScope("box"))
				{
					if (GUILayout.Button("DELETE", EditorStyles.miniButton, GUILayout.Width(60)))
					{
						toDelete = expression;
					}
				
					EditorGUI.BeginDisabledGroup(true);
					// EditorGUILayout.ObjectField(expression.Menu, typeof(VRCExpressionsMenu), false);
					EditorGUI.EndDisabledGroup();
					EditorGUILayout.LabelField(expression.Name);
				}
			}

			toDelete?.Delete();

			EditorGUILayout.EndVertical();
		}

		private bool DrawCreateAnimButton(VRCAvatarDescriptor.CustomAnimLayer layer)
		{
			using (new EditorGUILayout.HorizontalScope())
			{
				using (new EditorGUI.DisabledScope(layer.animatorController == null))
				{
					EditorGUILayout.ObjectField(layer.type.ToString(), layer.animatorController, typeof(AnimatorController), false);
					if (GUILayout.Button("SELECT", EditorStyles.miniButton, GUILayout.Width(90)))
					{
						AssetDatabase.OpenAsset(layer.animatorController);
						return true;
					}	
				}
			}
			return false;
		}

		[MenuItem("Expression Utility/Open Window")]
		public static void GetWindow() => EditorWindow.GetWindow(typeof(ExpressionWindow));

		public const string FOLDER_PREF = "VRCExpressionUtilityAnimationFolder";
		public const string DEFAULT_ANIMATION_FOLDER_PATH = "Assets/Animations";
	}
}