using UnityEditor;
using UnityEngine.UIElements;

namespace ExpressionUtility.UI
{
	internal class Intro : ExpressionUI
	{
		private UIController _controller;
		private bool _skipInto;
		
		private Toggle _skipIntroToggle;
		private Toggle _connectToVrcToggle;
		private Toggle _updatesToggle;
		private Button _nextButton;

		public override void BindControls(VisualElement root)
		{
			_skipIntroToggle = root.Q<Toggle>("skip-intro-toggle");
			_connectToVrcToggle = root.Q<Toggle>("connect-vrc-api-toggle");
			_updatesToggle = root.Q<Toggle>("check-updates-toggle");
			_nextButton = root.Q<Button>("footer-toolbar-next");
		}

		public override void OnEnter(UIController controller, ExpressionUI previousUI)
		{
			_controller = controller;

			_skipIntroToggle.value = Settings.SkipIntroPage;
			_skipIntroToggle.RegisterValueChangedCallback(evt => Settings.SkipIntroPage = evt.newValue);

			_updatesToggle.value = Settings.AllowCheckForUpdates;
			_updatesToggle.RegisterValueChangedCallback(evt => Settings.AllowCheckForUpdates = evt.newValue);
			
			_connectToVrcToggle.value = Settings.AllowConnectToVrcApi;
			_connectToVrcToggle.RegisterValueChangedCallback(evt =>
			{
				Settings.AllowConnectToVrcApi = evt.newValue;
				controller.AvatarCache.Refresh(true);
			});

			if (previousUI is null && Settings.SkipIntroPage)
			{
				OnClicked();
				return;
			}
			
			_nextButton.clickable = new Clickable(OnClicked);
		}

		private void OnClicked()
		{
			_controller.SetFrame<AvatarSelection>();
		}
	}
}