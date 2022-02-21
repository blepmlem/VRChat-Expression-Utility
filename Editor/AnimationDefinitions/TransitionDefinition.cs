using System.Collections.Generic;
using UnityEditor;
using UnityEditor.Animations;

namespace ExpressionUtility
{
	internal class TransitionDefinition : IAnimationDefinition
	{
		public TransitionDefinition(StateDefinition from, StateDefinition to)
		{
			Name = $"{from.Name} > {to.Name}";
			From = from;
			To = to;
		}

		public TransitionDefinition(AnimatorTransitionBase transition, StateDefinition from, StateDefinition to) : this(to, from)
		{
			StateTransition = transition;
			foreach (AnimatorCondition condition in transition.conditions)
			{
				this.AddChild(new ConditionDefinition(condition));
			}
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

		public IAnimationDefinition Parent { get; set; }

		public override string ToString() => $"{Name} (Animator Transition)";
	}
}