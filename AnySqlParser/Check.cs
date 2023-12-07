namespace AnySqlParser;
public sealed class Check: Element {
	public Expression Expression = null!;

	public Check(Location location): base(location) {
	}
}
