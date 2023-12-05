namespace AnySqlParser;
public sealed class QueryBinaryExpression: QueryExpression {
	public QueryOp Op;
	public QueryExpression Left, Right;

	public QueryBinaryExpression(QueryOp op, QueryExpression left, QueryExpression right) {
		Op = op;
		Left = left;
		Right = right;
	}
}
