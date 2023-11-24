namespace AnySqlParser;
public sealed class DropView: Statement {
	public bool IfExists;
	public List<QualifiedName> Names = new();

	public DropView(Location location): base(location) {
	}
}
