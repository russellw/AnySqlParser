namespace AnySqlParser;
public sealed class Cast: Expression {
	public Expression Operand;
	public DataType DataType;

	public Cast(Expression operand) {
		Operand = operand;
	}

	public override bool Equals(object? obj) {
		return obj is Cast cast && EqualityComparer<Expression>.Default.Equals(Operand, cast.Operand) &&
			   DataType.Equals(cast.DataType);
	}

	public override int GetHashCode() {
		return HashCode.Combine(Operand, DataType);
	}
}
