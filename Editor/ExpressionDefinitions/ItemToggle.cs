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
using Object = UnityEngine.Object;

namespace ExpressionUtility
{
	internal class ItemToggle : IExpressionDefinition, IExpressionUI
	{
		private UIController _controller;
		private ExpressionInfo _expressionInfo;
		private readonly List<Object> _dirtyAssets = new List<Object>();
		
		public void OnEnter(UIController controller, IExpressionUI previousUI)
		{
			_controller = controller;
			_expressionInfo = controller.ExpressionInfo;
			
			var nameField = controller.ContentFrame.Q<TextField>("name");
			var finishButton = controller.ContentFrame.Q<Button>("button-finish");
			var expressionObject = controller.ContentFrame.Q<ObjectField>("expression-object");

			expressionObject.objectType = typeof(Transform);
			expressionObject.RegisterValueChangedCallback(OnExpressionObjectChanged);
			
			finishButton.clickable = new Clickable(OnFinishClicked);
			nameField.value = _expressionInfo.ExpressionName;

			nameField.RegisterValueChangedCallback(e => _expressionInfo.ExpressionName = e.newValue);

			void OnFinishClicked()
			{
				Build();
				controller.SetFrame<Finish>();
			}
			
			finishButton.SetEnabled(false);
		}

		private void OnExpressionObjectChanged(ChangeEvent<Object> evt)
		{
			var value = evt.newValue as Transform;
			var finishButton = _controller.ContentFrame.Q<Button>("button-finish");
			if (value == null)
			{
				finishButton.SetEnabled(false);
				return;
			}

			bool isChildOfAvatar = _expressionInfo.AvatarDescriptor.GetComponentsInChildren<Transform>(true).Any(t => t == value);
			_controller.Messages.SetActive(!isChildOfAvatar, "item-not-child-of-avatar");
			finishButton.SetEnabled(isChildOfAvatar);
		}

		public void OnExit(IExpressionUI nextUI)
		{
			
		}
		
		public void Build()
		{
			var expressionActiveState = _controller.ContentFrame.Q<Toggle>("expression-active-state");
			var expressionObject = _controller.ContentFrame.Q<ObjectField>("expression-object");
			
			var expName = _expressionInfo.ExpressionName;
			var controller = _expressionInfo.Controller;

			AnimatorControllerLayer layer = AnimationUtility.AddLayer(controller, expName, _dirtyAssets);
			controller.AddParameter(expName, AnimatorControllerParameterType.Bool);
			
			AnimatorStateMachine stateMachine = layer.stateMachine;
			var empty = AnimationUtility.AddState(stateMachine, "Empty", true, _dirtyAssets);
			
			AnimatorState toggleState = AnimationUtility.AddState(stateMachine, expName, false, _dirtyAssets);

			var animationClip = AnimationUtility.CreateAnimation(_expressionInfo.AnimationsFolder, expName, _dirtyAssets);
			toggleState.motion = animationClip;
			AddToggleKeyframes(animationClip, expressionActiveState.value, expressionObject.value as Transform, _dirtyAssets);
			
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

		private void AddToggleKeyframes(AnimationClip animationClip, bool expressionActiveState, Transform expressionObject, List<Object> dirtyAssets)
		{
			var go = expressionObject.gameObject;
			Undo.RecordObject(go, $"Set expression starting state");
			go.SetActive(!expressionActiveState);
			
			var keyframe = new Keyframe(0, expressionActiveState ? 1 : 0);
			var curve = new AnimationCurve(keyframe);
			var path = $"{expressionObject.name}";
			var attribute = "m_IsActive";

			Transform target = expressionObject;

			while (true)
			{
				target = target.parent;
				if (target == null || target.GetComponent<VRCAvatarDescriptor>())
				{
					break;
				}
				path = $"{target.name}/{path}";
			}
			
			animationClip.SetCurve(path, typeof(GameObject),attribute, curve);
			
			dirtyAssets.Add(go);
			dirtyAssets.Add(animationClip);
		}
	}
}