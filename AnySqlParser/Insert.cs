namespace AnySqlParser;
public sealed class Insert: Statement {
	public QualifiedName TableName = null!;
	public List<string> Columns = new();
	public List<Expression> Values = new();

	public Insert(Location location): base(location) {
	}
}
