namespace AnySqlParser;
public sealed class Table: Statement {
	public bool Adding;
	public string Name;
	public List<Column> Columns = new();
	public Dictionary<string, Column> ColumnMap = new();
	public Key? PrimaryKey;
	public List<Key> Uniques = new();
	public List<ForeignKey> ForeignKeys = new();
	public List<Check> Checks = new();

	public Table(bool adding, string name) {
		Adding = adding;
		Name = name;
	}

	public override void AddTo(Schema schema) {
		schema.Tables.Add(this);
	}

	public void Add(Column column) {
		Columns.Add(column);
		if (!ColumnMap.TryAdd(column.Name, column))
			throw new SqlError($"{column.Location}: {this}.{column} already exists");
	}

	public void AddPrimaryKey(Key key) {
		if (PrimaryKey != null)
			throw new SqlError($"{key.Location}: {this} already has a primary key");
		PrimaryKey = key;
	}

	public Column GetColumn(Location location, string name) {
		if (ColumnMap.TryGetValue(name, out Column? column))
			return column;
		throw new SqlError($"{location}: {this}.{name} not found");
	}
}
