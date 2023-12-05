using System.Diagnostics;
using System.Text;

namespace AnySqlParser;
public sealed class Parser {
	public static IEnumerable<Statement> Parse(string file) {
		using var reader = new StreamReader(file);
		var parser = new Parser(reader, file, 1);
		while (parser.token.Value.Length != 0)
			yield return parser.StatementSemicolon();
	}

	public static IEnumerable<Statement> Parse(TextReader reader, string file = "SQL", int line = 1) {
		var parser = new Parser(reader, file, line);
		while (parser.token.Value.Length != 0)
			yield return parser.StatementSemicolon();
	}

	readonly TextReader reader;
	readonly string file;
	int line;
	int c;
	Token token;

	Parser(TextReader reader, string file, int line) {
		this.reader = reader;
		this.file = file;
		this.line = line;
		Read();
		Lex();
	}

	Statement StatementSemicolon() {
		var a = Statement();
		Eat(";");
		return a;
	}

	Statement Statement() {
		var location = new Location(file, line);
		switch (token.Value) {
		case "select":
			return Select();
		case "insert": {
			Lex();
			Eat("into");
			var a = new Insert(location);

			// Table
			a.TableName = QualifiedName();

			// Columns
			if (Eat("(")) {
				do
					a.Columns.Add(Name());
				while (Eat(","));
				Expect(")");
			}

			// Values
			Expect("values");
			Expect("(");
			do
				a.Values.Add(Expression());
			while (Eat(","));
			Expect(")");

			return a;
		}
		case "create": {
			Lex();
			var unique = Eat("unique");
			switch (token.Value) {
			case "index": {
				Lex();
				var a = new Index(unique);
				a.Name = Name();

				// Table
				Expect("on");
				a.TableName = QualifiedName();

				// Columns
				Expect("(");
				do
					a.Columns.Add(ColumnOrder());
				while (Eat(","));
				Expect(")");

				// Include
				if (Eat("include")) {
					Expect("(");
					do
						a.Include.Add(Name());
					while (Eat(","));
					Expect(")");
				}
				return a;
			}
			case "view": {
				Lex();
				var a = new View();
				a.Name = QualifiedName();
				Expect("as");
				a.Query = Select();
				return a;
			}
			case "table":
				return Table();
			}
			throw ErrorToken("expected noun");
		}
		case "alter": {
			Lex();
			switch (token.Value) {
			case "table": {
				Lex();
				var tableName = QualifiedName();
				switch (token.Value) {
				case "add": {
					Lex();
					var a = new AlterTableAdd(tableName);
					do {
						string? constraintName = null;
						if (Eat("constraint"))
							constraintName = Name();
						switch (token.Value) {
						case "foreign":
							a.ForeignKeys.Add(ForeignKey());
							break;
						case "check":
							a.Checks.Add(Check(constraintName));
							break;
						case "primary":
						case "unique":
							a.Keys.Add(Key());
							break;
						default:
							if (constraintName != null)
								throw ErrorToken("expected constraint");
							a.Columns.Add(Column());
							break;
						}
					} while (Eat(","));
					return a;
				}
				}
				throw ErrorToken("unknown syntax");
			}
			}
			throw ErrorToken("expected noun");
		}
		}
		throw ErrorToken("expected statement");
	}

	Table Table() {
		Debug.Assert(token.Value == "table");
		Lex();
		var a = new Table(QualifiedName());
		Expect("(");
		do {
			string? constraintName = null;
			if (Eat("constraint"))
				constraintName = Name();
			switch (token.Value) {
			case "foreign":
				a.ForeignKeys.Add(ForeignKey());
				break;
			case "check":
				a.Checks.Add(Check(constraintName));
				break;
			case "primary":
			case "unique":
				a.Keys.Add(Key());
				break;
			default:
				if (constraintName != null)
					throw ErrorToken("expected constraint");
				a.Columns.Add(Column());
				break;
			}
		} while (Eat(","));
		Expect(")");
		return a;
	}

	DataType DataType() {
		var a = new DataType(QualifiedName());
		if (Eat("(")) {
			a.Size = Int();
			if (Eat(","))
				a.Scale = Int();
			Expect(")");
		}
		return a;
	}

	Column Column() {
		var a = new Column(Name(), DataType());

		// Constraints etc
		while (token == kWord) {
			if (Eat("constraint"))
				Name();
			switch (token.Value) {
			case "default":
				Lex();
				a.Default = Expression();
				break;
			case "null":
				Lex();
				break;
			case "primary":
				Lex();
				Expect("key");
				a.PrimaryKey = true;
				break;
			case "identity":
				Lex();
				a.Identity = true;
				if (Eat("(")) {
					Int();
					Expect(",");
					Int();
					Expect(")");
				}
				break;
			case "not":
				Lex();
				switch (token.Value) {
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

	Key Key() {
		var location = new Location(file, line);
		var a = new Key(location);

		// Primary?
		switch (token.Value) {
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

		// Columns
		Expect("(");
		do
			a.Columns.Add(ColumnOrder());
		while (Eat(","));
		Expect(")");
		return a;
	}

	ForeignKey ForeignKey() {
		Expect("foreign");
		Expect("key");
		var a = new ForeignKey();

		// Columns
		Expect("(");
		do
			a.Columns.Add(Name());
		while (Eat(","));
		Expect(")");

		// References
		Expect("references");
		a.RefTableName = QualifiedName();
		if (Eat("(")) {
			do
				a.RefColumns.Add(Name());
			while (Eat(","));
			Expect(")");
		}

		// Actions
		while (Eat("on"))
			switch (token.Value) {
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
		switch (token.Value) {
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
			switch (token.Value) {
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

	Expression Check() {
		if (Eat("not")) {
			Expect("for");
			Expect("replication");
		}
		return Expression();
	}

	Select Select() {
		var a = new Select(QueryExpression());
		switch (token.Value) {
		case "order":
			Lex();
			Expect("by");
			a.OrderBy = Expression();
			a.Desc = Desc();
			break;
		}
		return a;
	}

	QueryExpression QueryExpression() {
		var a = Intersect();
		for (;;) {
			QueryOp op;
			switch (token.Value) {
			case "union":
				Lex();
				if (Eat("all")) {
					op = QueryOp.UnionAll;
					break;
				}
				op = QueryOp.Union;
				break;
			case "except":
				Lex();
				op = QueryOp.Except;
				break;
			default:
				return a;
			}
			a = new QueryBinaryExpression(op, a, Intersect());
		}
	}

	QueryExpression Intersect() {
		// https://stackoverflow.com/questions/56224171/does-intersect-have-a-higher-precedence-compared-to-union
		QueryExpression a = QuerySpecification();
		for (;;) {
			var location = new Location(file, line);
			if (!Eat("intersect"))
				return a;
			a = new QueryBinaryExpression(QueryOp.Intersect, a, QuerySpecification());
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
			switch (token.Value) {
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
		} while (Eat(","));

		// Any keyword after the select list, must be a clause
		while (token == kWord)
			switch (token.Value) {
			case "where":
				Lex();
				a.Where = Expression();
				break;
			case "group":
				Lex();
				Expect("by");
				do
					a.GroupBy.Add(Expression());
				while (Eat(","));
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
				while (Eat(","));
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
		for (;;)
			switch (token.Value) {
			case "inner": {
				Lex();
				Expect("join");
				var b = PrimaryTableSource();
				Expect("on");
				a = new Join(JoinType.Inner, a, b, Expression());
				break;
			}
			case "join": {
				Lex();
				var b = PrimaryTableSource();
				Expect("on");
				a = new Join(JoinType.Inner, a, b, Expression());
				break;
			}
			case "left": {
				Lex();
				Eat("outer");
				Expect("join");
				var b = PrimaryTableSource();
				Expect("on");
				a = new Join(JoinType.Left, a, b, Expression());
				break;
			}
			case "right": {
				Lex();
				Eat("outer");
				Expect("join");
				var b = PrimaryTableSource();
				Expect("on");
				a = new Join(JoinType.Right, a, b, Expression());
				break;
			}
			case "full": {
				Lex();
				Eat("outer");
				Expect("join");
				var b = PrimaryTableSource();
				Expect("on");
				a = new Join(JoinType.Full, a, b, Expression());
				break;
			}
			default:
				return a;
			}
	}

	TableSource PrimaryTableSource() {
		if (Eat("(")) {
			var b = TableSource();
			Expect(")");
			return b;
		}
		var a = new PrimaryTableSource(QualifiedName());
		switch (token) {
		case kQuotedName:
			break;
		case kWord:
			switch (token.Value) {
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

	ColumnOrder ColumnOrder() {
		var a = new ColumnOrder(Name());
		a.Desc = Desc();
		return a;
	}

	bool Desc() {
		switch (token.Value) {
		case "desc":
			Lex();
			return true;
		case "asc":
			Lex();
			return false;
		}
		return false;
	}

	Expression Expression() {
		var a = And();
		for (;;) {
			BinaryOp op;
			switch (token.Value) {
			case "not": {
				Lex();
				Expect("between");
				var b = Addition();
				Expect("and");
				return new TernaryExpression(TernaryOp.NotBetween, a, b, Addition());
			}
			case "between": {
				Lex();
				var b = Addition();
				Expect("and");
				return new TernaryExpression(TernaryOp.Between, a, b, Addition());
			}
			case "or":
				op = BinaryOp.Or;
				break;
			default:
				return a;
			}
			Lex();
			a = new BinaryExpression(op, a, And());
		}
	}

	Expression And() {
		var a = Not();
		for (;;) {
			if (Eat("and")) {
				a = new BinaryExpression(BinaryOp.And, a, Not());
				continue;
			}
			return a;
		}
	}

	Expression Not() {
		if (Eat("not"))
			return new UnaryExpression(UnaryOp.Not, Not());
		return Comparison();
	}

	Expression Comparison() {
		var a = Addition();
		BinaryOp op;
		switch (token.Value) {
		case "is":
			Lex();
			switch (token.Value) {
			case "null":
				Lex();
				return new UnaryExpression(UnaryOp.IsNull, a);
			case "not":
				Lex();
				Expect("null");
				return new UnaryExpression(UnaryOp.IsNull, a);
			}
			throw ErrorToken("expected NOT or NULL");
		case "=":
			op = BinaryOp.Equal;
			break;
		case "<":
			op = BinaryOp.Less;
			break;
		case "<>":
			op = BinaryOp.NotEqual;
			break;
		case ">":
			op = BinaryOp.Greater;
			break;
		case "<=":
			op = BinaryOp.LessEqual;
			break;
		case ">=":
			op = BinaryOp.GreaterEqual;
			break;
		default:
			return a;
		}
		Lex();
		return new BinaryExpression(op, a, Addition());
	}

	Expression Addition() {
		var a = Multiplication();
		for (;;) {
			BinaryOp op;
			switch (token.Value) {
			case "+":
				op = BinaryOp.Add;
				break;
			case "-":
				op = BinaryOp.Subtract;
				break;
			case "||":
				op = BinaryOp.Concat;
				break;
			case "&":
				op = BinaryOp.BitAnd;
				break;
			case "|":
				op = BinaryOp.BitOr;
				break;
			case "^":
				op = BinaryOp.BitXor;
				break;
			default:
				return a;
			}
			Lex();
			a = new BinaryExpression(op, a, Multiplication());
		}
	}

	Expression Multiplication() {
		var a = Prefix();
		for (;;) {
			BinaryOp op;
			switch (token.Value) {
			case "*":
				op = BinaryOp.Multiply;
				break;
			case "/":
				op = BinaryOp.Divide;
				break;
			case "%":
				op = BinaryOp.Remainder;
				break;
			default:
				return a;
			}
			Lex();
			a = new BinaryExpression(op, a, Prefix());
		}
	}

	Expression Prefix() {
		switch (token.Value) {
		case "select":
			return new Subquery(QueryExpression());
		case "exists": {
			Lex();
			Expect("(");
			var a = new Exists(Select());
			Expect(")");
			return a;
		}
		case "cast": {
			Lex();
			Expect("(");
			var a = new Cast(Expression());
			Expect("as");
			a.DataType = DataType();
			Expect(")");
			return a;
		}
		case "~":
			Lex();
			return new UnaryExpression(UnaryOp.BitNot, Prefix());
		case "-":
			Lex();
			return new UnaryExpression(UnaryOp.Minus, Prefix());
		}
		return Postfix();
	}

	Expression Postfix() {
		var a = Primary();
		if (Eat("(")) {
			if (a is QualifiedName a1) {
				var call = new Call(a1);
				if (token.Value != ")")
					do
						call.Arguments.Add(Expression());
					while (Eat(","));
				Expect(")");
				return call;
			}
			throw Error("call of non-function");
		}
		return a;
	}

	Expression Primary() {
		switch (token.Value) {
		case "@":
			Lex();
			return new ParameterRef(Name());
		case "null":
			Lex();
			return new Null();
		case "*":
			return QualifiedName();
		case "(": {
			Lex();
			var a = Expression();
			Expect(")");
			return a;
		}
		}
		switch (token.Value[0]) {
		case 'N':
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
		case '"':
		case '`':
		case '[':
			return QualifiedName();
		case '\'':
			return new StringLiteral(Etc.Unquote(Lex1()));
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
			return new Number(Lex1());
		case '.':
			if (1 < token.Value.Length && char.IsDigit(token.Value, 1))
				return new Number(Lex1());
			break;
		}
		throw ErrorToken("expected expression");
	}

	QualifiedName QualifiedName() {
		var a = new QualifiedName();
		if (!Eat("*"))
			do
				a.Names.Add(Name());
			while (Eat("."));
		return a;
	}

	int Int() {
		var n = int.Parse(token.Value, System.Globalization.CultureInfo.InvariantCulture);
		Lex();
		return n;
	}

	string Lex1() {
		var s = token.Value;
		Lex();
		return s;
	}

	string Name() {
		switch (token.Value[0]) {
		case 'N':
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
			return Lex1();
		case '"':
		case '`':
			return Etc.Unquote(Lex1());
		case '[':
			return Etc.Unquote(Lex1(), ']');
		}
		if (char.IsLetter(token.Value[0]))
			return Lex1();
		throw ErrorToken("expected name");
	}

	void Expect(string s) {
		if (!Eat(s))
			throw ErrorToken("expected " + s.ToUpperInvariant());
	}

	bool Eat(string s) {
		if (token.Value == s) {
			Lex();
			return true;
		}
		return false;
	}

	void Lex() {
		for (;;) {
			token.Location = new Location(file, line);
			switch (c) {
			case '\'':
			case '"':
			case '`':
				Quote();
				return;
			case '[':
				Quote(']');
				return;
			case '!':
				Read();
				switch (c) {
				case '=':
					Read();
					token.Value = "<>";
					return;
				case '<':
					// https://stackoverflow.com/questions/77475517/what-are-the-t-sql-and-operators-for
					Read();
					token.Value = ">=";
					return;
				case '>':
					Read();
					token.Value = "<=";
					return;
				}
				break;
			case '|':
				Read();
				switch (c) {
				case '|':
					Read();
					token.Value = "||";
					return;
				}
				break;
			case '>':
				Read();
				switch (c) {
				case '=':
					Read();
					token.Value = ">=";
					return;
				}
				token.Value = ">";
				return;
			case '<':
				Read();
				switch (c) {
				case '=':
					Read();
					token.Value = "<=";
					return;
				case '>':
					Read();
					token.Value = "<>";
					return;
				}
				token.Value = "<";
				return;
			case '/':
				Read();
				switch (c) {
				case '*':
					BlockComment();
					continue;
				}
				token.Value = "/";
				return;
			case '.':
				if (char.IsDigit((char)reader.Peek())) {
					Number();
					return;
				}
				Read();
				token.Value = ".";
				return;
			case ',':
				Read();
				token.Value = ",";
				return;
			case '=':
				Read();
				token.Value = "=";
				return;
			case '&':
				Read();
				token.Value = "&";
				return;
			case ';':
				Read();
				token.Value = ";";
				return;
			case '+':
				Read();
				token.Value = "+";
				return;
			case '%':
				Read();
				token.Value = "%";
				return;
			case '(':
				Read();
				token.Value = "(";
				return;
			case ')':
				Read();
				token.Value = ")";
				return;
			case '~':
				Read();
				token.Value = "~";
				return;
			case '*':
				Read();
				token.Value = "*";
				return;
			case '@':
				Read();
				token.Value = "@";
				return;
			case -1:
				Read();
				token.Value = "";
				return;
			case '-':
				Read();
				switch (c) {
				case '-':
					reader.ReadLine();
					line++;
					Read();
					continue;
				}
				token.Value = "-";
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
					Quote();
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
				if (char.IsLetter((char)c)) {
					Word();
					return;
				}

				// Likewise digits
				if (char.IsDigit((char)c)) {
					Number();
					return;
				}

				// And whitespace
				if (char.IsWhiteSpace((char)c)) {
					Read();
					continue;
				}
				break;
			}
			throw Error("stray " + (char)c);
		}
	}

	void BlockComment() {
		Debug.Assert(c == '*');
		var line1 = line;
		for (;;) {
			Read();
			switch (c) {
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
		while (Etc.IsWordPart(c));
		token.Value = sb.ToString().ToLowerInvariant();
	}

	void Number() {
		var sb = new StringBuilder();
		while (Etc.IsWordPart(c))
			AppendRead(sb);
		if (c == '.')
			do
				AppendRead(sb);
			while (Etc.IsWordPart(c));
		Debug.Assert(sb.Length != 0);
		token.Value = sb.ToString();
	}

	void Quote() {
		Quote((char)c);
	}

	void Quote(char q) {
		var line1 = line;
		var sb = new StringBuilder();
		AppendRead(sb);
		for (;;) {
			if (c == q) {
				Read();
				if (c != q) {
					sb.Append(q);
					token.Value = sb.ToString();
					return;
				}
			}
			if (c < 0)
				throw Error("unclosed " + q, line1);
			AppendRead(sb);
		}
	}

	void AppendRead(StringBuilder sb) {
		sb.Append((char)c);
		Read();
	}

	void Read() {
		if (c == '\n')
			line++;
		c = reader.Read();
	}

	Exception ErrorToken(string message) {
		return Error($"{token.Value}: {message}");
	}

	Exception Error(string message) {
		// Error functions return exception objects instead of throwing immediately
		// so 'throw Error(...)' can mark the end of a case block
		return Error(message, line);
	}

	Exception Error(string message, int line) {
		return new FormatException($"{file}:{line}: {message}");
	}
}
