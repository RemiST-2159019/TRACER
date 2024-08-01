using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace tracer
{
    /// <summary>
    /// An IBudgeter manages IBudgetables based on some budget.
    /// </summary>
    public interface IBudgeter<T> where T : IBudgetable
    {
        /// <summary>The total budget this IBudgeter can spend on choosing IBudgetables at the same time.</summary>
        public long Budget { get; set; }
        /// <summary>Chooses and returns IBudgetables based on their cost and priority until the total budget has been reached 
        /// or until all IBudgetables have been chosen without reaching the budget limit.
        /// If the budget is not enough to choose at least one IBudgetable, the highest priority IBudgetable is chosen anyway.</summary>
        public List<T> AssignBudget();
    }
}
