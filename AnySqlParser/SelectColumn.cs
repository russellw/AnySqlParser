namespace AnySqlParser;
public sealed class SelectColumn {
	public readonly Location Location;
	public Expression Expression;
	public Expression? ColumnAlias;

	public SelectColumn(Location location, Expression expression) {
		Location = location;
		Expression = expression;
	}
}
