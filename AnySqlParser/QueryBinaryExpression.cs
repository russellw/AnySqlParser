namespace AnySqlParser;
public sealed class QueryBinaryExpression: QueryExpression {
	public QueryExpression Left, Right;
	public QueryOp Op;

	public QueryBinaryExpression(QueryOp op, QueryExpression left, QueryExpression right) {
		Op = op;
		Left = left;
		Right = right;
	}
}
