namespace AnySqlParser
{
    public sealed class Column
    {
        public readonly Location Location;
        public string Name;

        //data type
        public QualifiedName TypeName = null!;
        public int Size = -1;
        public int Scale = -1;

        //etc
        public bool Filestream;
        public bool Sparse;

        public bool Identity;
        public int IdentitySeed = -1;
        public int IdentityIncrement = -1;

        public bool ForReplication = true;
        public bool Nullable = true;
        public bool Rowguidcol;

        //constraints
        public bool PrimaryKey;

        public Column(Location location, string name)
        {
            Location = location;
            Name = name;
        }
    }

    public sealed class KeyColumn
    {
        public readonly Location Location;

        public string Name;
        public bool Asc;
        public bool Desc;

        public KeyColumn(Location location, string name)
        {
            Location = location;
            Name = name;
        }
    }

    public sealed class Key
    {
        public readonly Location Location;

        public bool Primary;
        public bool? Clustered;

        public Key(Location location)
        {
            Location = location;
        }
    }

    public sealed class Table : AST
    {
        public QualifiedName Name;
        public List<Column> Columns = new();

        public Table(Location location, QualifiedName name) : base(location)
        {
            Name = name;
        }
    }
}
