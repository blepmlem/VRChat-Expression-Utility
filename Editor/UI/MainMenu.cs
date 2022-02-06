using UnityEngine.UIElements;

namespace ExpressionUtility.UI
{
	internal class MainMenu : ExpressionUI
	{
		private Button _createExpressionButton;
		private Button _avatarParameterDataButton;

		public override void BindControls(VisualElement root)
		{
			_createExpressionButton = root.Q<Button>("create-expression-button");
			_avatarParameterDataButton = root.Q<Button>("avatar-parameter-data-button");
		}

		public override void OnEnter(UIController controller, ExpressionUI previousUI)
		{
			_createExpressionButton.clicked += controller.SetFrame<Setup>;
			_avatarParameterDataButton.clicked += controller.SetFrame<AvatarParameterData>;
		}
	}
}