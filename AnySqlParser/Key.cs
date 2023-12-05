namespace AnySqlParser;
public sealed class Key {
	public bool Primary;
	public List<ColumnOrder> Columns = new();
}
