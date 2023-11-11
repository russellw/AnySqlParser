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
                }

                //Common whitespace characters are handled in the switch for speed
                //but there are other whitespace characters in Unicode
                if (char.IsWhiteSpace(c))
                {
                    textIndex++;
                    continue;
                }

                throw Err($"stray '{c}'");
            }
            token = Token.EOF;
        }

        Exception Err(string message)
        {
            var line=this.line;
            for (int i = 0; i < textIndex; i++)
                if (text[i] == '\n')
                    line++;
            return new FormatException($"{file}:{line}: {message}");
        }
    }
}