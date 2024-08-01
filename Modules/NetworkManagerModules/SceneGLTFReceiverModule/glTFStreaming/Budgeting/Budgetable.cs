using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace tracer
{
    public class Budgetable<T> : IBudgetable
    {
        public T BudgetableObj { get; private set; }
        public Budgetable(T objToBudget) : this(objToBudget, 0, 0) { }
        public Budgetable(T objToBudget, long cost, float priority)
        {
            BudgetableObj = objToBudget;
            Cost = cost;
            Priority = priority;
        }

        public long Cost { get; set; }
        public float Priority { get; set; }
    }
}
