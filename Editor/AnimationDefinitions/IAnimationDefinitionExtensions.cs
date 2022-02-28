using System.Collections.Generic;
using System.Linq;
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
			return def.FindAncestors<IAnimationDefinition>().Contains(parent);
		}

		public static bool IsRealizedRecursive(this IAnimationDefinition instance)
		{
			return instance.FindDescendants<IAnimationDefinition>().Any(i => !i.IsRealized);
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

		public static T FindDescendant<T>(this IAnimationDefinition instance, string name = "") where T : IAnimationDefinition
		{
			return instance.FindDescendants<T>(name).FirstOrDefault();
		}

		public static T FindAncestor<T>(this IAnimationDefinition instance, string name = "") where T : IAnimationDefinition
		{
			return instance.FindAncestors<T>(name).FirstOrDefault();
		}

		public static IEnumerable<T> FindDescendants<T>(this IAnimationDefinition instance, string name = "") where T : IAnimationDefinition
		{
			IEnumerable<T> Traverse(IAnimationDefinition i)
			{
				foreach (var child in i.Children)
				{
					if (child is T value)
					{
						yield return value;
					}

					foreach (var t in Traverse(child))
					{
						yield return t;
					}
				}
			}

			if (name == null)
			{
				return Enumerable.Empty<T>();
			}
			
			if (name != string.Empty)
			{
				return Traverse(instance).Where(c => c.Name == name);
			}
			return Traverse(instance);
		}

		public static IEnumerable<T> FindAncestors<T>(this IAnimationDefinition instance, string name = "") where T : IAnimationDefinition
		{
			IEnumerable<T> Traverse(IAnimationDefinition i)
			{
				if (i.Parent == null)
				{
					yield break;
				}

				if (i.Parent is T value)
				{
					yield return value;
				}

				foreach (var t in Traverse(i.Parent))
				{
					yield return t;
				}
			}
			
			if (name == null)
			{
				return Enumerable.Empty<T>();
			}
			
			if (name != string.Empty)
			{
				return Traverse(instance).Where(c => c.Name == name);
			}
			return Traverse(instance);
		}

		public static T AddChild<T>(this IAnimationDefinition instance, T value) where T : IAnimationDefinition
		{
			value.Parent = instance;
			instance.Children?.Add(value);
			return value;
		}
	}
}