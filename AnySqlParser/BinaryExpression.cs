namespace AnySqlParser;
public sealed class BinaryExpression: Expression {
	public BinaryOp Op;
	public Expression Left, Right;

	public BinaryExpression(BinaryOp op, Expression left, Expression right) {
		Op = op;
		Left = left;
		Right = right;
	}

	public override bool Eq(Expression b0) {
		if (b0 is BinaryExpression b)
			return Op == b.Op && Left.Eq(b.Left) && Right.Eq(b.Right);
		return false;
	}
}
