namespace AnySqlParser;
public sealed class Use: Statement {
	public string DatabaseName;

	public Use(Location location, string databaseName): base(location) {
		DatabaseName = databaseName;
	}
}
