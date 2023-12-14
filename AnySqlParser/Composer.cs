using System.Text;

namespace AnySqlParser;
public abstract class Composer {
	protected readonly StringBuilder sb = new();

	protected void Add(Schema schema) {
		foreach (var table in schema.Tables)
			Add(table);
		foreach (var table in schema.Tables)
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
		sb.Append(");\n");
	}

	protected void Add(Expression a) {
		sb.Append(a);
	}

	protected void Add(DataType type) {
		sb.Append(type.Name);
		if (null != type.Size) {
			sb.Append('(');
			Add(type.Size);
			if (null != type.Scale) {
				sb.Append(',');
				Add(type.Scale);
			}
			sb.Append(')');
			return;
		}
		if (null != type.Values) {
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
			sb.Append(",\n");
		}
		if (null != table.PrimaryKey) {
			sb.Append("\tPRIMARY KEY (");
			sb.Append(string.Join(',', table.PrimaryKey.Columns.Select(c => Name(c.Name))));
			sb.Append("),\n");
		}
		foreach (var key in table.Uniques) {
			sb.Append("\tUNIQUE (");
			sb.Append(string.Join(',', key.Columns.Select(c => Name(c.Name))));
			sb.Append("),\n");
		}
		sb.Remove(sb.Length - 2, 1);
		sb.Append(");\n");
	}

	protected virtual string Name(string s) {
		return Etc.Quote(s, '"');
	}
}
