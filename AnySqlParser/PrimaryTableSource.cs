namespace AnySqlParser;
public sealed class PrimaryTableSource: TableSource {
	public QualifiedName TableOrViewName;
	public string? TableAlias;

	public PrimaryTableSource(QualifiedName tableOrViewName) {
		TableOrViewName = tableOrViewName;
	}
}
