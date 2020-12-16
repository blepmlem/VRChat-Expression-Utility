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
	private AnimationBuilder _animationBuilder;


	[Serializable]
	private class AnimationBuilder
	{
		[field:SerializeField]
		public AnimatorController Controller { get; set; }
		
		[field:SerializeField]
		public string ParameterName { get; set; }

		[field:SerializeField]
		public VRCExpressionsMenu Menu { get; set; }

		[field: SerializeField]
		public VRCExpressionParameters.ValueType ParameterType { get; set; } = VRCExpressionParameters.ValueType.Int;
		public AnimationBuilder(VRCAvatarDescriptor.CustomAnimLayer layer)
		{
			Controller = layer.animatorController as AnimatorController;
		}
		public AnimationBuilder(AnimatorController controller)
		{
			Controller = controller;
		}
	}

	private void OnEnable()
	{
		_avatarDescriptor = Object.FindObjectOfType<VRCAvatarDescriptor>();
		this.minSize = new Vector2(400, 600);
	}

	private void OnGUI()
	{
		GUILayout.BeginScrollView(_scroll);
		if (!DrawInit())
		{
			return;
		}

		DrawLayers();
		DrawAnimationBuilder();
		GUILayout.EndScrollView();
	}

	private bool DrawInit()
	{
		_avatarDescriptor = EditorGUILayout.ObjectField("Active Avatar", _avatarDescriptor, typeof(VRCAvatarDescriptor), false) as VRCAvatarDescriptor;
		var folder = EditorGUILayout.ObjectField("Animations Folder", AnimationsFolder, typeof(DefaultAsset), false) as DefaultAsset;
		if (folder != AnimationsFolder)
		{
			AnimationsFolder = folder;
		}
		
		if (_avatarDescriptor == null)
		{
			EditorGUILayout.HelpBox($"Drag an object with a {nameof(VRCAvatarDescriptor)} component on it in here!", MessageType.Info);
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
				_animationBuilder = new AnimationBuilder(layer);
			}
		}
	}

	private void DrawAnimationBuilder()
	{
		if (_animationBuilder == null || _animationBuilder.Controller == null)
		{
			return;
		}
		
		EditorGUILayout.Space();
		EditorGUILayout.BeginVertical("box");
		EditorGUILayout.LabelField(_animationBuilder.Controller.name,EditorStyles.boldLabel);
		
		EditorGUILayout.Space();
		Expression toDelete = null;

		foreach (AnimatorControllerLayer animatorControllerLayer in _animationBuilder.Controller.layers)
		{
			var expression = new Expression(_animationBuilder.Controller, animatorControllerLayer.name);
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
		_animationBuilder.ParameterType = (VRCExpressionParameters.ValueType) EditorGUILayout.EnumPopup("Type", _animationBuilder.ParameterType);
		_animationBuilder.ParameterName = EditorGUILayout.TextField("Name", _animationBuilder.ParameterName);
		_animationBuilder.Menu = EditorGUILayout.ObjectField("Menu", _animationBuilder.Menu, typeof(VRCExpressionsMenu), false) as VRCExpressionsMenu;

		EditorGUI.BeginDisabledGroup(string.IsNullOrEmpty(_animationBuilder.ParameterName));
		if (GUILayout.Button("CREATE", EditorStyles.miniButton, GUILayout.Width(70)))
		{
			Expression.Create(_animationBuilder);
			_animationBuilder = new AnimationBuilder(_animationBuilder.Controller);
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
		
		public static Expression Create(AnimationBuilder builder)
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
				stateMachine = new AnimatorStateMachine()
			};

			AnimatorControllerParameterType type = builder.ParameterType == VRCExpressionParameters.ValueType.Int ? AnimatorControllerParameterType.Int : AnimatorControllerParameterType.Float;
			
			builder.Controller.AddLayer(layer);
			builder.Controller.AddParameter(builder.ParameterName, type);

			var instance = new Expression(builder.Controller, builder.ParameterName);
			
			var animation = new AnimationClip()
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
			
			var stateMachine = instance.Layer.stateMachine;
			var empty = stateMachine.AddState("Empty");
			stateMachine.defaultState = empty;

			var state = stateMachine.AddState(instance.Name);
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

			return instance;
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
