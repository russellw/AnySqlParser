using AnySqlParser;

class Program
{
    static void Main(string[] args)
    {
        foreach (var file in args)
        {
            Console.WriteLine(file);
            foreach(var a in Parser.Parse(file))
            {
                Console.WriteLine(a);
            }
        }
        }
    }
