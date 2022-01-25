using System;
using System.Collections.Generic;
using System.Linq;
using ExpressionUtility.UI;
using UnityEditor;
using UnityEditor.Animations;
using UnityEditor.UIElements;
using UnityEngine;
using UnityEngine.UIElements;
using VRC.SDK3.Avatars.Components;
using VRC.SDK3.Avatars.ScriptableObjects;
using static VRC.SDK3.Avatars.ScriptableObjects.VRCExpressionsMenu.Control;
using Object = UnityEngine.Object;

namespace ExpressionUtility
{
	[CreateAssetMenu(fileName = nameof(ItemToggle), menuName = "Expression Utility/"+nameof(ItemToggle))]
	internal class ItemToggle : ExpressionUI, IExpressionDefinition
	{
		private UIController _controller;
		private ExpressionInfo _expressionInfo;
		private Transform _expressionObject;
		private readonly List<Object> _dirtyAssets = new List<Object>();

		public override void OnEnter(UIController controller, ExpressionUI previousUI)
		{
			_controller = controller;
			_expressionInfo = controller.ExpressionInfo;

			var finishButton = controller.ContentFrame.Q<Button>("button-finish");

			BuildExpressionObjectSelection(controller);

			finishButton.clickable = new Clickable(OnFinishClicked);

			void OnFinishClicked()
			{
				Build();
				controller.SetFrame<Finish>();
			}
			
			ErrorValidate();
		}

		private void BuildExpressionObjectSelection(UIController controller)
		{
			var expressionObject = controller.ContentFrame.Q<ObjectField>("expression-object");
			expressionObject.objectType = typeof(Transform);
			expressionObject.RegisterValueChangedCallback(e => SetObject(e.newValue as Transform));
			
			void SetObject(Transform value)
			{
				expressionObject.SetValueWithoutNotify(value);
				_expressionObject = value;
				ErrorValidate();
			}
			
			SetObject(_expressionObject);
		}

		private void ErrorValidate()
		{
			var finishButton = _controller.ContentFrame.Q<Button>("button-finish");
			
			bool isNotChild = _expressionObject != null && !_expressionInfo.AvatarDescriptor.GetComponentsInChildren<Transform>(true).Any(t => t == _expressionObject);
			bool isNull = _expressionObject == null;
			
			_controller.Messages.SetActive(isNotChild, "item-not-child-of-avatar");
			_controller.Messages.SetActive(isNull, "item-object-is-null");
			
			bool hasErrors = isNull || isNotChild;
			finishButton.SetEnabled(!hasErrors);
		}

		public void Build()
		{
			var expressionActiveState = _controller.ContentFrame.Q<Toggle>("expression-active-state");
			var expressionObject = _controller.ContentFrame.Q<ObjectField>("expression-object");
			
			var expName = _expressionInfo.ExpressionName;
			var controller = _expressionInfo.Controller;

			AnimatorControllerLayer layer = AnimUtility.AddLayer(controller, expName, _dirtyAssets);
			controller.AddParameter(expName, AnimatorControllerParameterType.Bool);
			
			AnimatorStateMachine stateMachine = layer.stateMachine;
			var empty = AnimUtility.AddState(stateMachine, "Empty", true, _dirtyAssets);
			
			AnimatorState toggleState = AnimUtility.AddState(stateMachine, expName, false, _dirtyAssets);

			var animationClip = AnimUtility.CreateAnimation(_expressionInfo.AnimationsFolder.GetPath(), expName, _dirtyAssets);
			toggleState.motion = animationClip;
			AddToggleKeyframes(animationClip, expressionObject.value as Transform, expressionActiveState.value, _dirtyAssets);
			
			AnimatorStateTransition anyStateTransition = stateMachine.AddAnyStateTransition(toggleState);
			anyStateTransition.AddCondition(AnimatorConditionMode.If, 1, expName);

			AnimatorStateTransition exitTransition = toggleState.AddExitTransition(false);
			exitTransition.AddCondition(AnimatorConditionMode.IfNot, 0, expName);

			AnimUtility.AddVRCExpressionsParameter(_expressionInfo.AvatarDescriptor, VRCExpressionParameters.ValueType.Bool, expName, _dirtyAssets);
			AnimUtility.AddVRCExpressionsMenuControl(_expressionInfo.Menu, ControlType.RadialPuppet, expName, _dirtyAssets);

			_dirtyAssets.SetDirty();
			controller.AddObjectsToAsset(stateMachine, toggleState, anyStateTransition, exitTransition, empty);
			AssetDatabase.SaveAssets();
			AssetDatabase.Refresh();
		}

		private void AddToggleKeyframes(AnimationClip animationClip, Transform target, bool expressionActiveState, List<Object> dirtyAssets)
		{
			var go = target.gameObject;
			Undo.RecordObject(go, $"Set expression starting state");
			go.SetActive(!expressionActiveState);
			AnimUtility.SetKeyframe(animationClip, target, "m_IsActive", expressionActiveState ? 1 : 0, dirtyAssets);
		}
	}
}