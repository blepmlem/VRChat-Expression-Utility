using System.Collections.Generic;
using System.Linq;
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
		private VisualTreeAsset _objectElement;
		
		[SerializeField]
		private VisualTreeAsset _rowElement;
		
		[SerializeField]
		private VRCAvatarDescriptor _avatarDescriptor;

		private IEnumerable<AnimatorController> _animators;
		private object _animData;
		private List<VRCExpressionParameters.Parameter> _parameters;
		private IEnumerable<VRCExpressionsMenu> _menus;


		[MenuItem("Expression Utility/Avatar Data")]
		public static void GetWindow()
		{
			var vrcAvatarDescriptor = StageUtility.GetCurrentStageHandle().FindComponentOfType<VRCAvatarDescriptor>();
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
		
		private AvatarDataWindow Initialize()
		{
			this.SetAntiAliasing(4);
			titleContent = EditorGUIUtility.TrTextContentWithIcon("Avatar Data", "NetworkAnimator Icon");
			_layout.CloneTree(rootVisualElement);
			GatherData(_avatarDescriptor);
			return this;
		}

		public class Data
		{
			public List<VRCExpressionParameters.Parameter> Parameters { get; } = new List<VRCExpressionParameters.Parameter>();
			public List<AnimatorDefinition> AnimatorDefinitions { get; } = new List<AnimatorDefinition>();
			public List<VRCExpressionsMenu> Menus { get; } = new List<VRCExpressionsMenu>();
		}

		private void GatherData(VRCAvatarDescriptor avd)
		{
			var def = new AvatarDefinition(avd);

			foreach (var child in def.GetChildren<IAnimationDefinition>().Where(c => !c.IsRealized))
			{
				child.ToString().Log();
			}
		}
		
		
		private void BuildLayout()
		{
			GatherData(_avatarDescriptor);
			var container = rootVisualElement.Q("container");
			foreach (VRCExpressionParameters.Parameter parameter in _parameters)
			{
				var data = new Data()
				{
					Parameters = { parameter },
					
				};
			}
		}

		// class Row()
		// {
		// 	
		// }
	}
}