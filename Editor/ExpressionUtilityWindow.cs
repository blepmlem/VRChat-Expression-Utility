using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEditor.UIElements;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace ExpressionUtility.UI
{
	internal class ExpressionUtilityWindow : EditorWindow
	{
		[SerializeField]
		private AssetReferences _assetReferences;

		private UIController _controller;

		[MenuItem("Expression Utility/Window")]
		public static void GetWindow() => GetWindow(typeof(ExpressionUtilityWindow));

		private void OnEnable()
		{
			this.SetAntiAliasing(4);
			titleContent = EditorGUIUtility.TrTextContentWithIcon("Expression Utility", "NetworkAnimator Icon");
			_controller = new UIController(this, _assetReferences);
			_controller.SetFrame<Intro>();
			EditorSceneManager.sceneOpened += SceneChanged;
		}

		private void SceneChanged(Scene _, OpenSceneMode __)
		{
			rootVisualElement.Clear();
			_controller?.Dispose();
			OnEnable();
		}

		private void OnDisable() => EditorSceneManager.sceneOpened -= SceneChanged;
	}
}