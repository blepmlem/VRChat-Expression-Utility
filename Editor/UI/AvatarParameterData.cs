using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEngine.UIElements;

namespace ExpressionUtility.UI
{
	internal class AvatarParameterData : ExpressionUI
	{
		private VisualTreeAsset _dataRow;
		private ScrollView _scrollView;
		private Messages _messages;

		public override void BindControls(VisualElement root)
		{
			_scrollView = root.Q<ScrollView>("scrollView");
		}

		public override void OnEnter(UIController controller, ExpressionUI previousUI)
		{
			_dataRow = controller.Assets.AvatarParameterDataRow;
			_messages = controller.Messages;
			BuildLayout(controller.ExpressionInfo);
			ErrorValidate(controller.ExpressionInfo);
		}

		private void ErrorValidate(ExpressionInfo expressionInfo)
		{
			var controllerLayers = expressionInfo.AvatarDescriptor.baseAnimationLayers;
			bool missingRootMenu = !expressionInfo.AvatarDescriptor.expressionsMenu;
			bool noValidAnim = controllerLayers.All(a => a.animatorController == null || a.isDefault);
			_messages.SetActive(noValidAnim, "no-valid-animators");
			_messages.SetActive(missingRootMenu, "missing-root-menu");
		}

		private void BuildLayout(ExpressionInfo controllerExpressionInfo)
		{
			var def = new AvatarDefinition(controllerExpressionInfo.AvatarDescriptor);
			
			var parameters = def.Children.OfType<ParameterDefinition>();
			foreach (ParameterDefinition parameterDefinition in parameters)
			{
				var parameter = parameterDefinition.Name;
				var row = _dataRow.InstantiateTemplate(_scrollView.contentContainer);
				row.Q("parameter").Add(ObjectHolder.CreateHolderField(() => Selection.activeObject = def.VrcExpressionParameters, $"{parameter} ({parameterDefinition.Type})"));
				
				foreach (AnimatorLayerDefinition l in GetLayers(def, parameter))
				{
					if (!l.TryGetFirstParent(out AnimatorDefinition animDef))
					{
						continue;
					}
					
					row.Q("layer").Add(ObjectHolder.CreateHolderField(() => animDef.Animator.SelectAnimatorLayer(l.Layer), $"{animDef.Name}/{l.Layer.name}"));
				}
				
				foreach (var m in GetMotions(def, parameter))
				{
					if (m.Motion == null)
					{
						continue;
					}
					
					row.Q("motion").Add(ObjectHolder.CreateHolderField(() => Selection.activeObject = m.Motion, m.Motion.name));
				}
				
				foreach (var m in GetMenuControls(def, parameter))
				{
					if (!m.TryGetFirstParent(out MenuDefinition menu))
					{
						continue;
					}
					
					row.Q("menu").Add(ObjectHolder.CreateHolderField(() => Selection.activeObject = menu.Menu, menu.Menu.name));
				}
			}
		}

		private IEnumerable<AnimatorLayerDefinition> GetLayers(AvatarDefinition avatarDefinition, string parameter)
		{
			return avatarDefinition
				.GetChildren<AnimatorLayerDefinition>()
				.Where(a => a
					.GetChildren<ParameterDefinition>()
					.Any(p => p.Name == parameter))
				.Distinct().ToList();
		}

		private IEnumerable<MotionDefinition> GetMotions(AvatarDefinition avatarDefinition, string parameter)
		{
			return GetLayers(avatarDefinition, parameter)
				.SelectMany(l => l
					.GetChildren<MotionDefinition>())
				.Distinct().ToList();
		}

		private IEnumerable<MenuControlDefinition> GetMenuControls(AvatarDefinition avatarDefinition, string parameter)
		{
			return avatarDefinition
				.GetChildren<MenuControlDefinition>()
				.Where(m => m.Children
					.Any(c => (c as ParameterDefinition)?.Name == parameter))
				.Distinct().ToList();
		}
	}
}