namespace AnySqlParser;
public sealed class UnaryExpression: Expression {
	public UnaryOp Op;
	public Expression Operand;

	public UnaryExpression(UnaryOp op, Expression operand) {
		Op = op;
		Operand = operand;
	}

	public override bool Eq(Expression b0) {
		if (b0 is UnaryExpression b)
			return Op == b.Op && Operand.Eq(b.Operand);
		return false;
	}
}
