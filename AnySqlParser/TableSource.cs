namespace AnySqlParser {
public enum JoinType {
	Inner,
	Left,
	Right,
	Full,
}

public abstract class TableSource {
	public readonly Location Location;

	public TableSource(Location location) {
		Location = location;
	}
}

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

public sealed class PrimaryTableSource: TableSource {
	public QualifiedName TableOrViewName;
	public string? TableAlias;

	public PrimaryTableSource(Location location, QualifiedName tableOrViewName): base(location) {
		TableOrViewName = tableOrViewName;
	}
}
}
