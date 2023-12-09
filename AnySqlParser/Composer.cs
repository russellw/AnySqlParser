using System.Text;

namespace AnySqlParser;
public abstract class Composer {
	protected readonly StringBuilder sb = new();

	protected void Add(Database database) {
		foreach (var table in database.Tables) {
			Add(table);
			sb.AppendLine();
		}
		foreach (var table in database.Tables)
			foreach (var key in table.ForeignKeys)
				Add(table, key);
	}

	protected void Add(Table table, ForeignKey key) {
		sb.Append("ALTER TABLE ");
		sb.Append(Name(table.Name));
		sb.Append(" ADD FOREIGN KEY (");
		sb.Append(string.Join(',', key.Columns.Select(c => Name(c.Name))));
		sb.Append(") REFERENCES ");
		sb.Append(Name(key.RefTable.Name));
		sb.Append(" (");
		sb.Append(string.Join(',', key.RefColumns.Select(c => Name(c.Name))));
		sb.AppendLine(");");
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
		sb.Append(Name(column.Name));
		sb.Append(' ');
		Add(column.Type);
	}

	protected void Add(Table table) {
		sb.Append("CREATE TABLE ");
		sb.Append(Name(table.Name));
		sb.Append(" (\n");
		foreach (var column in table.Columns) {
			sb.Append('\t');
			Add(column);
			sb.AppendLine(",");
		}
		if (table.PrimaryKey != null) {
			sb.Append("\tPRIMARY KEY (");
			sb.Append(string.Join(',', table.PrimaryKey.Columns.Select(c => Name(c.Name))));
			sb.AppendLine("),");
		}
		foreach (var key in table.Uniques) {
			sb.Append("\tUNIQUE (");
			sb.Append(string.Join(',', key.Columns.Select(c => Name(c.Name))));
			sb.AppendLine("),");
		}
		sb.AppendLine(");");

		if (table.ExtraTokens.Count != 0) {
			sb.Append("--");
			foreach (var s in table.ExtraTokens) {
				sb.Append(' ');
				sb.Append(s);
			}
			sb.AppendLine();
		}
	}

	protected virtual string Name(string s) {
		return Etc.Quote(s, '"');
	}
}
