using System.Collections.Generic;
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
		private ExpressionInfo _expressionInfo;
		private readonly List<Object> _dirtyAssets = new List<Object>();
		private bool _createAnimation = true;
		
		public void OnEnter(UIController controller, IExpressionUI previousUI)
		{
			_expressionInfo = controller.ExpressionInfo;
			var createAnimation = controller.ContentFrame.Q<Toggle>("create-animation");
			var finishButton = controller.ContentFrame.Q<Button>("button-finish");

			createAnimation.value = _createAnimation;
			
			createAnimation.RegisterValueChangedCallback(evt => _createAnimation = evt.newValue);
			finishButton.clickable = new Clickable(OnFinishClicked);

			void OnFinishClicked()
			{
				Build();
				controller.SetFrame<Finish>();
			}
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

			if (_createAnimation)
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