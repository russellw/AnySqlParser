namespace AnySqlParser;
public sealed class Key: Element {
	public List<ColumnRef> Columns = new();
	public bool Primary;

	public Key(Location location): base(location) {
	}
}
