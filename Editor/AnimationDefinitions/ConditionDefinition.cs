using System.Collections.Generic;
using UnityEditor.Animations;

namespace ExpressionUtility
{
	internal class ConditionDefinition : IAnimationDefinition
	{
		public AnimatorConditionMode Mode { get; }
		public float Threshold { get; }
		public AnimatorCondition? AnimatorCondition { get; }
		
		public ConditionDefinition(IAnimationDefinition parent, AnimatorConditionMode mode, float threshold, string name = null)
		{
			Mode = mode;
			Threshold = threshold;
			Name = name ?? parent.Name;
			Parents.Add(parent);
			AddParameter(mode, false, Name);
		}

		public ConditionDefinition(IAnimationDefinition parent, AnimatorCondition condition)
		{
			AnimatorCondition = condition;
			Name = condition.parameter;
			Parents.Add(parent);
			Mode = condition.mode;
			Threshold = condition.threshold;
			AddParameter(condition.mode, true, Name);
		}

		public ParameterDefinition AddParameter(AnimatorConditionMode mode, bool isRealized,  string name)
		{
			return Children.AddChild(new ParameterDefinition(this, mode, name) {IsRealized = isRealized});
		}
		
		public string Name { get; }
		public bool IsRealized => AnimatorCondition != null;
		
		public List<IAnimationDefinition> Children { get; } = new List<IAnimationDefinition>();
		public List<IAnimationDefinition> Parents { get; } = new List<IAnimationDefinition>();
		
		public override string ToString() => $"[{GetType().Name}] {Name}";
	}
}