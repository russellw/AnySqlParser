namespace AnySqlParser;
public sealed class Subquery: Expression {
	public QueryExpression Query;

	public Subquery(Location location, QueryExpression query): base(location) {
		Query = query;
	}
}
