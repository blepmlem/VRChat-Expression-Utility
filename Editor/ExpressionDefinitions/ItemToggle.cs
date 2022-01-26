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
		[SerializeField]
		private VisualTreeAsset _targetObjectAsset;
		
		private UIController _controller;
		private ExpressionInfo _expressionInfo;
		private readonly List<Object> _dirtyAssets = new List<Object>();
		private ScrollView _targetObjectsScrollView;

		public override void OnEnter(UIController controller, ExpressionUI previousUI)
		{
			_controller = controller;
			_expressionInfo = controller.ExpressionInfo;

			var finishButton = controller.ContentFrame.Q<Button>("button-finish");
			finishButton.clickable = new Clickable(OnFinishClicked);

			SetupTargetObjectsList(controller);


			void OnFinishClicked()
			{
				Build();
				controller.SetFrame<Finish>();
			}
			
			ErrorValidate();
		}

		private void SetupTargetObjectsList(UIController controller)
		{
			void AddObject()
			{
				var obj = _targetObjectAsset.InstantiateTemplate(_targetObjectsScrollView.contentContainer);
				var objectField = obj.Q<ObjectField>("target-object");
				objectField.allowSceneObjects = true;
				objectField.objectType = typeof(Transform);

				objectField.RegisterValueChangedCallback(e => ErrorValidate());
				ErrorValidate();
			}

			void RemoveObject()
			{
				var children = _targetObjectsScrollView.contentContainer.Children();
				var last = children.LastOrDefault();
				if (last != null)
				{
					_targetObjectsScrollView.contentContainer.Remove(last);
				}

				if (!children.Any())
				{
					AddObject();
				}
				ErrorValidate();
			}
			
			var holder = controller.ContentFrame.Q("objects");
			_targetObjectsScrollView = holder.Q<ScrollView>("objects-list");
			holder.Q<Button>("add").clickable.clicked += AddObject;
			holder.Q<Button>("remove").clickable.clicked += RemoveObject;
			AddObject();
		}

		public IEnumerable<(Transform transform, bool isActive)> GetObjects()
		{
			var children = _targetObjectsScrollView.contentContainer.Children();
			foreach (VisualElement e in children)
			{
				var obj = e.Q<ObjectField>("target-object").value as Transform;
				var state = e.Q<Toggle>("target-active-state").value;
				yield return (obj, state);
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

			var animationClip = AnimUtility.CreateAnimation(_expressionInfo.AnimationsFolder.GetPath(), expName, _dirtyAssets);
			toggleState.motion = animationClip;
			
			foreach (var obj in GetObjects())
			{
				AddToggleKeyframes(animationClip, obj.transform, obj.isActive, _dirtyAssets);
			}
			
			AnimatorStateTransition anyStateTransition = stateMachine.AddAnyStateTransition(toggleState);
			anyStateTransition.AddCondition(AnimatorConditionMode.If, 1, expName);

			AnimatorStateTransition exitTransition = toggleState.AddExitTransition(false);
			exitTransition.AddCondition(AnimatorConditionMode.IfNot, 0, expName);

			AnimUtility.AddVRCExpressionsParameter(_expressionInfo.AvatarDescriptor, VRCExpressionParameters.ValueType.Bool, expName, _dirtyAssets);
			AnimUtility.AddVRCExpressionsMenuControl(_expressionInfo.Menu, ControlType.Toggle, expName, _dirtyAssets);

			_dirtyAssets.SetDirty();
			controller.AddObjectsToAsset(stateMachine, toggleState, anyStateTransition, exitTransition, empty);
			AssetDatabase.SaveAssets();
			AssetDatabase.Refresh();
		}

		private void AddToggleKeyframes(AnimationClip animationClip, Transform target, bool expressionActiveState, List<Object> dirtyAssets)
		{
			AnimUtility.SetKeyframe(animationClip, target, "m_IsActive", expressionActiveState ? 1 : 0, dirtyAssets);
		}

		private void ErrorValidate()
		{
			var finishButton = _controller.ContentFrame.Q<Button>("button-finish");

			var ownerTransforms = _expressionInfo.AvatarDescriptor.GetComponentsInChildren<Transform>(true);
			var children = GetObjects().ToList();

			bool childNull = children.Any(c => c.transform == null);
			bool isNotChild = children.Select(c => c.transform).Except(ownerTransforms).Any(t => t != null);

			_controller.Messages.SetActive(isNotChild, "item-not-child-of-avatar");
			_controller.Messages.SetActive(childNull, "item-object-is-null");
			
			bool hasErrors = childNull || isNotChild;
			finishButton.SetEnabled(!hasErrors);
		}
	}
}