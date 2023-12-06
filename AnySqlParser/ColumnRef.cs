namespace AnySqlParser;
public struct ColumnRef {
	public readonly Location Location;
	public string Name;
	public Column? Column;
	public bool Desc;

	public ColumnRef(Location location, string name) {
		Location = location;
		Name = name;
	}

	public ColumnRef(Column column) {
		Name = column.Name;
		Column = column;
	}
}
