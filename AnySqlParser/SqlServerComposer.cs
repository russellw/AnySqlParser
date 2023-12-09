using System.Text;

namespace AnySqlParser;
public sealed class SqlServerComposer: Composer {
	public static string Compose(Database database) {
		var composer = new SqlServerComposer();
		composer.Add(database);
		return composer.sb.ToString();
	}

	protected override string Name(string s) {
		var sb = new StringBuilder();
		sb.Append('[');
		foreach (char c in s) {
			sb.Append(c);
			if (c == ']')
				sb.Append(c);
		}
		sb.Append(']');
		return sb.ToString();
	}
}
