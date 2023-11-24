namespace AnySqlParser;
public sealed class SetIdentityInsert: Statement {
	public QualifiedName Name;
	public bool Value;

	public SetIdentityInsert(Location location, QualifiedName name, bool value): base(location) {
		Name = name;
		Value = value;
	}
}
