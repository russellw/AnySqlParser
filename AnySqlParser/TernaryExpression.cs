namespace AnySqlParser;
public sealed class TernaryExpression: Expression {
	public TernaryOp Op;
	public Expression First, Second, Third;

	public TernaryExpression(TernaryOp op, Expression first, Expression second, Expression third) {
		Op = op;
		First = first;
		Second = second;
		Third = third;
	}

	public override bool Equals(object? obj) {
		return obj is TernaryExpression expression && Op == expression.Op &&
			   EqualityComparer<Expression>.Default.Equals(First, expression.First) &&
			   EqualityComparer<Expression>.Default.Equals(Second, expression.Second) &&
			   EqualityComparer<Expression>.Default.Equals(Third, expression.Third);
	}

	public override int GetHashCode() {
		return HashCode.Combine(Op, First, Second, Third);
	}
}
