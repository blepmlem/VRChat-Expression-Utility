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
		IAnimationDefinition Parent { get; }
	}
}