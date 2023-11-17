namespace AnySqlParser {
public enum UnaryOp {
	Not,
	BitNot,
	Minus,
	Exists,
}

public sealed class UnaryExpression: Expression {
	public UnaryOp Op;
	public Expression Operand;

	public UnaryExpression(Location location, UnaryOp op, Expression operand): base(location) {
		Op = op;
		Operand = operand;
	}

	public override bool Eq(Expression b) {
		if (b is UnaryExpression b1)
			return Op == b1.Op && Operand.Eq(b1.Operand);
		return false;
	}
}
}
