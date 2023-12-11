namespace AnySqlParser;
public sealed class Cast: Expression {
	public Expression Operand;
	public DataType Type;

	public Cast(Expression operand) {
		Operand = operand;
	}

	public Cast(Expression operand, DataType type) {
		Operand = operand;
		Type = type;
	}

	public override bool Equals(object? obj) {
		return obj is Cast cast && EqualityComparer<Expression>.Default.Equals(Operand, cast.Operand) && Type.Equals(cast.Type);
	}

	public override int GetHashCode() {
		return HashCode.Combine(Operand, Type);
	}
}
