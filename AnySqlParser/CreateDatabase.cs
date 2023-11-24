namespace AnySqlParser;
public sealed class CreateDatabase: Statement {
	public string Name;
	public Containment? Containment;

	public CreateDatabase(Location location, string name): base(location) {
		Name = name;
	}
}
