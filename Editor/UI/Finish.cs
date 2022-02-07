namespace ExpressionUtility.UI
{
	internal class Finish : ExpressionUI
	{
		public override void OnEnter(UIController controller, ExpressionUI previousUI)
		{
			controller.Close();
		}
	}
}