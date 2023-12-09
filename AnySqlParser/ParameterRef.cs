namespace AnySqlParser;
public sealed class ParameterRef: Expression {
	public string Name;

	public ParameterRef(string name) {
		Name = name;
	}

	public override bool Equals(object? obj) {
		return obj is ParameterRef @ref && Name == @ref.Name;
	}

	public override int GetHashCode() {
		return HashCode.Combine(Name);
	}
}
