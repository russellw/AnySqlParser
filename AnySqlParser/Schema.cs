namespace AnySqlParser;
public sealed class Schema {
	public List<Table> Tables = new();
	public Dictionary<string, Table> TableMap = new();

	public Table CreateTable(Location location, string name) {
		var table = new Table(name);
		if (!TableMap.TryAdd(table.Name, table))
			throw new SqlError($"{location}: {name} already exists");
		Tables.Add(table);
		return table;
	}

	public Table GetTable(Location location, string name) {
		if (TableMap.TryGetValue(name, out Table? table))
			return table;
		throw new SqlError($"{location}: {name} not found");
	}
}
