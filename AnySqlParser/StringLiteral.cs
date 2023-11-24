namespace AnySqlParser;
public sealed class StringLiteral: Expression {
	public string Value;

	public StringLiteral(Location location, string value): base(location) {
		Value = value;
	}

	public override bool Eq(Expression b0) {
		if (b0 is StringLiteral b)
			return Value == b.Value;
		return false;
	}
}
