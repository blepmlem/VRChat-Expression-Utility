using UnityEditor;
using UnityEngine.UIElements;

namespace ExpressionUtility.UI
{
	internal class Intro : IExpressionUI
	{
		private const string SKIP_PREF = "expression-ui-skip-intro";
		private UIController _controller;
		private bool _skipInto;

		public void OnEnter(UIController controller, IExpressionUI previousUI)
		{
			_skipInto = EditorPrefs.GetBool(SKIP_PREF, false);
			
			_controller = controller;
			var toggle = _controller.ContentFrame.Q<Toggle>("skip-into-toggle");
			toggle.value = _skipInto;
			toggle.RegisterValueChangedCallback(evt => EditorPrefs.SetBool(SKIP_PREF, evt.newValue));
			var nextButton = controller.ContentFrame.Q<Button>("footer-toolbar-next");
			
			if (previousUI is null && toggle.value)
			{
				OnClicked();
				return;
			}
			
			nextButton.clickable = new Clickable(OnClicked);
		}

		public void OnExit(IExpressionUI nextUI)
		{

		}

		private void OnClicked()
		{
			_controller.SetFrame<AvatarSelection>();
		}
	}
}