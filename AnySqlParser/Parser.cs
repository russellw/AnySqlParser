namespace AnySqlParser
{
    public sealed class Parser
    {
        enum Token
        {
            EOF,
            Word,
            Minus,
            Quote,
            Plus,
            Star,
            Slash,
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
        readonly int line;
        int textIndex;
        Token token;
        string tokenString = "";

        public Parser(string text, string file = "SQL", int line = 1)
        {
            this.text = text;
            this.file = file;
            this.line = line;
            Lex();
        }

        void Lex()
        {
            while (textIndex < text.Length)
            {
                var c = text[textIndex];
                switch (c)
                {
                    case '!':
                        if (text[textIndex + 1] == '=')
                        {
                            textIndex += 2;
                            token = Token.NotEqual;
                            return;
                        }
                        break;
                    case '>':
                        if (text[textIndex + 1] == '=')
                        {
                            textIndex += 2;
                            token = Token.GreaterEqual;
                            return;
                        }
                        textIndex++;
                        token = Token.Greater;
                        return;
                    case '<':
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
                    case '+':
                        textIndex++;
                        token = Token.Plus;
                        return;
                    case '/':
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
                        if (text[textIndex + 1] == '-')
                        {
                            textIndex = text.IndexOf('\n', textIndex + 2);
                            if (textIndex < 0) textIndex = text.Length;
                            continue;
                        }
                        textIndex++;
                        token = Token.Minus;
                        return;
                    case '\n':
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
                        {
                            var i = textIndex;
                            do
                                i++;
                            while (IsIdPart(text[textIndex]));
                            token = Token.Word;
                            tokenString = text[textIndex..i];
                            textIndex = i;
                            return;
                        }
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
                            var i = textIndex;
                            do
                                i++;
                            while (IsIdPart(text[textIndex]));
                            token = Token.Word;
                            tokenString = text[textIndex..i];
                            textIndex = i;
                            return;
                        }
                        break;
                }
                throw Err($"stray '{c}'");
            }
            token = Token.EOF;
        }

        static bool IsIdPart(char c)
        {
            if (char.IsLetterOrDigit(c)) return true;
            return c == '_';
        }

        Exception Err(string message)
        {
            var line = this.line;
            for (int i = 0; i < textIndex; i++)
                if (text[i] == '\n')
                    line++;
            return new FormatException($"{file}:{line}: {message}");
        }
    }
}