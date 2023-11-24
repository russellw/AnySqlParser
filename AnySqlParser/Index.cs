namespace AnySqlParser;
public sealed class Index: Statement {
	public bool Unique;
	public bool? Clustered;
	public string Name = null!;
	public QualifiedName TableName = null!;
	public List<ColumnOrder> Columns = new();
	public List<string> Include = new();
	public Expression? Where;

	// Relational index options
	public bool? PadIndex;
	public int FillFactor = -1;
	public bool? SortInTempdb;
	public bool? IgnoreDupKey;
	public bool? StatisticsNorecompute;
	public bool? StatisticsIncremental;
	public bool? DropExisting;
	public bool? Online;
	public bool? Resumable;

	public int MaxDuration;
	public bool MaxDurationMinutes;

	public bool? AllowRowLocks;
	public bool? AllowPageLocks;
	public bool? OptimizeForSequentialKey;
	public int Maxdop = -1;

	public Index(Location location): base(location) {
	}
}
