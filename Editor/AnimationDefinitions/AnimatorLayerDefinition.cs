using System.Collections.Generic;
using UnityEditor.Animations;

namespace ExpressionUtility
{
	internal class AnimatorLayerDefinition : IAnimationDefinition
	{
		public AnimatorControllerLayer Layer { get; }
		public AnimatorLayerDefinition(IAnimationDefinition parent, string name = null)
		{
			Name = name ?? parent.Name;
			Parents.Add(parent);
		}

		public AnimatorLayerDefinition(IAnimationDefinition parent, AnimatorControllerLayer controllerLayer)
		{
			Layer = controllerLayer;
			Name = controllerLayer.name;
			Parents.Add(parent);
			
			AddStateMachine(controllerLayer.stateMachine);
		}

		public StateMachineDefinition AddStateMachine(string name = null)
		{
			return Children.AddChild(new StateMachineDefinition(this, name));
		}
		
		public StateMachineDefinition AddStateMachine(AnimatorStateMachine stateMachine)
		{
			return Children.AddChild(new StateMachineDefinition(this, stateMachine));
		}
		
		public string Name { get; }
		public bool IsRealized => Layer != null;
		
		public List<IAnimationDefinition> Children { get; } = new List<IAnimationDefinition>();
		public List<IAnimationDefinition> Parents { get; } = new List<IAnimationDefinition>();
		
		public override string ToString() => $"[{GetType().Name}] {Name}";
	}
}