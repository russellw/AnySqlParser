namespace AnySqlParser;
public sealed class PrimaryTableSource: TableSource {
	public QualifiedName TableOrViewName;
	public string? TableAlias;

	public PrimaryTableSource(Location location, QualifiedName tableOrViewName): base(location) {
		TableOrViewName = tableOrViewName;
	}
}
