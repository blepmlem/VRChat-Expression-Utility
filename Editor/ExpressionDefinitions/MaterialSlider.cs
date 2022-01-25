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
using static VRC.SDK3.Avatars.ScriptableObjects.VRCExpressionParameters;
using static VRC.SDK3.Avatars.ScriptableObjects.VRCExpressionsMenu.Control;
using Object = UnityEngine.Object;

namespace ExpressionUtility
{
	[CreateAssetMenu(fileName = nameof(MaterialSlider), menuName = "Expression Utility/"+nameof(MaterialSlider))]
	internal class MaterialSlider : ExpressionUI, IExpressionDefinition
	{
		private ExpressionInfo _expressionInfo;
		private readonly List<Object> _dirtyAssets = new List<Object>();
		private ScrollView _materialScrollView;
		private Renderer _renderer;
		private int _materialSlot;
		private Button _finishButton;
		private Messages _messages;

		public override void OnEnter(UIController controller, ExpressionUI previousUI)
		{
			_expressionInfo = controller.ExpressionInfo;
			_messages = controller.Messages;
			_finishButton = controller.ContentFrame.Q<Button>("button-finish");
			_finishButton.clickable = new Clickable(OnFinishClicked);

			SetupMaterialList(controller);
			SetupTargetRenderer(controller);

			void OnFinishClicked()
			{
				Build();
				controller.SetFrame<Finish>();
			}

			ErrorValidate();
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

			SetMenu(_materialSlot);
			holder.Add(selector);
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


		private void SetupMaterialList(UIController controller)
		{
			void AddMaterial()
			{
				var objectField = new ObjectField
				{
					objectType = typeof(Material),
					allowSceneObjects = false,
				};
				_materialScrollView.contentContainer.Add(objectField);
				objectField.RegisterValueChangedCallback(e => ErrorValidate());
				ErrorValidate();
			}

			void RemoveMaterial()
			{
				var children = _materialScrollView.contentContainer.Children();
				var last = children.LastOrDefault();
				if (last != null)
				{
					_materialScrollView.contentContainer.Remove(last);
				}

				if (!children.Any())
				{
					AddMaterial();
				}
				ErrorValidate();
			}
			
			var materialsHolder = controller.ContentFrame.Q("materials");
			_materialScrollView = materialsHolder.Q<ScrollView>("material-list");
			materialsHolder.Q<Button>("add").clickable.clicked += AddMaterial;
			materialsHolder.Q<Button>("remove").clickable.clicked += RemoveMaterial;
			AddMaterial();
		}

		private IEnumerable<Material> GetMaterials()
		{
			var objectFields = _materialScrollView.contentContainer.Children().OfType<ObjectField>();
			foreach (ObjectField objectField in objectFields)
			{
				yield return objectField.value as Material;
			}
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
			
			var materials = GetMaterials().ToList();

			var directory = $"{_expressionInfo.AnimationsFolder.GetPath()}/{expName}";
			var emptyClip = AnimUtility.CreateAnimation(directory, $"{expName}_{empty}", _dirtyAssets);
			blendTree.AddChild(emptyClip);
			
			for (var i = 0; i < materials.Count; i++)
			{
				Material material = materials[i];
				var animationClip = AnimUtility.CreateAnimation(directory, $"{expName} [{i}] {material.name}", _dirtyAssets);
				AnimUtility.SetObjectReferenceKeyframe(animationClip, _renderer, $"m_Materials.Array.data[{_materialSlot}]", material, _dirtyAssets);
				blendTree.AddChild(animationClip);
				_dirtyAssets.Add(animationClip);
			}

			AnimatorStateTransition anyStateTransition = stateMachine.AddAnyStateTransition(state);
			anyStateTransition.AddCondition(AnimatorConditionMode.Greater, 0.01f, expName);

			AnimatorStateTransition exitTransition = state.AddExitTransition(false);
			exitTransition.AddCondition(AnimatorConditionMode.Less, 0.01f, expName);

			AnimUtility.AddVRCExpressionsParameter(_expressionInfo.AvatarDescriptor, VRCExpressionParameters.ValueType.Float, expName, _dirtyAssets);
			AnimUtility.AddVRCExpressionsMenuControl(_expressionInfo.Menu, ControlType.RadialPuppet, expName, _dirtyAssets);

			_dirtyAssets.SetDirty();
			controller.AddObjectsToAsset(stateMachine, empty, state, anyStateTransition, exitTransition, blendTree);
			AssetDatabase.SaveAssets();
			AssetDatabase.Refresh();
		}

		private void ErrorValidate()
		{
			bool rendererIsNull = _renderer == null;
			bool rendererIsNotOwnedByAvatar = !rendererIsNull && !_expressionInfo.AvatarDescriptor.GetComponentsInChildren<Renderer>(true).Any(r => r == _renderer);
			bool materialIsNull = GetMaterials().Any(m => m == null);

			_messages.SetActive(rendererIsNull, "renderer-is-null");
			_messages.SetActive(rendererIsNotOwnedByAvatar, "renderer-is-not-owned");
			_messages.SetActive(!rendererIsNull && materialIsNull, "material-is-null");
			
			bool hasErrors = rendererIsNull || rendererIsNotOwnedByAvatar || materialIsNull;
			_finishButton.SetEnabled(!hasErrors);
		}
	}
}