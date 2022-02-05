using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using UnityEditor;
using UnityEditor.Animations;
using UnityEditor.SceneManagement;
using UnityEditor.UIElements;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UIElements;
using VRC.SDK3.Avatars.Components;
using VRC.SDK3.Avatars.ScriptableObjects;

namespace ExpressionUtility.UI
{
	internal class AvatarDataWindow : EditorWindow
	{
		[SerializeField]
		private VisualTreeAsset _layout;
		
		[SerializeField]
		private VisualTreeAsset _rowLayout;

		[SerializeField]
		private VRCAvatarDescriptor _avatarDescriptor;

		private bool _initialized;

		[MenuItem("Expression Utility/Avatar Data")]
		public static void GetWindow()
		{
			var vrcAvatarDescriptor = StageUtility.GetCurrentStageHandle().FindComponentsOfType<VRCAvatarDescriptor>().Where(d => d.gameObject.activeInHierarchy).ToList().FirstOrDefault();
			CreateWindow(vrcAvatarDescriptor, false);
		}

		public static AvatarDataWindow CreateWindow(VRCAvatarDescriptor avatarDescriptor, bool asModal)
		{
			var window = GetWindow<AvatarDataWindow>();
			window._avatarDescriptor = avatarDescriptor;
			
			if (asModal)
			{
				window.ShowModal();
			}
			else
			{
				window.Show();
			}
			
			return window.Initialize();
		}

		private void OnEnable()
		{
			Initialize();
		}

		private AvatarDataWindow Initialize()
		{
			if (_initialized || _avatarDescriptor == null)
			{
				return this;
			}
			
			this.SetAntiAliasing(4);
			titleContent = EditorGUIUtility.TrTextContentWithIcon("Avatar Data", "NetworkAnimator Icon");
			_layout.CloneTree(rootVisualElement);
			BuildLayout();
			_initialized = true;
			return this;
		}

		private void BuildLayout()
		{
			var def = new AvatarDefinition(_avatarDescriptor);
			var scroll = rootVisualElement.Q<ScrollView>("scrollview");

			var parameters = def.Children.OfType<ParameterDefinition>();
			foreach (ParameterDefinition parameterDefinition in parameters)
			{
				var parameter = parameterDefinition.Name;
				var row = _rowLayout.InstantiateTemplate(scroll.contentContainer);
				row.Q("parameter").Add(ObjectHolder.CreateHolderField(() => Selection.activeObject = def.VrcExpressionParameters, parameter));
				
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

					var path = AssetDatabase.GetAssetPath(m.Motion);
					
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

		private IEnumerable<AnimatorDefinition> GetAnimators(AvatarDefinition avatarDefinition, string parameter)
		{
			return avatarDefinition
				.GetChildren<AnimatorDefinition>()
				.Where(a => a
					.GetChildren<ParameterDefinition>()
					.Any(p => p.Name == parameter))
				.Distinct().ToList();
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