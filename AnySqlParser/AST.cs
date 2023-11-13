namespace AnySqlParser
{
    public readonly record struct Location(string File, int Line);

    public abstract class AST
    {
        public readonly Location Location;

        public AST(Location location)
        {
            Location = location;
        }
    }

    public sealed class Column : AST
    {
        public string Name = null!;

        //data type
        public string? TypeSchemaName;
        public string TypeName = null!;
        public int Size = -1;
        public int Scale = -1;

        //constraints
        public bool Nullable = true;
        public bool PrimaryKey;

        //etc
        public bool Filestream;
        public bool Sparse;
        public bool ForReplication = true;
        public bool Rowguidcol;

        public Column(Location location) : base(location)
        {
        }
    }

    public sealed class Select : AST
    {
        public List<AST> SelectList = new();

        public Select(Location location) : base(location)
        {
        }
    }

    public sealed class Insert : AST
    {
        public string TableName = null!;
        public List<string> Columns = new();
        public List<AST> Values = new();

        public Insert(Location location) : base(location)
        {
        }
    }

    public sealed class Start : AST
    {
        public Start(Location location) : base(location)
        {
        }
    }

    public sealed class Commit : AST
    {
        public Commit(Location location) : base(location)
        {
        }
    }

    public sealed class Rollback : AST
    {
        public Rollback(Location location) : base(location)
        {
        }
    }

    public sealed class Block : AST
    {
        public List<AST> Body = new();

        public Block(Location location) : base(location)
        {
        }
    }

    public sealed class SetParameter : AST
    {
        public string Name = null!;
        public string Value = null!;

        public SetParameter(Location location) : base(location)
        {
        }
    }

    public sealed class Table : AST
    {
        public string? DatabaseName;
        public string? SchemaName;
        public string TableName = null!;
        public List<Column> Columns = new();

        public Table(Location location) : base(location)
        {
        }
    }

    public sealed class StringLiteral : AST
    {
        public string Value;

        public StringLiteral(Location location, string value) : base(location)
        {
            Value = value;
        }
    }

    public sealed class Number : AST
    {
        public string Value;

        public Number(Location location, string value) : base(location)
        {
            Value = value;
        }
    }

    public sealed class Null : AST
    {
        public Null(Location location) : base(location)
        {
        }
    }
}
