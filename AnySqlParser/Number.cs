namespace AnySqlParser;
public sealed class Number: Expression {
	public string Value;

	public Number(string value) {
		Value = value;
	}

	public override bool Equals(object? obj) {
		return obj is Number number && Value == number.Value;
	}

	public override int GetHashCode() {
		return HashCode.Combine(Value);
	}
}
