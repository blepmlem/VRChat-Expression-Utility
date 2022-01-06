using ExpressionUtility.UI;
using UnityEngine.Scripting;

namespace ExpressionUtility.UI
{
	interface IExpressionUI
	{
		void OnEnter(UIController controller, IExpressionUI previousUI);
		void OnExit(IExpressionUI nextUI);
	}
}