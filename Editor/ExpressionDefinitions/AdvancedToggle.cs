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

namespace ExpressionUtility
{
	[CreateAssetMenu(fileName = nameof(AdvancedToggle), menuName = "Expression Utility/"+nameof(AdvancedToggle))]
	internal class AdvancedToggle : ExpressionUI, IExpressionDefinition
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

		private void SetupListItem(VisualElement listItem)
		{
			var objectField = listItem.Q<ObjectField>("target-object");
			objectField.RegisterValueChangedCallback(e =>
			{
				SetupListMaterialSlotPicker();
				SetupBlendShapeParameterField();
				ErrorValidate();
			});
			objectField.allowSceneObjects = true;
			var type = listItem.Q<EnumField>(_targetTypeName);

			var materialField = listItem.Q<ObjectField>(_targetMaterialName);
			materialField.objectType = typeof(Material);
			materialField.RegisterValueChangedCallback(e => ErrorValidate());
			
			type.RegisterValueChangedCallback(e =>
			{
				SetupVRCParameterField();
				SetupBlendShapeParameterField();
				ShowTypeFields(e.newValue);
			});
			
			ShowTypeFields(type.value);

			void SetupBlendShapeParameterField()
			{
				var pickerHolder = listItem.Q("blend-shape-parameters").Q("parameters-picker");
				var valueSlider = listItem.Q<Slider>(_blendShapeParameterValueImplementationName);
				
				pickerHolder.Clear();

				var selector = new PopupField<int>
				{
					name = _blendShapeParameterName,
					label = "Blend Shape",
				};
				pickerHolder.Add(selector);

				if(objectField.value is not SkinnedMeshRenderer renderer)
				{
					valueSlider.Display(false);
					selector.Display(false);
					return;
				}
				
				valueSlider.Display(true);
				selector.Display(true);
				var mesh = renderer.sharedMesh;
				var count = mesh.blendShapeCount;
				var slots = Enumerable.Range(0, count).ToList();
				
				string PrettifyName(int arg) => $"{arg} ({mesh.GetBlendShapeName(arg)})";
				 
				selector.formatListItemCallback = PrettifyName;
				selector.formatSelectedValueCallback = PrettifyName;
				
				selector.choices = slots;
				selector.RegisterValueChangedCallback(e =>
				{
					ErrorValidate();
				});
			}

			void SetupVRCParameterField()
			{
				var pickerHolder = listItem.Q("parameters").Q("parameters-picker");
				var valueHolder = listItem.Q("parameters").Q("parameters-value");
				pickerHolder.Clear();

				VRCExpressionParameters.Parameter[] parameters = _controller.ExpressionInfo.AvatarDescriptor.expressionParameters.parameters;
				var slots = new List<VRCExpressionParameters.Parameter>();
				slots.AddRange(parameters);
			
				string PrettifyName(VRCExpressionParameters.Parameter arg) => $"{arg.name} ({arg.valueType})";
			
				var selector = new PopupField<VRCExpressionParameters.Parameter>(slots, 0, PrettifyName, PrettifyName)
				{
					name = _vrcParameterName,
					label = "VRC Parameter"
				};

				selector.RegisterValueChangedCallback(e => SetValue(e.newValue));
				
				void SetValue(VRCExpressionParameters.Parameter obj)
				{
					selector.SetValueWithoutNotify(obj);
					valueHolder.Clear();

					VisualElement valueElement;
					switch (obj.valueType)
					{
						case VRCExpressionParameters.ValueType.Int:
							valueElement = new IntegerField{label = "Toggled value"};
							break;
						case VRCExpressionParameters.ValueType.Float:
							valueElement = new FloatField{label = "Toggled value"};
							break;
						case VRCExpressionParameters.ValueType.Bool:
							valueElement = new Toggle{label = "Toggled state"};
							break;
						default:
							throw new ArgumentOutOfRangeException();
					}

					valueElement.name = _vrcParameterValueImplementationName;
					valueHolder.Add(valueElement);
					ErrorValidate();
				}

				SetValue(selector.value);
				pickerHolder.Add(selector);
			}

			void SetupListMaterialSlotPicker()
			{
				var holder = listItem.Q(_materialSlotName);
				holder.Clear();
			
				if (objectField.value is not Renderer renderer || renderer is SkinnedMeshRenderer)
				{
					return;
				}

				var materials = renderer.sharedMaterials.ToList();
				var slots = new List<int>();
				for (var i = 0; i < materials.Count; i++)
				{
					slots.Add(i);
				}
			
				string PrettifyName(int arg) => $"{arg} ({materials[arg].name})";
			
				var selector = new PopupField<int>(slots, 0, PrettifyName, PrettifyName)
				{
					tooltip = "Material slot",
				};

				selector.RegisterValueChangedCallback(e => SetMaterial(e.newValue));
				
				void SetMaterial(int obj)
				{
					selector.SetValueWithoutNotify(obj);
					ErrorValidate();
				}

				SetMaterial(selector.value);
				holder.Add(selector);
			}
			
			void ShowTypeFields(Enum enumValue)
			{
				var value = (AdvancedToggleObjectMode) enumValue;
				var activeToggle = listItem.Q(_targetActiveStateName);
				var vrcParameter = listItem.Q("parameters");
				var blendShapeParameter = listItem.Q("blend-shape-parameters");

				activeToggle.Display(false);
				materialField.Display(false);
				objectField.Display(false);
				vrcParameter.Display(false);
				blendShapeParameter.Display(false);
				
				switch (value)
				{
					case AdvancedToggleObjectMode.GameObject:
						objectField.label = "Target Object";
						activeToggle.Display(true);
						objectField.Display(true);
						objectField.objectType = typeof(Transform);
						objectField.value = null;
						break;
					case AdvancedToggleObjectMode.Material:
						objectField.label = "Target Renderer";
						materialField.Display(true);
						objectField.Display(true);
						objectField.objectType = typeof(Renderer);
						objectField.value = null;
						break;
					case AdvancedToggleObjectMode.Parameter:
						vrcParameter.Display(true);
						break;
					case AdvancedToggleObjectMode.BlendShape:
						objectField.Display(true);
						objectField.objectType = typeof(SkinnedMeshRenderer);
						blendShapeParameter.Display(true);
						break;
					default:
						throw new ArgumentOutOfRangeException();
				}
				
				ErrorValidate();
			}
		}

		private void SetupTargetObjectsList(UIController controller)
		{
			void AddObject()
			{
				var obj = _targetObjectAsset.InstantiateTemplate(_targetObjectsScrollView.contentContainer);
				SetupListItem(obj);
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

		private IEnumerable<ObjectData> GetObjects()
		{
			var children = _targetObjectsScrollView.contentContainer.Children();
			foreach (VisualElement e in children)
			{
				yield return new ObjectData(e);
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
				switch (obj.Type)
				{
					case AdvancedToggleObjectMode.GameObject:
						AddToggleKeyframes(animationClip, obj.Target as Transform, obj.ToggleState, _dirtyAssets);
						break;
					case AdvancedToggleObjectMode.Material:
						AnimUtility.SetObjectReferenceKeyframe(animationClip, obj.Target, $"m_Materials.Array.data[{obj.MaterialSlot}]", obj.NewMaterial, _dirtyAssets);
						break;
					case AdvancedToggleObjectMode.BlendShape:
						AnimUtility.SetBlendShapeKeyframe(animationClip, obj.Target, obj.BlendShapeAttributeName, obj.BlendShapeParameterValue, _dirtyAssets);
						break;
				}
			}
			
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
			
			_dirtyAssets.Clear();
			foreach (ObjectData obj in GetObjects().Where(o => o.Type == AdvancedToggleObjectMode.Parameter))
			{
				AnimUtility.AddVRCParameterDriver(toggleState, obj.VRCParameterName, obj.VRCParameterValue, _dirtyAssets);
			}
			_dirtyAssets.SetDirty();
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

			bool childNull = children.Any(c => c.Target == null && c.Type != AdvancedToggleObjectMode.Parameter);
			bool isNotChild = children.Where(o => o.Target != null).Select(c => c.Target.transform).Except(ownerTransforms).Any(t => t != null);

			bool materialChildrenNull = children.Any(o => o.Type == AdvancedToggleObjectMode.Material && o.NewMaterial == null);
			bool rendererIsNull = children.Any(o => o.Type == AdvancedToggleObjectMode.Material && o.Target == null);
			bool modifiedParameters = children.Any(o => o.Type == AdvancedToggleObjectMode.Parameter);
			bool blendShapeParameters = children.Any(o => o.Type == AdvancedToggleObjectMode.BlendShape);
			
			_controller.Messages.SetActive(blendShapeParameters, "blend-shape-parameters-active");
			_controller.Messages.SetActive(modifiedParameters, "modified-parameters");
			_controller.Messages.SetActive(rendererIsNull, "renderer-is-null");
			_controller.Messages.SetActive(!rendererIsNull && materialChildrenNull, "material-is-null");
			_controller.Messages.SetActive(isNotChild, "item-not-child-of-avatar");
			_controller.Messages.SetActive(true, "item-toggle-info");
			
			bool hasErrors = childNull || isNotChild || materialChildrenNull;
			finishButton.SetEnabled(!hasErrors);
		}

		private readonly struct ObjectData
		{
			public AdvancedToggleObjectMode Type { get; }
			public Component Target { get; }
			public bool ToggleState { get; }
			public Material NewMaterial { get; }
			public int MaterialSlot { get; }
			
			public string VRCParameterName { get; }
			public float VRCParameterValue { get; }
			
			public string BlendShapeAttributeName { get; }
			public float BlendShapeParameterValue { get; }

			public ObjectData(VisualElement element)
			{
				Type = (AdvancedToggleObjectMode) element.Q<EnumField>(_targetTypeName).value;
				Target = element.Q<ObjectField>(_targetObjectName)?.value as Component;
				NewMaterial = element.Q<ObjectField>(_targetMaterialName)?.value as Material;
				ToggleState = element.Q<Toggle>(_targetActiveStateName)?.value ?? false;
				MaterialSlot = element.Q<PopupField<int>>(_materialSlotName)?.value ?? 0;
				VRCParameterName = element.Q<PopupField<VRCExpressionParameters.Parameter>>(_vrcParameterName)?.value?.name;
				VRCParameterValue = 0;
				
				var field = element.Q<BindableElement>(_vrcParameterValueImplementationName);
				switch (field)
				{
					case BaseField<int> intField:
						VRCParameterValue = intField.value;
						break;
					case BaseField<float> floatField:
						VRCParameterValue = floatField.value;
						break;
					case BaseField<bool> boolField:
						VRCParameterValue = boolField.value ? 1 : 0;
						break;
				}

				BlendShapeAttributeName = string.Empty;
				BlendShapeParameterValue = 0;
				
				var blendShapeIndex = element.Q<PopupField<int>>(_blendShapeParameterName)?.value;
				var renderer = element.Q<ObjectField>(_targetObjectName)?.value as SkinnedMeshRenderer;
				if(renderer != null && blendShapeIndex.HasValue)
				{
					BlendShapeAttributeName = $"blendShape.{renderer.sharedMesh.GetBlendShapeName(blendShapeIndex.Value)}";
					BlendShapeParameterValue = element.Q<Slider>(_blendShapeParameterValueImplementationName)?.value ?? 0;
				}

			}
		}
		
		private const string _materialSlotName = "material-slot";

		private const string _blendShapeParameterName = "blend-shape-parameter";
		
		private const string _vrcParameterName = "vrc-parameter";

		private const string _targetActiveStateName = "target-active-state";

		private const string _targetMaterialName = "target-material";

		private const string _targetTypeName = "target-type";
			
		private const string _vrcParameterValueImplementationName = "parameter-value-implementation";
		
		private const string _blendShapeParameterValueImplementationName = "blend-shape-slider";
			
		private const string _targetObjectName = "target-object";
	}
}