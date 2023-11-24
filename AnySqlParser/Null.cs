namespace AnySqlParser;
public sealed class Null: Expression {
	public Null(Location location): base(location) {
	}

	public override bool Eq(Expression b) {
		return b is Null;
	}
}
