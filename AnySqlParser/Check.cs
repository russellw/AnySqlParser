namespace AnySqlParser;
public sealed class Check {
	public readonly Location Location;
	public string? ConstraintName;

	public Expression Expression = null!;
	public bool ForReplication = true;

	public Check(Location location, string? constraintName) {
		Location = location;
		ConstraintName = constraintName;
	}
}
