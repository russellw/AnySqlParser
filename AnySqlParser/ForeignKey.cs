namespace AnySqlParser;
public sealed class ForeignKey {
	public readonly Location Location;
	public string? ConstraintName;

	public List<string> Columns = new();

	public QualifiedName RefTableName = null!;
	public List<string> RefColumns = new();

	public Action OnDelete = Action.NoAction;
	public Action OnUpdate = Action.NoAction;
	public bool ForReplication = true;

	public ForeignKey(Location location, string? constraintName) {
		Location = location;
		ConstraintName = constraintName;
	}
}
