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

		public override void BindControls(VisualElement root)
		{
			base.BindControls(root);
		}

		public override void OnEnter(UIController controller, ExpressionUI previousUI)
		{
			_dirtyAssets.Clear();
			_expressionInfo = controller.ExpressionInfo;
			var createAnimation = controller.ContentFrame.Q<Toggle>("create-animation");
			var finishButton = controller.ContentFrame.Q<Button>("button-finish");

			createAnimation.value = _createAnimation;
			
			createAnimation.RegisterValueChangedCallback(evt => _createAnimation = evt.newValue);
			finishButton.clickable = new Clickable(OnFinishClicked);

			controller.Messages.SetActive(true, "generic-toggle-create-animation");
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

			var avatarDef = new AvatarDefinition(_expressionInfo.AvatarDescriptor);
			var animatorDef = avatarDef.FindDescendant<AnimatorDefinition>(_expressionInfo.Controller.name);

			animatorDef.AddParameter(expName, ParameterValueType.Bool);
			var layerDef = animatorDef.AddLayer(expName);
			var stateMachineDef = layerDef.AddStateMachine(expName);

			stateMachineDef.AddState("Empty", isDefault: true);

			var mainState = stateMachineDef.AddState(expName);

			var motion = mainState.AddMotion(expName);
			motion.

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
			if(_expressionInfo.Menu != null)
			{
				AnimUtility.AddVRCExpressionsMenuControl(_expressionInfo.Menu, ControlType.Toggle, expName, _dirtyAssets);
			}

			_dirtyAssets.SetDirty();
			controller.AddObjectsToAsset(stateMachine, toggleState, anyStateTransition, exitTransition, empty);
			AssetDatabase.SaveAssets();
			AssetDatabase.Refresh();
		}
	}
}