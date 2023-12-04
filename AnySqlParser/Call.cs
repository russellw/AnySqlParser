namespace AnySqlParser;
public sealed class Call: Expression {
	public QualifiedName Function;
	public List<Expression> Arguments = new();

	public Call(QualifiedName function) {
		Function = function;
	}
}
