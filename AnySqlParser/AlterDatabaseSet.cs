namespace AnySqlParser;
public sealed class AlterDatabaseSet: Statement {
	public string? DatabaseName;
	public List<AlterDatabaseSetOption> Options = new();
	public Termination? Termination;

	public AlterDatabaseSet(Location location, string? databaseName): base(location) {
		DatabaseName = databaseName;
	}
}
