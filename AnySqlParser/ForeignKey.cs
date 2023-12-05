namespace AnySqlParser;
public sealed class ForeignKey {
	public List<string> Columns = new();

	public QualifiedName RefTableName = null!;
	public List<string> RefColumns = new();

	public Action OnDelete = Action.NoAction;
	public Action OnUpdate = Action.NoAction;
}
