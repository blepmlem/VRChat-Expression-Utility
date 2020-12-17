using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.Animations;
using UnityEngine;
using VRC.SDK3.Avatars.Components;
using VRC.SDK3.Avatars.ScriptableObjects;
using Object = UnityEngine.Object;

public class ExpressionWindow : EditorWindow
{
	private static VRCAvatarDescriptor _avatarDescriptor;
	private static IEnumerable<VRCExpressionsMenu> Menus => AssetDatabase.FindAssets("t:VRCExpressionsMenu").Select(AssetDatabase.GUIDToAssetPath).Select(AssetDatabase.LoadAssetAtPath<VRCExpressionsMenu>);

	private static DefaultAsset AnimationsFolder
	{
		get => AssetDatabase.LoadAssetAtPath(EditorPrefs.GetString(FOLDER_PREF, "Assets/Animations"), typeof(DefaultAsset)) as DefaultAsset;
		set
		{
			if (value == null)
			{
				return;
			}
			EditorPrefs.SetString(FOLDER_PREF, AssetDatabase.GetAssetPath(value));
		}
	}

	[SerializeField]
	private Vector2 _scroll;

	[SerializeField]
	private ExpressionBuilder expressionBuilder;
	
	[SerializeField]
	private GUIStyle _helpStyle;


	[Serializable]
	private class ExpressionBuilder
	{
		[field: SerializeField]
		public bool CreateAnimation { get; set; } = true;
		
		[field:SerializeField]
		public AnimatorController Controller { get; set; }
		
		[field:SerializeField]
		public string ParameterName { get; set; }

		[field:SerializeField]
		public VRCExpressionsMenu Menu { get; set; }

		[field: SerializeField]
		public VRCExpressionParameters.ValueType ParameterType { get; set; } = VRCExpressionParameters.ValueType.Int;
		public ExpressionBuilder(VRCAvatarDescriptor.CustomAnimLayer layer)
		{
			Controller = layer.animatorController as AnimatorController;
		}
		public ExpressionBuilder(AnimatorController controller)
		{
			Controller = controller;
		}
	}

	private void OnEnable()
	{
		_avatarDescriptor = Object.FindObjectOfType<VRCAvatarDescriptor>();
		this.minSize = new Vector2(600, 600);
	}

	private void OnGUI()
	{
		GUILayout.BeginScrollView(_scroll);
		
		GUILayout.BeginHorizontal();
		GUILayout.BeginVertical();
		if (DrawInit())
		{
			DrawLayers();
			DrawAnimationBuilder();
		}
		GUILayout.EndVertical();
		DrawHelp();
		GUILayout.EndHorizontal();
		GUILayout.EndScrollView();
	}

	private void DrawHelp()
	{
		string txt = "";
		if (_avatarDescriptor == null)
		{
			txt = $"Please open this window in a scene containing an avatar with a {nameof(VRCAvatarDescriptor)} component on it! You can also manually drag your avatar prefab into this slot instead.";
		}
		else if (expressionBuilder == null || expressionBuilder.Controller == null)
		{
			txt = $"Welcome! \n\nTo create a new Expression, you should select one of the Animators to the right that you want to put a new Expression on. To do so, click the <b>Select</b> button for that Animator. \n\nMost of the time you'll want to use your FX Animator. If the layer you want to use is empty, you will need to select your avatar and set up the Playable Layer (Animator) you want in your Avatar Descriptor in the inspector.\n\n";
		}
		else
		{
			txt = $"You're ready to make an Expression! \n\nNow you can see which Layers this Animator has. \n\nTo add a new Expression you simply put in a new Expression name under <b>Create New Expression</b> and select which menu you want to put it in. This name will be used to create new Layers, Transitions, Menu Controls, And a new empty animation if wanted! \n\nAfter this is done, you will need to take the animation associated with this expression and set up what it will actually do. \n\nThe system will select your avatar and put in the Animator you used so you quickly can set up an animation! \n\nAs soon as that is done, you can upload your avatar and test it! \n\nHave fun <3";
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
		
		_avatarDescriptor = EditorGUILayout.ObjectField("Active Avatar", _avatarDescriptor, typeof(VRCAvatarDescriptor), true) as VRCAvatarDescriptor;

		var folder = EditorGUILayout.ObjectField("Animations Folder", AnimationsFolder, typeof(DefaultAsset), false) as DefaultAsset;
		if (folder != AnimationsFolder)
		{
			AnimationsFolder = folder;
		}
		
		if (_avatarDescriptor == null)
		{
			return false;
		}

		EditorGUILayout.Space();
		return true;
	}

	private void DrawLayers()
	{
		foreach (VRCAvatarDescriptor.CustomAnimLayer layer in _avatarDescriptor.baseAnimationLayers)
		{
			if (DrawCreateAnimButton(layer))
			{
				expressionBuilder = new ExpressionBuilder(layer);
			}
		}
	}

	private void DrawAnimationBuilder()
	{
		if (expressionBuilder == null || expressionBuilder.Controller == null)
		{
			return;
		}
		
		EditorGUILayout.Space();
		EditorGUILayout.BeginVertical("box");
		EditorGUILayout.LabelField(expressionBuilder.Controller.name,EditorStyles.boldLabel);
		
		EditorGUILayout.Space();
		Expression toDelete = null;

		foreach (AnimatorControllerLayer animatorControllerLayer in expressionBuilder.Controller.layers)
		{
			var expression = new Expression(expressionBuilder.Controller, animatorControllerLayer.name);
			// if (expression.Menu == null)
			// {
			// 	continue;
			// }
			
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

		EditorGUILayout.Space();
		EditorGUILayout.LabelField("Create new Expression", EditorStyles.boldLabel);
		expressionBuilder.ParameterType = (VRCExpressionParameters.ValueType) EditorGUILayout.EnumPopup("Type", expressionBuilder.ParameterType);
		expressionBuilder.ParameterName = EditorGUILayout.TextField("Name", expressionBuilder.ParameterName);
		expressionBuilder.Menu = EditorGUILayout.ObjectField("Menu", expressionBuilder.Menu, typeof(VRCExpressionsMenu), false) as VRCExpressionsMenu;
		expressionBuilder.CreateAnimation = EditorGUILayout.Toggle("Create Animation", expressionBuilder.CreateAnimation);
		EditorGUI.BeginDisabledGroup(string.IsNullOrEmpty(expressionBuilder.ParameterName));
		if (GUILayout.Button("CREATE", EditorStyles.miniButton, GUILayout.Width(70)))
		{
			var expression = Expression.Create(expressionBuilder);
			if(expressionBuilder.CreateAnimation)
			{
				var anim = _avatarDescriptor.gameObject.GetComponent<Animator>();
				if (anim != null)
				{
					anim.runtimeAnimatorController = expressionBuilder.Controller;
					Selection.activeGameObject = _avatarDescriptor.gameObject;
				}
				else
				{
					AssetDatabase.OpenAsset(expression.AnimationClip);
				}
			}
			expressionBuilder = new ExpressionBuilder(expressionBuilder.Controller);
		}
		EditorGUI.EndDisabledGroup();


		EditorGUILayout.EndVertical();
	}

	[Serializable]
	private class Expression
	{
		[field: SerializeField]
		public string Name { get; set; }
		
		[field:SerializeField]
		private AnimatorController Controller { get; set; }
		
		public int ParameterIndex => Controller.parameters.ToList().FindIndex(p => Name.Equals(p.name, StringComparison.OrdinalIgnoreCase));
		
		public int LayerIndex => Controller.layers.ToList().FindIndex(l => Name.Equals(l.name, StringComparison.OrdinalIgnoreCase));
		
		public AnimatorControllerLayer Layer => Controller.layers.FirstOrDefault(l => Name.Equals(l.name, StringComparison.OrdinalIgnoreCase));
		
		public VRCExpressionsMenu Menu =>  Menus.FirstOrDefault(m => m.controls.Exists(c => c.parameter?.name == Name));

		public AnimationClip AnimationClip => AnimationsFolder != null ? AssetDatabase.LoadAssetAtPath<AnimationClip>($"{AssetDatabase.GetAssetPath(AnimationsFolder)}/{Controller.name}/{Name}.anim") : null;
		
		public Expression(AnimatorController controller, string name)
		{
			Name = name;
			Controller = controller;
		}
		
		public static Expression Create(ExpressionBuilder builder)
		{
			AnimatorCondition CreateCondition(bool isEntry)
			{
				var condition = new AnimatorCondition();
				condition.mode = AnimatorConditionMode.Equals;
				condition.parameter = builder.ParameterName;
				condition.threshold = isEntry ? 1 : 0;
				return condition;
			}
			
			var layer = new AnimatorControllerLayer
			{
				name = builder.ParameterName,
				defaultWeight = 1f,
				stateMachine = new AnimatorStateMachine
				{
					name = builder.ParameterName,
				}
			};

			AnimatorControllerParameterType type = builder.ParameterType == VRCExpressionParameters.ValueType.Int ? AnimatorControllerParameterType.Int : AnimatorControllerParameterType.Float;
			
			builder.Controller.AddLayer(layer);
			builder.Controller.AddParameter(builder.ParameterName, type);

			var instance = new Expression(builder.Controller, builder.ParameterName);

			AnimationClip animation = null;
			
			if(builder.CreateAnimation)
			{
				animation = new AnimationClip()
				{
					name = instance.Name,
				};

				if (AnimationsFolder != null)
				{
					var path = AssetDatabase.GetAssetPath(AnimationsFolder);
					Directory.CreateDirectory($"{path}/{instance.Controller.name}");
					AssetDatabase.CreateAsset(animation, $"{path}/{instance.Controller.name}/{instance.Name}.anim");
					AssetDatabase.Refresh();
				}
			}
			
			
			var stateMachine = instance.Layer.stateMachine;
			var empty = stateMachine.AddState("Empty");
			stateMachine.defaultState = empty;

			var state = stateMachine.AddState(instance.Name);
			state.name = instance.Name;
			state.motion = animation;
			var entry = stateMachine.AddAnyStateTransition(state);
			entry.conditions = new[] {CreateCondition(true)};
			var exit = state.AddExitTransition(false);
			exit.conditions = new[] {CreateCondition(false)};
			
			var parameters = _avatarDescriptor.expressionParameters.parameters;
			if (parameters.All(p => p.name != builder.ParameterName))
			{
				for (var i = 0; i < parameters.Length; i++)
				{
					if (string.IsNullOrEmpty(parameters[i]?.name))
					{
						parameters[i] = new VRCExpressionParameters.Parameter
						{
							name = builder.ParameterName,
							valueType = builder.ParameterType,
						};
						break;
					}
				}
			}

			if (builder.Menu != null)
			{
				var control = new VRCExpressionsMenu.Control
				{
					name = ObjectNames.NicifyVariableName(builder.ParameterName),
					parameter = new VRCExpressionsMenu.Control.Parameter{name = builder.ParameterName},
					type = VRCExpressionsMenu.Control.ControlType.Toggle,
				};
				builder.Menu.controls.Add(control);
			}
			
			SetDirty(stateMachine, builder.Controller, builder.Menu, _avatarDescriptor.expressionParameters, _avatarDescriptor.gameObject, state, exit, entry, empty);
			AddObjectToAsset(instance.Controller, stateMachine, state, entry, exit, empty);
			AssetDatabase.Refresh();
			return instance;
		}

		private static void AddObjectToAsset(Object asset, params Object[] objs)
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
		
		private static void SetDirty(params Object[] objs)
		{
			foreach (var o in objs)
			{
				if (o == null)
				{
					continue;
				}
				EditorUtility.SetDirty(o);
			}

			AssetDatabase.SaveAssets();
		}
		
		public void Delete()
		{
			if(LayerIndex >= 0)
			{
				Controller.RemoveLayer(LayerIndex);
			}
			if(ParameterIndex >= 0)
			{
				Controller.RemoveParameter(ParameterIndex);
			}

			var parameters = _avatarDescriptor.expressionParameters.parameters;
			for (var i = 0; i < parameters.Length; i++)
			{
				if (parameters[i].name == Name)
				{
					parameters[i] = new VRCExpressionParameters.Parameter();
				}
			}
			
			if (Menu != null)
			{
				Menu.controls.Remove(Menu.controls.First(c => c.name == Name));
			}

			SetDirty(Controller, Menu, _avatarDescriptor.expressionParameters);
			
			if (AnimationClip != null)
			{
				if (EditorUtility.DisplayDialog("Remove Animation?", "Do you also want to remove the animation associated with this expression?", "Yes!", "Nah"))
				{
					var path = AssetDatabase.GetAssetPath(AnimationClip);
					FileUtil.DeleteFileOrDirectory(path);
					AssetDatabase.Refresh();
				}
			}
		}
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
	public static void GetWindow()
	{
		var window = EditorWindow.GetWindow(typeof(ExpressionWindow));
	}

	private const string FOLDER_PREF = "VRCExpressionUtilityAnimationFolder";
}
