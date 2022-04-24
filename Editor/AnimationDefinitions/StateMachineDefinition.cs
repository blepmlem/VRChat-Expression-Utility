using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.Animations;
using UnityEngine;

namespace ExpressionUtility
{
	internal class StateMachineDefinition : IAnimationDefinition, IRealizable<AnimatorStateMachine>
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
		
		public AnimatorStateMachine StateMachine { get; private set; }

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

		public AnimatorStateMachine RealizeSelf(DirectoryInfo creationDirectory)
		{
			if (!IsRealized)
			{
				StateMachine = new AnimatorStateMachine { name = Name };
				// AssetDatabase.CreateAsset(StateMachine, $"{creationDirectory.FullName}/{Name}.controller");
			}
			
			foreach (var realizable in Children.OfType<IRealizable<AnimatorStateMachine>>())
			{
				var stateMachine = realizable.RealizeSelf(creationDirectory);
				if (StateMachine.stateMachines.Any(sm => sm.stateMachine.NotNull()?.name == stateMachine.name))
				{
					continue;
				}
				
				StateMachine.AddStateMachine(stateMachine, Vector3.zero);
				StateMachine.AddSubObject(stateMachine);
			}
			
			foreach (var realizable in Children.OfType<IRealizable<AnimatorState>>())
			{
				var state = realizable.RealizeSelf(creationDirectory);
				if (StateMachine.states.Any(s => s.state.NotNull()?.name == state.name))
				{
					continue;
				}
				
				StateMachine.AddState(state, Vector3.one);
				StateMachine.AddSubObject(state);
			}
			
			foreach (var realizable in Children.OfType<IRealizable<AnimatorTransitionBase>>())
			{
				var transitionBase = realizable.RealizeSelf(creationDirectory);
				// transitionBase.
			}

			return StateMachine;
		}

		public override string ToString() => $"{Name} (Animator State Machine)";
	}
}