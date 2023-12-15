namespace AnySqlParser;
public sealed class Index: Statement {
	public List<ColumnRef> Columns = new();
	public List<string> Include = new();
	public string Name = null!;
	public QualifiedName TableName = null!;
	public bool Unique;
	public Expression? Where;

	public Index(bool unique) {
		Unique = unique;
	}
}
