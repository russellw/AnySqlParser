namespace AnySqlParser;
public sealed class QualifiedName: Expression {
	public List<string> Names = new();
	public bool Star;

	public QualifiedName() {
	}

	public QualifiedName(string name) {
		Names.Add(name);
	}

	public override bool Eq(Expression b0) {
		if (b0 is QualifiedName b)
			return Names == b.Names;
		return false;
	}
}
