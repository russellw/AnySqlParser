namespace AnySqlParser;
public sealed class ForeignKey {
	public List<ColumnRef> Columns = new();
	public TableRef RefTable;
	public List<ColumnRef> RefColumns = new();
	public Action OnDelete = Action.NoAction;
	public Action OnUpdate = Action.NoAction;
}
