using UnityEditor.Animations;

namespace ExpressionUtility
{
	internal static class AnimatorDefinitionExtensions
	{
		public static TransitionDefinition AddTransition(this IAnimationDefinition instance, StateDefinition from, StateDefinition to, string name = null)
		{
			return instance.Children.AddChild(new TransitionDefinition(instance, from, to, name));
		}
	}
}