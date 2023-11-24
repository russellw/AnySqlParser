namespace AnySqlParser;
public sealed class Key {
	public readonly Location Location;
	public string? ConstraintName;

	public bool Primary;
	public bool? Clustered;
	public List<ColumnOrder> Columns = new();
	public StorageOption? On;

	public Key(Location location, string? constraintName) {
		Location = location;
		ConstraintName = constraintName;
	}
}
