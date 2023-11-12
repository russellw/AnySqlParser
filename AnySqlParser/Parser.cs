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

        enum Token
        {
            EOF,
            Semicolon,
            DoublePipe,
            Word,
            Minus,
            Plus,
            Star,
            Slash,
            Dot,
            StringLiteral,
            QuotedName,
            Comma,
            LParen,
            RParen,
            Percent,
            Equal,
            NotEqual,
            Less,
            LessEqual,
            Greater,
            GreaterEqual,
        }

        readonly string text;
        readonly string file;
        int line;
        int textIndex;
        Token token;
        string tokenString = "";
        readonly List<Statement> statements = new();

        Parser(string text, string file, int line)
        {
            this.text = text;
            this.file = file;
            this.line = line;
            Lex();
            while (token != Token.EOF)
            {
                var location = new Location(file, line);
                Statement statement;
                switch (Keyword())
                {
                    case "create":
                        Lex();
                        switch (Keyword())
                        {
                            case "table":
                                {
                                    Lex();
                                    var a = new CreateTable(location, Name());
                                    if (Eat(Token.Dot))
                                    {
                                        a.schemaName = a.tableName;
                                        a.tableName = Name();
                                        if (Eat(Token.Dot))
                                        {
                                            a.databaseName = a.schemaName;
                                            a.schemaName = a.tableName;
                                            a.tableName = Name();
                                        }
                                    }
                                    Expect(Token.LParen, '(');
                                    do
                                        a.columnDefinitions.Add(ColumnDefinition());
                                    while (Eat(Token.Comma));
                                    Expect(Token.RParen, ')');
                                    statement = a;
                                    break;
                                }
                            default:
                                throw Err("unknown noun");
                        }
                        break;
                    default:
                        throw Err("expected statement");
                }
                statements.Add(statement);
                Eat(Token.Semicolon);
                Eat("go");
            }
        }

        ColumnDefinition ColumnDefinition()
        {
            var location = new Location(file, line);
            var a = new ColumnDefinition(location, Name());

            //data type
            var k = token;
            var s = Name();
            if (Eat(Token.Dot))
            {
                a.typeSchemaName = s;
                a.typeName = Keyword();
            }
            else
            {
                if (k == Token.Word) s = s.ToLowerInvariant();
                a.typeName = s;
            }

            while (token == Token.Word)
                switch (Keyword())
                {
                    case "filestream":
                        Lex();
                        a.filestream = true;
                        break;
                    case "sparse":
                        Lex();
                        a.sparse = true;
                        break;
                    case "rowguidcol":
                        Lex();
                        a.rowguidcol = true;
                        break;
                    case "not":
                        Lex();
                        Expect("for");
                        Expect("replication");
                        a.notForReplication = true;
                        break;
                    default:
                        throw Err(tokenString + ": unknown keyword");
                }
            return a;
        }

        string Keyword()
        {
            if (token == Token.Word)
                return tokenString.ToLowerInvariant();
            throw Err("expected keyword");
        }

        string Name()
        {
            switch (token)
            {
                case Token.Word:
                case Token.QuotedName: return tokenString;
            }
            throw Err("expected name");
        }

        void Expect(Token k, char c)
        {
            if (!Eat(k)) throw Err($"expected '{c}'");
        }

        void Expect(string s)
        {
            if (!Eat(s)) throw Err($"expected '{s}'");
        }

        bool Eat(Token k)
        {
            if (token == k) { Lex(); return true; }
            return false;
        }

        bool Eat(string s)
        {
            if (token == Token.Word && string.Equals(tokenString, s, StringComparison.OrdinalIgnoreCase))
            {
                Lex();
                return true;
            }
            return false;
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
                                        token = Token.StringLiteral;
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
                                        token = Token.QuotedName;
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
                                        token = Token.QuotedName;
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
                                        token = Token.QuotedName;
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
                            token = Token.NotEqual;
                            return;
                        }
                        break;
                    case '|':
                        if (textIndex + 1 < text.Length && text[textIndex + 1] == '|')
                        {
                            textIndex += 2;
                            token = Token.DoublePipe;
                            return;
                        }
                        break;
                    case '>':
                        if (textIndex + 1 < text.Length && text[textIndex + 1] == '=')
                        {
                            textIndex += 2;
                            token = Token.GreaterEqual;
                            return;
                        }
                        textIndex++;
                        token = Token.Greater;
                        return;
                    case '<':
                        if (textIndex + 1 < text.Length)
                            switch (text[textIndex + 1])
                            {
                                case '=':
                                    textIndex += 2;
                                    token = Token.LessEqual;
                                    return;
                                case '>':
                                    textIndex += 2;
                                    token = Token.NotEqual;
                                    return;
                            }
                        textIndex++;
                        token = Token.Less;
                        return;
                    case ',':
                        textIndex++;
                        token = Token.Plus;
                        return;
                    case ';':
                        textIndex++;
                        token = Token.Comma;
                        return;
                    case '.':
                        textIndex++;
                        token = Token.Dot;
                        return;
                    case '+':
                        textIndex++;
                        token = Token.Plus;
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
                        token = Token.Slash;
                        return;
                    case '%':
                        textIndex++;
                        token = Token.Percent;
                        return;
                    case '*':
                        textIndex++;
                        token = Token.Star;
                        return;
                    case '-':
                        if (textIndex + 1 < text.Length && text[textIndex + 1] == '-')
                        {
                            textIndex = text.IndexOf('\n', textIndex + 2);
                            if (textIndex < 0) textIndex = text.Length;
                            continue;
                        }
                        textIndex++;
                        token = Token.Minus;
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
                    default:
                        //Common whitespace characters are handled in the switch for speed
                        //but there are other whitespace characters in Unicode
                        if (char.IsWhiteSpace(c))
                        {
                            textIndex++;
                            continue;
                        }

                        //Common letters are handled in the switch for speed
                        //but there are other letters in Unicode
                        if (char.IsLetter(c))
                        {
                            Word();
                            return;
                        }
                        break;
                }
                throw Err($"stray '{c}'");
            }
            token = Token.EOF;
        }

        void Word()
        {
            var i = textIndex;
            do
                i++;
            while (i < text.Length && IsWordPart(text[textIndex]));
            token = Token.Word;
            tokenString = text[textIndex..i];
            textIndex = i;
        }

        static bool IsWordPart(char c)
        {
            if (char.IsLetterOrDigit(c)) return true;
            return c == '_';
        }

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