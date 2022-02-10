using System.Collections.Generic;
using UnityEditor;
using UnityEditor.Animations;

namespace ExpressionUtility
{
	internal class TransitionDefinition : IAnimationDefinition
	{
		public TransitionDefinition(IAnimationDefinition parent, StateDefinition from, StateDefinition to, string name = null)
		{
			Name = name ?? parent.Name;
			Parent = parent;
			From = from;
			To = to;
		}

		public TransitionDefinition(IAnimationDefinition parent, AnimatorTransitionBase transition, StateDefinition from, StateDefinition to)
		{
			StateTransition = transition;
			Name = string.IsNullOrEmpty(transition.name) ? parent.Name : transition.name;
			Parent = parent;
			From = from;
			To = to;

			foreach (AnimatorCondition condition in transition.conditions)
			{
				AddCondition(condition);
			}
		}
		
		public ConditionDefinition AddCondition(AnimatorCondition condition)
		{
			return Children.AddChild(new ConditionDefinition(this, condition));
		}

		public ConditionDefinition AddCondition(ParameterDefinition parameter, bool whenTrue)
		{
			return Children.AddChild(new ConditionDefinition(this, whenTrue ? AnimatorConditionMode.If : AnimatorConditionMode.IfNot, 0, parameter.Name));
		}

		public StateDefinition To { get; set; }

		public StateDefinition From { get; set; }

		public AnimatorTransitionBase StateTransition { get; }

		public string Name { get; }

		public bool IsRealized => StateTransition != null;

		public void DeleteSelf()
		{
			if (StateTransition != null)
			{
				Undo.DestroyObjectImmediate(StateTransition);
			}
		}

		public List<IAnimationDefinition> Children { get; } = new List<IAnimationDefinition>();

		public IAnimationDefinition Parent { get; }

		public override string ToString() => $"{Name} (Animator Transition)";
	}
}