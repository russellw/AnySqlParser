namespace AnySqlParser;
public sealed class Database {
	public List<Table> Tables = new();

	public void Add(IEnumerable<Statement> statements) {
		var adds = new List<AlterTableAdd>();
		foreach (var statement in statements)
			switch (statement) {
			case AlterTableAdd add:
				adds.Add(add);
				break;
			case Table table:
				Tables.Add(table);
				break;
			}
	}
}
