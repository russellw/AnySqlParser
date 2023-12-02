namespace AnySqlParser;
public sealed class Key {
	public readonly Location Location;

	public bool Primary;
	public bool? Clustered;
	public List<ColumnOrder> Columns = new();

	public Key(Location location) {
		Location = location;
	}
}
