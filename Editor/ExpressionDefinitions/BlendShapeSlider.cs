using System;
using System.Collections.Generic;
using System.Linq;
using ExpressionUtility.UI;
using UnityEditor;
using UnityEditor.Animations;
using UnityEditor.UIElements;
using UnityEngine;
using UnityEngine.UIElements;
using VRC.SDK3.Avatars.ScriptableObjects;
using static VRC.SDK3.Avatars.ScriptableObjects.VRCExpressionsMenu.Control;
using Object = UnityEngine.Object;
// ReSharper disable PossibleMultipleEnumeration

namespace ExpressionUtility
{
	[CreateAssetMenu(fileName = nameof(MaterialSlider), menuName = "Expression Utility/"+nameof(BlendShapeSlider))]
	internal class BlendShapeSlider : ExpressionUI, IExpressionDefinition
	{
		private class BlendShapeControl
		{
			private readonly PopupField<int> _blendShapePicker;
			public VisualElement VisualElement { get; }
			public SkinnedMeshRenderer Renderer { get; private set; }
			public int Slot { get; private set; }
			public float MaxRange { get; private set; } = 100;
			
			public string AnimationAttribute => $"blendShape.{Name}";
			
			public string Name => Renderer.sharedMesh.GetBlendShapeName(Slot);

			public event Action OnDirty;

			public BlendShapeControl(VisualElement visualElement, BlendShapeControl last = null)
			{
				VisualElement = visualElement;
				var rendererField = VisualElement.Q<ObjectField>("target-renderer");
				var maxRangeField = VisualElement.Q<FloatField>("max-range");
				var pickerContainer = VisualElement.Q("blend-shape-picker-container");
				_blendShapePicker = new PopupField<int>(new List<int>(), 0)
				{
					label = "BlendShape",
				};
				pickerContainer.Add(_blendShapePicker);
				
				maxRangeField.RegisterValueChangedCallback(e =>
				{
					MaxRange = e.newValue;
					OnDirty?.Invoke();
				});
				
				rendererField.objectType = typeof(SkinnedMeshRenderer);
				rendererField.RegisterValueChangedCallback(e => Setup(e.newValue as SkinnedMeshRenderer));
				
				if (last != null)
				{
					rendererField.value = last.Renderer;
					_blendShapePicker.value = last.Slot;
				}
				OnDirty?.Invoke();
			}

			private void Setup(SkinnedMeshRenderer evtNewValue)
			{
				Renderer = evtNewValue;
				_blendShapePicker.Display(Renderer != null);
				_blendShapePicker.formatListItemCallback = null;
				_blendShapePicker.formatSelectedValueCallback = null;
				
				OnDirty?.Invoke();
				if (Renderer == null)
				{
					return;
				}
				
				var mesh = Renderer.sharedMesh;
				var count = mesh.blendShapeCount;
				var slots = Enumerable.Range(0, count).ToList();
				
				string PrettifyName(int arg) => $"{arg} ({mesh.GetBlendShapeName(arg)})";
				 
				_blendShapePicker.formatListItemCallback = PrettifyName;
				_blendShapePicker.formatSelectedValueCallback = PrettifyName;
				
				_blendShapePicker.choices = slots;
				_blendShapePicker.RegisterValueChangedCallback(e =>
				{
					Slot = e.newValue;
					OnDirty?.Invoke();
				});
				 
			}
			
			public bool HasErrors(Messages messages, ExpressionInfo info)
			{
				bool rendererIsNull = Renderer == null;
				bool rendererIsNotOwnedByAvatar = !rendererIsNull && info.AvatarDescriptor.GetComponentsInChildren<Renderer>(true)
				                                                         .All(r => r != Renderer);
				messages.SetActive(rendererIsNull, "blend-shape-renderer-is-null");
				messages.SetActive(rendererIsNotOwnedByAvatar, "renderer-is-not-owned");
				
				bool hasErrors = rendererIsNull || rendererIsNotOwnedByAvatar;
				return hasErrors;
			}
		}
		
		[SerializeField]
		private VisualTreeAsset _sliderAsset;
		
		private readonly List<BlendShapeControl> _blendShapeControls = new List<BlendShapeControl>();
		
		private ExpressionInfo _expressionInfo;
		private readonly List<Object> _dirtyAssets = new List<Object>();
		private Button _finishButton;
		private Messages _messages;
		
		private ScrollView _blendShapeScrollView;

		public override void OnEnter(UIController controller, ExpressionUI previousUI)
		{
			_expressionInfo = controller.ExpressionInfo;
			_messages = controller.Messages;
			_finishButton = controller.ContentFrame.Q<Button>("button-finish");
			_finishButton.clickable = new Clickable(OnFinishClicked);

			SetupTargetObjectsList(controller);

			void OnFinishClicked()
			{
				Build();
				controller.SetFrame<Finish>();
			}

			_messages.SetActive(true, "blend-shape-slider");
			ErrorValidate();
		}

		private void SetupTargetObjectsList(UIController controller)
		{
			void AddObject()
			{
				VisualElement obj = _sliderAsset.InstantiateTemplate(_blendShapeScrollView.contentContainer);
				var last = _blendShapeControls.LastOrDefault();
				var control = new BlendShapeControl(obj, last);
				control.OnDirty += ErrorValidate;
				_blendShapeControls.Add(control);
				ErrorValidate();
			}
		
			void RemoveObject()
			{
				var children = _blendShapeScrollView.contentContainer.Children();
				var last = children.LastOrDefault();
				if (last != null)
				{
					var control = _blendShapeControls.FirstOrDefault(b => b.VisualElement == last);
					if(control != null)
					{
						control.OnDirty -= ErrorValidate;
						_blendShapeControls.Remove(control);
					}
					_blendShapeScrollView.contentContainer.Remove(last);
				}
		
				if (!children.Any())
				{
					AddObject();
				}
				ErrorValidate();
			}
		
			_blendShapeControls.Clear();
			_blendShapeScrollView = controller.ContentFrame.Q<ScrollView>("blend-shape-list");
			var holder = controller.ContentFrame.Q("blend-shape");
			holder.Q<Button>("add").clickable.clicked += AddObject;
			holder.Q<Button>("remove").clickable.clicked += RemoveObject;
			AddObject();
		}
		

		public void Build()
		{
			var expName = _expressionInfo.ExpressionName;
			var controller = _expressionInfo.Controller;
			
			AnimatorControllerLayer layer = AnimUtility.AddLayer(controller, expName, _dirtyAssets);
			controller.AddParameter(expName, AnimatorControllerParameterType.Float);
			
			AnimatorStateMachine stateMachine = layer.stateMachine;
			var empty = AnimUtility.AddState(stateMachine, "Empty", true, _dirtyAssets);
			AnimatorState state = AnimUtility.AddState(stateMachine, expName, false, _dirtyAssets);
			
			var blendTree = new BlendTree
			{
				name = "BlendTree",
				blendParameter = expName,
			};
			state.motion = blendTree;
			_dirtyAssets.Add(blendTree);
			
			var directory = $"{_expressionInfo.AnimationsFolder.GetPath()}/{expName}";
			var emptyClip = AnimUtility.CreateAnimation(directory, $"{expName}_{empty}", _dirtyAssets);
			blendTree.AddChild(emptyClip);
			
			var animationClip = AnimUtility.CreateAnimation(directory, $"{expName} (BlendShape)", _dirtyAssets);
			foreach (BlendShapeControl control in _blendShapeControls)
			{
				var animAttribute = control.AnimationAttribute;
				AnimUtility.SetBlendShapeKeyframe(animationClip, control.Renderer, animAttribute, control.MaxRange, _dirtyAssets);
			}
			blendTree.AddChild(animationClip);
			_dirtyAssets.Add(animationClip);

			AnimatorStateTransition anyStateTransition = stateMachine.AddAnyStateTransition(state);
			anyStateTransition.AddCondition(AnimatorConditionMode.Greater, 0.01f, expName);
			
			AnimatorStateTransition exitTransition = state.AddExitTransition(false);
			exitTransition.AddCondition(AnimatorConditionMode.Less, 0.01f, expName);
			
			AnimUtility.AddVRCExpressionsParameter(_expressionInfo.AvatarDescriptor, VRCExpressionParameters.ValueType.Float, expName, _dirtyAssets);
			if(_expressionInfo.Menu != null)
			{
				AnimUtility.AddVRCExpressionsMenuControl(_expressionInfo.Menu, ControlType.RadialPuppet, expName, _dirtyAssets);
			}
			
			_dirtyAssets.SetDirty();
			controller.AddObjectsToAsset(stateMachine, empty, state, anyStateTransition, exitTransition, blendTree);
			AssetDatabase.SaveAssets();
			AssetDatabase.Refresh();
		}

		private void ErrorValidate()
		{
			bool controlErrors = _blendShapeControls.Any(c => c.HasErrors(_messages, _expressionInfo));
			bool multipleSameSlotsUsed = _blendShapeControls.GroupBy(c => new
			{
				c.Slot,
				c.Renderer,
			}).Any(g => g.Count() > 1);
			
			_messages.SetActive(multipleSameSlotsUsed, "multiple-same-blend-shape-slots-used");
			
			bool hasErrors = controlErrors | multipleSameSlotsUsed;
			_finishButton.SetEnabled(!hasErrors);
		}
	}
}