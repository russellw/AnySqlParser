namespace AnySqlParser {
public sealed class ColumnOrder {
	public readonly Location Location;

	public string Name;
	public bool Desc;

	public ColumnOrder(Location location, string name) {
		Location = location;
		Name = name;
	}
}

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

public sealed class Select: Statement {
	public bool All;
	public bool Distinct;

	public Expression? Top;
	public bool Percent;
	public bool WithTies;

	public List<Expression> SelectList = new();

	public List<Expression> From = new();
	public Expression? Where;
	public Expression? GroupBy;
	public Expression? Having;
	public Expression? Window;

	public Expression? OrderBy;
	public bool Desc;

	public Select(Location location): base(location) {
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

public sealed class SetParameter: Statement {
	public string Name = null!;
	public string Value = null!;

	public SetParameter(Location location): base(location) {
	}
}

public sealed class View: Statement {
	public QualifiedName Name = null!;
	public Select Query = null!;

	public View(Location location): base(location) {
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
