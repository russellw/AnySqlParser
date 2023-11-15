namespace AnySqlParser
{
    public readonly record struct Location(string File, int Line);

    public abstract class AST
    {
        public readonly Location Location;

        protected AST(Location location)
        {
            Location = location;
        }

        //The omission of an override for Equals is intentional
        //in most cases, syntax trees have reference semantics
        //equality comparison by value is useful only in unusual situations
        //and should not be the default
        public virtual bool Eq(AST b)
        {
            return this == b;
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

    public sealed class If : AST
    {
        public Expression condition = null!;
        public AST then = null!;
        public AST? @else;

        public If(Location location) : base(location)
        {
        }
    }

    public sealed class Select : AST
    {
        public List<Expression> SelectList = new();

        public Select(Location location) : base(location)
        {
        }

        public override bool Eq(AST b)
        {
            if (b is Select b1)
            {
                if (SelectList.Count != b1.SelectList.Count)
                    return false;
                for (int i = 0; i < SelectList.Count; i++)
                    if (!SelectList[i].Eq(b1.SelectList[i])) return false;
                return true;
            }
            return false;
        }
    }

    public sealed class Insert : AST
    {
        public string TableName = null!;
        public List<string> Columns = new();
        public List<Expression> Values = new();

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
}
