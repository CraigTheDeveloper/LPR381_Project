using Common;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BusinessLogic.Algorithms
{
    public abstract class Algorithm
    {
        public abstract void PutModelInCanonicalForm(Model model);
        public abstract void Solve(Model model);
    }
}
