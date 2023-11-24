namespace AnySqlParser;
public sealed class DropDatabase: Statement {
	public bool IfExists;
	public List<string> Names = new();

	public DropDatabase(Location location): base(location) {
	}
}
