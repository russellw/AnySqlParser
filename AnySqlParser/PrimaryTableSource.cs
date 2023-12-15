namespace AnySqlParser;
public sealed class PrimaryTableSource: TableSource {
	public string? TableAlias;
	public QualifiedName TableOrViewName;

	public PrimaryTableSource(QualifiedName tableOrViewName) {
		TableOrViewName = tableOrViewName;
	}
}
