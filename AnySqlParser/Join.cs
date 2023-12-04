namespace AnySqlParser;
public sealed class Join: TableSource {
	public JoinType JoinType;
	public TableSource Left, Right;
	public Expression? On;

	public Join(JoinType joinType, TableSource left, TableSource right) {
		JoinType = joinType;
		Left = left;
		Right = right;
	}

	public Join(JoinType joinType, TableSource left, TableSource right, Expression on) {
		JoinType = joinType;
		Left = left;
		Right = right;
		On = on;
	}
}
