using System.Text;

namespace AnySqlParser;
public abstract class Composer {
	protected readonly StringBuilder sb = new();

	protected void Add(Database database) {
		foreach (var table in database.Tables) {
			Add(table);
		}
	}

	protected void Add(Column column) {
		Name(column.Name);
		sb.Append(' ');
	}

	protected void Add(Table table) {
		sb.Append("CREATE TABLE ");
		Name(table.Name);
		sb.Append("(\n");
		foreach (var column in table.Columns) {
			Add(column);
		}
		sb.Append(")\n");
	}

	protected virtual void Name(string name) {
		sb.Append(Etc.Quote(name, '"'));
	}
}
