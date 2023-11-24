namespace AnySqlParser;
public sealed class UnaryExpression: Expression {
	public UnaryOp Op;
	public Expression Operand;

	public UnaryExpression(Location location, UnaryOp op, Expression operand): base(location) {
		Op = op;
		Operand = operand;
	}

	public override bool Eq(Expression b0) {
		if (b0 is UnaryExpression b)
			return Op == b.Op && Operand.Eq(b.Operand);
		return false;
	}
}
