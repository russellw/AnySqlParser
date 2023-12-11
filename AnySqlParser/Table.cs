namespace AnySqlParser;
public sealed class Table {
	public string Name;
	public List<Column> Columns = new();
	public Dictionary<string, Column> ColumnMap = new();
	public Key? PrimaryKey;
	public List<Key> Uniques = new();
	public List<ForeignKey> ForeignKeys = new();
	public List<Check> Checks = new();

	public Table(string name) {
		Name = name;
	}

	public void Add(Column column) {
		Columns.Add(column);
		if (!ColumnMap.TryAdd(column.Name.ToLowerInvariant(), column))
			throw new SqlError($"{column.Location}: {this}.{column} already exists");
	}

	public void AddPrimaryKey(Key key) {
		if (PrimaryKey != null)
			throw new SqlError($"{key.Location}: {this} already has a primary key");
		PrimaryKey = key;
	}

	public Column GetColumn(Location location, string name) {
		if (ColumnMap.TryGetValue(name.ToLowerInvariant(), out Column? column))
			return column;
		throw new SqlError($"{location}: {this}.{name} not found");
	}
}
