namespace ExpressionUtility
{
	public interface IRealizable<out T>
	{
		T RealizeSelf();
		bool IsRealized { get; }
		
		bool IsRealizedRecursive { get; }
	}
}