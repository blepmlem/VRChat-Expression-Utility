using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEditor.Animations;

namespace ExpressionUtility
{
	internal class StateMachineDefinition : IAnimationDefinition
	{
		public StateMachineDefinition(string name)
		{
			Name = name;
			Entry = new StateDefinition(nameof(Entry)){Type = StateDefinition.StateType.Entry};
			Exit = new StateDefinition(nameof(Exit)){Type = StateDefinition.StateType.Exit};
			Any = new StateDefinition(nameof(Any)){Type = StateDefinition.StateType.Any};
		}
		
		public StateMachineDefinition(AnimatorStateMachine stateMachine) : this(stateMachine.name)
		{
			StateMachine = stateMachine;

			foreach (ChildAnimatorStateMachine childAnimatorStateMachine in stateMachine.stateMachines)
			{
				this.AddChild(new StateMachineDefinition(childAnimatorStateMachine.stateMachine));
			}
			
			foreach (ChildAnimatorState childAnimatorState in stateMachine.states)
			{
				var state = childAnimatorState.state;
				var stateDefinition = this.AddChild(new StateDefinition(state));
	
				if (stateMachine.defaultState == state)
				{
					DefaultState = stateDefinition;
				}
				
				foreach (AnimatorTransition transition in stateMachine.entryTransitions)
				{
					if(transition.destinationState == state)
					{
						this.AddChild(new TransitionDefinition(transition, Entry, stateDefinition));
					}
				}
			
				foreach (AnimatorStateTransition transition in stateMachine.anyStateTransitions)
				{
					if(transition.destinationState == state)
					{
						this.AddChild(new TransitionDefinition(transition, Any, stateDefinition));
					}
				}
			}
			
			foreach (var stateDefinition in Children.OfType<StateDefinition>())
			{
				AnimatorStateTransition[] animatorStateTransitions = stateDefinition.State.NotNull()?.transitions;
				if (animatorStateTransitions != null)
				{
					foreach (var transition in animatorStateTransitions)
					{
						if (transition.destinationState == null)
						{
							continue;	
						}

						var to = GetState(transition.destinationState.name);
						if (to == null)
						{
							continue;
						}
						
						stateDefinition.AddChild(new TransitionDefinition(transition, stateDefinition, to));
					}
				}
			}
		}

		public StateDefinition GetState(string name) => Children.OfType<StateDefinition>().FirstOrDefault(c => c.Name == name);
		public AnimatorStateMachine StateMachine { get; }

		public StateDefinition DefaultState { get; set; }

		public StateDefinition Entry { get; }
		public StateDefinition Exit { get; }
		public StateDefinition Any { get; }

		public string Name { get; }

		public bool IsRealized => StateMachine != null;

		public void DeleteSelf()
		{
			if(StateMachine != null)
			{
				Undo.DestroyObjectImmediate(StateMachine);
			}
		}

		public List<IAnimationDefinition> Children { get; } = new List<IAnimationDefinition>();

		public IAnimationDefinition Parent { get; set; }

		public override string ToString() => $"{Name} (Animator State Machine)";
	}
}