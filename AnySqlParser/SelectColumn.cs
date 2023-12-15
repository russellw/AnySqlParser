namespace AnySqlParser;
public sealed class SelectColumn {
	public Expression? ColumnAlias;
	public Expression Expression;
	public Location Location;

	public SelectColumn(Location location, Expression expression) {
		Location = location;
		Expression = expression;
	}
}
