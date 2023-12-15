namespace AnySqlParser;
public sealed class Schema {
	public Dictionary<string, Table> TableMap = new();
	public List<Table> Tables = new();

	public void Add(Location location, Table table) {
		if (!TableMap.TryAdd(table.Name.ToLowerInvariant(), table))
			throw new SqlError($"{location}: {table} already exists");
		Tables.Add(table);
	}

	public Table GetTable(Location location, string name) {
		if (TableMap.TryGetValue(name.ToLowerInvariant(), out Table? table))
			return table;
		throw new SqlError($"{location}: {name} not found");
	}
}
