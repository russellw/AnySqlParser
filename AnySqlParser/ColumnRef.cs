namespace AnySqlParser;
public struct ColumnRef {
	public readonly Location Location;
	public string Name;
	public Column? Column;
	public bool Desc;

	public ColumnRef(Location location, string name): this() {
		Location = location;
		Name = name;
	}
}
