namespace AnySqlParser;
public sealed class BinaryExpression: Expression {
	public BinaryOp Op;
	public Expression Left, Right;

	public BinaryExpression(BinaryOp op, Expression left, Expression right) {
		Op = op;
		Left = left;
		Right = right;
	}

	public override bool Equals(object? obj) {
		return obj is BinaryExpression expression && Op == expression.Op &&
			   EqualityComparer<Expression>.Default.Equals(Left, expression.Left) &&
			   EqualityComparer<Expression>.Default.Equals(Right, expression.Right);
	}

	public override int GetHashCode() {
		return HashCode.Combine(Op, Left, Right);
	}
}
