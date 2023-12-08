using System.Diagnostics;
using System.Text;

namespace AnySqlParser;
public sealed class Parser {
	public static IEnumerable<Statement> Parse(string file) {
		return new Parser(new StreamReader(file), file, 1).Statements();
	}

	public static IEnumerable<Statement> Parse(TextReader reader, string file = "SQL", int line = 1) {
		return new Parser(reader, file, line).Statements();
	}

	delegate bool Callback();

	// All tokens are represented as strings of positive length
	// so the first character of the current token can always be tested
	const string Eof = " ";

	readonly TextReader reader;
	readonly string file;
	int line;
	int c;
	string token = null!;
	int tokenLine = 1;
	int tokenTextCount;
	readonly StringBuilder text = new();

	Parser(TextReader reader, string file, int line) {
		this.reader = reader;
		this.file = file;
		this.line = line;
		Read();
		Lex();
	}

	ExtraText ExtraText(int textLine, int n) {
		var location = new Location(file, textLine);
		return new ExtraText(location, text.ToString(0, n));
	}

	IEnumerable<Statement> Statements() {
		for (;;) {
			// Next statement
			// in general, there will be a block of extra text before the statement
			// consisting of comments, and unrecognized statements
			// remember the line where the extra text began
			var extraTextLine = tokenLine;

		nextToken:
			// This token might begin a recognized statement
			// remember the character count where it began
			// so everything up to that point, can be saved as extra text
			var extraTextCount = tokenTextCount;

			// Does this token begin a recognized statement?
			switch (token) {
			case Eof:
				yield break;
			case "insert": {
				yield return ExtraText(extraTextLine, extraTextCount);
				Lex();
				Eat("into");
				var a = new Insert();

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
				EndStatement();
				yield return a;
				continue;
			}
			case "create": {
				Lex();
				var unique = Eat("unique");
				switch (token) {
				case "index": {
					yield return ExtraText(extraTextLine, extraTextCount);
					Lex();
					var a = new Index(unique);
					a.Name = Name();

					// Table
					Expect("on");
					a.TableName = QualifiedName();

					// Columns
					Expect("(");
					do
						a.Columns.Add(ColumnRef());
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
					EndStatement();
					yield return a;
					continue;
				}
				case "view": {
					yield return ExtraText(extraTextLine, extraTextCount);
					Lex();
					var a = new View();
					a.Name = QualifiedName();
					Expect("as");
					a.Query = Select();
					EndStatement();
					yield return a;
					continue;
				}
				case "table": {
					yield return ExtraText(extraTextLine, extraTextCount);
					Lex();
					var a = new Table(false, UnqualifiedName());
					while (!Eat("("))
						Skip(a.ExtraTokens);
					do {
						var b = TableElement(a, IsElementEnd);
						while (!IsElementEnd())
							Skip(b.ExtraTokens);
					} while (Eat(","));
					Expect(")");
					EndStatement();
					yield return a;
					continue;
				}
				}
				break;
			}
			case "alter": {
				Lex();
				switch (token) {
				case "table": {
					Lex();
					var tableName = UnqualifiedName();
					switch (token) {
					case "add": {
						yield return ExtraText(extraTextLine, extraTextCount);
						Lex();
						var a = new Table(true, tableName);
						do
							TableElement(a, IsStatementEnd);
						while (Eat(","));
						EndStatement();
						yield return a;
						continue;
					}
					}
					throw ErrorToken("unknown syntax");
				}
				}
				break;
			}
			}
			Skip(null);
			goto nextToken;
		}
	}

	bool IsElementEnd() {
		switch (token) {
		case ")":
		case ",":
			return true;
		}
		return false;
	}

	void EndStatement() {
		Eat(";");
		Eat("go");

		// Throw away extra text associated with the statement just recognized
		// but keep extra text associated with the following token
		text.Remove(0, tokenTextCount);
	}

	bool IsStatementEnd() {
		switch (token) {
		case Eof:
		case ";":
		case "go":
			return true;
		}
		return false;
	}

	void Skip(List<string>? extraTokens) {
		var line1 = line;
		int depth = 0;
		do {
			switch (token) {
			case Eof:
				throw Error(depth == 0 ? "missing element" : "unclosed (", line1);
			case "(":
				depth++;
				break;
			case ")":
				depth--;
				break;
			}
			extraTokens?.Add(token);
			Lex();
		} while (depth != 0);
	}

	string UnqualifiedName() {
		string name;
		do
			name = Name();
		while (Eat("."));
		return name;
	}

	TableRef TableRef() {
		var location = new Location(file, line);
		return new TableRef(location, UnqualifiedName());
	}

	ColumnRef ColumnRef() {
		var location = new Location(file, line);
		var a = new ColumnRef(location, Name());
		a.Desc = Desc();
		return a;
	}

	ForeignKey ForeignKey(Column? column, Callback isEnd) {
		var location = new Location(file, line);
		if (Eat("foreign"))
			Expect("key");
		var a = new ForeignKey(location);

		// Columns
		if (column == null) {
			Expect("(");
			do
				a.Columns.Add(ColumnRef());
			while (Eat(","));
			Expect(")");
		} else
			a.Columns.Add(new ColumnRef(column));

		// References
		Expect("references");
		a.RefTable = TableRef();
		if (Eat("(")) {
			do
				a.RefColumns.Add(ColumnRef());
			while (Eat(","));
			Expect(")");
		}

		// Search the postscript for actions
		while (!isEnd()) {
			switch (token) {
			case "on":
				Lex();
				switch (token) {
				case "delete":
					Lex();
					a.OnDelete = GetAction();
					continue;
				case "update":
					Lex();
					a.OnUpdate = GetAction();
					continue;
				}
				break;
			}
			Skip(a.ExtraTokens);
		}
		return a;
	}

	Element TableConstraint(Table table, Callback isEnd) {
		switch (token) {
		case "foreign": {
			var a = ForeignKey(null, isEnd);
			table.ForeignKeys.Add(a);
			return a;
		}
		case "primary": {
			var a = Key(null);
			table.AddPrimaryKey(a);
			return a;
		}
		case "check": {
			var a = Check();
			table.Checks.Add(a);
			return a;
		}
		case "unique":
		case "key": {
			var a = Key(null);
			table.Uniques.Add(a);
			return a;
		}
		}
		throw ErrorToken("expected constraint");
	}

	Key Key(Column? column) {
		var location = new Location(file, line);
		switch (token) {
		case "primary":
			Lex();
			Expect("key");
			break;
		case "unique":
			Lex();
			Eat("key");
			break;
		case "key":
			Lex();
			break;
		default:
			throw Error("expected key");
		}
		var a = new Key(location);

		if (column == null) {
			while (!Eat("("))
				Skip(a.ExtraTokens);
			do
				a.Columns.Add(ColumnRef());
			while (Eat(","));
			Expect(")");
		} else
			a.Columns.Add(new ColumnRef(column));
		return a;
	}

	Element TableElement(Table table, Callback isEnd) {
		// Might be a table constraint
		if (Eat("constraint")) {
			Name();
			return TableConstraint(table, isEnd);
		}
		switch (token) {
		case "foreign":
		case "key":
		case "primary":
		case "unique":
		case "check":
		case "exclude":
			return TableConstraint(table, isEnd);
		}

		// This is a column
		var location = new Location(file, line);
		var a = new Column(location, Name(), DataType());

		// Search the postscript for column constraints
		while (!isEnd()) {
			switch (token) {
			case "default":
				Lex();
				a.Default = Expression();
				break;
			case "identity":
				Lex();
				a.AutoIncrement = true;
				if (Eat("(")) {
					Int();
					Expect(",");
					Int();
					Expect(")");
				}
				continue;
			case "foreign":
			case "references":
				ForeignKey(a, isEnd);
				continue;
			case "check":
				table.Checks.Add(Check());
				continue;
			case "primary":
				table.AddPrimaryKey(Key(a));
				continue;
			case "null":
				Lex();
				continue;
			case "not":
				Lex();
				switch (token) {
				case "null":
					Lex();
					a.Nullable = false;
					continue;
				}
				a.ExtraTokens.Add("not");
				break;
			}
			Skip(a.ExtraTokens);
		}

		// Add to table
		table.Add(a);
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

	Action GetAction() {
		switch (token) {
		case "cascade":
			Lex();
			return Action.Cascade;
		case "no":
			Lex();
			Expect("action");
			return Action.NoAction;
		case "restrict":
			Lex();
			return Action.NoAction;
		case "set":
			Lex();
			switch (token) {
			case "null":
				Lex();
				return Action.SetNull;
			case "default":
				Lex();
				return Action.SetDefault;
			}
			throw ErrorToken("expected replacement value");
		}
		throw ErrorToken("expected action");
	}

	Check Check() {
		Debug.Assert(token == "check");
		var location = new Location(file, line);
		var a = new Check(location);
		while (!Eat("("))
			Skip(a.ExtraTokens);
		a.Expression = Expression();
		Expect(")");
		return a;
	}

	Select Select() {
		var a = new Select(QueryExpression());
		switch (token) {
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
			switch (token) {
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
			if (!Eat("intersect"))
				return a;
			a = new QueryBinaryExpression(QueryOp.Intersect, a, QuerySpecification());
		}
	}

	QuerySpecification QuerySpecification() {
		Expect("select");
		var a = new QuerySpecification();

		// Some clauses are written before the select list
		// but unknown keywords must be left alone
		// as they might be part of the select list
		for (;;) {
			switch (token) {
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
		for (;;)
			switch (token) {
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
	}

	TableSource TableSource() {
		return Join();
	}

	TableSource Join() {
		var a = PrimaryTableSource();
		for (;;)
			switch (token) {
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
		default:
			if (!IsName())
				return a;
			break;
		}
		a.TableAlias = Name();
		return a;
	}

	bool Desc() {
		switch (token) {
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
			switch (token) {
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
			case "like":
				op = BinaryOp.Like;
				break;
			case "in": {
				Lex();
				Expect("(");
				var b = new List<Expression>();
				do
					b.Add(Expression());
				while (Eat(","));
				Expect(")");
				a = new InList(false, a, b);
				continue;
			}
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
		switch (token) {
		case "is":
			Lex();
			switch (token) {
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
			switch (token) {
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
			switch (token) {
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
		switch (token) {
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
				if (token != ")")
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
		switch (token) {
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
		switch (token[0]) {
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
			if (1 < token.Length && char.IsDigit(token, 1))
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
		var n = int.Parse(token, System.Globalization.CultureInfo.InvariantCulture);
		Lex();
		return n;
	}

	string Lex1() {
		var s = token;
		Lex();
		return s;
	}

	string Name() {
		switch (token[0]) {
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
		if (char.IsLetter(token[0]))
			return Lex1();
		throw ErrorToken("expected name");
	}

	bool IsName() {
		switch (token[0]) {
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
			return true;
		}
		return char.IsLetter(token[0]);
	}

	void Expect(string s) {
		if (!Eat(s))
			throw ErrorToken("expected " + s.ToUpperInvariant());
	}

	bool Eat(string s) {
		if (token == s) {
			Lex();
			return true;
		}
		return false;
	}

	void Lex() {
		// Comments are more likely to belong to the following token than the previous one
		tokenLine = line;
		tokenTextCount = text.Length;
		for (;;) {
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
					token = "<>";
					return;
				case '<':
					// https://stackoverflow.com/questions/77475517/what-are-the-t-sql-and-operators-for
					Read();
					token = ">=";
					return;
				case '>':
					Read();
					token = "<=";
					return;
				}
				break;
			case '|':
				Read();
				switch (c) {
				case '|':
					Read();
					token = "||";
					return;
				}
				break;
			case '>':
				Read();
				switch (c) {
				case '=':
					Read();
					token = ">=";
					return;
				}
				token = ">";
				return;
			case '<':
				Read();
				switch (c) {
				case '=':
					Read();
					token = "<=";
					return;
				case '>':
					Read();
					token = "<>";
					return;
				}
				token = "<";
				return;
			case '/':
				Read();
				switch (c) {
				case '*':
					BlockComment();
					continue;
				}
				token = "/";
				return;
			case '.':
				if (char.IsDigit((char)reader.Peek())) {
					Number();
					return;
				}
				Read();
				token = ".";
				return;
			case ',':
				Read();
				token = ",";
				return;
			case '=':
				Read();
				token = "=";
				return;
			case '&':
				Read();
				token = "&";
				return;
			case ';':
				Read();
				token = ";";
				return;
			case '+':
				Read();
				token = "+";
				return;
			case '%':
				Read();
				token = "%";
				return;
			case '(':
				Read();
				token = "(";
				return;
			case ')':
				Read();
				token = ")";
				return;
			case '~':
				Read();
				token = "~";
				return;
			case '*':
				Read();
				token = "*";
				return;
			case '@':
				Read();
				token = "@";
				return;
			case -1:
				token = Eof;
				return;
			case '-':
				Read();
				switch (c) {
				case '-':
					do
						Read();
					while (c != '\n' && 0 <= c);
					continue;
				}
				token = "-";
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
		token = sb.ToString().ToLowerInvariant();
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
		token = sb.ToString();
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
					token = sb.ToString();
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
		text.Append((char)c);
		if (c == '\n')
			line++;
		c = reader.Read();
	}

	Exception ErrorToken(string message) {
		return Error($"{token}: {message}");
	}

	// Error functions return exception objects instead of throwing immediately
	// so 'throw Error(...)' can mark the end of a case block
	Exception Error(string message) {
		return Error(message, line);
	}

	Exception Error(string message, int line) {
		return new FormatException($"{file}:{line}: {message}");
	}
}
