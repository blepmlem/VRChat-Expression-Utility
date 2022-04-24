using System.IO;

namespace ExpressionUtility
{
	public interface IRealizable<out T>
	{
		T RealizeSelf(DirectoryInfo creationDirectory);
		bool IsRealized { get; }
	}
}