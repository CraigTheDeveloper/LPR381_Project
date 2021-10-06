using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Common
{
    public class Constraint
    {
        public List<DecisionVariable> DecisionVariables { get; set; } = new List<DecisionVariable>();
        public InequalitySign InequalitySign { get; set; }
        public double RightHandSide { get; set; }
    }
}
