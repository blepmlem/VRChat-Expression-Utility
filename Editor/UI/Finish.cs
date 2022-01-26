using UnityEditor;

namespace ExpressionUtility.UI
{
	internal class Finish : ExpressionUI
	{
		public override void OnEnter(UIController controller, ExpressionUI previousUI)
		{
			//It's nice to show where it ended up, and we have to at least show the menu in the inspector once or lyuma.Av3Emulator gets mad    
			// Selection.activeObject = controller.ExpressionInfo.Menu;
			controller.Close();
		}
	}
}