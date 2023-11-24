namespace AnySqlParser;
public sealed class SetGlobal: Statement {
	public string Name;
	public Expression Value;

	public SetGlobal(Location location, string name, Expression value): base(location) {
		Name = name;
		Value = value;
	}
}
