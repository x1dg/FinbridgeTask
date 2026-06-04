using System;

namespace Finbridge.Api.Features
{
    // Simple record for pattern matching demo
    public record Person(string Name, int Age, string City);

    public static class PatternMatchingExample
    {
        // Demonstrates pattern matching enhancements (C# 7.0 to C# 11)
        public static void ShowExamples()
        {
            object[] items = { 
                1, 
                "hello", 
                new DateTime(2023, 1, 1), 
                new Person("John", 30, "New York"),
                null
            };

            foreach (var item in items)
            {
                string message = item switch
                {
                    int i when i > 0 => $"Positive integer: {i}",
                    int i => $"Non-positive integer: {i}",
                    string s => $"String: {s}",
                    DateTime dt => $"Date: {dt:yyyy-MM-dd}",
                    Person { Name: var name, Age: var age } => $"Person: {name}, age {age}",
                    null => "Null object",
                    _ => "Unknown type"
                };
                Console.WriteLine(message);
            }

            // Property pattern with nested properties
            var person = new Person("Alice", 25, "New York");
            if (person is { City: "New York" })
            {
                Console.WriteLine("Person lives in New York");
            }

            // List pattern (C# 11)
            int[] numbers = { 1, 2, 3, 4, 5 };
            bool isConsecutive = numbers switch
            {
                [1, 2, 3, 4, 5] => true,
                [int first, int second, int third, int fourth, int fifth] when 
                    second == first + 1 && 
                    third == second + 1 && 
                    fourth == third + 1 && 
                    fifth == fourth + 1 => true,
                _ => false
            };
            Console.WriteLine($"Numbers are consecutive: {isConsecutive}");
        }
    }
}