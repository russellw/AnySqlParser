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
                        break;
                    case '\n':
                    case '\r':
                    case '\t':
                    case '\f':
                    case '\v':
                    case ' ':
                        textIndex++;
                        continue;
                    default:
                        //Common whitespace characters are handled in the switch for speed
                        //but there are other whitespace characters in Unicode
                        if (char.IsWhiteSpace(c))
                        {
                            textIndex++;
                            continue;
                        }
                        break;
                }

                //Punctuation
                textIndex++;
                token = c;
                return;
            }
            token = -1;
        }
    }
}