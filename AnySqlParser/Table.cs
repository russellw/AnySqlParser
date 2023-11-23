namespace AnySqlParser {
public struct DataType {
	public QualifiedName TypeName;
	public int Size = -1;
	public int Scale = -1;

	public DataType(QualifiedName typeName) {
		TypeName = typeName;
	}
}

public sealed class Column {
	public readonly Location Location;
	public string Name;
	public DataType DataType;

	// Etc
	public bool Filestream;
	public bool Sparse;
	public Expression? Default;

	public bool Identity;
	public int IdentitySeed = -1;
	public int IdentityIncrement = -1;

	public bool ForReplication = true;
	public bool Nullable = true;
	public bool Rowguidcol;

	// Constraints
	public bool PrimaryKey;

	public Column(Location location, string name, DataType dataType) {
		Location = location;
		Name = name;
		DataType = dataType;
	}
}

public sealed class Key {
	public readonly Location Location;
	public string? ConstraintName;

	public bool Primary;
	public bool? Clustered;
	public List<ColumnOrder> Columns = new();

	public Key(Location location, string? constraintName) {
		Location = location;
		ConstraintName = constraintName;
	}
}

public enum Action {
	NoAction,
	Cascade,
	SetNull,
	SetDefault,
}

public sealed class ForeignKey {
	public readonly Location Location;
	public string? ConstraintName;

	public List<string> Columns = new();

	public QualifiedName RefTableName = null!;
	public List<string> RefColumns = new();

	public Action OnDelete = Action.NoAction;
	public Action OnUpdate = Action.NoAction;
	public bool ForReplication = true;

	public ForeignKey(Location location, string? constraintName) {
		Location = location;
		ConstraintName = constraintName;
	}
}

public sealed class Check {
	public readonly Location Location;
	public string? ConstraintName;

	public Expression Expression = null!;
	public bool ForReplication = true;

	public Check(Location location, string? constraintName) {
		Location = location;
		ConstraintName = constraintName;
	}
}

public abstract class StorageOption {
	public readonly Location Location;

	protected StorageOption(Location location) {
		Location = location;
	}
}

public sealed class PartitionSchemeRef: StorageOption {
	public string PartitionSchemeName;
	public string PartitionColumnName;

	public PartitionSchemeRef(Location location, string partitionSchemeName, string partitionColumnName): base(location) {
		PartitionSchemeName = partitionSchemeName;
		PartitionColumnName = partitionColumnName;
	}
}

public sealed class FilegroupRef: StorageOption {
	public string FilegroupName;

	public FilegroupRef(Location location, string filegroupName): base(location) {
		FilegroupName = filegroupName;
	}
}

public sealed class Table: Statement {
	public QualifiedName Name;
	public List<Column> Columns = new();

	// Table constraints
	public List<Key> Keys = new();
	public List<ForeignKey> ForeignKeys = new();
	public List<Check> Checks = new();
	public StorageOption? On;
	public StorageOption? TextimageOn;
	public StorageOption? FilestreamOn;

	public Table(Location location, QualifiedName name): base(location) {
		Name = name;
	}
}
}
