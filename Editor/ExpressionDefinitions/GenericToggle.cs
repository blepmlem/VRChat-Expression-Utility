using System.Collections.Generic;
using ExpressionUtility.UI;
using UnityEditor;
using UnityEditor.Animations;
using UnityEngine;
using UnityEngine.UIElements;
using VRC.SDK3.Avatars.ScriptableObjects;
using static VRC.SDK3.Avatars.ScriptableObjects.VRCExpressionParameters;
using static VRC.SDK3.Avatars.ScriptableObjects.VRCExpressionsMenu.Control;
using Object = UnityEngine.Object;

namespace ExpressionUtility
{
	[CreateAssetMenu(fileName = nameof(GenericToggle), menuName = "Expression Utility/"+nameof(GenericToggle))]
	internal class GenericToggle : ExpressionUI, IExpressionDefinition
	{
		private ExpressionInfo _expressionInfo;
		private readonly List<Object> _dirtyAssets = new List<Object>();
		private bool _createAnimation = true;
		
		public override void OnEnter(UIController controller, ExpressionUI previousUI)
		{
			_dirtyAssets.Clear();
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
		
		public void Build()
		{
			var expName = _expressionInfo.ExpressionName;
			var controller = _expressionInfo.Controller;

			AnimatorControllerLayer layer = AnimUtility.AddLayer(controller, expName, _dirtyAssets);
			controller.AddParameter(expName, AnimatorControllerParameterType.Bool);
			
			AnimatorStateMachine stateMachine = layer.stateMachine;
			var empty = AnimUtility.AddState(stateMachine, "Empty", true, _dirtyAssets);
			
			AnimatorState toggleState = AnimUtility.AddState(stateMachine, expName, false, _dirtyAssets);

			if (_createAnimation)
			{
				var animationClip = AnimUtility.CreateAnimation(_expressionInfo.AnimationsFolder.GetPath(), expName, _dirtyAssets);
				toggleState.motion = animationClip;
			}
			
			AnimatorStateTransition anyStateTransition = stateMachine.AddAnyStateTransition(toggleState);
			anyStateTransition.AddCondition(AnimatorConditionMode.If, 1, expName);

			AnimatorStateTransition exitTransition = toggleState.AddExitTransition(false);
			exitTransition.AddCondition(AnimatorConditionMode.IfNot, 0, expName);

			AnimUtility.AddVRCExpressionsParameter(_expressionInfo.AvatarDescriptor, ValueType.Bool, expName, _dirtyAssets);
			AnimUtility.AddVRCExpressionsMenuControl(_expressionInfo.Menu, ControlType.Toggle, expName, _dirtyAssets);

			_dirtyAssets.SetDirty();
			controller.AddObjectsToAsset(stateMachine, toggleState, anyStateTransition, exitTransition, empty);
			AssetDatabase.SaveAssets();
			AssetDatabase.Refresh();
		}
	}
}