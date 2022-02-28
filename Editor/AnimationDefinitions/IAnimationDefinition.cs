using System.Collections.Generic;
using System.Linq;
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
}