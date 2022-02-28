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
			
			var avatar = new AvatarDefinition(_expressionInfo.AvatarDescriptor);
			avatar.AddChild(new VrcParameterDefinition(expName, ParameterValueType.Bool));
			
			avatar.FindAncestor<MenuDefinition>(_expressionInfo.Menu.NotNull()?.name)?.AddChild(new MenuControlDefinition(expName, ControlType.Toggle));

			var animator = avatar.FindDescendant<AnimatorDefinition>(_expressionInfo.Controller.name);
			animator.AddChild(new AnimatorParameterDefinition(expName, ParameterValueType.Bool));
			var stateMachine = animator.AddChild(new AnimatorLayerDefinition(expName))
			                           .AddChild(new StateMachineDefinition(expName));
			stateMachine.DefaultState = stateMachine.AddChild(new StateDefinition("Empty"));
			var mainState = stateMachine.AddChild(new StateDefinition(expName));
			var motion = mainState.AddChild(new MotionDefinition(expName, MotionDefinition.MotionType.AnimationClip));


			if (_createAnimation)
			{
				// motion.AddChild(new KeyframeDefinition())
			}
			
			// if (_createAnimation)
			// {
				var animationClip = AnimUtility.CreateAnimation(_expressionInfo.AnimationsFolder.GetPath(), expName, _dirtyAssets);
			// 	toggleState.motion = animationClip;
			// }

			stateMachine.AddChild(new TransitionDefinition(stateMachine.Any, mainState))
			            .AddChild(new ConditionDefinition(expName, AnimatorConditionMode.If, 1));
			stateMachine.AddChild(new TransitionDefinition(mainState, stateMachine.Exit))
			            .AddChild(new ConditionDefinition(expName, AnimatorConditionMode.IfNot, 0));
		}
	}
}