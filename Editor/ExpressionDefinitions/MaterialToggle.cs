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
	[CreateAssetMenu(fileName = nameof(MaterialToggle), menuName = "Expression Utility/"+nameof(MaterialToggle))]
	internal class MaterialToggle : ExpressionUI, IExpressionDefinition
	{
		private UIController _controller;
		private ExpressionInfo _expressionInfo;
		private Renderer _renderer;
		private int _materialSlot;
		private Material _material;
		private readonly List<Object> _dirtyAssets = new List<Object>();
		private Messages _messages;
		private Button _finishButton;

		public override void OnEnter(UIController controller, ExpressionUI previousUI)
		{
			_messages = controller.Messages;
			_controller = controller;
			_expressionInfo = controller.ExpressionInfo;

			_finishButton = controller.ContentFrame.Q<Button>("button-finish");

			SetupMaterialSelection(controller);
			SetupTargetRenderer(controller);
			SetupMaterialSelection(controller);

			_finishButton.clickable = new Clickable(OnFinishClicked);

			void OnFinishClicked()
			{
				Build();
				controller.SetFrame<Finish>();
			}
			
			ErrorValidate();
		}
		
		private void SetupTargetRenderer(UIController controller)
		{
			var rendererField = controller.ContentFrame.Q<ObjectField>("target-renderer");
			rendererField.objectType = typeof(Renderer);
			rendererField.allowSceneObjects = true;
			rendererField.RegisterValueChangedCallback(e => SetTargetRenderer(e.newValue as Renderer));
			
			void SetTargetRenderer(Renderer renderer)
			{
				rendererField.SetValueWithoutNotify(renderer);
				_renderer = renderer;
				SetupMaterialSlotPicker(controller);
				ErrorValidate();
			}

			SetTargetRenderer(_renderer);
		}
		
		private void SetupMaterialSlotPicker(UIController controller)
		{
			var holder = controller.ContentFrame.Q("material-slot");
			holder.Clear();
			
			if (_renderer == null)
			{
				return;
			}

			var materials = _renderer.sharedMaterials.ToList();
			var slots = new List<int>();
			for (var i = 0; i < materials.Count; i++)
			{
				slots.Add(i);
			}
			
			string PrettifyName(int arg) => $"{arg} ({materials[arg].name})";
			
			var selector = new PopupField<int>(slots, 0, PrettifyName, PrettifyName)
			{
				label = "Material slot",
			};

			selector.RegisterValueChangedCallback(e => SetMenu(e.newValue));
				
			void SetMenu(int obj)
			{
				selector.SetValueWithoutNotify(obj);
				_materialSlot = obj;
				ErrorValidate();
			}

			holder.Add(selector);
			SetMenu(_materialSlot);
		}
		
		private void SetupMaterialSelection(UIController controller)
		{
			var material = controller.ContentFrame.Q<ObjectField>("toggle-material");
			material.objectType = typeof(Material);
			material.RegisterValueChangedCallback(e => SetObject(e.newValue as Material));
			
			void SetObject(Material value)
			{
				material.SetValueWithoutNotify(value);
				_material = value;
				ErrorValidate();
			}

			SetObject(_material);
		}

		private void ErrorValidate()
		{
			bool rendererIsNull = _renderer == null;
			bool rendererIsNotOwnedByAvatar = !rendererIsNull && !_expressionInfo.AvatarDescriptor.GetComponentsInChildren<Renderer>(true).Any(t => t == _renderer);
			bool materialIsNull = _material == null;
			
			_messages.SetActive(rendererIsNull, "renderer-is-null");
			_messages.SetActive(rendererIsNotOwnedByAvatar, "renderer-is-not-owned");
			_messages.SetActive(!rendererIsNull && materialIsNull, "material-is-null");
			
			bool hasErrors = rendererIsNull || rendererIsNotOwnedByAvatar || materialIsNull;
			_finishButton.SetEnabled(!hasErrors);
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
			AnimUtility.SetObjectReferenceKeyframe(animationClip, _renderer, $"m_Materials.Array.data[{_materialSlot}]", _material, _dirtyAssets);
			_dirtyAssets.Add(animationClip);
			toggleState.motion = animationClip;

			AnimatorStateTransition anyStateTransition = stateMachine.AddAnyStateTransition(toggleState);
			anyStateTransition.AddCondition(AnimatorConditionMode.If, 1, expName);

			AnimatorStateTransition exitTransition = toggleState.AddExitTransition(false);
			exitTransition.AddCondition(AnimatorConditionMode.IfNot, 0, expName);

			AnimUtility.AddVRCExpressionsParameter(_expressionInfo.AvatarDescriptor, VRCExpressionParameters.ValueType.Bool, expName, _dirtyAssets);
			if(_expressionInfo.Menu != null)
			{
				AnimUtility.AddVRCExpressionsMenuControl(_expressionInfo.Menu, ControlType.Toggle, expName, _dirtyAssets);
			}

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