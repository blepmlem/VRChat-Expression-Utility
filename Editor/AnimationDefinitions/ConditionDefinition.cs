using System.Collections.Generic;
using UnityEditor;
using UnityEditor.Animations;

namespace ExpressionUtility
{
	internal class ConditionDefinition : IAnimationDefinition
	{
		public AnimatorConditionMode Mode { get; }
		public float Threshold { get; }
		public AnimatorCondition? AnimatorCondition { get; }
		
		public ConditionDefinition(string parameter, AnimatorConditionMode mode, float threshold)
		{
			Name = parameter;
			Mode = mode;
			Threshold = threshold;
			this.AddChild(new ParameterDefinition(parameter, nameof(ConditionDefinition)));
		}

		public ConditionDefinition(AnimatorCondition condition) : this(condition.parameter, condition.mode, condition.threshold)
		{
			AnimatorCondition = condition;
		}

		public string Name { get; }
		public bool IsRealized => AnimatorCondition != null;

		public void DeleteSelf()
		{
	
		}

		public List<IAnimationDefinition> Children { get; } = new List<IAnimationDefinition>();
		public IAnimationDefinition Parent { get; set; }
		
		public override string ToString() => $"{Name} (Condition)";
	}
}