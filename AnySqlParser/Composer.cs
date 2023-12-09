using System.Text;

namespace AnySqlParser;
public abstract class Composer {
	protected readonly StringBuilder sb = new();

	protected void Add(Database database) {
		foreach (var table in database.Tables) {
			Add(table);
		}
	}

	protected void Add(DataType type) {
		sb.Append(type.Name);
		if (type.Size >= 0) {
			sb.Append('(');
			sb.Append(type.Size);
			if (type.Scale >= 0) {
				sb.Append(',');
				sb.Append(type.Scale);
			}
			sb.Append(')');
			return;
		}
		if (type.Values != null) {
			sb.Append('(');
			sb.Append(string.Join(',', type.Values.Select(s => Etc.Quote(s, '\''))));
			sb.Append(')');
		}
	}

	protected void Add(Column column) {
		Name(column.Name);
		sb.Append(' ');
		sb.Append(column.Type);
	}

	protected void Add(Table table) {
		sb.Append("CREATE TABLE ");
		Name(table.Name);
		sb.Append("(\n");
		foreach (var column in table.Columns) {
			sb.Append('\t');
			Add(column);
			sb.Append(",\n");
		}
		sb.Append(")\n");
	}

	protected virtual void Name(string name) {
		sb.Append(Etc.Quote(name, '"'));
	}
}
