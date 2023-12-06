namespace AnySqlParser;
public sealed class Key {
	public bool Primary;
	public List<ColumnRef> Columns = new();
	public List<string> Ignored = new();
}
