namespace AnySqlParser;
public sealed class InList: Expression {
	public bool Not;
	public Expression Left;
	public List<Expression> Right;

	public InList(bool not, Expression left, List<Expression> right) {
		Not = not;
		Left = left;
		Right = right;
	}
}
