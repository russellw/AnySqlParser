namespace AnySqlParser;
public enum BinaryOp {
	Add,
	Subtract,
	Multiply,
	Divide,
	Remainder,
	Equal,
	NotEqual,
	Less,
	LessEqual,
	Greater,
	GreaterEqual,
	And,
	Or,
	BitAnd,
	BitOr,
	BitXor,
	Concat,
}

public sealed class BinaryExpression: Expression {
	public BinaryOp Op;
	public Expression Left, Right;

	public BinaryExpression(Location location, BinaryOp op, Expression left, Expression right): base(location) {
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
