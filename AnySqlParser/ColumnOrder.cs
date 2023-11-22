namespace AnySqlParser {
public sealed class ColumnOrder {
	public readonly Location Location;

	public string Name;
	public bool Desc;

	public ColumnOrder(Location location, string name) {
		Location = location;
		Name = name;
	}
}
}
