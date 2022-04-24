using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.Animations;

namespace ExpressionUtility
{
	internal class TransitionDefinition : IAnimationDefinition, IRealizable<AnimatorTransition>,  IRealizable<AnimatorStateTransition>
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

		public AnimatorTransitionBase StateTransition { get; private set; }

		public string Name { get; }

		AnimatorTransition IRealizable<AnimatorTransition>.RealizeSelf(DirectoryInfo creationDirectory)
		{
			if (!IsRealized)
			{
				StateTransition = new AnimatorTransition();
			}

			StateTransition.conditions = Children.OfType<IRealizable<AnimatorCondition>>().Select(r => r.RealizeSelf(creationDirectory)).ToArray();
			
			return StateTransition as AnimatorTransition;
		}

		AnimatorStateTransition IRealizable<AnimatorStateTransition>.RealizeSelf(DirectoryInfo creationDirectory)
		{
			if (!IsRealized)
			{
				StateTransition = new AnimatorStateTransition();
			}

			StateTransition.conditions = Children.OfType<IRealizable<AnimatorCondition>>().Select(r => r.RealizeSelf(creationDirectory)).ToArray();
			
			return StateTransition as AnimatorStateTransition;
		}
		
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