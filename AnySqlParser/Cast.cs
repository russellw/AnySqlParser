namespace AnySqlParser;
public sealed class Cast: Expression {
	public Expression Operand;
	public DataType DataType;

	public Cast(Location location, Expression operand): base(location) {
		Operand = operand;
	}

	public override bool Eq(Expression b0) {
		if (b0 is Cast b)
			return Operand.Eq(b.Operand) && DataType == b.DataType;
		return false;
	}
}
