namespace AnySqlParser;
public sealed class ParameterRef: Expression {
	public string Name;

	public ParameterRef(string name) {
		Name = name;
	}

	public override bool Eq(Expression b0) {
		if (b0 is ParameterRef b)
			return Name == b.Name;
		return false;
	}
}
