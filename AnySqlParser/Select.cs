namespace AnySqlParser {
public enum QueryOp {
	Union,
	UnionAll,
	Except,
	Intersect,
}

public sealed class SelectColumn {
	public readonly Location Location;
	public Expression Expression;
	public Expression? ColumnAlias;

	public SelectColumn(Location location, Expression expression) {
		Location = location;
		Expression = expression;
	}
}

public abstract class QueryExpression {
	public readonly Location Location;

	public QueryExpression(Location location) {
		Location = location;
	}
}

public sealed class QueryBinaryExpression: QueryExpression {
	public QueryOp Op;
	public QueryExpression Left, Right;

	public QueryBinaryExpression(Location location, QueryOp op, QueryExpression left, QueryExpression right): base(location) {
		Op = op;
		Left = left;
		Right = right;
	}
}

public sealed class QuerySpecification: QueryExpression {
	public bool All;
	public bool Distinct;

	public Expression? Top;
	public bool Percent;
	public bool WithTies;

	public List<SelectColumn> SelectList = new();

	public List<TableSource> From = new();
	public Expression? Where;
	public List<Expression> GroupBy = new();
	public Expression? Having;
	public Expression? Window;

	public QuerySpecification(Location location): base(location) {
	}
}

public sealed class Select: Statement {
	public QueryExpression QueryExpression;

	public Expression? OrderBy;
	public bool Desc;

	public Select(Location location, QueryExpression queryExpression): base(location) {
		QueryExpression = queryExpression;
	}
}
}
