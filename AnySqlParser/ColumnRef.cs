namespace AnySqlParser;
public struct ColumnRef {
	public string Name;
	public Column? Column;

	public ColumnRef(string name): this() {
		Name = name;
	}
}
