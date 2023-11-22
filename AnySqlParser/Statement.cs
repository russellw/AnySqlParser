namespace AnySqlParser {
public abstract class Statement {
	public readonly Location Location;

	protected Statement(Location location) {
		Location = location;
	}
}

public sealed class DropProcedure: Statement {
	public bool IfExists;
	public List<QualifiedName> Names = new();

	public DropProcedure(Location location): base(location) {
	}
}

public sealed class Go: Statement {
	public Go(Location location): base(location) {
	}
}

public sealed class DropView: Statement {
	public bool IfExists;
	public List<QualifiedName> Names = new();

	public DropView(Location location): base(location) {
	}
}

public sealed class DropTable: Statement {
	public bool IfExists;
	public List<QualifiedName> Names = new();

	public DropTable(Location location): base(location) {
	}
}

public sealed class If: Statement {
	public Expression condition = null!;
	public Statement then = null!;
	public Statement? @else;

	public If(Location location): base(location) {
	}
}

public sealed class Insert: Statement {
	public QualifiedName TableName = null!;
	public List<string> Columns = new();
	public List<Expression> Values = new();

	public Insert(Location location): base(location) {
	}
}

public sealed class Start: Statement {
	public Start(Location location): base(location) {
	}
}

public sealed class Commit: Statement {
	public Commit(Location location): base(location) {
	}
}

public sealed class Rollback: Statement {
	public Rollback(Location location): base(location) {
	}
}

public sealed class Block: Statement {
	public List<Statement> Body = new();

	public Block(Location location): base(location) {
	}
}

public sealed class SetGlobal: Statement {
	public string Name;
	public Expression Value;

	public SetGlobal(Location location, string name, Expression value): base(location) {
		Name = name;
		Value = value;
	}
}

public sealed class SetIdentityInsert: Statement {
	public QualifiedName Name;
	public bool Value;

	public SetIdentityInsert(Location location, QualifiedName name, bool value): base(location) {
		Name = name;
		Value = value;
	}
}

public sealed class View: Statement {
	public QualifiedName Name = null!;
	public Select Query = null!;

	public View(Location location): base(location) {
	}
}

public sealed class AlterTableCheckConstraints: Statement {
	public QualifiedName TableName;
	public bool Check;
	public List<string> ConstraintNames = new();

	public AlterTableCheckConstraints(Location location, QualifiedName tableName, bool check): base(location) {
		TableName = tableName;
		Check = check;
	}
}

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
}
