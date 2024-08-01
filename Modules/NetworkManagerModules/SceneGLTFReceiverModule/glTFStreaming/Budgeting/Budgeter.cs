using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;

namespace tracer
{
    public class Budgeter<T> : IBudgeter<T> where T : IBudgetable
    {
        public HashSet<T> ManagedBudgetables { get; protected set; }

        public Budgeter() : this(new List<T>(), 1000000) { }

        public Budgeter(List<T> budgetables, long budget)
        {
            Budget = budget;
            ManagedBudgetables = budgetables.ToHashSet();
            _remainingBudget = Budget;
        }
        public long Budget { get; set; }
        private long _remainingBudget;
        public long RemainingBudget
        {
            get => _remainingBudget;
            set => _remainingBudget = value;
        }

        public void SetBudgetables(List<T> budgetables)
        {
            ManagedBudgetables = budgetables.ToHashSet();
        }

      
        public List<T> AssignBudget()
        {
            List<T> chosenBudgetables = new List<T>();

            var budgetables = ManagedBudgetables.OrderByDescending(n => n.Priority).ToList();

            foreach (var budgetable in budgetables)
            {
                if (budgetable.Cost <= RemainingBudget)
                {
                    chosenBudgetables.Add(budgetable);
                    RemainingBudget -= budgetable.Cost;
                    ManagedBudgetables.Remove(budgetable);
                }
            }

            // Always choose at least one node
            if (chosenBudgetables.Count == 0 && budgetables.Count != 0)
            {
                var firstNode = budgetables.First();
                chosenBudgetables.Add(firstNode);
            }
            return chosenBudgetables;
        }
    }
}
