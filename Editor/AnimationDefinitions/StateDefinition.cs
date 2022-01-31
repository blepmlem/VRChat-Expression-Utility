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
		
		public StateDefinition(IAnimationDefinition parent, string name = null)
		{
			Name = name ?? parent.Name;
			Parents.Add(parent);
		}
		
		public StateDefinition(IAnimationDefinition parent, AnimatorState state)
		{
			Name = state.name;
			Parents.Add(parent);
			State = state;
			AddMotion(state.motion);
		}

		public MotionDefinition AddMotion(bool isBlendTree = false, string name = null)
		{
			return Children.AddChild(new MotionDefinition(this, isBlendTree, name));
		}

		public MotionDefinition AddMotion(Motion motion)
		{
			return Children.AddChild(new MotionDefinition(this, motion));
		}

		public StateType Type { get; set; } = StateType.Normal;
		public AnimatorState State { get; }
		
		public string Name { get; }
		public bool IsRealized => State != null;
		
		public List<IAnimationDefinition> Children { get; } = new List<IAnimationDefinition>();
		public List<IAnimationDefinition> Parents { get; } = new List<IAnimationDefinition>();
						
		public override string ToString() => $"[{GetType().Name}] {Name}";
	}
}