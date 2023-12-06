namespace AnySqlParser;
public sealed class Table: Statement {
	public string Name;
	public List<Column> Columns = new();
	public Dictionary<string, Column> ColumnMap = new();
	public Key? PrimaryKey;
	public List<Key> Uniques = new();
	public List<ForeignKey> ForeignKeys = new();
	public List<Expression> Checks = new();

	public Table(string name) {
		Name = name;
	}

	public void Add(Location location, Column column) {
		Columns.Add(column);
		if (!ColumnMap.TryAdd(column.Name, column))
			throw new SqlError($"{location}: {this}.{column} already exists");
	}

	public void AddPrimaryKey(Location location, Key key) {
		if (PrimaryKey != null)
			throw new SqlError($"{location}: {this} already has a primary key");
		PrimaryKey = key;
	}

	public Column GetColumn(Location location, string name) {
		if (ColumnMap.TryGetValue(name, out Column? column))
			return column;
		throw new SqlError($"{location}: {this}.{name} not found");
	}

	public override string ToString() {
		return Name;
	}
}
