namespace AnySqlParser;
public sealed class QuerySpecification: QueryExpression {
	public bool All;
	public bool Distinct;

	public Expression? Top;
	public bool Percent;
	public bool WithTies;

	public List<SelectColumn> SelectList = new();

	public List<TableSource> From = new();
	public Expression? Where;
	public List<Expression> GroupBy = new();
	public Expression? Having;
	public Expression? Window;

	public QuerySpecification(Location location): base(location) {
	}
}
