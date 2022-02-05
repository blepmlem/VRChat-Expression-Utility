using System.Collections.Generic;
using UnityEditor;
using UnityEditor.Animations;
using UnityEngine;

namespace ExpressionUtility
{
	internal class StateDefinition : IAnimationDefinition
	{
		public enum StateType
		{
			Normal,
			Entry,
			Exit,
			Any,
		}
		
		public StateDefinition(StateMachineDefinition parent, string name = null)
		{
			Name = name ?? parent.Name;
			Parents.Add(parent);
		}
		
		public StateDefinition(StateMachineDefinition parent, AnimatorState state)
		{
			Name = state.name;
			Parents.Add(parent);
			State = state;
			AddMotion(state.motion);
			
			foreach (var transition in state.transitions)
			{
				if (transition.destinationState == null)
				{
					
					continue;
				}
				
				var from = this;
				var to = parent.GetState(transition.destinationState.name);
				if (to == null)
				{
					continue;
				}
				
				AddTransition(transition, from, to);
			}
			
			if(state.speedParameterActive)
			{
				SpeedParameter = AddParameter(state.speedParameter, ParameterDefinition.ParameterType.SpeedParameter, true);
			}

			if(state.mirrorParameterActive)
			{
				MirrorParameter = AddParameter(state.mirrorParameter, ParameterDefinition.ParameterType.MirrorParameter, true);
			}

			if(state.timeParameterActive)
			{
				TimeParameter = AddParameter(state.timeParameter, ParameterDefinition.ParameterType.TimeParameter, true);
			}

			if(state.cycleOffsetParameterActive)
			{
				CycleOffsetParameter = AddParameter(state.cycleOffsetParameter, ParameterDefinition.ParameterType.CycleOffsetParameter, true);
			}
		}
		
		public TransitionDefinition AddTransition(AnimatorTransitionBase transition, StateDefinition from, StateDefinition to)
		{
			return Children.AddChild(new TransitionDefinition(this, transition, from, to));
		}
		
		public ParameterDefinition CycleOffsetParameter { get; private set; }

		public ParameterDefinition TimeParameter { get; private set; }

		public ParameterDefinition MirrorParameter { get; private set; }

		public ParameterDefinition SpeedParameter { get; private set; }

		public ParameterDefinition AddParameter(string parameter, ParameterDefinition.ParameterType type, bool isRealized)
		{
			return Children.AddChild(new ParameterDefinition(this, parameter, type) {IsRealized = isRealized});
		}

		public MotionDefinition AddMotion(bool isBlendTree = false, string name = null) => Children.AddChild(new MotionDefinition(this, isBlendTree, name));

		public MotionDefinition AddMotion(Motion motion) => Children.AddChild(new MotionDefinition(this, motion));

		public StateType Type { get; set; } = StateType.Normal;
		public AnimatorState State { get; }
		
		public string Name { get; }
		public bool IsRealized => State != null;
		
		public List<IAnimationDefinition> Children { get; } = new List<IAnimationDefinition>();
		public List<IAnimationDefinition> Parents { get; } = new List<IAnimationDefinition>();
						
		public override string ToString() => $"[{GetType().Name}] {Name}";
	}
}