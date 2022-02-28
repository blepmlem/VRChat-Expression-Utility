using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEditor.Animations;
using UnityEngine;
using VRC.SDK3.Avatars.Components;

namespace ExpressionUtility
{
	internal class StateDefinition : IAnimationDefinition, IRealizable<AnimatorState>
	{
		public enum StateType
		{
			Normal,
			Entry,
			Exit,
			Any,
		}
		
		public StateDefinition(string name)
		{
			Name = name;
		}
		
		public StateDefinition(AnimatorState state) : this(state.name)
		{
			State = state;
			
			if(state.motion != null)
			{
				this.AddChild(new MotionDefinition(state.motion));
			}

			foreach (VRCAvatarParameterDriver vrcAvatarParameterDriver in state.behaviours.OfType<VRCAvatarParameterDriver>())
			{
				this.AddChild(new VrcParameterDriverDefinition(vrcAvatarParameterDriver));
			}

			if(state.speedParameterActive)
			{
				SpeedParameter = this.AddChild(new ParameterDefinition(state.speedParameter, nameof(state.speedParameter)));
			}

			if(state.mirrorParameterActive)
			{
				MirrorParameter = this.AddChild(new ParameterDefinition(state.mirrorParameter, nameof(state.mirrorParameter)));
			}

			if(state.timeParameterActive)
			{
				TimeParameter = this.AddChild(new ParameterDefinition(state.timeParameter, nameof(state.timeParameter)));
			}

			if(state.cycleOffsetParameterActive)
			{
				CycleOffsetParameter = this.AddChild(new ParameterDefinition(state.cycleOffsetParameter, nameof(state.cycleOffsetParameter)));
			}
		}

		public ParameterDefinition CycleOffsetParameter { get; private set; }

		public ParameterDefinition TimeParameter { get; private set; }

		public ParameterDefinition MirrorParameter { get; private set; }

		public ParameterDefinition SpeedParameter { get; private set; }
		
		public StateType Type { get; set; } = StateType.Normal;
		public AnimatorState State { get; private set; }
		
		public string Name { get; }
		
		public AnimatorState RealizeSelf()
		{
			if (!IsRealized)
			{
				State = new AnimatorState
				{
					name = Name,
					speedParameterActive = SpeedParameter != null,
					speedParameter = SpeedParameter?.Name,
					cycleOffsetParameterActive = CycleOffsetParameter != null,
					cycleOffsetParameter = CycleOffsetParameter?.Name,
					mirrorParameterActive = MirrorParameter != null,
					mirrorParameter = MirrorParameter?.Name,
					timeParameterActive =  TimeParameter != null,
					timeParameter = TimeParameter?.Name,
				};
			}

			State.speedParameter = SpeedParameter?.Name;
			
			return State;
		}

		public bool IsRealized => State != null;
		public bool IsRealizedRecursive => this.IsRealizedRecursive();

		public void DeleteSelf()
		{
			Undo.DestroyObjectImmediate(State);
		}

		public List<IAnimationDefinition> Children { get; } = new List<IAnimationDefinition>();
		public IAnimationDefinition Parent { get; set; }
						
		public override string ToString() => $"{Name} (Animation State)";
	}
}