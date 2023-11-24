namespace AnySqlParser;
public sealed class DropTable: Statement {
	public bool IfExists;
	public List<QualifiedName> Names = new();

	public DropTable(Location location): base(location) {
	}
}
