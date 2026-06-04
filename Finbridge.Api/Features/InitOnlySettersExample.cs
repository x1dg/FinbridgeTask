using System;

namespace Finbridge.Api.Features
{
    // Init-only setters (C# 9) - properties can only be set during object initialization
    public class InitOnlySettersExample
    {
        public required string Name { get; init; }
        public required int Age { get; init; }
        public required string Email { get; init; }

        public override string ToString() => $"Name: {Name}, Age: {Age}, Email: {Email}";

        public static void ShowExample()
        {
            var person = new InitOnlySettersExample
            {
                Name = "Alice",
                Age = 30,
                Email = "alice@example.com"
            };

            // This would cause a compile error:
            // person.Name = "Bob"; // Error: Init-only property can only be assigned in object initializer

            Console.WriteLine(person);
        }
    }

    // Record with init-only properties (C# 9)
    public record PersonRecord(string Name, int Age, string Email)
    {
        // Records have init-only properties by default
        // You can use with-expressions to create modified copies
        public PersonRecord WithAge(int newAge) => this with { Age = newAge };
    }

    public static class RecordExample
    {
        public static void ShowExample()
        {
            var person = new PersonRecord("Bob", 25, "bob@example.com");
            var olderPerson = person.WithAge(26); // Create a new record with modified Age

            Console.WriteLine($"Original: {person}");
            Console.WriteLine($"Modified: {olderPerson}");
        }
    }
}