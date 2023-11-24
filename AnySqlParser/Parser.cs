using System.Diagnostics;
using System.Text;

namespace AnySqlParser;
public sealed class Parser {
	public static IEnumerable<Statement> Parse(string file) {
		using var reader = new StreamReader(file);
		var parser = new Parser(reader, file, 1);
		while (parser.token != -1)
			yield return parser.StatementSemicolon();
	}

	public static IEnumerable<Statement> Parse(TextReader reader, string file = "SQL", int line = 1) {
		var parser = new Parser(reader, file, line);
		while (parser.token != -1)
			yield return parser.StatementSemicolon();
	}

	const int kDoublePipe = -2;
	const int kGreaterEqual = -3;
	const int kLessEqual = -4;
	const int kNotEqual = -5;
	const int kNumber = -6;
	const int kQuotedName = -7;
	const int kStringLiteral = -8;
	const int kWord = -9;

	readonly TextReader reader;
	readonly string file;
	int line;
	int ch;
	int token;
	string tokenString = null!;

	Parser(TextReader reader, string file, int line) {
		this.reader = reader;
		this.file = file;
		this.line = line;
		Read();
		Lex();
	}

	Statement StatementSemicolon() {
		var a = Statement();
		Eat(';');
		return a;
	}

	Statement Statement() {
		var location = new Location(file, line);
		switch (Keyword()) {
		case "raiserror": {
			Lex();
			var a = new Raiserror(location);
			Expect('(');
			do
				a.Arguments.Add(Expression());
			while (Eat(','));
			Expect(')');
			if (Eat("with"))
				do
					switch (Keyword()) {
					case "log":
						Lex();
						a.Log = true;
						break;
					case "nowait":
						Lex();
						a.Nowait = true;
						break;
					case "seterror":
						Lex();
						a.Seterror = true;
						break;
					default:
						throw ErrorToken("expected option");
					}
				while (Eat(','));
			return a;
		}
		case "declare": {
			Lex();
			Expect('@');
			var a = new Declare(location);
			do {
				location = new Location(file, line);
				var name = Name();
				switch (Keyword()) {
				case "cursor":
					Lex();
					a.CursorVariables.Add(new CursorVariable(location, name));
					continue;
				case "as":
					Lex();
					break;
				}
				var b = new LocalVariable(location, name, DataType());
				if (Eat('='))
					b.Value = Expression();
				a.LocalVariables.Add(b);
			} while (Eat(','));
			return a;
		}
		case "exec":
		case "execute": {
			Lex();
			var a = new ExecuteProcedure(location, Name());
			do
				a.Arguments.Add(Expression());
			while (Eat(','));
			return a;
		}
		case "go":
			Lex();
			return new Go(location);
		case "use":
			Lex();
			return new Use(location, Name());
		case "if": {
			Lex();
			var a = new If(location);
			a.condition = Expression();
			a.then = StatementSemicolon();
			if (Eat("else"))
				a.@else = StatementSemicolon();
			return a;
		}
		case "set":
			Lex();
			switch (Keyword()) {
			case "identity_insert":
				Lex();
				return new SetIdentityInsert(location, QualifiedName(), OnOff());
			}
			return new SetGlobal(location, Name(), Expression());
		case "begin":
			Lex();
			switch (Keyword()) {
			case "transaction":
			case "tran":
				Lex();
				return new Start(location);
			default: {
				var a = new Block(location);
				while (!Eat("end"))
					a.Body.Add(StatementSemicolon());
				return a;
			}
			}
		case "start":
			Lex();
			switch (Keyword()) {
			case "transaction":
			case "tran":
				Lex();
				break;
			}
			return new Start(location);
		case "commit":
			Lex();
			switch (Keyword()) {
			case "transaction":
			case "tran":
				Lex();
				break;
			}
			return new Commit(location);
		case "rollback":
			Lex();
			switch (Keyword()) {
			case "transaction":
			case "tran":
				Lex();
				break;
			}
			return new Rollback(location);
		case "select":
			return Select();
		case "insert": {
			Lex();
			Eat("into");
			var a = new Insert(location);

			// Table
			a.TableName = QualifiedName();

			// Columns
			if (Eat('(')) {
				do
					a.Columns.Add(Name());
				while (Eat(','));
				Expect(')');
			}

			// Values
			Expect("values");
			Expect('(');
			do
				a.Values.Add(Expression());
			while (Eat(','));
			Expect(')');

			return a;
		}
		case "create": {
			Lex();
			var unique = Eat("unique");
			var clustered = Clustered();
			switch (Keyword()) {
			case "database": {
				Lex();
				var a = new CreateDatabase(location, Name());
				if (Eat("containment")) {
					Expect('=');
					switch (Keyword()) {
					case "none":
						a.Containment = Containment.None;
						break;
					case "partial":
						a.Containment = Containment.Partial;
						break;
					default:
						throw ErrorToken("expected containment");
					}
				}
				return a;
			}
			case "index": {
				Lex();
				var a = new Index(location);
				a.Unique = unique;
				a.Clustered = clustered;
				a.Name = Name();

				// Table
				Expect("on");
				a.TableName = QualifiedName();

				// Columns
				Expect('(');
				do
					a.Columns.Add(ColumnOrder());
				while (Eat(','));
				Expect(')');

				// Include
				if (Eat("include")) {
					Expect('(');
					do
						a.Include.Add(Name());
					while (Eat(','));
					Expect(')');
				}

				// Where
				if (Eat("where"))
					a.Where = Expression();

				// Relational index options
				if (Eat("with")) {
					Expect('(');
					do
						switch (Keyword()) {
						case "pad_index":
							Lex();
							Eat('=');
							a.PadIndex = OnOff();
							break;
						case "sort_in_tempdb":
							Lex();
							Eat('=');
							a.SortInTempdb = OnOff();
							break;
						case "ignore_dup_key":
							Lex();
							Eat('=');
							a.IgnoreDupKey = OnOff();
							break;
						case "statistics_norecompute":
							Lex();
							Eat('=');
							a.StatisticsNorecompute = OnOff();
							break;
						case "statistics_incremental":
							Lex();
							Eat('=');
							a.StatisticsIncremental = OnOff();
							break;
						case "drop_existing":
							Lex();
							Eat('=');
							a.DropExisting = OnOff();
							break;
						case "online":
							Lex();
							Eat('=');
							a.Online = OnOff();
							break;
						case "resumable":
							Lex();
							Eat('=');
							a.Resumable = OnOff();
							break;
						case "allow_row_locks":
							Lex();
							Eat('=');
							a.AllowRowLocks = OnOff();
							break;
						case "allow_page_locks":
							Lex();
							Eat('=');
							a.AllowPageLocks = OnOff();
							break;
						case "optimize_for_sequential_key":
							Lex();
							Eat('=');
							a.OptimizeForSequentialKey = OnOff();
							break;
						case "fillfactor":
							Lex();
							Eat('=');
							a.FillFactor = Int();
							break;
						case "maxdop":
							Lex();
							Eat('=');
							a.Maxdop = Int();
							break;
						case "max_duration":
							Lex();
							Eat('=');
							a.MaxDuration = Int();
							a.MaxDurationMinutes = Eat("minutes");
							break;
						default:
							throw ErrorToken("expected relational index option");
						}
					while (Eat(','));
					Expect(')');
				}
				return a;
			}
			case "view": {
				Lex();
				var a = new View(location);
				a.Name = QualifiedName();
				Expect("as");
				a.Query = Select();
				return a;
			}
			case "proc":
			case "procedure":
				return Procedure();
			case "table":
				return Table();
			}
			throw ErrorToken("expected noun");
		}
		case "checkpoint": {
			Lex();
			var a = new Checkpoint(location);
			if (token == kNumber)
				a.Duration = Int();
			return a;
		}
		case "drop":
			Lex();
			switch (Keyword()) {
			case "proc":
			case "procedure": {
				Lex();
				var a = new DropProcedure(location);
				if (Eat("if")) {
					Expect("exists");
					a.IfExists = true;
				}
				do
					a.Names.Add(QualifiedName());
				while (Eat(','));
				return a;
			}
			case "view": {
				Lex();
				var a = new DropView(location);
				if (Eat("if")) {
					Expect("exists");
					a.IfExists = true;
				}
				do
					a.Names.Add(QualifiedName());
				while (Eat(','));
				return a;
			}
			case "database": {
				Lex();
				var a = new DropDatabase(location);
				if (Eat("if")) {
					Expect("exists");
					a.IfExists = true;
				}
				do
					a.Names.Add(Name());
				while (Eat(','));
				return a;
			}
			case "table": {
				Lex();
				var a = new DropTable(location);
				if (Eat("if")) {
					Expect("exists");
					a.IfExists = true;
				}
				do
					a.Names.Add(QualifiedName());
				while (Eat(','));
				return a;
			}
			}
			throw ErrorToken("expected noun");
		case "alter": {
			Lex();
			switch (Keyword()) {
			case "database": {
				Lex();
				string? databaseName = null;
				if (!Eat("current"))
					databaseName = Name();
				switch (Keyword()) {
				case "set": {
					var a = new AlterDatabaseSet(location, databaseName);
					do
						switch (Keyword()) {
						case "torn_page_detection":
							Lex();
							a.Options.Add(new TornPageDetection(OnOff()));
							break;
						case "page_verify":
							Lex();
							switch (Keyword()) {
							case "checksum":
								Lex();
								a.Options.Add(new PageVerifyChecksum());
								break;
							case "none":
								Lex();
								a.Options.Add(new PageVerifyNone());
								break;
							case "torn_page_detection":
								Lex();
								a.Options.Add(new PageVerifyTornPageDetection());
								break;
							default:
								throw ErrorToken("expected recovery option");
							}
							break;
						case "recovery":
							Lex();
							switch (Keyword()) {
							case "full":
								Lex();
								a.Options.Add(new RecoveryFull());
								break;
							case "bulk_logged":
								Lex();
								a.Options.Add(new RecoveryBulkLogged());
								break;
							case "simple":
								Lex();
								a.Options.Add(new RecoverySimple());
								break;
							default:
								throw ErrorToken("expected recovery option");
							}
							break;
						}
					while (Eat(','));
					if (Eat("with"))
						switch (Keyword()) {
						case "no_wait":
							Lex();
							a.Termination = new NoWait();
							break;
						case "rollback":
							Lex();
							switch (Keyword()) {
							case "after":
								Lex();
								a.Termination = new RollbackAfter(Int());
								Eat("seconds");
								break;
							case "immediate":
								Lex();
								a.Termination = new RollbackImmediate();
								break;
							default:
								throw ErrorToken("expected termination option");
							}
							break;
						}
					return a;
				}
				}
				throw ErrorToken("unknown syntax");
			}
			case "table": {
				Lex();
				var tableName = QualifiedName();
				switch (Keyword()) {
				case "add": {
					Lex();
					var a = new AlterTableAdd(location, tableName);
					do {
						string? constraintName = null;
						if (Eat("constraint"))
							constraintName = Name();
						switch (Keyword()) {
						case "foreign":
							a.ForeignKeys.Add(ForeignKey(constraintName));
							break;
						case "check":
							a.Checks.Add(Check(constraintName));
							break;
						case "primary":
						case "unique":
							a.Keys.Add(Key(constraintName));
							break;
						default:
							if (constraintName != null)
								throw ErrorToken("expected constraint");
							a.Columns.Add(Column());
							break;
						}
					} while (Eat(','));
					return a;
				}
				case "with":
					Lex();
					switch (Keyword()) {
					case "check":
						Lex();
						switch (Keyword()) {
						case "constraint":
							return AlterTableCheckConstraints(tableName, true);
						}
						break;
					case "nocheck":
						Lex();
						switch (Keyword()) {
						case "constraint":
							return AlterTableCheckConstraints(tableName, false);
						}
						break;
					}
					break;
				case "check":
					Lex();
					switch (Keyword()) {
					case "constraint":
						return AlterTableCheckConstraints(tableName, true);
					}
					break;
				case "nocheck":
					Lex();
					switch (Keyword()) {
					case "constraint":
						return AlterTableCheckConstraints(tableName, false);
					}
					break;
				}
				throw ErrorToken("unknown syntax");
			}
			}
			throw ErrorToken("expected noun");
		}
		}
		throw ErrorToken("expected statement");
	}

	Procedure Procedure() {
		Debug.Assert(Keyword() == "proc" || Keyword() == "procedure");
		var location = new Location(file, line);
		Lex();
		var a = new Procedure(location, QualifiedName());
		if (Eat(';'))
			a.Number = Int();

		// Parameters
		if (token == '@')
			do
				a.Parameters.Add(Parameter());
			while (Eat(','));

		// Options
		if (Eat("with"))
			do
				switch (Keyword()) {
				case "encryption":
					a.Encryption = true;
					break;
				case "recompile":
					a.Recompile = true;
					break;
				default:
					throw ErrorToken("expected procedure option");
				}
			while (Eat(','));
		if (Eat("for")) {
			Expect("replication");
			a.ForReplication = true;
		}

		// Body
		Expect("as");
		// https://stackoverflow.com/questions/41802057/the-end-of-the-body-of-the-sql-stored-procecdure-where-is-it
		while (!Eat("go"))
			a.Body.Add(StatementSemicolon());
		return a;
	}

	Parameter Parameter() {
		var location = new Location(file, line);
		Expect('@');
		var a = new Parameter(location, Name(), DataType());
		if (Eat("varying"))
			a.Varying = true;
		if (Eat("null"))
			a.Nullable = true;
		if (Eat('='))
			a.Default = Expression();
		switch (Keyword()) {
		case "out":
		case "output":
			Lex();
			a.Out = true;
			break;
		}
		return a;
	}

	Table Table() {
		Debug.Assert(Keyword() == "table");
		var location = new Location(file, line);
		Lex();
		var a = new Table(location, QualifiedName());
		Expect('(');
		do {
			string? constraintName = null;
			if (Eat("constraint"))
				constraintName = Name();
			switch (Keyword()) {
			case "foreign":
				a.ForeignKeys.Add(ForeignKey(constraintName));
				break;
			case "check":
				a.Checks.Add(Check(constraintName));
				break;
			case "primary":
			case "unique":
				a.Keys.Add(Key(constraintName));
				break;
			default:
				if (constraintName != null)
					throw ErrorToken("expected constraint");
				a.Columns.Add(Column());
				break;
			}
		} while (Eat(','));
		Expect(')');
		if (Eat("on"))
			a.On = StorageOption();
		if (Eat("textimage_on"))
			a.TextimageOn = StorageOption();
		if (Eat("filestream_on"))
			a.FilestreamOn = StorageOption();
		return a;
	}

	StorageOption? StorageOption() {
		if (Eat("default"))
			return null;
		var location = new Location(file, line);
		var name = Name();
		if (Eat('(')) {
			var a = new PartitionSchemeRef(location, name, Name());
			Expect(')');
			return a;
		}
		return new FilegroupRef(location, name);
	}

	DataType DataType() {
		var a = new DataType(QualifiedName());
		if (Eat('(')) {
			a.Size = Int();
			if (Eat(','))
				a.Scale = Int();
			Expect(')');
		}
		return a;
	}

	Column Column() {
		var location = new Location(file, line);
		var a = new Column(location, Name(), DataType());

		// Constraints etc
		while (token == kWord) {
			string? constraintName = null;
			if (Eat("constraint"))
				constraintName = Name();
			switch (Keyword()) {
			case "default":
				Lex();
				a.Default = Expression();
				break;
			case "null":
				Lex();
				break;
			case "filestream":
				Lex();
				a.Filestream = true;
				break;
			case "sparse":
				Lex();
				a.Sparse = true;
				break;
			case "primary":
				Lex();
				Expect("key");
				a.PrimaryKey = true;
				break;
			case "rowguidcol":
				Lex();
				a.Rowguidcol = true;
				break;
			case "identity":
				Lex();
				a.Identity = true;
				if (Eat('(')) {
					a.IdentitySeed = Int();
					Expect(',');
					a.IdentityIncrement = Int();
					Expect(')');
				}
				break;
			case "not":
				Lex();
				switch (Keyword()) {
				case "null":
					Lex();
					a.Nullable = false;
					break;
				case "for":
					Lex();
					Expect("replication");
					a.ForReplication = false;
					break;
				default:
					throw ErrorToken("expected option");
				}
				break;
			default:
				throw ErrorToken("expected constraint");
			}
		}
		return a;
	}

	Key Key(string? constraintName) {
		var location = new Location(file, line);
		var a = new Key(location, constraintName);

		// Primary?
		switch (Keyword()) {
		case "primary":
			Lex();
			Expect("key");
			a.Primary = true;
			break;
		case "unique":
			Lex();
			break;
		default:
			throw ErrorToken("expected key type");
		}

		// Clustered?
		a.Clustered = Clustered();

		// Columns
		Expect('(');
		do
			a.Columns.Add(ColumnOrder());
		while (Eat(','));
		Expect(')');

		if (Eat("on"))
			a.On = StorageOption();
		return a;
	}

	ForeignKey ForeignKey(string? constraintName) {
		var location = new Location(file, line);
		Expect("foreign");
		Expect("key");
		var a = new ForeignKey(location, constraintName);

		// Columns
		Expect('(');
		do
			a.Columns.Add(Name());
		while (Eat(','));
		Expect(')');

		// References
		Expect("references");
		a.RefTableName = QualifiedName();
		if (Eat('(')) {
			do
				a.RefColumns.Add(Name());
			while (Eat(','));
			Expect(')');
		}

		// Actions
		while (Eat("on"))
			switch (Keyword()) {
			case "delete":
				Lex();
				a.OnDelete = Action();
				break;
			case "update":
				Lex();
				a.OnUpdate = Action();
				break;
			default:
				throw ErrorToken("expected event type");
			}

		// Replication
		if (Eat("not")) {
			Expect("for");
			Expect("replication");
			a.ForReplication = false;
		}
		return a;
	}

	Action Action() {
		switch (Keyword()) {
		case "cascade":
			Lex();
			return AnySqlParser.Action.Cascade;
		case "no":
			Lex();
			Expect("action");
			return AnySqlParser.Action.NoAction;
		case "restrict":
			Lex();
			return AnySqlParser.Action.NoAction;
		case "set":
			Lex();
			switch (Keyword()) {
			case "null":
				Lex();
				return AnySqlParser.Action.SetNull;
			case "default":
				Lex();
				return AnySqlParser.Action.SetDefault;
			}
			throw ErrorToken("expected replacement value");
		}
		throw ErrorToken("expected action");
	}

	Check Check(string? constraintName) {
		var location = new Location(file, line);
		var a = new Check(location, constraintName);
		if (Eat("not")) {
			Expect("for");
			Expect("replication");
			a.ForReplication = false;
		}
		a.Expression = Expression();
		return a;
	}

	AlterTableCheckConstraints AlterTableCheckConstraints(QualifiedName tableName, bool check) {
		Debug.Assert(Keyword() == "constraint");
		var location = new Location(file, line);
		Lex();
		var a = new AlterTableCheckConstraints(location, tableName, check);
		if (!Eat("all"))
			do
				a.ConstraintNames.Add(Name());
			while (Eat(','));
		return a;
	}

	Select Select() {
		var location = new Location(file, line);
		var a = new Select(location, QueryExpression());
		while (token == kWord)
			switch (Keyword()) {
			case "order":
				Lex();
				Expect("by");
				a.OrderBy = Expression();
				a.Desc = Desc();
				break;
			default:
				return a;
			}
		return a;
	}

	QueryExpression QueryExpression() {
		var a = Intersect();
		for (;;) {
			QueryOp op;
			switch (Keyword()) {
			case "union":
				if (Eat("all")) {
					op = QueryOp.UnionAll;
					break;
				}
				op = QueryOp.Union;
				break;
			case "except":
				op = QueryOp.Except;
				break;
			default:
				return a;
			}
			var location = new Location(file, line);
			Lex();
			a = new QueryBinaryExpression(location, op, a, Intersect());
		}
	}

	QueryExpression Intersect() {
		// https://stackoverflow.com/questions/56224171/does-intersect-have-a-higher-precedence-compared-to-union
		QueryExpression a = QuerySpecification();
		for (;;) {
			var location = new Location(file, line);
			if (!Eat("intersect"))
				return a;
			a = new QueryBinaryExpression(location, QueryOp.Intersect, a, QuerySpecification());
		}
	}

	QuerySpecification QuerySpecification() {
		var location = new Location(file, line);
		Expect("select");
		var a = new QuerySpecification(location);

		// Some clauses are written before the select list
		// but unknown keywords must be left alone
		// as they might be part of the select list
		for (;;) {
			switch (Keyword()) {
			case "all":
				Lex();
				a.All = true;
				continue;
			case "distinct":
				Lex();
				a.Distinct = true;
				continue;
			case "top":
				Lex();
				a.Top = Expression();
				if (Eat("percent"))
					a.Percent = true;
				if (Eat("with")) {
					Expect("ties");
					a.WithTies = true;
				}
				continue;
			}
			break;
		}

		// Select list
		do {
			var c = new SelectColumn(new Location(file, line), Expression());
			if (Eat("as"))
				c.ColumnAlias = Expression();
			a.SelectList.Add(c);
		} while (Eat(','));

		// Any keyword after the select list, must be a clause
		while (token == kWord)
			switch (Keyword()) {
			case "where":
				Lex();
				a.Where = Expression();
				break;
			case "group":
				Lex();
				Expect("by");
				do
					a.GroupBy.Add(Expression());
				while (Eat(','));
				break;
			case "having":
				Lex();
				a.Having = Expression();
				break;
			case "window":
				Lex();
				a.Window = Expression();
				break;
			case "from":
				Lex();
				do
					a.From.Add(TableSource());
				while (Eat(','));
				break;
			default:
				return a;
			}
		return a;
	}

	TableSource TableSource() {
		return Join();
	}

	TableSource Join() {
		var a = PrimaryTableSource();
		for (;;) {
			var location = new Location(file, line);
			switch (Keyword()) {
			case "inner": {
				Lex();
				Expect("join");
				var b = PrimaryTableSource();
				Expect("on");
				a = new Join(location, JoinType.Inner, a, b, Expression());
				break;
			}
			case "join": {
				Lex();
				var b = PrimaryTableSource();
				Expect("on");
				a = new Join(location, JoinType.Inner, a, b, Expression());
				break;
			}
			case "left": {
				Lex();
				Eat("outer");
				Expect("join");
				var b = PrimaryTableSource();
				Expect("on");
				a = new Join(location, JoinType.Left, a, b, Expression());
				break;
			}
			case "right": {
				Lex();
				Eat("outer");
				Expect("join");
				var b = PrimaryTableSource();
				Expect("on");
				a = new Join(location, JoinType.Right, a, b, Expression());
				break;
			}
			case "full": {
				Lex();
				Eat("outer");
				Expect("join");
				var b = PrimaryTableSource();
				Expect("on");
				a = new Join(location, JoinType.Full, a, b, Expression());
				break;
			}
			default:
				return a;
			}
		}
	}

	TableSource PrimaryTableSource() {
		if (Eat('(')) {
			var b = TableSource();
			Expect(')');
			return b;
		}
		var location = new Location(file, line);
		var a = new PrimaryTableSource(location, QualifiedName());
		switch (token) {
		case kQuotedName:
			break;
		case kWord:
			switch (Keyword()) {
			case "as":
				Lex();
				break;
			case "where":
			case "inner":
			case "join":
			case "left":
			case "right":
			case "full":
			case "on":
			case "group":
			case "order":
				return a;
			}
			break;
		default:
			return a;
		}
		a.TableAlias = Name();
		return a;
	}

	bool? Clustered() {
		switch (Keyword()) {
		case "clustered":
			Lex();
			return true;
		case "nonclustered":
			Lex();
			return false;
		}
		return null;
	}

	ColumnOrder ColumnOrder() {
		var location = new Location(file, line);
		var a = new ColumnOrder(location, Name());
		a.Desc = Desc();
		return a;
	}

	bool Desc() {
		switch (Keyword()) {
		case "desc":
			Lex();
			return true;
		case "asc":
			Lex();
			return false;
		}
		return false;
	}

	bool OnOff() {
		switch (Keyword()) {
		case "on":
			Lex();
			return true;
		case "off":
			Lex();
			return false;
		}
		throw ErrorToken("expected ON or OFF");
	}

	Expression Expression() {
		var a = And();
		for (;;) {
			var location = new Location(file, line);
			BinaryOp op;
			switch (Keyword()) {
			case "not": {
				Lex();
				Expect("between");
				var b = Addition();
				Expect("and");
				return new TernaryExpression(location, TernaryOp.NotBetween, a, b, Addition());
			}
			case "between": {
				Lex();
				var b = Addition();
				Expect("and");
				return new TernaryExpression(location, TernaryOp.Between, a, b, Addition());
			}
			case "or":
				op = BinaryOp.Or;
				break;
			default:
				return a;
			}
			Lex();
			a = new BinaryExpression(location, op, a, And());
		}
	}

	Expression And() {
		var a = Not();
		for (;;) {
			var location = new Location(file, line);
			if (Eat("and")) {
				a = new BinaryExpression(location, BinaryOp.And, a, Not());
				continue;
			}
			return a;
		}
	}

	Expression Not() {
		var location = new Location(file, line);
		if (Eat("not"))
			return new UnaryExpression(location, UnaryOp.Not, Not());
		return Comparison();
	}

	Expression Comparison() {
		var a = Addition();
		var location = new Location(file, line);
		BinaryOp op;
		switch (token) {
		case kWord:
			switch (tokenString.ToLowerInvariant()) {
			case "is":
				Lex();
				switch (Keyword()) {
				case "null":
					Lex();
					return new UnaryExpression(location, UnaryOp.IsNull, a);
				case "not":
					Lex();
					Expect("null");
					return new UnaryExpression(location, UnaryOp.IsNull, a);
				}
				throw ErrorToken("expected NOT or NULL");
			}
			return a;
		case '=':
			op = BinaryOp.Equal;
			break;
		case '<':
			op = BinaryOp.Less;
			break;
		case kNotEqual:
			op = BinaryOp.NotEqual;
			break;
		case '>':
			op = BinaryOp.Greater;
			break;
		case kLessEqual:
			op = BinaryOp.LessEqual;
			break;
		case kGreaterEqual:
			op = BinaryOp.GreaterEqual;
			break;
		default:
			return a;
		}
		Lex();
		return new BinaryExpression(location, op, a, Addition());
	}

	Expression Addition() {
		var a = Multiplication();
		for (;;) {
			BinaryOp op;
			switch (token) {
			case '+':
				op = BinaryOp.Add;
				break;
			case '-':
				op = BinaryOp.Subtract;
				break;
			case kDoublePipe:
				op = BinaryOp.Concat;
				break;
			case '&':
				op = BinaryOp.BitAnd;
				break;
			case '|':
				op = BinaryOp.BitOr;
				break;
			case '^':
				op = BinaryOp.BitXor;
				break;
			default:
				return a;
			}
			var location = new Location(file, line);
			Lex();
			a = new BinaryExpression(location, op, a, Multiplication());
		}
	}

	Expression Multiplication() {
		var a = Prefix();
		for (;;) {
			BinaryOp op;
			switch (token) {
			case '*':
				op = BinaryOp.Multiply;
				break;
			case '/':
				op = BinaryOp.Divide;
				break;
			case '%':
				op = BinaryOp.Remainder;
				break;
			default:
				return a;
			}
			var location = new Location(file, line);
			Lex();
			a = new BinaryExpression(location, op, a, Prefix());
		}
	}

	Expression Prefix() {
		var location = new Location(file, line);
		switch (token) {
		case kWord:
			switch (tokenString.ToLowerInvariant()) {
			case "select":
				return new Subquery(location, QueryExpression());
			case "exists": {
				Lex();
				Expect('(');
				var a = new Exists(location, Select());
				Expect(')');
				return a;
			}
			case "cast": {
				Lex();
				Expect('(');
				var a = new Cast(location, Expression());
				Expect("as");
				a.DataType = DataType();
				Expect(')');
				return a;
			}
			}
			break;
		case '~':
			Lex();
			return new UnaryExpression(location, UnaryOp.BitNot, Prefix());
		case '-':
			Lex();
			return new UnaryExpression(location, UnaryOp.Minus, Prefix());
		}
		return Postfix();
	}

	Expression Postfix() {
		var a = Primary();
		var location = new Location(file, line);
		if (Eat('(')) {
			if (a is QualifiedName a1) {
				var call = new Call(location, a1);
				if (token != ')')
					do
						call.Arguments.Add(Expression());
					while (Eat(','));
				Expect(')');
				return call;
			}
			throw Error("call of non-function", location.Line);
		}
		return a;
	}

	Expression Primary() {
		var location = new Location(file, line);
		switch (token) {
		case '@':
			Lex();
			return new ParameterRef(location, Name());
		case kStringLiteral: {
			var a = new StringLiteral(location, tokenString);
			Lex();
			return a;
		}
		case kNumber: {
			var a = new Number(location, tokenString);
			Lex();
			return a;
		}
		case kWord:
			if (string.Equals(tokenString, "null", StringComparison.OrdinalIgnoreCase)) {
				Lex();
				return new Null(location);
			}
			return QualifiedName();
		case kQuotedName:
		case '*':
			return QualifiedName();
		case '(': {
			Lex();
			var a = Expression();
			Expect(')');
			return a;
		}
		}
		throw ErrorToken("expected expression");
	}

	QualifiedName QualifiedName() {
		var location = new Location(file, line);
		var a = new QualifiedName(location);
		do {
			if (Eat('*')) {
				a.Star = true;
				break;
			}
			a.Names.Add(Name());
		} while (Eat('.'));
		return a;
	}

	int Int() {
		if (token != kNumber)
			throw ErrorToken("expected integer");
		var n = int.Parse(tokenString, System.Globalization.CultureInfo.InvariantCulture);
		Lex();
		return n;
	}

	string? Keyword() {
		if (token != kWord)
			return null;
		return tokenString.ToLowerInvariant();
	}

	string Name() {
		switch (token) {
		case kWord:
		case kQuotedName: {
			var s = tokenString;
			Lex();
			return s;
		}
		}
		throw ErrorToken("expected name");
	}

	void Expect(char k) {
		if (!Eat(k))
			throw ErrorToken("expected " + k);
	}

	void Expect(string s) {
		if (!Eat(s))
			throw ErrorToken("expected " + s.ToUpperInvariant());
	}

	bool Eat(int k) {
		if (token == k) {
			Lex();
			return true;
		}
		return false;
	}

	bool Eat(string s) {
		if (token == kWord && string.Equals(tokenString, s, StringComparison.OrdinalIgnoreCase)) {
			Lex();
			return true;
		}
		return false;
	}

	void Lex() {
		for (;;) {
			token = ch;
			switch (ch) {
			case '\'':
				SingleQuote();
				return;
			case '"':
				DoubleQuote();
				return;
			case '`':
				Backquote();
				return;
			case '[':
				Square();
				return;
			case '!':
				Read();
				switch (ch) {
				case '=':
					Read();
					token = kNotEqual;
					return;
				case '<':
					// https://stackoverflow.com/questions/77475517/what-are-the-t-sql-and-operators-for
					Read();
					token = kGreaterEqual;
					return;
				case '>':
					Read();
					token = kLessEqual;
					return;
				}
				break;
			case '|':
				Read();
				switch (ch) {
				case '|':
					Read();
					token = kDoublePipe;
					return;
				}
				break;
			case '>':
				Read();
				switch (ch) {
				case '=':
					Read();
					token = kGreaterEqual;
					return;
				}
				return;
			case '<':
				Read();
				switch (ch) {
				case '=':
					Read();
					token = kLessEqual;
					return;
				case '>':
					Read();
					token = kNotEqual;
					return;
				}
				return;
			case '/':
				Read();
				switch (ch) {
				case '*':
					BlockComment();
					continue;
				}
				return;
			case '.':
				if (char.IsDigit((char)reader.Peek())) {
					// Clang-format can't handle 'goto case'
					Number();
					return;
				}
				Read();
				return;
			case ',':
			case '=':
			case '&':
			case ';':
			case '+':
			case '%':
			case '(':
			case ')':
			case '~':
			case '*':
			case '@':
			case -1:
				Read();
				return;
			case '-':
				Read();
				switch (ch) {
				case '-':
					reader.ReadLine();
					line++;
					Read();
					continue;
				}
				return;
			case '\n':
			case '\r':
			case '\t':
			case '\f':
			case '\v':
			case ' ':
				Read();
				continue;
			case 'N':
				if (reader.Peek() == '\'') {
					// We are reading everything as Unicode anyway
					// so the prefix has no special meaning
					Read();
					SingleQuote();
					return;
				}
				Word();
				return;
			case 'A':
			case 'B':
			case 'C':
			case 'D':
			case 'E':
			case 'F':
			case 'G':
			case 'H':
			case 'I':
			case 'J':
			case 'K':
			case 'L':
			case 'M':
			case 'O':
			case 'P':
			case 'Q':
			case 'R':
			case 'S':
			case 'T':
			case 'U':
			case 'V':
			case 'W':
			case 'X':
			case 'Y':
			case 'Z':
			case '_':
			case 'a':
			case 'b':
			case 'c':
			case 'd':
			case 'e':
			case 'f':
			case 'g':
			case 'h':
			case 'i':
			case 'j':
			case 'k':
			case 'l':
			case 'm':
			case 'n':
			case 'o':
			case 'p':
			case 'q':
			case 'r':
			case 's':
			case 't':
			case 'u':
			case 'v':
			case 'w':
			case 'x':
			case 'y':
			case 'z':
				Word();
				return;
			case '0':
			case '1':
			case '2':
			case '3':
			case '4':
			case '5':
			case '6':
			case '7':
			case '8':
			case '9':
				Number();
				return;
			default:
				// Common letters are handled in the switch for speed
				// but there are other letters in Unicode
				if (char.IsLetter((char)ch)) {
					Word();
					return;
				}

				// Likewise digits
				if (char.IsDigit((char)ch)) {
					Number();
					return;
				}

				// And whitespace
				if (char.IsWhiteSpace((char)ch)) {
					Read();
					continue;
				}
				break;
			}
			throw Error("stray " + (char)ch);
		}
	}

	void BlockComment() {
		Debug.Assert(ch == '*');
		var line1 = line;
		for (;;) {
			Read();
			switch (ch) {
			case -1:
				throw Error("unclosed /*", line1);
			case '*':
				if (reader.Peek() == '/') {
					Read();
					Read();
					return;
				}
				break;
			}
		}
	}

	void Word() {
		var sb = new StringBuilder();
		do
			AppendRead(sb);
		while (IsWordPart());
		token = kWord;
		tokenString = sb.ToString();
	}

	void Number() {
		var sb = new StringBuilder();
		while (IsWordPart())
			AppendRead(sb);
		if (ch == '.')
			do
				AppendRead(sb);
			while (IsWordPart());
		Debug.Assert(sb.Length > 0);
		token = kNumber;
		tokenString = sb.ToString();
	}

	bool IsWordPart() {
		if (char.IsLetterOrDigit((char)ch))
			return true;
		return ch == '_';
	}

	void SingleQuote() {
		// For string literals, single quote is reliably portable across dialects
		Debug.Assert(ch == '\'');
		var line1 = line;
		Read();
		var sb = new StringBuilder();
		for (;;) {
			switch (ch) {
			case -1:
				throw Error("unclosed '", line1);
			case '\\':
				switch (reader.Peek()) {
				case '\'':
				case '\\':
					Read();
					break;
				}
				break;
			case '\'':
				Read();
				switch (ch) {
				case '\'':
					break;
				default:
					token = kStringLiteral;
					tokenString = sb.ToString();
					return;
				}
				break;
			}
			AppendRead(sb);
		}
	}

	void DoubleQuote() {
		// For unusual identifiers, standard SQL uses double quotes
		Debug.Assert(ch == '"');
		var line1 = line;
		Read();
		var sb = new StringBuilder();
		for (;;) {
			switch (ch) {
			case -1:
				throw Error("unclosed \"", line1);
			case '\\':
				switch (reader.Peek()) {
				case '"':
				case '\\':
					Read();
					break;
				}
				break;
			case '"':
				Read();
				switch (ch) {
				case '"':
					break;
				default:
					token = kQuotedName;
					tokenString = sb.ToString();
					return;
				}
				break;
			}
			AppendRead(sb);
		}
	}

	void Backquote() {
		// For unusual identifiers, MySQL uses backquotes
		Debug.Assert(ch == '`');
		var line1 = line;
		Read();
		var sb = new StringBuilder();
		for (;;) {
			switch (ch) {
			case -1:
				throw Error("unclosed `", line1);
			case '\\':
				switch (reader.Peek()) {
				case '`':
				case '\\':
					Read();
					break;
				}
				break;
			case '`':
				Read();
				switch (ch) {
				case '`':
					break;
				default:
					token = kQuotedName;
					tokenString = sb.ToString();
					return;
				}
				break;
			}
			AppendRead(sb);
		}
	}

	void Square() {
		// For unusual identifiers, SQL Server uses square brackets
		Debug.Assert(ch == '[');
		var line1 = line;
		Read();
		var sb = new StringBuilder();
		for (;;) {
			switch (ch) {
			case -1:
				throw Error("unclosed [", line1);
			case ']':
				Read();
				switch (ch) {
				case ']':
					break;
				default:
					token = kQuotedName;
					tokenString = sb.ToString();
					return;
				}
				break;
			}
			AppendRead(sb);
		}
	}

	void AppendRead(StringBuilder sb) {
		sb.Append((char)ch);
		Read();
	}

	void Read() {
		if (ch == '\n')
			line++;
		ch = reader.Read();
	}

	Exception ErrorToken(string message) {
		// Error functions return exception objects instead of throwing immediately
		// so 'throw Error(...)' can mark the end of a case block
		return Error($"{Echo()}: {message}");
	}

	string Echo() {
		if (token >= 0)
			return char.ToString((char)token);
		switch (token) {
		case kDoublePipe:
			return "||";
		case kGreaterEqual:
			return ">=";
		case kLessEqual:
			return "<=";
		case kNotEqual:
			return "<>";
		case kStringLiteral:
			return $"'{tokenString}'";
		}
		return tokenString;
	}

	Exception Error(string message) {
		return Error(message, line);
	}

	Exception Error(string message, int line) {
		return new FormatException($"{file}:{line}: {message}");
	}
}
