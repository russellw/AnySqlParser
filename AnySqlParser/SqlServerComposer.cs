namespace AnySqlParser;
public sealed class SqlServerComposer: Composer {
	public static string Compose(Database database) {
		var composer = new SqlServerComposer();
		composer.Add(database);
		return composer.sb.ToString();
	}
}
