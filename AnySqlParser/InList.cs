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

	public override bool Equals(object? obj) {
		return obj is InList list && Not == list.Not && EqualityComparer<Expression>.Default.Equals(Left, list.Left) &&
			   Right.SequenceEqual(list.Right);
	}

	public override int GetHashCode() {
		return HashCode.Combine(Not, Left, Right);
	}
}
