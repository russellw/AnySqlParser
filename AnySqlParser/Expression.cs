namespace AnySqlParser {
public abstract class Expression {
	public readonly Location Location;

	protected Expression(Location location) {
		Location = location;
	}

	// The omission of an override for Equals is intentional
	// in most cases, syntax trees have reference semantics
	// equality comparison by value is useful only in unusual situations
	// and should not be the default
	public virtual bool Eq(Expression b) {
		return this == b;
	}
}

public sealed class Call: Expression {
	public QualifiedName Function;
	public List<Expression> Arguments = new();

	public Call(Location location, QualifiedName function): base(location) {
		Function = function;
	}
}

public sealed class Exists: Expression {
	public Select Query;

	public Exists(Location location, Select query): base(location) {
		Query = query;
	}
}

public sealed class StringLiteral: Expression {
	public string Value;

	public StringLiteral(Location location, string value): base(location) {
		Value = value;
	}

	public override bool Eq(Expression b) {
		if (b is StringLiteral b1)
			return Value == b1.Value;
		return false;
	}
}

public sealed class Subquery: Expression {
	public QueryExpression Query;

	public Subquery(Location location, QueryExpression query): base(location) {
		Query = query;
	}
}

public sealed class QualifiedName: Expression {
	public List<string> Names = new();
	public bool Star;

	public QualifiedName(Location location): base(location) {
	}

	public QualifiedName(Location location, string name): base(location) {
		Names.Add(name);
	}

	public override bool Eq(Expression b) {
		if (b is QualifiedName b1)
			return Names == b1.Names;
		return false;
	}
}

public sealed class Number: Expression {
	public string Value;

	public Number(Location location, string value): base(location) {
		Value = value;
	}

	public override bool Eq(Expression b) {
		if (b is Number b1)
			return Value == b1.Value;
		return false;
	}
}

public sealed class Null: Expression {
	public Null(Location location): base(location) {
	}

	public override bool Eq(Expression b) {
		return b is Null;
	}
}
}
