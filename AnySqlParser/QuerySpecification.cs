namespace AnySqlParser;
public sealed class QuerySpecification: QueryExpression {
	public bool All;
	public bool Distinct;
	public List<TableSource> From = new();
	public List<Expression> GroupBy = new();
	public Expression? Having;
	public bool Percent;
	public List<SelectColumn> SelectList = new();
	public Expression? Top;
	public Expression? Where;
	public Expression? Window;
	public bool WithTies;
}
