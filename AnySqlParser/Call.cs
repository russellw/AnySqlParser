namespace AnySqlParser;
public sealed class Call: Expression {
	public QualifiedName Function;
	public List<Expression> Arguments = new();

	public Call(Location location, QualifiedName function): base(location) {
		Function = function;
	}
}
