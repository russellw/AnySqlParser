namespace AnySqlParser;
public sealed class Null: Expression {
	public Null() {
	}

	public override bool Eq(Expression b) {
		return b is Null;
	}
}
