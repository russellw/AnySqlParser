namespace AnySqlParser;
public sealed class UnaryExpression: Expression {
	public UnaryOp Op;
	public Expression Operand;

	public UnaryExpression(UnaryOp op, Expression operand) {
		Op = op;
		Operand = operand;
	}

	public override bool Equals(object? obj) {
		return obj is UnaryExpression expression && Op == expression.Op &&
			   EqualityComparer<Expression>.Default.Equals(Operand, expression.Operand);
	}

	public override int GetHashCode() {
		return HashCode.Combine(Op, Operand);
	}
}
