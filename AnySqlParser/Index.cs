namespace AnySqlParser;
public sealed class Index: Statement {
	public bool Unique;
	public string Name = null!;
	public QualifiedName TableName = null!;
	public List<ColumnRef> Columns = new();
	public List<string> Include = new();
	public Expression? Where;

	public Index(bool unique) {
		Unique = unique;
	}
}
