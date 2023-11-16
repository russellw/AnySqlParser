namespace AnySqlParser
{
    public readonly record struct Location(string File, int Line);

    public sealed class ColumnOrder
    {
        public readonly Location Location;

        public string Name;
        public bool Desc;

        public ColumnOrder(Location location, string name)
        {
            Location = location;
            Name = name;
        }
    }

    public abstract class AST
    {
        public readonly Location Location;

        protected AST(Location location)
        {
            Location = location;
        }
    }

    public sealed class DropProcedure : AST
    {
        public bool IfExists;
        public List<QualifiedName> Names = new();

        public DropProcedure(Location location) : base(location)
        {
        }
    }

    public sealed class DropView : AST
    {
        public bool IfExists;
        public List<QualifiedName> Names = new();

        public DropView(Location location) : base(location)
        {
        }
    }

    public sealed class DropTable : AST
    {
        public bool IfExists;
        public List<QualifiedName> Names = new();

        public DropTable(Location location) : base(location)
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
        public bool All;
        public bool Distinct;

        public Expression? Top;
        public bool Percent;
        public bool WithTies;

        public List<Expression> SelectList = new();

        public List<Expression> From = new();
        public Expression? Where;
        public Expression? GroupBy;
        public Expression? Having;
        public Expression? Window;

        public Expression? OrderBy;
        public bool Desc;

        public Select(Location location) : base(location)
        {
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
}
