﻿using System.Text;

namespace AnySqlParser
{
    public sealed class Parser
    {
        public static List<Statement> ParseFile(string file)
        {
            return ParseText(File.ReadAllText(file), file);
        }

        public static List<Statement> ParseText(string text, string file = "SQL", int line = 1)
        {
            var parser = new Parser(text, file, line);
            return parser.statements;
        }

        const int kDoublePipe = -2;
        const int kGreaterEqual = -3;
        const int kLessEqual = -4;
        const int kNotEqual = -5;
        const int kNumber = -6;
        const int kQuotedName = -7;
        const int kStringLiteral = -8;
        const int kWord = -9;

        readonly string text;
        readonly string file;
        int line;
        int textIndex;
        int token;
        string tokenString = null!;
        readonly List<Statement> statements = new();

        Parser(string text, string file, int line)
        {
            this.text = text;
            this.file = file;
            this.line = line;
            Lex();
            while (token != -1)
            {
                if (Eat("go"))
                    continue;
                statements.Add(StatementSemicolon());
            }
        }

        //statements
        Statement StatementSemicolon()
        {
            var a = Statement();
            Eat(';');
            return a;
        }

        Statement Statement()
        {
            var location = new Location(file, line);
            switch (Keyword())
            {
                case "if":
                    {
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
                    switch (token)
                    {
                        case kWord:
                            {
                                Lex();
                                var a = new SetParameter(location);
                                a.Name = Name();
                                a.Value = Name();
                                return a;
                            }
                    }
                    throw Err(Echo() + ": expected parameter");
                case "begin":
                    Lex();
                    switch (Keyword())
                    {
                        case "transaction":
                        case "tran":
                            Lex();
                            return new Start(location);
                        default:
                            {
                                var a = new Block(location);
                                while (!Eat("end"))
                                    a.Body.Add(StatementSemicolon());
                                return a;
                            }
                    }
                case "start":
                    Lex();
                    switch (Keyword())
                    {
                        case "transaction":
                        case "tran":
                            Lex();
                            break;
                    }
                    return new Start(location);
                case "commit":
                    Lex();
                    switch (Keyword())
                    {
                        case "transaction":
                        case "tran":
                            Lex();
                            break;
                    }
                    return new Commit(location);
                case "rollback":
                    Lex();
                    switch (Keyword())
                    {
                        case "transaction":
                        case "tran":
                            Lex();
                            break;
                    }
                    return new Rollback(location);
                case "select":
                    return Select();
                case "insert":
                    {
                        Lex();
                        Eat("into");
                        var a = new Insert(location);

                        //table
                        a.TableName = QualifiedName();

                        //columns
                        if (Eat('('))
                        {
                            do
                                a.Columns.Add(Name());
                            while (Eat(','));
                            Expect(')');
                        }

                        //values
                        Expect("values");
                        Expect('(');
                        do
                            a.Values.Add(Expression());
                        while (Eat(','));
                        Expect(')');

                        return a;
                    }
                case "create":
                    Lex();
                    switch (Keyword())
                    {
                        case "table":
                            {
                                Lex();
                                var a = new Table(location, QualifiedName());
                                Expect('(');
                                do
                                {
                                    var constraintName = "";
                                    if (Eat("constraint"))
                                        constraintName = Name();
                                    switch (Keyword())
                                    {
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
                                            if (constraintName != "")
                                                throw Err(Echo() + ": expected constraint");
                                            a.Columns.Add(Column());
                                            break;
                                    }
                                } while (Eat(','));
                                Expect(')');
                                return a;
                            }
                    }
                    throw Err(Echo() + ": expected noun");
                case "drop":
                    Lex();
                    switch (Keyword())
                    {
                        case "proc":
                        case "procedure":
                            {
                                Lex();
                                var a = new DropProcedure(location);
                                if (Eat("if"))
                                {
                                    Expect("exists");
                                    a.IfExists = true;
                                }
                                do
                                    a.Names.Add(QualifiedName());
                                while (Eat(','));
                                return a;
                            }
                        case "view":
                            {
                                Lex();
                                var a = new DropView(location);
                                if (Eat("if"))
                                {
                                    Expect("exists");
                                    a.IfExists = true;
                                }
                                do
                                    a.Names.Add(QualifiedName());
                                while (Eat(','));
                                return a;
                            }
                        case "table":
                            {
                                Lex();
                                var a = new DropTable(location);
                                if (Eat("if"))
                                {
                                    Expect("exists");
                                    a.IfExists = true;
                                }
                                do
                                    a.Names.Add(QualifiedName());
                                while (Eat(','));
                                return a;
                            }
                    }
                    throw Err(Echo() + ": expected noun");
            }
            throw Err(Echo() + ": expected statement");
        }

        Select Select()
        {
            var location = new Location(file, line);
            Expect("select");
            var a = new Select(location);

            //Some clauses are written before the select list
            //but unknown keywords must be left alone
            //as they might be part of the select list
            for (; ; )
            {
                switch (Keyword())
                {
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
                        if (Eat("with"))
                        {
                            Expect("ties");
                            a.WithTies = true;
                        }
                        continue;
                }
                break;
            }

            //select list
            if (!Eat('*'))
                do
                    a.SelectList.Add(Expression());
                while (Eat(','));

            //Any keyword after the select list, must be a clause
            while (token == kWord)
                switch (Keyword())
                {
                    case "where":
                        Lex();
                        a.Where = Expression();
                        break;
                    case "group":
                        Lex();
                        Expect("by");
                        a.GroupBy = Expression();
                        break;
                    case "order":
                        Lex();
                        Expect("by");
                        a.OrderBy = Expression();
                        a.Desc = Desc();
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
                            a.From.Add(Expression());
                        while (Eat(','));
                        break;
                    default:
                        throw Err(Echo() + ": expected clause");
                }
            return a;
        }

        //tables
        Column Column()
        {
            var location = new Location(file, line);
            var a = new Column(location, Name());

            //data type
            a.TypeName = QualifiedName();
            if (Eat('('))
            {
                a.Size = Int();
                if (Eat(','))
                    a.Scale = Int();
                Expect(')');
            }

            //constraints etc
            while (token == kWord)
                switch (Keyword())
                {
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
                        if (Eat('('))
                        {
                            a.IdentitySeed = Int();
                            Expect(',');
                            a.IdentityIncrement = Int();
                            Expect(')');
                        }
                        break;
                    case "not":
                        Lex();
                        switch (Keyword())
                        {
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
                                throw Err(Echo() + ": expected option");
                        }
                        break;
                    default:
                        throw Err(Echo() + ": expected constraint");
                }
            return a;
        }

        Key Key(string constraintName)
        {
            var location = new Location(file, line);
            var a = new Key(location, constraintName);

            //primary?
            switch (Keyword())
            {
                case "primary":
                    Lex();
                    Expect("key");
                    a.Primary = true;
                    break;
                case "unique":
                    Lex();
                    break;
                default:
                    throw Err(Echo() + ": expected key type");
            }

            //clustered?
            switch (Keyword())
            {
                case "clustered":
                    Lex();
                    a.Clustered = true;
                    break;
                case "nonclustered":
                    Lex();
                    a.Clustered = false;
                    break;
            }

            //columns
            Expect('(');
            do
                a.Columns.Add(ColumnOrder());
            while (Eat(','));
            Expect(')');
            return a;
        }

        ForeignKey ForeignKey(string constraintName)
        {
            var location = new Location(file, line);
            Expect("foreign");
            Expect("key");
            var a = new ForeignKey(location, constraintName);

            //columns
            Expect('(');
            do
                a.Columns.Add(Name());
            while (Eat(','));
            Expect(')');

            //references
            Expect("references");
            a.RefTableName = QualifiedName();
            if (Eat('('))
            {
                do
                    a.RefColumns.Add(Name());
                while (Eat(','));
                Expect(')');
            }

            //actions
            while (Eat("on"))
                switch (Keyword())
                {
                    case "delete":
                        Lex();
                        a.OnDelete = Action();
                        break;
                    case "update":
                        Lex();
                        a.OnUpdate = Action();
                        break;
                    default:
                        throw Err(Echo() + ": expected event type");
                }

            //replication
            if (Eat("not"))
            {
                Expect("for");
                Expect("replication");
                a.ForReplication = false;
            }
            return a;
        }

        Action Action()
        {
            switch (Keyword())
            {
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
                    switch (Keyword())
                    {
                        case "null":
                            Lex();
                            return AnySqlParser.Action.SetNull;
                        case "default":
                            Lex();
                            return AnySqlParser.Action.SetDefault;
                    }
                    throw Err(Echo() + ": expected replacement value");
            }
            throw Err(Echo() + ": expected action");
        }

        Check Check(string constraintName)
        {
            var location = new Location(file, line);
            var a = new Check(location, constraintName);
            if (Eat("not"))
            {
                Expect("for");
                Expect("replication");
                a.ForReplication = false;
            }
            a.Expression = Expression();
            return a;
        }

        //etc
        ColumnOrder ColumnOrder()
        {
            var location = new Location(file, line);
            var a = new ColumnOrder(location, Name());
            a.Desc = Desc();
            return a;
        }

        bool Desc()
        {
            switch (Keyword())
            {
                case "desc":
                    Lex();
                    return true;
                case "asc":
                    Lex();
                    return false;
            }
            return false;
        }

        //expressions
        Expression Expression()
        {
            return And();
        }

        Expression And()
        {
            var a = Not();
            var location = new Location(file, line);
            if (Eat("and"))
                return new BinaryExpression(location, BinaryOp.And, a, Not());
            return a;
        }

        Expression Not()
        {
            var location = new Location(file, line);
            if (Eat("not"))
                return new UnaryExpression(location, UnaryOp.Not, Not());
            return Comparison();
        }

        Expression Comparison()
        {
            var a = Addition();
            BinaryOp op;
            switch (token)
            {
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
            var location = new Location(file, line);
            Lex();
            return new BinaryExpression(location, op, a, Addition());
        }

        Expression Addition()
        {
            var a = Multiplication();
            for (; ; )
            {
                BinaryOp op;
                switch (token)
                {
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

        Expression Multiplication()
        {
            var a = Prefix();
            for (; ; )
            {
                BinaryOp op;
                switch (token)
                {
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

        Expression Prefix()
        {
            var location = new Location(file, line);
            switch (token)
            {
                case kWord:
                    switch (tokenString.ToLowerInvariant())
                    {
                        case "exists":
                            {
                                Lex();
                                Expect('(');
                                var a = new Exists(location, Select());
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

        Expression Postfix()
        {
            var a = Primary();
            var location = new Location(file, line);
            if (Eat('('))
            {
                if (a is QualifiedName a1)
                {
                    var call = new Call(location, a1);
                    if (token != ')')
                        do
                            call.Arguments.Add(Expression());
                        while (Eat(','));
                    Expect(')');
                    return call;
                }
                throw Err("call of non-function", location.Line);
            }
            return a;
        }

        Expression Primary()
        {
            var location = new Location(file, line);
            switch (token)
            {
                case kStringLiteral:
                    {
                        var a = new StringLiteral(location, tokenString);
                        Lex();
                        return a;
                    }
                case kNumber:
                    {
                        var a = new Number(location, tokenString);
                        Lex();
                        return a;
                    }
                case kWord:
                    if (string.Equals(tokenString, "null", StringComparison.OrdinalIgnoreCase))
                    {
                        Lex();
                        return new Null(location);
                    }
                    return QualifiedName();
                case kQuotedName:
                    return QualifiedName();
                case '(':
                    {
                        Lex();
                        var a = Expression();
                        Expect(')');
                        return a;
                    }
            }
            throw Err(Echo() + ": expected expression");
        }

        QualifiedName QualifiedName()
        {
            var location = new Location(file, line);
            var a = new QualifiedName(location);
            do
                a.Names.Add(Name());
            while (Eat('.'));
            return a;
        }

        //etc
        int Int()
        {
            if (token != kNumber)
                throw Err(Echo() + ": expected integer");
            var n = int.Parse(tokenString, System.Globalization.CultureInfo.InvariantCulture);
            Lex();
            return n;
        }

        string? Keyword()
        {
            if (token != kWord)
                return null;
            return tokenString.ToLowerInvariant();
        }

        string Name()
        {
            switch (token)
            {
                case kWord:
                case kQuotedName:
                    {
                        var s = tokenString;
                        Lex();
                        return s;
                    }
            }
            throw Err(Echo() + ": expected name");
        }

        void Expect(char k)
        {
            if (!Eat(k)) throw Err($"{Echo()}: expected '{k}'");
        }

        void Expect(string s)
        {
            if (!Eat(s)) throw Err($"{Echo()}: expected '{s}'");
        }

        bool Eat(int k)
        {
            if (token == k) { Lex(); return true; }
            return false;
        }

        bool Eat(string s)
        {
            if (token == kWord && string.Equals(tokenString, s, StringComparison.OrdinalIgnoreCase))
            {
                Lex();
                return true;
            }
            return false;
        }

        //tokenizer
        void Lex()
        {
            while (textIndex < text.Length)
            {
                var c = text[textIndex];
                switch (c)
                {
                    case '\'':
                        {
                            var line1 = line;
                            var sb = new StringBuilder();
                            for (var i = textIndex + 1; i < text.Length;)
                            {
                                switch (text[i])
                                {
                                    case '\n':
                                        line++;
                                        break;
                                    case '\\':
                                        switch (text[i + 1])
                                        {
                                            case '\'':
                                            case '\\':
                                                sb.Append(text[i + 1]);
                                                i += 2;
                                                continue;
                                        }
                                        break;
                                    case '\'':
                                        if (text[i + 1] == '\'')
                                        {
                                            i += 2;
                                            sb.Append('\'');
                                            continue;
                                        }
                                        textIndex = i + 1;
                                        token = kStringLiteral;
                                        tokenString = sb.ToString();
                                        return;
                                }
                                sb.Append(text[i++]);
                            }
                            throw Err("unclosed '", line1);
                        }
                    case '"':
                        {
                            var line1 = line;
                            var sb = new StringBuilder();
                            for (var i = textIndex + 1; i < text.Length;)
                            {
                                switch (text[i])
                                {
                                    case '\n':
                                        line++;
                                        break;
                                    case '\\':
                                        switch (text[i + 1])
                                        {
                                            case '"':
                                            case '\\':
                                                sb.Append(text[i + 1]);
                                                i += 2;
                                                continue;
                                        }
                                        break;
                                    case '"':
                                        if (text[i + 1] == '"')
                                        {
                                            i += 2;
                                            sb.Append('"');
                                            continue;
                                        }
                                        textIndex = i + 1;
                                        token = kQuotedName;
                                        tokenString = sb.ToString();
                                        return;
                                }
                                sb.Append(text[i++]);
                            }
                            throw Err("unclosed \"", line1);
                        }
                    case '`':
                        {
                            var line1 = line;
                            var sb = new StringBuilder();
                            for (var i = textIndex + 1; i < text.Length;)
                            {
                                switch (text[i])
                                {
                                    case '\n':
                                        line++;
                                        break;
                                    case '\\':
                                        switch (text[i + 1])
                                        {
                                            case '`':
                                            case '\\':
                                                sb.Append(text[i + 1]);
                                                i += 2;
                                                continue;
                                        }
                                        break;
                                    case '`':
                                        if (text[i + 1] == '`')
                                        {
                                            i += 2;
                                            sb.Append('`');
                                            continue;
                                        }
                                        textIndex = i + 1;
                                        token = kQuotedName;
                                        tokenString = sb.ToString();
                                        return;
                                }
                                sb.Append(text[i++]);
                            }
                            throw Err("unclosed `", line1);
                        }
                    case '[':
                        {
                            var line1 = line;
                            var sb = new StringBuilder();
                            for (var i = textIndex + 1; i < text.Length;)
                            {
                                switch (text[i])
                                {
                                    case '\n':
                                        line++;
                                        break;
                                    case ']':
                                        if (text[i + 1] == ']')
                                        {
                                            i += 2;
                                            sb.Append(']');
                                            continue;
                                        }
                                        textIndex = i + 1;
                                        token = kQuotedName;
                                        tokenString = sb.ToString();
                                        return;
                                }
                                sb.Append(text[i++]);
                            }
                            throw Err("unclosed [", line1);
                        }
                    case '!':
                        if (textIndex + 1 < text.Length)
                            switch (text[textIndex + 1])
                            {
                                case '=':
                                    textIndex += 2;
                                    token = kNotEqual;
                                    return;
                                case '<':
                                    //https://stackoverflow.com/questions/77475517/what-are-the-t-sql-and-operators-for
                                    textIndex += 2;
                                    token = kGreaterEqual;
                                    return;
                                case '>':
                                    textIndex += 2;
                                    token = kLessEqual;
                                    return;
                            }
                        break;
                    case '|':
                        if (textIndex + 1 < text.Length && text[textIndex + 1] == '|')
                        {
                            textIndex += 2;
                            token = kDoublePipe;
                            return;
                        }
                        break;
                    case '>':
                        if (textIndex + 1 < text.Length && text[textIndex + 1] == '=')
                        {
                            textIndex += 2;
                            token = kGreaterEqual;
                            return;
                        }
                        textIndex++;
                        token = c;
                        return;
                    case '<':
                        if (textIndex + 1 < text.Length)
                            switch (text[textIndex + 1])
                            {
                                case '=':
                                    textIndex += 2;
                                    token = kLessEqual;
                                    return;
                                case '>':
                                    textIndex += 2;
                                    token = kNotEqual;
                                    return;
                            }
                        textIndex++;
                        token = c;
                        return;
                    case '/':
                        if (textIndex + 1 < text.Length && text[textIndex + 1] == '*')
                        {
                            var line1 = line;
                            var i = textIndex + 2;
                            for (; ; )
                            {
                                if (text.Length <= i + 1) throw Err("unclosed /*", line1);
                                if (text[i] == '\n') line++;
                                else
                                    if (text[i] == '*' && text[i + 1] == '/') break;
                                i++;
                            }
                            textIndex = i + 2;
                            continue;
                        }
                        textIndex++;
                        token = c;
                        return;
                    case ',':
                    case '=':
                    case '&':
                    case ';':
                    case '.':
                    case '+':
                    case '%':
                    case '(':
                    case ')':
                    case '~':
                    case '*':
                        textIndex++;
                        token = c;
                        return;
                    case '-':
                        if (textIndex + 1 < text.Length && text[textIndex + 1] == '-')
                        {
                            textIndex = text.IndexOf('\n', textIndex + 2);
                            if (textIndex < 0) textIndex = text.Length;
                            continue;
                        }
                        textIndex++;
                        token = c;
                        return;
                    case '\n':
                        textIndex++;
                        line++;
                        continue;
                    case '\r':
                    case '\t':
                    case '\f':
                    case '\v':
                    case ' ':
                        textIndex++;
                        continue;
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
                        //Common letters are handled in the switch for speed
                        //but there are other letters in Unicode
                        if (char.IsLetter(c))
                        {
                            Word();
                            return;
                        }

                        //likewise digits
                        if (char.IsDigit(c))
                        {
                            Number();
                            return;
                        }

                        //and whitespace
                        if (char.IsWhiteSpace(c))
                        {
                            textIndex++;
                            continue;
                        }
                        break;
                }
                throw Err($"stray '{c}'");
            }
            token = -1;
        }

        void Word()
        {
            var i = textIndex;
            do
                i++;
            while (i < text.Length && IsWordPart(text[i]));
            token = kWord;
            tokenString = text[textIndex..i];
            textIndex = i;
        }

        void Number()
        {
            var i = textIndex;
            do
                i++;
            while (i < text.Length && IsWordPart(text[i]));
            token = kNumber;
            tokenString = text[textIndex..i];
            textIndex = i;
        }

        static bool IsWordPart(char c)
        {
            if (char.IsLetterOrDigit(c)) return true;
            return c == '_';
        }

        string Echo()
        {
            if (token >= 0)
                return char.ToString((char)token);
            switch (token)
            {
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

        //Error functions return exception objects instead of throwing immediately
        //so 'throw Err(...)' can mark the end of a case block
        Exception Err(string message)
        {
            return Err(message, line);
        }

        Exception Err(string message, int line)
        {
            return new FormatException($"{file}:{line}: {message}");
        }
    }
}