namespace AnySqlParser;
public sealed class Number: Expression {
	public string Value;

	public Number(string value) {
		Value = value;
	}

	public override bool Eq(Expression b0) {
		if (b0 is Number b)
			return Value == b.Value;
		return false;
	}
}
