using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEditor.UIElements;
using UnityEngine.UIElements;
using VRC.SDK3.Avatars.Components;

namespace ExpressionUtility.UI
{
	internal class AvatarParameterData : ExpressionUI
	{
		private UIController _controller;
		private VisualTreeAsset _dataRow;
		private Button _deleteButton;
		private VisualElement _deletionHolder;
		private ScrollView _deletionList;
		private Messages _messages;
		private ScrollView _parameterList;
		private Dictionary<IAnimationDefinition, ObjectField> _toDelete;

		public override void BindControls(VisualElement root)
		{
			_deletionHolder = root.Q("delete-element");
			_deleteButton = root.Q<Button>("button-delete");
			_deletionList = root.Q<ScrollView>("deletion-list");
			_parameterList = root.Q<ScrollView>("scrollView");
		}

		public override void OnEnter(UIController controller, ExpressionUI previousUI)
		{
			_deletionList.Clear();
			_parameterList.Clear();

			_controller = controller;
			_deletionHolder.Display(false);
			_toDelete = new Dictionary<IAnimationDefinition, ObjectField>();
			_dataRow = controller.Assets.AvatarParameterDataRow;
			_messages = controller.Messages;
			_deleteButton.clicked += DeleteObjects;
			Undo.undoRedoPerformed -= UndoRedoPerformed;
			Undo.undoRedoPerformed += UndoRedoPerformed;
			BuildLayout(controller.ExpressionInfo);
			ErrorValidate(controller.ExpressionInfo);
		}

		private void ErrorValidate(ExpressionInfo expressionInfo)
		{
			VRCAvatarDescriptor.CustomAnimLayer[] controllerLayers = expressionInfo.AvatarDescriptor.baseAnimationLayers;
			bool missingRootMenu = !expressionInfo.AvatarDescriptor.expressionsMenu;
			bool noValidAnim = controllerLayers.All(a => a.animatorController == null || a.isDefault);
			_messages.SetActive(noValidAnim, "no-valid-animators");
			_messages.SetActive(missingRootMenu, "missing-root-menu");
		}

		public bool UnMarkForDeletion(IAnimationDefinition definition)
		{
			if (!_toDelete.TryGetValue(definition, out ObjectField objectField))
			{
				return false;
			}

			objectField.RemoveFromHierarchy();
			_toDelete.Remove(definition);
			_deletionHolder.Display(_toDelete.Any());
			return true;
		}

		public bool MarkForDeletion(ObjectHolder holder)
		{
			if (_toDelete.ContainsKey(holder.AnimationDefinition))
			{
				return false;
			}

			_deletionHolder.Display(true);
			var o = new ObjectField
			{
				value = holder,
			};

			void Callback(MouseDownEvent e)
			{
				e.StopImmediatePropagation();
				holder.SelectionAction?.Invoke();
			}

			o.RegisterCallback<MouseDownEvent>(Callback);

			_deletionList.Add(o);
			_toDelete.Add(holder.AnimationDefinition, o);
			return true;
		}

		private void DeleteObjects()
		{
			List<IAnimationDefinition> delete = _toDelete.Keys.ToList();
			List<AnimatorLayerDefinition> layers = delete.OfType<AnimatorLayerDefinition>().ToList();

			//Find orphaned animator parameters
			foreach (AnimatorLayerDefinition layer in layers)
			{
				if (!(layer.Parent is AnimatorDefinition animDef))
				{
					continue;
				}

				AnimatorParameterDefinition animatorParam = animDef.ParameterDefinitions.FirstOrDefault(p => p.Name == layer.Name);
				if (animatorParam == null)
				{
					continue;
				}

				IEnumerable<ParameterDefinition> usages = animDef.GetChildren<ParameterDefinition>(animatorParam.Name).Where(c => !(c is AnimatorParameterDefinition));
				IEnumerable<ParameterDefinition> layerUsages = layer.GetChildren<ParameterDefinition>(animatorParam.Name);

				if (!layerUsages.Except(usages).Any())
				{
					delete.Add(animatorParam);
				}
			}

			IEnumerable<MotionDefinition> skip = delete.SelectMany(s => s.GetChildren<MotionDefinition>()).Except(delete.OfType<MotionDefinition>());

			delete.Delete(skip);
			_controller.SetFrame(this);
		}

		private void UndoRedoPerformed()
		{
			_controller.SetFrame(this);
		}

		private void BuildLayout(ExpressionInfo controllerExpressionInfo)
		{
			var def = new AvatarDefinition(controllerExpressionInfo.AvatarDescriptor);

			IEnumerable<VrcParameterDefinition> parameters = def.Children.OfType<VrcParameterDefinition>();
			foreach (VrcParameterDefinition parameterDefinition in parameters)
			{
				string parameter = parameterDefinition.Name;
				VisualElement row = _dataRow.InstantiateTemplate(_parameterList.contentContainer);
				row.Q("parameter").Add(ObjectHolder.CreateHolder(parameterDefinition, this));

				foreach (AnimatorLayerDefinition l in GetLayers(def, parameter))
				{
					if (!l.TryGetFirstParent(out AnimatorDefinition _))
					{
						continue;
					}

					row.Q("layer").Add(ObjectHolder.CreateHolder(l, this));
				}

				foreach (MotionDefinition m in GetMotions(def, parameter))
				{
					if (m.Motion == null)
					{
						continue;
					}

					row.Q("misc").Add(ObjectHolder.CreateHolder(m, this));
				}

				foreach (VrcParameterDriverDefinition d in GetDrivers(def, parameter))
				{
					row.Q("misc").Add(ObjectHolder.CreateHolder(d, this));
				}


				foreach (MenuControlDefinition m in GetMenuControls(def, parameter))
				{
					if (!m.TryGetFirstParent(out MenuDefinition _))
					{
						continue;
					}

					row.Q("menu").Add(ObjectHolder.CreateHolder(m, this));
				}
			}
		}

		private IEnumerable<AnimatorLayerDefinition> GetLayers(AvatarDefinition avatarDefinition, string parameter)
		{
			return avatarDefinition.GetChildren<AnimatorLayerDefinition>().Where(a => a.GetChildren<ParameterDefinition>().Any(p => p.Name == parameter)).Distinct().ToList();
		}

		private IEnumerable<MotionDefinition> GetMotions(AvatarDefinition avatarDefinition, string parameter)
		{
			return GetLayers(avatarDefinition, parameter).SelectMany(l => l.GetChildren<MotionDefinition>()).Distinct().ToList();
		}

		private IEnumerable<VrcParameterDriverDefinition> GetDrivers(AvatarDefinition avatarDefinition, string parameter)
		{
			return GetLayers(avatarDefinition, parameter).SelectMany(l => l.GetChildren<VrcParameterDriverDefinition>()).Where(v => v.GetChildren<ParameterDefinition>(parameter).Any()).Distinct().ToList();
		}

		private IEnumerable<MenuControlDefinition> GetMenuControls(AvatarDefinition avatarDefinition, string parameter)
		{
			return avatarDefinition.GetChildren<MenuControlDefinition>().Where(m => m.Children.Any(c => (c as ParameterDefinition)?.Name == parameter)).Distinct().ToList();
		}
	}
}