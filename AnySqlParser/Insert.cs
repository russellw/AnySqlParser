namespace AnySqlParser;
public sealed class Insert: Statement {
	public List<string> Columns = new();
	public QualifiedName TableName = null!;
	public List<Expression> Values = new();
}
