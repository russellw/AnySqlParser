namespace AnySqlParser;
public sealed class Key {
	public bool Primary;
	public List<Column> Columns = new();
	public List<string> Ignored = new();
}
