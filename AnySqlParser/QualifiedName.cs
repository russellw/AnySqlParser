namespace AnySqlParser;
public sealed class QualifiedName: Expression {
	public List<string> Names = new();
	public bool Star;

	public QualifiedName() {
	}

	public QualifiedName(string name) {
		Names.Add(name);
	}

	public override bool Equals(object? obj) {
		return obj is QualifiedName name && Names.SequenceEqual(name.Names) && Star == name.Star;
	}

	public override int GetHashCode() {
		return HashCode.Combine(Names, Star);
	}
}
