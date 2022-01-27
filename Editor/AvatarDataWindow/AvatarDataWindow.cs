using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEditor.UIElements;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UIElements;
using VRC.SDK3.Avatars.Components;

namespace ExpressionUtility.UI
{
	internal class AvatarDataModal : EditorWindow
	{
		[SerializeField]
		private VisualTreeAsset _layout;

		[MenuItem("Expression Utility/Avatar Data")]
		public static void GetWindow() => CreateWindow(null, false);

		public static AvatarDataModal CreateWindow(VRCAvatarDescriptor avatarDescriptor, bool asModal)
		{
			var window = GetWindow<AvatarDataModal>();
			if (asModal)
			{
				window.ShowModal();
			}
			else
			{
				window.Show();
			}

			return window;
		}
		
		private void OnEnable()
		{
			this.SetAntiAliasing(4);
			titleContent = EditorGUIUtility.TrTextContentWithIcon("Avatar Data", "NetworkAnimator Icon");
			EditorSceneManager.sceneOpened += SceneChanged;
		}

		private void SceneChanged(Scene _, OpenSceneMode __)
		{
			rootVisualElement.Clear();
			OnEnable();
		}

		private void OnDisable() => EditorSceneManager.sceneOpened -= SceneChanged;
	}
}