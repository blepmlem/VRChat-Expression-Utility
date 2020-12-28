using System;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.Animations;
using UnityEngine;
using VRC.SDK3.Avatars.ScriptableObjects;
using Object = UnityEngine.Object;

namespace ExpresionUtility
{
	[Serializable]
	internal class Expression
	{
		[field: SerializeField]
		public string Name { get; set; }
		
		[field:SerializeField]
		private AnimatorController Controller { get; set; }
		
		public int ParameterIndex => Controller.parameters.ToList().FindIndex(p => Name.Equals(p.name, StringComparison.OrdinalIgnoreCase));
		
		public int LayerIndex => Controller.layers.ToList().FindIndex(l => Name.Equals(l.name, StringComparison.OrdinalIgnoreCase));
		
		public AnimatorControllerLayer Layer => Controller.layers.FirstOrDefault(l => Name.Equals(l.name, StringComparison.OrdinalIgnoreCase));
		
		public VRCExpressionsMenu Menu =>  ExpressionWindow.Menus.FirstOrDefault(m => m.controls.Exists(c => c.parameter?.name == Name));

		public AnimationClip AnimationClip => ExpressionWindow.AnimationsFolder != null ? AssetDatabase.LoadAssetAtPath<AnimationClip>($"{AssetDatabase.GetAssetPath(ExpressionWindow.AnimationsFolder)}/{Controller.name}/{Name}.anim") : null;
		
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

				if (ExpressionWindow.AnimationsFolder != null)
				{
					var path = AssetDatabase.GetAssetPath(ExpressionWindow.AnimationsFolder);
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
			
			var parameters = ExpressionWindow.AvatarDescriptor.expressionParameters.parameters;
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
			
			SetDirty(stateMachine, builder.Controller, builder.Menu, ExpressionWindow.AvatarDescriptor.expressionParameters, ExpressionWindow.AvatarDescriptor.gameObject, state, exit, entry, empty);
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

			var parameters = ExpressionWindow.AvatarDescriptor.expressionParameters.parameters;
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

			SetDirty(Controller, Menu, ExpressionWindow.AvatarDescriptor.expressionParameters);
			
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
}