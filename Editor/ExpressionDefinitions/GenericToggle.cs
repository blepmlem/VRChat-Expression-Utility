using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using ExpressionUtility.UI;
using UnityEditor;
using UnityEditor.Animations;
using UnityEngine;
using UnityEngine.UIElements;
using VRC.SDK3.Avatars.Components;
using VRC.SDK3.Avatars.ScriptableObjects;
using static VRC.SDK3.Avatars.ScriptableObjects.VRCExpressionsMenu.Control;
using Object = UnityEngine.Object;

namespace ExpressionUtility
{
	internal class GenericToggle : IExpressionDefinition, IExpressionUI
	{
		private UIController _controller;
		private ExpressionInfo ExpressionInfo { get; set; }
		private string ExpressionName => ExpressionInfo.ExpressionName;
		private VRCExpressionsMenu Menu => ExpressionInfo.Menu;
		private AnimatorController Controller => ExpressionInfo.Controller;
		private VRCAvatarDescriptor AvatarDescriptor => ExpressionInfo.AvatarDescriptor;
		private List<Object> DirtyAssets { get; } = new List<Object>();


		public void OnEnter(UIController controller, IExpressionUI previousUI)
		{
			_controller = controller;
			ExpressionInfo = controller.ExpressionInfo;
			var nameField = controller.ContentFrame.Q<TextField>("name");
			var createAnimation = controller.ContentFrame.Q<Toggle>("create-animation");
			var finishButton = controller.ContentFrame.Q<Button>("button-finish");

			createAnimation.value = ExpressionInfo.CreateAnimations;
			nameField.value = ExpressionName;
			
			createAnimation.RegisterValueChangedCallback(evt => ExpressionInfo.CreateAnimations = evt.newValue);
			finishButton.clickable = new Clickable(OnFinishClicked);
			nameField.RegisterValueChangedCallback(e => ExpressionInfo.ExpressionName = e.newValue);

			void OnFinishClicked()
			{
				Build();
				controller.SetFrame<Finish>();
			}
		}

		private void ErrorValidate()
		{
			var finishButton = _controller.ContentFrame.Q<Button>("button-finish");
			bool layerNameExists = Controller.layers.Any(l => l.name.Equals(ExpressionName, StringComparison.InvariantCultureIgnoreCase));
			bool parameterExists = AvatarDescriptor.expressionParameters.parameters.Any(p => p.name.Equals(ExpressionName, StringComparison.InvariantCultureIgnoreCase));

			bool inUse = layerNameExists || parameterExists;
			finishButton.SetEnabled(!inUse && !string.IsNullOrEmpty(ExpressionName));

			if (string.IsNullOrEmpty(ExpressionName))
			{
				_controller.SetInfo($"Give your expression a name! This name will be used as the name for the expression itself, its layer, and the VRC parameter");
			}
			if (Menu == null)
			{
				_controller.SetInfo($"Pick which menu or submenu to put the expression");
			}
			else if (inUse)
			{
				_controller.SetError($"The name {ExpressionName} is already in use!");
			}
			else
			{
				_controller.SetInfo($"Give your expression a name, and ");
			}
		}

		public void OnExit(IExpressionUI nextUI)
		{
			
		}
		
		public void Build()
		{
			AnimatorControllerLayer layer = AddLayer(Controller, ExpressionName);
			Controller.AddParameter(ExpressionName, AnimatorControllerParameterType.Bool);
			
			AnimatorStateMachine stateMachine = layer.stateMachine;
			var empty = AddState(stateMachine, "Empty", isDefault: true);
			
			AnimatorState toggleState = AddState(stateMachine, ExpressionName);

			if (ExpressionInfo.CreateAnimations)
			{
				// var animationClip = CreateAnimation(AnimExpressionName);
			}
			
			AnimatorStateTransition anyStateTransition = stateMachine.AddAnyStateTransition(toggleState);
			anyStateTransition.AddCondition(AnimatorConditionMode.If, 1, ExpressionName);

			AnimatorStateTransition exitTransition = toggleState.AddExitTransition(false);
			exitTransition.AddCondition(AnimatorConditionMode.IfNot, 0, ExpressionName);

			AddVRCExpressionsParameter(AvatarDescriptor, ExpressionName);
			AddVRCExpressionsMenuControl(Menu, ExpressionName);

			DirtyAssets.SetDirty();
			Controller.AddObjectsToAsset(stateMachine, toggleState, anyStateTransition, exitTransition, empty);
			AssetDatabase.SaveAssets();
			AssetDatabase.Refresh();
		}
		
		private VRCExpressionsMenu.Control AddVRCExpressionsMenuControl(VRCExpressionsMenu menu, string parameterName)
		{
			var control = new VRCExpressionsMenu.Control
			{
				name = ObjectNames.NicifyVariableName(parameterName),
				parameter = new VRCExpressionsMenu.Control.Parameter{name = parameterName},
				type = ControlType.Toggle,
			};
			
			if (menu != null)
			{
				menu.controls.Add(control);
				DirtyAssets.Add(Menu);
			}
			
			return control;
		}
		
		private VRCExpressionParameters.Parameter AddVRCExpressionsParameter(VRCAvatarDescriptor avatarDescriptor, string name)
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

			DirtyAssets.Add(parameters);
			DirtyAssets.Add(avatarDescriptor.gameObject);
			return parameter;
		}
		
		private AnimatorState AddState(AnimatorStateMachine stateMachine, string stateName, bool isDefault = false)
		{
			$"Adding State: \"{stateName}\" to StateMachine: \"{stateMachine.name}\"".Log();
			AnimatorState state = stateMachine.AddState(stateName);
			if (isDefault)
			{
				stateMachine.defaultState = state;
			}

			DirtyAssets.Add(state);
			return state;
		}

		private AnimatorControllerLayer AddLayer(AnimatorController controller, string name)
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
			DirtyAssets.Add(stateMachine);
			return layer;
		}

		private AnimationClip CreateAnimation(string animationFolder, string name)
		{
			var animation = new AnimationClip { name = name };
			AssetDatabase.CreateAsset(animation, $"{animationFolder}/{Controller.name}/{name}.anim");
			DirtyAssets.Add(animation);
			return animation;
		}
	}
}