namespace AnySqlParser;
public sealed class Database {
	public List<Table> Tables = new();

	public void Add(IEnumerable<Statement> statements) {
		foreach (var statement in statements)
			switch (statement) {
			case Table table:
				Tables.Add(table);
				break;
			}
	}
}
