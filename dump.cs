using System;
using SD.LLBLGen.Pro.ORMSupportClasses;

class Program {
    static void Main() {
        foreach(var n in Enum.GetNames(typeof(ComparisonOperator))) {
            Console.WriteLine(n);
        }
    }
}
