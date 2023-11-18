namespace AnySqlParser {
public sealed class SelectColumn {
	public readonly Location Location;
	public Expression Expression;
	public Expression? ColumnAlias;

	public SelectColumn(Location location, Expression expression) {
		Location = location;
		Expression = expression;
	}
}

public sealed class Select: Statement {
	public bool All;
	public bool Distinct;

	public Expression? Top;
	public bool Percent;
	public bool WithTies;

	public List<SelectColumn> SelectList = new();

	public List<Expression> From = new();
	public Expression? Where;
	public Expression? GroupBy;
	public Expression? Having;
	public Expression? Window;

	public Expression? OrderBy;
	public bool Desc;

	public Select(Location location): base(location) {
	}
}
}
