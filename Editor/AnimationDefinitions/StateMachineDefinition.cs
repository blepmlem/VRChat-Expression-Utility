using System.Collections.Generic;
using System.Linq;
using UnityEditor.Animations;

namespace ExpressionUtility
{
	internal class StateMachineDefinition : IAnimationDefinition
	{
		public StateMachineDefinition(IAnimationDefinition parent, string name = null)
		{
			Name = name ?? parent.Name;
			Parents.Add(parent);
			Entry = new StateDefinition(this, nameof(Entry)){Type = StateDefinition.StateType.Entry};
			Exit = new StateDefinition(this, nameof(Exit)){Type = StateDefinition.StateType.Exit};
			Any = new StateDefinition(this, nameof(Any)){Type = StateDefinition.StateType.Any};
		}
		
		public StateMachineDefinition(IAnimationDefinition parent, AnimatorStateMachine stateMachine)
		{
			StateMachine = stateMachine;
			Name = stateMachine.name;
			Parents.Add(parent);
			
			Entry = new StateDefinition(this, nameof(Entry)){Type = StateDefinition.StateType.Entry};
			Exit = new StateDefinition(this, nameof(Exit)){Type = StateDefinition.StateType.Exit};
			Any = new StateDefinition(this, nameof(Any)){Type = StateDefinition.StateType.Any};
			
			foreach (ChildAnimatorState childAnimatorState in stateMachine.states)
			{
				var state = childAnimatorState.state;
				var stateDefinition = AddState(state);
				if (stateMachine.defaultState == state)
				{
					DefaultState = stateDefinition;
				}
				
				foreach (AnimatorTransition transition in stateMachine.entryTransitions)
				{
					if(transition.destinationState == state)
					{
						AddTransition(transition, Entry, stateDefinition);
					}
				}
			
				foreach (AnimatorStateTransition transition in stateMachine.anyStateTransitions)
				{
					if(transition.destinationState == state)
					{
						AddTransition(transition, Any, stateDefinition);
					}
				}
			}
		}

		public StateDefinition GetState(string name) => Children.OfType<StateDefinition>().FirstOrDefault(c => c.Name == name);

		public StateDefinition AddState(string name = null)
		{
			return Children.AddChild(new StateDefinition(this, name));
		}

		private StateDefinition AddState(AnimatorState state)
		{
			return Children.AddChild(new StateDefinition(this, state));
		}
		
		public TransitionDefinition AddTransition(AnimatorTransitionBase transition, StateDefinition from, StateDefinition to)
		{
			return Children.AddChild(new TransitionDefinition(this, transition, from, to));
		}
		
		public TransitionDefinition AddTransition(StateDefinition from, StateDefinition to, string name = null)
		{
			return Children.AddChild(new TransitionDefinition(this, from, to, name));
		}

		public AnimatorStateMachine StateMachine { get; }

		public StateDefinition DefaultState { get; set; }

		public StateDefinition Entry { get; }
		public StateDefinition Exit { get; }
		public StateDefinition Any { get; }

		public string Name { get; }

		public bool IsRealized => StateMachine != null;

		public List<IAnimationDefinition> Children { get; } = new List<IAnimationDefinition>();

		public List<IAnimationDefinition> Parents { get; } = new List<IAnimationDefinition>();

		public override string ToString() => $"[{GetType().Name}] {Name}";
	}
}