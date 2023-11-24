namespace AnySqlParser;
public sealed class DropProcedure: Statement {
	public bool IfExists;
	public List<QualifiedName> Names = new();

	public DropProcedure(Location location): base(location) {
	}
}
