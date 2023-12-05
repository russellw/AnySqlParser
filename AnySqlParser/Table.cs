namespace AnySqlParser;
public sealed class Table: Statement {
	public QualifiedName Name;
	public List<Column> Columns = new();
	public List<Key> Keys = new();
	public List<ForeignKey> ForeignKeys = new();
	public List<Expression> Checks = new();

	public Table(QualifiedName name) {
		Name = name;
	}
}
