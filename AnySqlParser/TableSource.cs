namespace AnySqlParser {
public sealed class TableSource {
	public readonly Location Location;
	public QualifiedName TableOrViewName;

	public TableSource(Location location, QualifiedName tableOrViewName) {
		Location = location;
		TableOrViewName = tableOrViewName;
	}
}
}
