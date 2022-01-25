using ExpressionUtility.UI;
using UnityEngine;

namespace ExpressionUtility
{
	[CreateAssetMenu(fileName = nameof(TextureAtlasSlider), menuName = "Expression Utility/"+nameof(TextureAtlasSlider))]
	internal class TextureAtlasSlider : ExpressionUI, IExpressionDefinition
	{
		public void Build()
		{
		}

		public override void OnEnter(UIController controller, ExpressionUI previousUI)
		{
		}
	}
}