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
		
		public ConditionDefinition(IAnimationDefinition parent, AnimatorConditionMode mode, float threshold, string name = null)
		{
			Mode = mode;
			Threshold = threshold;
			Name = name ?? parent.Name;
			Parent = parent;
			AddParameter(false, Name);
		}

		public ConditionDefinition(IAnimationDefinition parent, AnimatorCondition condition)
		{
			AnimatorCondition = condition;
			Name = condition.parameter;
			Parent = parent;
			Mode = condition.mode;
			Threshold = condition.threshold;
			AddParameter(true, Name);
		}

		public ParameterDefinition AddParameter(bool isRealized,  string name)
		{
			return Children.AddChild(new ParameterDefinition(this, name) {IsRealized = isRealized});
		}
		
		public string Name { get; }
		public bool IsRealized => AnimatorCondition != null;

		public void DeleteSelf()
		{
	
		}

		public List<IAnimationDefinition> Children { get; } = new List<IAnimationDefinition>();
		public IAnimationDefinition Parent { get; }
		
		public override string ToString() => $"{Name} (Condition)";
	}
}