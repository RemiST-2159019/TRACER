using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace tracer
{
    /// <summary>
    /// An IBudgetable is any object that can be managed based on a budget.
    /// </summary>
    public interface IBudgetable
    {
        long Cost { get; set; }
        /// <summary>The priority of this IBudgetable. The priority determines
        /// which IBudgetable gets chosen first if the cost is within budget.</summary>
        float Priority { get; set; }
    }

}


