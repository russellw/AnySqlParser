namespace AnySqlParser;
public sealed class Null: Expression {
	public Null() {
	}

	public override bool Equals(object? obj) {
		return obj is Null;
	}

	public override int GetHashCode() {
		return 0;
	}
}
