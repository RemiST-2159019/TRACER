using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace tracer
{
    public interface IBudgetableFactory<T, U> where T : IBudgetable
    {
        public List<T> CreateBudgetables(List<U> values);
        public List<T> CreateBudgetables();
    }
}
