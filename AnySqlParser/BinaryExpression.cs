namespace AnySqlParser {
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

	public override bool Eq(Expression b) {
		if (b is BinaryExpression b1)
			return Op == b1.Op && Left.Eq(b1.Left) && Right.Eq(b1.Right);
		return false;
	}
}
}
