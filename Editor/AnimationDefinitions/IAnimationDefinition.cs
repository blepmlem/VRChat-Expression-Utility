using System.Collections.Generic;
using UnityEngine;

namespace ExpressionUtility
{
	internal interface IAnimationDefinition
	{
		string Name { get; }
		bool IsRealized { get; }

		List<IAnimationDefinition> Children { get; }
		List<IAnimationDefinition> Parents { get; }
	}
}