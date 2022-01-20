using System;
using System.Collections.Generic;
using System.Linq;
using ExpressionUtility.UI;
using UnityEditor;
using UnityEditor.Animations;
using UnityEngine;
using UnityEngine.UIElements;
using Object = UnityEngine.Object;

namespace ExpressionUtility
{
	internal class GenericToggle : IExpressionDefinition, IExpressionUI
	{
		private UIController _controller;
		private ExpressionInfo _expressionInfo;
		private readonly List<Object> _dirtyAssets = new List<Object>();
		
		public void OnEnter(UIController controller, IExpressionUI previousUI)
		{
			_controller = controller;
			_expressionInfo = controller.ExpressionInfo;
			var nameField = controller.ContentFrame.Q<TextField>("name");
			var createAnimation = controller.ContentFrame.Q<Toggle>("create-animation");
			var finishButton = controller.ContentFrame.Q<Button>("button-finish");

			createAnimation.value = _expressionInfo.CreateAnimations;
			nameField.value = _expressionInfo.ExpressionName;
			
			createAnimation.RegisterValueChangedCallback(evt => _expressionInfo.CreateAnimations = evt.newValue);
			finishButton.clickable = new Clickable(OnFinishClicked);
			nameField.RegisterValueChangedCallback(e => _expressionInfo.ExpressionName = e.newValue);
			ErrorValidate();

			_expressionInfo.DataWasUpdated += e =>ErrorValidate();
			void OnFinishClicked()
			{
				Build();
				controller.SetFrame<Finish>();
			}
		}

		private void ErrorValidate()
		{
			var finishButton = _controller.ContentFrame.Q<Button>("button-finish");
			bool layerNameExists = _expressionInfo.Controller.layers.Any(l => l.name.Equals(_expressionInfo.ExpressionName, StringComparison.InvariantCultureIgnoreCase));
			bool parameterExists = _expressionInfo.AvatarDescriptor.expressionParameters.parameters.Any(p => p.name.Equals(_expressionInfo.ExpressionName, StringComparison.InvariantCultureIgnoreCase));

			bool isEmpty = string.IsNullOrEmpty(_expressionInfo.ExpressionName);
			bool inUse = !isEmpty && (layerNameExists || parameterExists);
			finishButton?.SetEnabled(!inUse && !string.IsNullOrEmpty(_expressionInfo.ExpressionName));

			_controller.Messages.SetActive(isEmpty, "give-expr-name");
			_controller.Messages.SetActive(inUse, "expr-in-use");
		}

		public void OnExit(IExpressionUI nextUI)
		{
			
		}
		
		public void Build()
		{
			var expName = _expressionInfo.ExpressionName;
			var controller = _expressionInfo.Controller;

			AnimatorControllerLayer layer = AnimationUtility.AddLayer(controller, expName, _dirtyAssets);
			controller.AddParameter(expName, AnimatorControllerParameterType.Bool);
			
			AnimatorStateMachine stateMachine = layer.stateMachine;
			var empty = AnimationUtility.AddState(stateMachine, "Empty", true, _dirtyAssets);
			
			AnimatorState toggleState = AnimationUtility.AddState(stateMachine, expName, false, _dirtyAssets);

			if (_expressionInfo.CreateAnimations)
			{
				var animationClip = AnimationUtility.CreateAnimation(_expressionInfo.AnimationsFolder, expName, _dirtyAssets);
				toggleState.motion = animationClip;
			}
			
			AnimatorStateTransition anyStateTransition = stateMachine.AddAnyStateTransition(toggleState);
			anyStateTransition.AddCondition(AnimatorConditionMode.If, 1, expName);

			AnimatorStateTransition exitTransition = toggleState.AddExitTransition(false);
			exitTransition.AddCondition(AnimatorConditionMode.IfNot, 0, expName);

			AnimationUtility.AddVRCExpressionsParameter(_expressionInfo.AvatarDescriptor, expName, _dirtyAssets);
			AnimationUtility.AddVRCExpressionsMenuControl(_expressionInfo.Menu, expName, _dirtyAssets);

			_dirtyAssets.SetDirty();
			controller.AddObjectsToAsset(stateMachine, toggleState, anyStateTransition, exitTransition, empty);
			AssetDatabase.SaveAssets();
			AssetDatabase.Refresh();
		}
	}
}