using System.Collections.Generic;
using UnityEngine;

namespace ExpressionUtility
{
	internal interface IAnimationDefinition
	{
		string Name { get; }
		bool IsRealized { get; }

		void DeleteSelf();
		
		List<IAnimationDefinition> Children { get; }
		IAnimationDefinition Parent { get; set; }
	}

	internal class KeyframeDefinition : IAnimationDefinition
	{
		public string Name { get; }
		public bool IsRealized => Parent.IsRealized;
		public void DeleteSelf()
		{
			throw new System.NotImplementedException();
		}

		public List<IAnimationDefinition> Children { get; }
		public IAnimationDefinition Parent { get; set; }
	}
}