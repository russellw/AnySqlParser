using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AnySqlParser
{
    public class Statement : AST
    {
        public Statement(Location location) : base(location)
        {
        }
    }
}
