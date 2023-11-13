using System.Text;

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
        int prevLine;
        int textIndex;
        int token;
        string tokenString = null!;
        string prevTokenString = null!;
        readonly List<Statement> statements = new();

        Parser(string text, string file, int line)
        {
            this.text = text;
            this.file = file;
            this.line = line;
            Lex();
            while (token != -1)
            {
                var location = new Location(file, line);
                Statement statement;
                switch (Keyword())
                {
                    case "begin":
                    case "start":
                        {
                            switch (KeywordMaybe())
                            {
                                case "transaction":
                                case "tran":
                                    Lex();
                                    break;
                            }
                            statement = new StartTransaction(location);
                            break;
                        }
                    case "insert":
                        {
                            Eat("into");
                            var a = new Insert(location, Name());

                            //columns
                            if (Eat('('))
                            {
                                do
                                    a.columns.Add(Name());
                                while (Eat(','));
                                Expect(')');
                            }

                            //values
                            Expect("values");
                            Expect('(');
                            do
                                a.values.Add(Expression());
                            while (Eat(','));
                            Expect(')');

                            statement = a;
                            break;
                        }
                    case "create":
                        switch (Keyword())
                        {
                            case "table":
                                {
                                    var a = new CreateTable(location, Name());
                                    if (Eat('.'))
                                    {
                                        a.schemaName = a.tableName;
                                        a.tableName = Name();
                                        if (Eat('.'))
                                        {
                                            a.databaseName = a.schemaName;
                                            a.schemaName = a.tableName;
                                            a.tableName = Name();
                                        }
                                    }
                                    Expect('(');
                                    do
                                        a.columnDefinitions.Add(ColumnDefinition());
                                    while (Eat(','));
                                    Expect(')');
                                    statement = a;
                                    break;
                                }
                            default:
                                throw Err(prevTokenString + ": unknown noun", prevLine);
                        }
                        break;
                    default:
                        throw Err(prevTokenString + ": unknown statement", prevLine);
                }
                statements.Add(statement);
                Eat(';');
                Eat("go");
            }
        }

        //statements
        ColumnDefinition ColumnDefinition()
        {
            var location = new Location(file, line);
            var a = new ColumnDefinition(location, Name());

            //data type
            var k = token;
            var s = Name();
            if (Eat('.'))
            {
                a.typeSchemaName = s;
                a.typeName = Keyword();
            }
            else
            {
                if (k == kWord) s = s.ToLowerInvariant();
                a.typeName = s;
            }
            if (Eat('('))
            {
                a.size = Int();
                if (Eat(','))
                    a.scale = Int();
                Expect(')');
            }

            //constraints etc
            while (token == kWord)
                switch (Keyword())
                {
                    case "null":
                        break;
                    case "filestream":
                        a.filestream = true;
                        break;
                    case "sparse":
                        a.sparse = true;
                        break;
                    case "primary":
                        Expect("key");
                        a.primaryKey = true;
                        break;
                    case "rowguidcol":
                        a.rowguidcol = true;
                        break;
                    case "not":
                        switch (Keyword())
                        {
                            case "null":
                                a.nullable = false;
                                break;
                            case "for":
                                Expect("replication");
                                a.forReplication = false;
                                break;
                            default:
                                throw Err(prevTokenString + ": unknown keyword", prevLine);
                        }
                        break;
                    default:
                        throw Err(prevTokenString + ": unknown keyword", prevLine);
                }
            return a;
        }

        //expressions
        Expression Expression()
        {
            return Primary();
        }

        Expression Primary()
        {
            var prevToken = token;
            StashLex();
            var location = new Location(file, prevLine);
            switch (prevToken)
            {
                case kStringLiteral:
                    return new StringLiteral(location, tokenString);
                case kNumber:
                    return new Number(location, tokenString);
                case kWord:
                    if (string.Equals(prevTokenString, "null", StringComparison.OrdinalIgnoreCase))
                        return new Null(location);
                    throw Err("variables not yet implemented", prevLine);
                case '(':
                    {
                        var a = Expression();
                        Expect(')');
                        return a;
                    }
            }
            throw Err("expected expression", prevLine);
        }

        //etc
        int Int()
        {
            if (token != kNumber)
                throw Err("expected integer");
            var n = int.Parse(tokenString, System.Globalization.CultureInfo.InvariantCulture);
            Lex();
            return n;
        }

        string Keyword()
        {
            if (token != kWord)
                throw Err("expected keyword");
            StashLex();
            return prevTokenString.ToLowerInvariant();
        }

        string KeywordMaybe()
        {
            if (token != kWord)
                return "";
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
            throw Err("expected name");
        }

        void Expect(char k)
        {
            if (!Eat(k)) throw Err($"expected '{k}'");
        }

        void Expect(string s)
        {
            if (!Eat(s)) throw Err($"expected '{s}'");
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
        void StashLex()
        {
            prevLine = line;
            prevTokenString = tokenString;
            Lex();
        }

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
                        if (textIndex + 1 < text.Length && text[textIndex + 1] == '=')
                        {
                            textIndex += 2;
                            token = kNotEqual;
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
                    case ';':
                    case '.':
                    case '+':
                    case '%':
                    case '(':
                    case ')':
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

                        //Common digits are handled in the switch for speed
                        //but there are other digits in Unicode
                        if (char.IsDigit(c))
                        {
                            Number();
                            return;
                        }

                        //Common whitespace characters are handled in the switch for speed
                        //but there are other whitespace characters in Unicode
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

        static bool IsWordPart(char c)
        {
            if (char.IsLetterOrDigit(c)) return true;
            return c == '_';
        }

        void Number()
        {
            var i = textIndex;
            do
                i++;
            while (i < text.Length && char.IsDigit(text, i));
            token = kNumber;
            tokenString = text[textIndex..i];
            textIndex = i;
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