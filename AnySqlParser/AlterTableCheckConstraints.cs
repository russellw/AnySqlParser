namespace AnySqlParser;
public sealed class AlterTableCheckConstraints: Statement {
	public QualifiedName TableName;
	public bool Check;
	public List<string> ConstraintNames = new();

	public AlterTableCheckConstraints(Location location, QualifiedName tableName, bool check): base(location) {
		TableName = tableName;
		Check = check;
	}
}
