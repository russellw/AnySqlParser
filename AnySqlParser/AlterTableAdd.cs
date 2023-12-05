namespace AnySqlParser;
public sealed class AlterTableAdd: Statement {
	public QualifiedName TableName;
	public List<Column> Columns = new();
	public List<Key> Keys = new();
	public List<ForeignKey> ForeignKeys = new();
	public List<Expression> Checks = new();

	public AlterTableAdd(QualifiedName tableName) {
		TableName = tableName;
	}
}
