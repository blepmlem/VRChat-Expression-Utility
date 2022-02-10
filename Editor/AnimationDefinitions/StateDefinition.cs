using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEditor.Animations;
using UnityEngine;
using VRC.SDK3.Avatars.Components;

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
			Parent = parent;
		}
		
		public StateDefinition(StateMachineDefinition parent, AnimatorState state) : this(parent)
		{
			Name = state.name;
			State = state;
			AddMotion(state.motion);

			foreach (VRCAvatarParameterDriver vrcAvatarParameterDriver in state.behaviours.OfType<VRCAvatarParameterDriver>())
			{
				AddParameterDriverDefinition(vrcAvatarParameterDriver);
			}
			
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
				SpeedParameter = AddParameter(state.speedParameter, nameof(state.speedParameter), true);
			}

			if(state.mirrorParameterActive)
			{
				MirrorParameter = AddParameter(state.mirrorParameter, nameof(state.mirrorParameter), true);
			}

			if(state.timeParameterActive)
			{
				TimeParameter = AddParameter(state.timeParameter, nameof(state.timeParameter), true);
			}

			if(state.cycleOffsetParameterActive)
			{
				CycleOffsetParameter = AddParameter(state.cycleOffsetParameter, nameof(state.cycleOffsetParameter), true);
			}
		}

		public VrcParameterDriverDefinition AddParameterDriverDefinition(VRCAvatarParameterDriver driver)
		{
			return Children.AddChild(new VrcParameterDriverDefinition(this, driver));
		}
		
		public TransitionDefinition AddTransition(AnimatorTransitionBase transition, StateDefinition from, StateDefinition to)
		{
			return Children.AddChild(new TransitionDefinition(this, transition, from, to));
		}
		
		public ParameterDefinition CycleOffsetParameter { get; private set; }

		public ParameterDefinition TimeParameter { get; private set; }

		public ParameterDefinition MirrorParameter { get; private set; }

		public ParameterDefinition SpeedParameter { get; private set; }

		public ParameterDefinition AddParameter(string parameter, string label, bool isRealized)
		{
			return Children.AddChild(new ParameterDefinition(this, parameter, label) {IsRealized = isRealized});
		}

		public MotionDefinition AddMotion(bool isBlendTree = false, string name = null) => Children.AddChild(new MotionDefinition(this, isBlendTree, name));

		public MotionDefinition AddMotion(Motion motion) => Children.AddChild(new MotionDefinition(this, motion));

		public StateType Type { get; set; } = StateType.Normal;
		public AnimatorState State { get; }
		
		public string Name { get; }
		public bool IsRealized => State != null;

		public void DeleteSelf()
		{
			Undo.DestroyObjectImmediate(State);
		}

		public List<IAnimationDefinition> Children { get; } = new List<IAnimationDefinition>();
		public IAnimationDefinition Parent { get; }
						
		public override string ToString() => $"{Name} (Animation State)";
	}
}