namespace AnySqlParser;
public struct TableRef {
	public string Name;
	public Table? Table;

	public TableRef(string name): this() {
		Name = name;
	}
}
