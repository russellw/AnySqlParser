namespace AnySqlParser
{
    public sealed class Parser
    {
        readonly string text;
        int textIndex;
        int token;

        public Parser(string text)
        {
            this.text = text;
            Lex();
        }

        void Lex()
        {
            while (textIndex < text.Length)
            {
                switch (text[textIndex])
                {
                    case '-':
                        if (text[textIndex + 1] == '-')
                        {
                            textIndex = text.IndexOf('\n', textIndex + 2);
                            if (textIndex < 0) textIndex = text.Length;
                            continue;
                        }
                        break;
                    case '\n':
                    case '\r':
                    case '\t':
                    case '\f':
                    case '\v':
                    case ' ':
                        textIndex++;
                        continue;
                }
                token = text[textIndex++];
                return;
            }
            token = -1;
        }
    }
}