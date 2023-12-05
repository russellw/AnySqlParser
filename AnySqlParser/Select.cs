namespace AnySqlParser;
public sealed class Select: Statement {
	public QueryExpression QueryExpression;

	public Expression? OrderBy;
	public bool Desc;

	public Select(QueryExpression queryExpression) {
		QueryExpression = queryExpression;
	}
}
