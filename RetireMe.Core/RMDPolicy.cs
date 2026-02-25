using System.Collections.Generic;

namespace RetireMe.Core
{
    public class RmdPolicy
    {
        // e.g., 73 or 75
        public int RmdStartAge { get; set; }

        // Uniform lifetime table: Age -> Divisor
        public Dictionary<int, decimal> LifeExpectancyDivisors { get; set; } = new();
    }
}
