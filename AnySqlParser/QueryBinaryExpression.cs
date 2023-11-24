namespace AnySqlParser;
public sealed class QueryBinaryExpression: QueryExpression {
	public QueryOp Op;
	public QueryExpression Left, Right;

	public QueryBinaryExpression(Location location, QueryOp op, QueryExpression left, QueryExpression right): base(location) {
		Op = op;
		Left = left;
		Right = right;
	}
}
