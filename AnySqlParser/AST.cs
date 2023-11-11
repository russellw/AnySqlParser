using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AnySqlParser
{
    public class AST
    {
        public readonly Location location;

        public AST(Location location)
        {
            this.location = location;
        }
    }

    public readonly record struct Location(string File, int Line);
}
