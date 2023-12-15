namespace AnySqlParser;
public sealed class Select {
	public bool Desc;
	public Expression? OrderBy;
	public QueryExpression QueryExpression;

	public Select(QueryExpression queryExpression) {
		QueryExpression = queryExpression;
	}
}
