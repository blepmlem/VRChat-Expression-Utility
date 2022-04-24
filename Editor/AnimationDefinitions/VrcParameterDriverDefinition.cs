using System.Collections.Generic;
using System.Linq;
using Newtonsoft.Json;
using UnityEditor;
using VRC.SDK3.Avatars.Components;

namespace ExpressionUtility
{
	internal class VrcParameterDriverDefinition : IAnimationDefinition
	{
		public VrcParameterDriverDefinition(VRCAvatarParameterDriver driver)
		{
			Behaviour = driver;
			foreach (var driverParameter in driver.parameters)
			{
				this.AddChild(new ParameterDefinition(driverParameter.name, nameof(VrcParameterDriverDefinition)));
			}
		}
		
		[JsonIgnore]
		public VRCAvatarParameterDriver Behaviour { get; }
		public string Name => $"{Parent?.Name} (Parameter Driver)";
		public bool IsRealized => Behaviour != null;
		public void DeleteSelf()
		{
			if (IsRealized && Parent.IsRealized && Parent is StateDefinition stateDefinition)
			{
				Undo.RegisterCompleteObjectUndo(stateDefinition.State, $"Remove behaviour: {Behaviour.name}");
				var behaviours = stateDefinition.State.behaviours.ToList();
				behaviours.Remove(Behaviour);
				stateDefinition.State.behaviours = behaviours.ToArray();
				Undo.DestroyObjectImmediate(Behaviour);
				EditorUtility.SetDirty(stateDefinition.State);
			}
		}

		public List<IAnimationDefinition> Children { get; } = new List<IAnimationDefinition>();
		public IAnimationDefinition Parent { get; set; }
		public override string ToString() => Name;
	}
}