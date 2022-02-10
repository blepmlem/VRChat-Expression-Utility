using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEditor;

namespace ExpressionUtility
{
	internal static class IAnimationDefinitionExtensions
	{
		public static void Delete(this ICollection<IAnimationDefinition> definitions, IEnumerable<IAnimationDefinition> skip = null)
		{
			var collection = definitions.FilterOutDescendants().ToList();
			
			Undo.SetCurrentGroupName($"Delete {collection.Count} objects" );
			foreach (var definition in collection)
			{
				definition.Delete(skip);
			}
			
			Undo.CollapseUndoOperations(Undo.GetCurrentGroup());
		}
		
		public static void Delete(this IAnimationDefinition definition, IEnumerable<IAnimationDefinition> skip = null)
		{
			if (definition.ShouldSkip(skip))
			{
				return;	
			}
			
			definition.DeleteInternal(skip);
			definition.Parent?.Children.Remove(definition);
			AssetDatabase.Refresh();
		}

		private static void DeleteInternal(this IAnimationDefinition definition, IEnumerable<IAnimationDefinition> skip = null)
		{
			if (definition.ShouldSkip(skip))
			{
				return;	
			}
			
			$"DELETING: {definition}".Log();
			definition.Children.ForEach(c=>c.DeleteInternal(skip));
			definition.Children.Clear();
			definition.DeleteSelf();
		}

		private static bool ShouldSkip(this IAnimationDefinition def, IEnumerable<IAnimationDefinition> skip)
		{
			return skip?.Contains(def) ?? false;
		}
		
		public static bool IsDescendantOf(this IAnimationDefinition def, IAnimationDefinition parent)
		{
			return def.GetParents<IAnimationDefinition>().Contains(parent);
		}

		public static IEnumerable<IAnimationDefinition> FilterOutDescendants(this IEnumerable<IAnimationDefinition> def)
		{
			foreach (IAnimationDefinition outer in def)
			{
				foreach (IAnimationDefinition inner in def)
				{
					if (!inner.IsDescendantOf(outer))
					{
						yield return inner;
					}
				}
			}
		}

		public static IEnumerable<T> GetChildren<T>(this IAnimationDefinition instance, string name) where T : IAnimationDefinition
		{
			return instance.GetChildren<T>().Where(c => c.Name == name);
		}
		
		public static IEnumerable<T> GetChildren<T>(this IAnimationDefinition instance) where T : IAnimationDefinition
		{
			foreach (var child in instance.Children)
			{
				if (child is T value)
				{
					yield return value;
				}

				foreach (var t in child.GetChildren<T>())
				{
					yield return t;
				}
			}
		}
		
		public static bool TryGetFirstParent<T>(this IAnimationDefinition instance, out T result) where T : IAnimationDefinition
		{
			result = default;
			foreach (T parent in GetParents<T>(instance))
			{
				result = parent;
				return true;
			}

			return false;
		}
		
		public static IEnumerable<T> GetParents<T>(this IAnimationDefinition instance) where T : IAnimationDefinition
		{
			if (instance.Parent == null)
			{
				yield break;
			}

			if (instance.Parent is T value)
			{
				yield return value;
			}

			foreach (var t in instance.Parent.GetParents<T>())
			{
				yield return t;
			}
		}

		public static T AddChild<T>(this List<IAnimationDefinition> instance, T value) where T : IAnimationDefinition
		{
			instance?.Add(value);
			return value;
		}
	}
}