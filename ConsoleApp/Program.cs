using System;

namespace ConsoleApp
{
    class Program
    {
        static void Main(string[] args)
        {
            SomeValueObject t2 = new SomeValueObject{Property1 = 1.2m, Property3 = "viv"};
            SomeValueObject t3 = new SomeValueObject{Property1 = 1.2m, Property3 = "viv"};

            Console.WriteLine(t2 == t3);
        }
    }

    public class ValueObject
    {
        // Content will be generated at build time
    }

    public partial class SomeValueObject : ValueObject
    {
        public decimal Property1 { get; init; }
        public string Property2 { get; init; }
        public string Property3 { get; init; }
    }
    
    public partial class SomeOtherValueObject : ValueObject
    {
        public SomeValueObject Temp { get; init; }
        public string Name { get; init; }
    }
}
