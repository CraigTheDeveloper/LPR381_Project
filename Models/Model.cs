using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Common
{
    public class Model
    {
        public ProblemType ProblemType { get; set; }
        public ObjectiveFunction ObjectiveFunction { get; set; } = new ObjectiveFunction();
        public List<Constraint> Constraints { get; set; } = new List<Constraint>();
        public List<SignRestriction> SignRestrictions { get; set; } = new List<SignRestriction>();
        public List<List<List<double>>> Result { get; set; } = new List<List<List<double>>>();
    }
}
