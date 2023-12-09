namespace AnySqlParser;
public sealed class StringLiteral: Expression {
	public string Value;

	public StringLiteral(string value) {
		Value = value;
	}

	public override bool Equals(object? obj) {
		return obj is StringLiteral literal && Value == literal.Value;
	}

	public override int GetHashCode() {
		return HashCode.Combine(Value);
	}
}
