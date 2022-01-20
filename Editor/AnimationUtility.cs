using System.Collections.Generic;
using System.Linq;
using ExpressionUtility;
using UnityEditor;
using UnityEditor.Animations;
using UnityEngine;
using VRC.SDK3.Avatars.Components;
using VRC.SDK3.Avatars.ScriptableObjects;

namespace ExpressionUtility
{
	public static class AnimationUtility
	{
		public static VRCExpressionsMenu.Control AddVRCExpressionsMenuControl(VRCExpressionsMenu menu, string parameterName, List<Object> dirtyAssets)
		{
			var control = new VRCExpressionsMenu.Control
			{
				name = ObjectNames.NicifyVariableName(parameterName),
				parameter = new VRCExpressionsMenu.Control.Parameter{name = parameterName},
				type = VRCExpressionsMenu.Control.ControlType.Toggle,
			};
			
			if (menu != null)
			{
				menu.controls.Add(control);
				dirtyAssets.Add(menu);
			}
			
			return control;
		}
		
		public static VRCExpressionParameters.Parameter AddVRCExpressionsParameter(VRCAvatarDescriptor avatarDescriptor, string name, List<Object> dirtyAssets)
		{
			VRCExpressionParameters parameters = avatarDescriptor.expressionParameters;
			VRCExpressionParameters.Parameter parameter = parameters.FindParameter(name);

			if (parameter == null)
			{
				var list = parameters.parameters.Where(e => e != null && !string.IsNullOrEmpty(e.name)).ToList();
				parameter = new VRCExpressionParameters.Parameter
				{
					name = name,
					valueType = VRCExpressionParameters.ValueType.Bool,
				};
				list.Add(parameter);
				parameters.parameters = list.ToArray();
			}

			dirtyAssets.Add(parameters);
			dirtyAssets.Add(avatarDescriptor.gameObject);
			return parameter;
		}
		
		public static AnimatorState AddState(AnimatorStateMachine stateMachine, string stateName, bool isDefault, List<Object> dirtyAssets)
		{
			$"Adding State: \"{stateName}\" to StateMachine: \"{stateMachine.name}\"".Log();
			AnimatorState state = stateMachine.AddState(stateName);
			if (isDefault)
			{
				stateMachine.defaultState = state;
			}

			dirtyAssets.Add(state);
			return state;
		}

		public static AnimatorControllerLayer AddLayer(AnimatorController controller, string name, List<Object> dirtyAssets)
		{
			$"Adding Layer: \"{name}\" to Controller: \"{controller.name}\"".Log();
			var stateMachine = new AnimatorStateMachine { name = name };
			var layer = new AnimatorControllerLayer
			{
				name = name,
				defaultWeight = 1f,
				stateMachine = stateMachine,
			};

			controller.AddLayer(layer);
			dirtyAssets.Add(stateMachine);
			return layer;
		}

		public static AnimationClip CreateAnimation(DefaultAsset animationFolder, string name, List<Object> dirtyAssets)
		{
			var folderPath = AssetDatabase.GetAssetPath(animationFolder);
			var animation = new AnimationClip { name = name };
			AssetDatabase.CreateAsset(animation, $"{folderPath}/{name}.anim");
			dirtyAssets.Add(animation);
			return animation;
		}
	}
}