namespace AnySqlParser;
public sealed class Join: TableSource {
	public JoinType JoinType;
	public TableSource Left, Right;
	public Expression? On;

	public Join(Location location, JoinType joinType, TableSource left, TableSource right): base(location) {
		JoinType = joinType;
		Left = left;
		Right = right;
	}

	public Join(Location location, JoinType joinType, TableSource left, TableSource right, Expression on): base(location) {
		JoinType = joinType;
		Left = left;
		Right = right;
		On = on;
	}
}
