namespace AnySqlParser
{
    public readonly record struct Location(string File, int Line);

    public abstract class AST
    {
        public readonly Location location;

        public AST(Location location)
        {
            this.location = location;
        }
    }

    public sealed class Column : AST
    {
        public string name = null!;

        //data type
        public string? typeSchemaName;
        public string typeName = null!;
        public int size = -1;
        public int scale = -1;

        //constraints
        public bool nullable = true;
        public bool primaryKey;

        //etc
        public bool filestream;
        public bool sparse;
        public bool forReplication = true;
        public bool rowguidcol;

        public Column(Location location) : base(location)
        {
        }
    }

    public abstract class Statement : AST
    {
        public Statement(Location location) : base(location)
        {
        }
    }

    public sealed class Insert : Statement
    {
        public string tableName;
        public List<string> columns = new();
        public List<Expression> values = new();

        public Insert(Location location, string tableName) : base(location)
        {
            this.tableName = tableName;
        }
    }

    public sealed class Start : Statement
    {
        public Start(Location location) : base(location)
        {
        }
    }

    public sealed class Commit : Statement
    {
        public Commit(Location location) : base(location)
        {
        }
    }

    public sealed class Rollback : Statement
    {
        public Rollback(Location location) : base(location)
        {
        }
    }

    public sealed class Block : Statement
    {
        public List<Statement> statements = new();

        public Block(Location location) : base(location)
        {
        }
    }

    public sealed class SetParameter : Statement
    {
        public string name = null!;
        public string value = null!;

        public SetParameter(Location location) : base(location)
        {
        }
    }

    public sealed class Table : Statement
    {
        public string? databaseName;
        public string? schemaName;
        public string tableName = null!;
        public List<Column> columnDefinitions = new();

        public Table(Location location) : base(location)
        {
        }
    }

    public abstract class Expression : AST
    {
        public Expression(Location location) : base(location)
        {
        }
    }

    public sealed class StringLiteral : Expression
    {
        public string value;

        public StringLiteral(Location location, string value) : base(location)
        {
            this.value = value;
        }
    }

    public sealed class Number : Expression
    {
        public string value;

        public Number(Location location, string value) : base(location)
        {
            this.value = value;
        }
    }

    public sealed class Null : Expression
    {
        public Null(Location location) : base(location)
        {
        }
    }
}
