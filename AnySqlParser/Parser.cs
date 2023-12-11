using System.Diagnostics;
using System.Text;

namespace AnySqlParser;
public sealed class Parser {
	public static IEnumerable<Statement> Parse(string file, Schema schema) {
		return Parse(new StreamReader(file), schema, file, 1);
	}

	public static IEnumerable<Statement> Parse(TextReader reader, Schema schema, string file = "SQL", int line = 1) {
		return new Parser(reader, file, line).Statements(schema);
	}

	delegate bool Callback();

	// All tokens are represented as strings of positive length
	// so the first character of the current token can always be tested
	const string Eof = " ";

	readonly TextReader reader;
	readonly string file;
	int line;
	int c;
	bool newline;
	string token = null!;

	Parser(TextReader reader, string file, int line) {
		this.reader = reader;
		this.file = file;
		this.line = line;
		Read();
		Lex();
	}

	IEnumerable<Statement> Statements(Schema schema) {
		for (;;) {
			switch (token.ToUpperInvariant()) {
			case Eof:
				yield break;
			case "INSERT": {
				Lex();
				if (token == ",")
					break;
				Eat("INTO");
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
				Expect("VALUES");
				Expect("(");
				do
					a.Values.Add(Expression());
				while (Eat(","));
				Expect(")");
				EndStatement();
				yield return a;
				continue;
			}
			case "CREATE": {
				var location = new Location(file, line);
				Lex();
				var unique = Eat("UNIQUE");
				switch (token.ToUpperInvariant()) {
				case "INDEX": {
					Lex();
					var a = new Index(unique);
					a.Name = Name();

					// Table
					Expect("ON");
					a.TableName = QualifiedName();

					// Columns
					Expect("(");
					do
						a.Columns.Add(ColumnRef());
					while (Eat(","));
					Expect(")");

					// Include
					if (Eat("INCLUDE")) {
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
				case "VIEW": {
					Lex();
					var a = new View();
					a.Name = QualifiedName();
					Expect("AS");
					a.Query = Select();
					EndStatement();
					yield return a;
					continue;
				}
				case "TABLE": {
					Lex();
					var a = schema.CreateTable(location, UnqualifiedName());
					while (!Eat("("))
						Skip();
					do {
						if (token == ")")
							break;
						var b = TableElement(a, IsElementEnd);
						while (!IsElementEnd())
							Skip();
					} while (Eat(","));
					Expect(")");
					EndStatement();
					continue;
				}
				}
				break;
			}
			case "ALTER": {
				var location = new Location(file, line);
				Lex();
				switch (token.ToUpperInvariant()) {
				case "TABLE": {
					Lex();
					var tableName = UnqualifiedName();
					switch (token.ToUpperInvariant()) {
					case "ADD": {
						Lex();
						var a = schema.GetTable(location, tableName);
						do
							TableElement(a, IsStatementEnd);
						while (Eat(","));
						EndStatement();
						continue;
					}
					}
					break;
				}
				}
				break;
			}
			}
			Skip();
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
		Eat("GO");
	}

	bool IsStatementEnd() {
		switch (token.ToUpperInvariant()) {
		case Eof:
		case ";":
		case "GO":
			return true;
		}
		return false;
	}

	void Skip() {
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
				if (depth == 0)
					throw Error("unexpected )");
				depth--;
				break;
			}
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
		if (Eat("FOREIGN"))
			Expect("KEY");
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
		Expect("REFERENCES");
		a.RefTable = TableRef();
		if (Eat("(")) {
			do
				a.RefColumns.Add(ColumnRef());
			while (Eat(","));
			Expect(")");
		}

		// Search the postscript for actions
		while (!isEnd()) {
			switch (token.ToUpperInvariant()) {
			case "ON":
				Lex();
				switch (token.ToUpperInvariant()) {
				case "DELETE":
					Lex();
					a.OnDelete = GetAction();
					continue;
				case "UPDATE":
					Lex();
					a.OnUpdate = GetAction();
					continue;
				}
				break;
			}
			Skip();
		}
		return a;
	}

	Element TableConstraint(Table table, Callback isEnd) {
		switch (token.ToUpperInvariant()) {
		case "FOREIGN": {
			var a = ForeignKey(null, isEnd);
			table.ForeignKeys.Add(a);
			return a;
		}
		case "PRIMARY": {
			var a = Key(null);
			table.AddPrimaryKey(a);
			return a;
		}
		case "CHECK": {
			var a = Check();
			table.Checks.Add(a);
			return a;
		}
		case "UNIQUE":
		case "KEY": {
			var a = Key(null);
			table.Uniques.Add(a);
			return a;
		}
		}
		throw ErrorToken("expected constraint");
	}

	Key Key(Column? column) {
		var location = new Location(file, line);
		switch (token.ToUpperInvariant()) {
		case "PRIMARY":
			Lex();
			Expect("KEY");
			break;
		case "UNIQUE":
			Lex();
			Eat("KEY");
			break;
		case "KEY":
			Lex();
			break;
		default:
			throw Error("expected key");
		}
		var a = new Key(location);

		if (column == null) {
			while (!Eat("("))
				Skip();
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
		if (Eat("CONSTRAINT")) {
			Name();
			return TableConstraint(table, isEnd);
		}
		switch (token.ToUpperInvariant()) {
		case "FOREIGN":
		case "KEY":
		case "PRIMARY":
		case "UNIQUE":
		case "CHECK":
		case "EXCLUDE":
			return TableConstraint(table, isEnd);
		}

		// This is a column
		var location = new Location(file, line);
		var a = new Column(location, Name(), DataType());

		// Search the postscript for column constraints
		while (!isEnd()) {
			switch (token.ToUpperInvariant()) {
			case "DEFAULT":
				Lex();
				a.Default = Expression();
				continue;
			case "IDENTITY":
				Lex();
				a.AutoIncrement = true;
				if (Eat("(")) {
					Int();
					Expect(",");
					Int();
					Expect(")");
				}
				continue;
			case "FOREIGN":
			case "REFERENCES":
				ForeignKey(a, isEnd);
				continue;
			case "CHECK":
				table.Checks.Add(Check());
				continue;
			case "PRIMARY":
				table.AddPrimaryKey(Key(a));
				continue;
			case "NULL":
				Lex();
				continue;
			case "NOT":
				Lex();
				switch (token.ToUpperInvariant()) {
				case "NULL":
					Lex();
					a.Nullable = false;
					continue;
				}
				continue;
			}
			Skip();
		}

		// Add to table
		table.Add(a);
		return a;
	}

	string DataTypeName() {
		switch (token.ToUpperInvariant()) {
		case "CHARACTER":
		case "CHAR":
			Lex();
			switch (token.ToUpperInvariant()) {
			case "LARGE":
				Lex();
				Expect("OBJECT");
				return "CLOB";
			case "VARYING":
				Lex();
				return "VARCHAR";
			}
			return "CHAR";
		case "BINARY":
			Lex();
			switch (token.ToUpperInvariant()) {
			case "LARGE":
				Lex();
				Expect("OBJECT");
				return "BLOB";
			}
			return "BINARY";
		case "DOUBLE":
			Eat("PRECISION");
			return "DOUBLE";
		case "LONG":
			Lex();
			switch (token.ToUpperInvariant()) {
			case "RAW":
			case "VARBINARY":
			case "VARCHAR":
				return "LONG " + Lex1();
			}
			return "LONG";
		case "TIME":
			Lex();
			switch (token.ToUpperInvariant()) {
			case "WITH":
				Lex();
				Expect("TIMEZONE");
				return "TIME WITH TIMEZONE";
			}
			return "TIME";
		case "TIMESTAMP":
			Lex();
			switch (token.ToUpperInvariant()) {
			case "WITH":
				Lex();
				Expect("TIMEZONE");
				return "TIMESTAMP WITH TIMEZONE";
			}
			return "TIMESTAMP";
		case "INTERVAL":
			Lex();
			switch (token.ToUpperInvariant()) {
			case "DAY":
				Lex();
				Expect("TO");
				Expect("SECOND");
				return "INTERVAL DAY TO SECOND";
			case "YEAR":
				Lex();
				Expect("TO");
				Expect("MONTH");
				return "INTERVAL YEAR TO MONTH";
			}
			return "INTERVAL";
		}
		return Name().ToUpperInvariant();
	}

	DataType DataType() {
		var a = new DataType(DataTypeName());
		if (a.Name == "ENUM") {
			Expect("(");
			a.Values = new();
			do
				a.Values.Add(StringLiteral());
			while (Eat(","));
			Expect(")");
		} else if (Eat("(")) {
			a.Size = Int();
			if (Eat(","))
				a.Scale = Int();
			Expect(")");
		}
		return a;
	}

	Action GetAction() {
		switch (token.ToUpperInvariant()) {
		case "CASCADE":
			Lex();
			return Action.Cascade;
		case "NO":
			Lex();
			Expect("ACTION");
			return Action.NoAction;
		case "RESTRICT":
			Lex();
			return Action.NoAction;
		case "SET":
			Lex();
			switch (token.ToUpperInvariant()) {
			case "NULL":
				Lex();
				return Action.SetNull;
			case "DEFAULT":
				Lex();
				return Action.SetDefault;
			}
			throw ErrorToken("expected replacement value");
		}
		throw ErrorToken("expected action");
	}

	Check Check() {
		Debug.Assert(Token("CHECK"));
		var location = new Location(file, line);
		var a = new Check(location);
		while (!Eat("("))
			Skip();
		a.Expression = Expression();
		Expect(")");
		return a;
	}

	Select Select() {
		var a = new Select(QueryExpression());
		switch (token.ToUpperInvariant()) {
		case "ORDER":
			Lex();
			Expect("BY");
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
			switch (token.ToUpperInvariant()) {
			case "UNION":
				Lex();
				if (Eat("ALL")) {
					op = QueryOp.UnionAll;
					break;
				}
				op = QueryOp.Union;
				break;
			case "EXCEPT":
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
		while (Eat("INTERSECT"))
			a = new QueryBinaryExpression(QueryOp.Intersect, a, QuerySpecification());
		return a;
	}

	QuerySpecification QuerySpecification() {
		Expect("SELECT");
		var a = new QuerySpecification();

		// Some clauses are written before the select list
		// but unknown keywords must be left alone
		// as they might be part of the select list
		for (;;) {
			switch (token.ToUpperInvariant()) {
			case "ALL":
				Lex();
				a.All = true;
				continue;
			case "DISTINCT":
				Lex();
				a.Distinct = true;
				continue;
			case "TOP":
				Lex();
				a.Top = Expression();
				if (Eat("PERCENT"))
					a.Percent = true;
				if (Eat("WITH")) {
					Expect("TIES");
					a.WithTies = true;
				}
				continue;
			}
			break;
		}

		// Select list
		do {
			var c = new SelectColumn(new Location(file, line), Expression());
			if (Eat("AS"))
				c.ColumnAlias = Expression();
			a.SelectList.Add(c);
		} while (Eat(","));

		// Any keyword after the select list, must be a clause
		for (;;)
			switch (token.ToUpperInvariant()) {
			case "WHERE":
				Lex();
				a.Where = Expression();
				break;
			case "GROUP":
				Lex();
				Expect("BY");
				do
					a.GroupBy.Add(Expression());
				while (Eat(","));
				break;
			case "HAVING":
				Lex();
				a.Having = Expression();
				break;
			case "WINDOW":
				Lex();
				a.Window = Expression();
				break;
			case "FROM":
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
			switch (token.ToUpperInvariant()) {
			case "INNER": {
				Lex();
				Expect("JOIN");
				var b = PrimaryTableSource();
				Expect("ON");
				a = new Join(JoinType.Inner, a, b, Expression());
				break;
			}
			case "JOIN": {
				Lex();
				var b = PrimaryTableSource();
				Expect("ON");
				a = new Join(JoinType.Inner, a, b, Expression());
				break;
			}
			case "LEFT": {
				Lex();
				Eat("OUTER");
				Expect("JOIN");
				var b = PrimaryTableSource();
				Expect("ON");
				a = new Join(JoinType.Left, a, b, Expression());
				break;
			}
			case "RIGHT": {
				Lex();
				Eat("OUTER");
				Expect("JOIN");
				var b = PrimaryTableSource();
				Expect("ON");
				a = new Join(JoinType.Right, a, b, Expression());
				break;
			}
			case "FULL": {
				Lex();
				Eat("OUTER");
				Expect("JOIN");
				var b = PrimaryTableSource();
				Expect("ON");
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
		switch (token.ToUpperInvariant()) {
		case "AS":
			Lex();
			break;
		case "WHERE":
		case "INNER":
		case "JOIN":
		case "LEFT":
		case "RIGHT":
		case "FULL":
		case "ON":
		case "GROUP":
		case "ORDER":
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
		switch (token.ToUpperInvariant()) {
		case "DESC":
			Lex();
			return true;
		case "ASC":
			Lex();
			return false;
		}
		return false;
	}

	Expression Expression() {
		var a = And();
		for (;;) {
			BinaryOp op;
			switch (token.ToUpperInvariant()) {
			case "NOT": {
				Lex();
				Expect("BETWEEN");
				var b = Addition();
				Expect("AND");
				return new TernaryExpression(TernaryOp.NotBetween, a, b, Addition());
			}
			case "BETWEEN": {
				Lex();
				var b = Addition();
				Expect("AND");
				return new TernaryExpression(TernaryOp.Between, a, b, Addition());
			}
			case "OR":
				op = BinaryOp.Or;
				break;
			case "LIKE":
				op = BinaryOp.Like;
				break;
			case "IN": {
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
		while (Eat("AND"))
			a = new BinaryExpression(BinaryOp.And, a, Not());
		return a;
	}

	Expression Not() {
		if (Eat("NOT"))
			return new UnaryExpression(UnaryOp.Not, Not());
		return Comparison();
	}

	Expression Comparison() {
		var a = Addition();
		BinaryOp op;
		switch (token.ToUpperInvariant()) {
		case "IS":
			Lex();
			switch (token.ToUpperInvariant()) {
			case "NULL":
				Lex();
				return new UnaryExpression(UnaryOp.IsNull, a);
			case "NOT":
				Lex();
				Expect("NULL");
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
		switch (token.ToUpperInvariant()) {
		case "SELECT":
			return new Subquery(QueryExpression());
		case "EXISTS": {
			Lex();
			Expect("(");
			var a = new Exists(Select());
			Expect(")");
			return a;
		}
		case "CAST": {
			Lex();
			Expect("(");
			var a = new Cast(Expression());
			Expect("AS");
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
		switch (token.ToUpperInvariant()) {
		case "@":
			Lex();
			return new ParameterRef(Name());
		case "NULL":
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
			return new StringLiteral(StringLiteral());
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

	string StringLiteral() {
		if (token[0] == '\'')
			return Etc.Unquote(Lex1());
		throw ErrorToken("expected string literal");
	}

	QualifiedName QualifiedName() {
		var a = new QualifiedName();
		do {
			if (Eat("*")) {
				a.Star = true;
				break;
			}
			a.Names.Add(Name());
		} while (Eat("."));
		return a;
	}

	int Int() {
		int n;
		try {
			n = int.Parse(token, System.Globalization.CultureInfo.InvariantCulture);
		} catch (FormatException e) {
			throw Error(e.Message);
		}
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
			throw ErrorToken("expected " + s);
	}

	bool Token(string s) {
		return string.Equals(token, s, StringComparison.OrdinalIgnoreCase);
	}

	bool Eat(string s) {
		if (Token(s)) {
			Lex();
			return true;
		}
		return false;
	}

	void Lex() {
		newline = false;
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
			case ':':
				Read();
				switch (c) {
				case ':':
					Read();
					token = "::";
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
					reader.ReadLine();
					c = '\n';
					continue;
				}
				token = "-";
				return;
			case '#':
			case '\\':
				reader.ReadLine();
				c = '\n';
				continue;
			case '\n':
				newline = true;
				Read();
				continue;
			case '\r':
			case '\t':
			case '\f':
			case '\v':
			case ' ':
				Read();
				continue;
			case '$':
				if (char.IsDigit((char)reader.Peek())) {
					Read();
					Number();
					return;
				}
				break;
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
		token = sb.ToString();
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
		return new SqlError($"{file}:{line}: {message}");
	}
}
