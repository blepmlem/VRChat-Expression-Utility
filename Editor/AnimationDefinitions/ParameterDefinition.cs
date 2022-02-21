using System.Collections.Generic;

namespace ExpressionUtility
{
	internal class ParameterDefinition : IAnimationDefinition
	{
		public ParameterDefinition(string name, string label = "")
		{
			Name = name;
			Label = label;
		}
		
		public string Label { get; }
		public string Name { get; }
		public virtual bool IsRealized => Parent?.IsRealized ?? false;
		public virtual void DeleteSelf()
		{
			
		}

		public List<IAnimationDefinition> Children => new List<IAnimationDefinition>();
		public IAnimationDefinition Parent { get; set; }

		public override string ToString() => $"{Name}{(!string.IsNullOrEmpty(Label) ? $"+{Label}" : "")} (Animation Parameter)";
	}
}