namespace AnySqlParser;
public sealed class Table: Statement {
	public string Name;
	public List<Column> Columns = new();
	public List<Key> Keys = new();
	public List<ForeignKey> ForeignKeys = new();
	public List<Expression> Checks = new();

	public Table(string name) {
		Name = name;
	}
}
