namespace AnySqlParser;
public struct TableRef {
	public readonly Location Location;
	public string Name;
	public Table? Table;

	public TableRef(Location location, string name): this() {
		Location = location;
		Name = name;
	}
}
