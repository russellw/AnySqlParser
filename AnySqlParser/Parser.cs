using System.Diagnostics;
using System.Text;

namespace AnySqlParser;
public sealed class Parser {
	public static IEnumerable<Statement> Parse(TextReader reader, Schema schema, string file = "SQL", int line = 1) {
		return new Parser(reader, file, line).Statements(schema);
	}

	public static IEnumerable<Statement> Parse(string file, Schema schema) {
		return Parse(new StreamReader(file), schema, file, 1);
	}

	delegate bool Callback();

	const string Eof = " ";

	int c;
	readonly string file;
	int line;
	bool newline;
	readonly TextReader reader;
	string token = null!;
	string wordOriginalCase = null!;

	Parser(TextReader reader, string file, int line) {
		this.reader = reader;
		this.file = file;
		this.line = line;
		Read();
		Lex();
	}

	Expression Addition() {
		var a = Multiplication();
		for (;;) {
			BinaryOp op;
			switch (token) {
			case "&":
				op = BinaryOp.BitAnd;
				break;
			case "+":
				op = BinaryOp.Add;
				break;
			case "-":
				op = BinaryOp.Subtract;
				break;
			case "^":
				op = BinaryOp.BitXor;
				break;
			case "|":
				op = BinaryOp.BitOr;
				break;
			case "||":
				op = BinaryOp.Concat;
				break;
			default:
				return a;
			}
			Lex();
			a = new BinaryExpression(op, a, Multiplication());
		}
	}

	Expression And() {
		var a = Not();
		while (Eat("AND"))
			a = new BinaryExpression(BinaryOp.And, a, Not());
		return a;
	}

	void AppendRead(StringBuilder sb) {
		sb.Append((char)c);
		Read();
	}

	void BlockComment() {
		Debug.Assert('*' == c);
		var line1 = line;
		for (;;) {
			Read();
			switch (c) {
			case '*':
				if ('/' == reader.Peek()) {
					Read();
					Read();
					return;
				}
				break;
			case -1:
				throw Error("unclosed /*", line1);
			}
		}
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

	ColumnRef ColumnRef() {
		var location = new Location(file, line);
		var a = new ColumnRef(location, Name());
		a.Desc = Desc();
		return a;
	}

	Expression Comparison() {
		var a = Addition();
		BinaryOp op;
		switch (token) {
		case "<":
			op = BinaryOp.Less;
			break;
		case "<=":
			op = BinaryOp.LessEqual;
			break;
		case "<>":
			op = BinaryOp.NotEqual;
			break;
		case "=":
			op = BinaryOp.Equal;
			break;
		case ">":
			op = BinaryOp.Greater;
			break;
		case ">=":
			op = BinaryOp.GreaterEqual;
			break;
		case "IS":
			Lex();
			switch (token) {
			case "NOT":
				Lex();
				Expect("NULL");
				return new UnaryExpression(UnaryOp.IsNull, a);
			case "NULL":
				Lex();
				return new UnaryExpression(UnaryOp.IsNull, a);
			}
			throw ErrorToken("expected NOT or NULL");
		default:
			return a;
		}
		Lex();
		return new BinaryExpression(op, a, Addition());
	}

	DataType DataType() {
		var a = new DataType(DataTypeName());
		if ("ENUM" == a.Name) {
			Expect("(");
			a.Values = new();
			do
				a.Values.Add(StringLiteral());
			while (Eat(","));
			Expect(")");
		} else if (Eat("(")) {
			a.Size = Expression();
			if (Eat(","))
				a.Scale = Expression();
			Expect(")");
		}
		return a;
	}

	string DataTypeName() {
		switch (token) {
		case "BINARY":
			Lex();
			switch (token) {
			case "LARGE":
				Lex();
				Expect("OBJECT");
				return "BLOB";
			}
			return "BINARY";
		case "CHAR":
		case "CHARACTER":
			Lex();
			switch (token) {
			case "LARGE":
				Lex();
				Expect("OBJECT");
				return "CLOB";
			case "VARYING":
				Lex();
				return "VARCHAR";
			}
			return "CHAR";
		case "DOUBLE":
			Eat("PRECISION");
			return "DOUBLE";
		case "INTERVAL":
			Lex();
			switch (token) {
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
		case "LONG":
			Lex();
			switch (token) {
			case "RAW":
			case "VARBINARY":
			case "VARCHAR":
				return "LONG " + Lex1();
			}
			return "LONG";
		case "TIME":
			Lex();
			switch (token) {
			case "WITH":
				Lex();
				Expect("TIMEZONE");
				return "TIME WITH TIMEZONE";
			}
			return "TIME";
		case "TIMESTAMP":
			Lex();
			switch (token) {
			case "WITH":
				Lex();
				Expect("TIMEZONE");
				return "TIMESTAMP WITH TIMEZONE";
			}
			return "TIMESTAMP";
		}
		return Name();
	}

	bool Desc() {
		switch (token) {
		case "ASC":
			Lex();
			return false;
		case "DESC":
			Lex();
			return true;
		}
		return false;
	}

	bool Eat(string s) {
		if (Token(s)) {
			Lex();
			return true;
		}
		return false;
	}

	void EndStatement() {
		Eat(";");
		Eat("GO");
	}

	Exception Error(string message) {
		// Error functions return exception objects instead of throwing immediately
		// so 'throw Error(...)' can mark the end of a case block
		return Error(message, line);
	}

	Exception Error(string message, int line) {
		return new SqlError($"{file}:{line}: {message}");
	}

	Exception ErrorToken(string message) {
		return Error($"'{token}': {message}");
	}

	void Expect(string s) {
		if (!Eat(s))
			throw ErrorToken($"expected '{s}'");
	}

	Expression Expression() {
		var a = And();
		for (;;) {
			BinaryOp op;
			switch (token) {
			case "BETWEEN": {
				Lex();
				var b = Addition();
				Expect("AND");
				return new TernaryExpression(TernaryOp.Between, a, b, Addition());
			}
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
			case "LIKE":
				op = BinaryOp.Like;
				break;
			case "NOT": {
				Lex();
				Expect("BETWEEN");
				var b = Addition();
				Expect("AND");
				return new TernaryExpression(TernaryOp.NotBetween, a, b, Addition());
			}
			case "OR":
				op = BinaryOp.Or;
				break;
			default:
				return a;
			}
			Lex();
			a = new BinaryExpression(op, a, And());
		}
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
			switch (token) {
			case "ON":
				Lex();
				switch (token) {
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

	Action GetAction() {
		switch (token) {
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
			switch (token) {
			case "DEFAULT":
				Lex();
				return Action.SetDefault;
			case "NULL":
				Lex();
				return Action.SetNull;
			}
			throw ErrorToken("expected replacement value");
		}
		throw ErrorToken("expected action");
	}

	QueryExpression Intersect() {
		// https://stackoverflow.com/questions/56224171/does-intersect-have-a-higher-precedence-compared-to-union
		QueryExpression a = QuerySpecification();
		while (Eat("INTERSECT"))
			a = new QueryBinaryExpression(QueryOp.Intersect, a, QuerySpecification());
		return a;
	}

	bool IsElementEnd() {
		switch (token) {
		case ")":
		case ",":
			return true;
		}
		return false;
	}

	bool IsName() {
		switch (token[0]) {
		case '"':
		case '$':
		case '@':
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
		case 'N':
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
		case '[':
		case '_':
		case '`':
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
			return true;
		}
		return char.IsLetter(token[0]);
	}

	bool IsStatementEnd() {
		switch (token) {
		case ";":
		case "GO":
		case Eof:
			return true;
		}
		return false;
	}

	TableSource Join() {
		var a = PrimaryTableSource();
		for (;;)
			switch (token) {
			case "FULL": {
				Lex();
				Eat("OUTER");
				Expect("JOIN");
				var b = PrimaryTableSource();
				Expect("ON");
				a = new Join(JoinType.Full, a, b, Expression());
				break;
			}
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
			default:
				return a;
			}
	}

	Key Key(Column? column) {
		var location = new Location(file, line);
		switch (token) {
		case "KEY":
			Lex();
			break;
		case "PRIMARY":
			Lex();
			Expect("KEY");
			break;
		case "UNIQUE":
			Lex();
			Eat("KEY");
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

	void Lex() {
		newline = false;
		for (;;) {
			switch (c) {
			case ' ':
			case '\f':
			case '\r':
			case '\t':
			case '\v':
				Read();
				continue;
			case '!':
				Read();
				switch (c) {
				case '<':
					// https://stackoverflow.com/questions/77475517/what-are-the-t-sql-and-operators-for
					Read();
					token = ">=";
					return;
				case '=':
					Read();
					token = "<>";
					return;
				case '>':
					Read();
					token = "<=";
					return;
				}
				throw Error($"stray '!'");
			case '"':
			case '\'':
			case '`':
				Quote();
				return;
			case '#':
			case '\\':
				reader.ReadLine();
				c = '\n';
				continue;
			case '$':
				if (char.IsDigit((char)reader.Peek())) {
					Read();
					Number();
					return;
				}
				Word();
				return;
			case '%':
				Read();
				token = "%";
				return;
			case '&':
				Read();
				token = "&";
				return;
			case '(':
				Read();
				token = "(";
				return;
			case ')':
				Read();
				token = ")";
				return;
			case '*':
				Read();
				token = "*";
				return;
			case '+':
				Read();
				token = "+";
				return;
			case ',':
				Read();
				token = ",";
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
			case '.':
				if (char.IsDigit((char)reader.Peek())) {
					Number();
					return;
				}
				Read();
				token = ".";
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
			case ':':
				Read();
				switch (c) {
				case ':':
					Read();
					token = "::";
					return;
				}
				token = ":";
				return;
			case ';':
				Read();
				token = ";";
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
			case '=':
				Read();
				token = "=";
				return;
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
			case '@':
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
			case 'N':
				if ('\'' == reader.Peek()) {
					// We are reading everything as Unicode anyway
					// so the prefix has no special meaning
					Read();
					Quote();
					return;
				}
				Word();
				return;
			case '[':
				Quote(']');
				return;
			case '\n':
				newline = true;
				Read();
				continue;
			case '^':
				Read();
				token = "^";
				return;
			case '|':
				Read();
				switch (c) {
				case '|':
					Read();
					token = "||";
					return;
				}
				token = "|";
				return;
			case '~':
				Read();
				token = "~";
				return;
			case -1:
				token = Eof;
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
			throw Error($"stray '{(char)c}'");
		}
	}

	string Lex1() {
		var s = token;
		Lex();
		return s;
	}

	Expression Multiplication() {
		var a = Prefix();
		for (;;) {
			BinaryOp op;
			switch (token) {
			case "%":
				op = BinaryOp.Remainder;
				break;
			case "*":
				op = BinaryOp.Multiply;
				break;
			case "/":
				op = BinaryOp.Divide;
				break;
			default:
				return a;
			}
			Lex();
			a = new BinaryExpression(op, a, Prefix());
		}
	}

	string Name() {
		switch (token[0]) {
		case '"':
		case '`':
			return Etc.Unquote(Lex1());
		case '$':
		case '@':
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
		case 'N':
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
		case 'z': {
			var s = wordOriginalCase;
			Lex();
			return s;
		}
		case '[':
			return Etc.Unquote(Lex1(), ']');
		}
		if (char.IsLetter(token[0]))
			return Lex1();
		throw ErrorToken("expected name");
	}

	Expression Not() {
		if (Eat("NOT"))
			return new UnaryExpression(UnaryOp.Not, Not());
		return Comparison();
	}

	void Number() {
		var sb = new StringBuilder();
		while (Etc.IsWordPart(c))
			AppendRead(sb);
		if ('.' == c)
			do
				AppendRead(sb);
			while (Etc.IsWordPart(c));
		Debug.Assert(0 != sb.Length);
		token = sb.ToString();
	}

	Expression Postfix() {
		var a = Primary();
		for (;;)
			switch (token) {
			case "(":
				Lex();
				if (a is QualifiedName a1) {
					var call = new Call(a1);
					if (")" != token)
						do
							call.Arguments.Add(Expression());
						while (Eat(","));
					Expect(")");
					return call;
				}
				throw Error("call of non-function");
			case "::":
			case "AS":
				Lex();
				a = new Cast(a, DataType());
				break;
			default:
				return a;
			}
	}

	Expression Prefix() {
		switch (token) {
		case "-":
			Lex();
			return new UnaryExpression(UnaryOp.Minus, Prefix());
		case "CAST": {
			Lex();
			Expect("(");
			var a = new Cast(Expression());
			Expect("AS");
			a.Type = DataType();
			Expect(")");
			return a;
		}
		case "EXISTS": {
			Lex();
			Expect("(");
			var a = new Exists(Select());
			Expect(")");
			return a;
		}
		case "SELECT":
			return new Subquery(QueryExpression());
		case "~":
			Lex();
			return new UnaryExpression(UnaryOp.BitNot, Prefix());
		}
		return Postfix();
	}

	Expression Primary() {
		switch (token) {
		case "(": {
			Lex();
			var a = Expression();
			Expect(")");
			return a;
		}
		case "*":
			return QualifiedName();
		case "NULL":
			Lex();
			return new Null();
		}
		switch (token[0]) {
		case '"':
		case '$':
		case '@':
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
		case 'N':
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
		case '[':
		case '_':
		case '`':
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
			return QualifiedName();
		case '.':
			if (1 < token.Length && char.IsDigit(token, 1))
				return new Number(Lex1());
			break;
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
		case '\'':
			return new StringLiteral(StringLiteral());
		}
		throw ErrorToken("expected expression");
	}

	TableSource PrimaryTableSource() {
		if (Eat("(")) {
			var b = TableSource();
			Expect(")");
			return b;
		}
		var a = new PrimaryTableSource(QualifiedName());
		switch (token) {
		case "AS":
			Lex();
			break;
		case "FULL":
		case "GROUP":
		case "INNER":
		case "JOIN":
		case "LEFT":
		case "ON":
		case "ORDER":
		case "RIGHT":
		case "WHERE":
			return a;
		default:
			if (!IsName())
				return a;
			break;
		}
		a.TableAlias = Name();
		return a;
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

	QueryExpression QueryExpression() {
		var a = Intersect();
		for (;;) {
			QueryOp op;
			switch (token) {
			case "EXCEPT":
				Lex();
				op = QueryOp.Except;
				break;
			case "UNION":
				Lex();
				if (Eat("ALL")) {
					op = QueryOp.UnionAll;
					break;
				}
				op = QueryOp.Union;
				break;
			default:
				return a;
			}
			a = new QueryBinaryExpression(op, a, Intersect());
		}
	}

	QuerySpecification QuerySpecification() {
		Expect("SELECT");
		var a = new QuerySpecification();

		// Some clauses are written before the select list
		// but unknown keywords must be left alone
		// as they might be part of the select list
		for (;;) {
			switch (token) {
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
			switch (token) {
			case "FROM":
				Lex();
				do
					a.From.Add(TableSource());
				while (Eat(","));
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
			case "WHERE":
				Lex();
				a.Where = Expression();
				break;
			case "WINDOW":
				Lex();
				a.Window = Expression();
				break;
			default:
				return a;
			}
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

	void Read() {
		if ('\n' == c)
			line++;
		c = reader.Read();
	}

	Select Select() {
		var a = new Select(QueryExpression());
		switch (token) {
		case "ORDER":
			Lex();
			Expect("BY");
			a.OrderBy = Expression();
			a.Desc = Desc();
			break;
		}
		return a;
	}

	void Skip() {
		var line1 = line;
		int depth = 0;
		do {
			switch (token) {
			case "(":
				depth++;
				break;
			case ")":
				if (0 == depth)
					throw Error("unexpected )");
				depth--;
				break;
			case Eof:
				throw Error(0 == depth ? "missing element" : "unclosed (", line1);
			}
			Lex();
		} while (0 != depth);
	}

	IEnumerable<Statement> Statements(Schema schema) {
		for (;;) {
			switch (token) {
			case "ALTER": {
				var location = new Location(file, line);
				Lex();
				switch (token) {
				case "TABLE": {
					Lex();
					var tableName = UnqualifiedName();
					switch (token) {
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
			case "CREATE": {
				var location = new Location(file, line);
				Lex();
				var unique = Eat("UNIQUE");
				switch (token) {
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
				case "PROC":
				case "PROCEDURE":
					do
						Skip();
					while ("GO" != token && Eof != token);
					continue;
				case "TABLE": {
					Lex();
					var table = new Table(UnqualifiedName());
					if (Eat("AS"))
						Name();
					if (";" == token)
						continue;
					Expect("(");
					do {
						if (")" == token)
							break;
						var a = TableElement(table, IsElementEnd);
						while (!IsElementEnd())
							Skip();
					} while (Eat(","));
					Expect(")");
					EndStatement();
					schema.Add(location, table);
					continue;
				}
				case "VIEW": {
					Lex();
					var a = new View();
					a.Name = QualifiedName();
					if (Eat("WITH"))
						do
							Name();
						while (Eat(","));
					Expect("AS");
					a.Query = Select();
					EndStatement();
					yield return a;
					continue;
				}
				}
				break;
			}
			case "INSERT": {
				Lex();
				if ("," == token)
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
				if (!Eat("VALUES"))
					continue;
				Expect("(");
				do
					a.Values.Add(Expression());
				while (Eat(","));
				Expect(")");
				EndStatement();
				yield return a;
				continue;
			}
			case Eof:
				yield break;
			}
			Skip();
		}
	}

	string StringLiteral() {
		if ('\'' == token[0])
			return Etc.Unquote(Lex1());
		throw ErrorToken("expected string literal");
	}

	Element TableConstraint(Table table, Callback isEnd) {
		switch (token) {
		case "CHECK": {
			var a = Check();
			table.Checks.Add(a);
			return a;
		}
		case "FOREIGN": {
			var a = ForeignKey(null, isEnd);
			table.ForeignKeys.Add(a);
			return a;
		}
		case "KEY":
		case "UNIQUE": {
			var a = Key(null);
			table.Uniques.Add(a);
			return a;
		}
		case "PRIMARY": {
			var a = Key(null);
			table.AddPrimaryKey(a);
			return a;
		}
		}
		throw ErrorToken("expected constraint");
	}

	Element TableElement(Table table, Callback isEnd) {
		// Might be a table constraint
		if (Eat("CONSTRAINT")) {
			Name();
			return TableConstraint(table, isEnd);
		}
		switch (token) {
		case "CHECK":
		case "EXCLUDE":
		case "FOREIGN":
		case "KEY":
		case "PRIMARY":
		case "UNIQUE":
			return TableConstraint(table, isEnd);
		}

		// This is a column
		var location = new Location(file, line);
		var a = new Column(location, Name(), DataType());

		// Search the postscript for column constraints
		while (!isEnd()) {
			switch (token) {
			case "CHECK":
				table.Checks.Add(Check());
				continue;
			case "DEFAULT":
				Lex();
				a.Default = Expression();
				continue;
			case "FOREIGN":
			case "REFERENCES":
				ForeignKey(a, isEnd);
				continue;
			case "IDENTITY":
				Lex();
				a.AutoIncrement = true;
				if (Eat("(")) {
					Expression();
					Expect(",");
					Expression();
					Expect(")");
				}
				continue;
			case "NOT":
				Lex();
				switch (token) {
				case "NULL":
					Lex();
					a.Nullable = false;
					continue;
				}
				continue;
			case "NULL":
				Lex();
				continue;
			case "PRIMARY":
				table.AddPrimaryKey(Key(a));
				continue;
			}
			Skip();
		}

		// Add to table
		table.Add(a);
		return a;
	}

	TableRef TableRef() {
		var location = new Location(file, line);
		return new TableRef(location, UnqualifiedName());
	}

	TableSource TableSource() {
		return Join();
	}

	bool Token(string s) {
		return string.Equals(token, s, StringComparison.OrdinalIgnoreCase);
	}

	string UnqualifiedName() {
		string name;
		do
			name = Name();
		while (Eat("."));
		return name;
	}

	void Word() {
		var sb = new StringBuilder();
		do
			AppendRead(sb);
		while (Etc.IsWordPart(c));
		wordOriginalCase = sb.ToString();
		token = wordOriginalCase.ToUpperInvariant();
	}
}
