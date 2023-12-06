namespace AnySqlParser;
public sealed class Key: Element {
	public bool Primary;
	public List<ColumnRef> Columns = new();

	public Key(Location location): base(location) {
	}
}
