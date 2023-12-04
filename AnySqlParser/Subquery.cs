namespace AnySqlParser;
public sealed class Subquery: Expression {
	public QueryExpression Query;

	public Subquery(QueryExpression query) {
		Query = query;
	}
}
