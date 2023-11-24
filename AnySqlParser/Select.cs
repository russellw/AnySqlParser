namespace AnySqlParser;
public sealed class Select: Statement {
	public QueryExpression QueryExpression;

	public Expression? OrderBy;
	public bool Desc;

	public Select(Location location, QueryExpression queryExpression): base(location) {
		QueryExpression = queryExpression;
	}
}
