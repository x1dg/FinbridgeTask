using System;

namespace Finbridge.Api.Features
{
    public static class RawStringLiteralsExample
    {
        // Raw string literals (C# 11) - available in .NET 7 with SDK that supports C# 11
        public static void ShowExample()
        {
            // Example 1: Simple raw string
            string simple = """Hello, World!""";
            Console.WriteLine(simple);

            // Example 2: Raw string with quotes and new lines
            string json = """
                {
                    "name": "John Doe",
                    "age": 30,
                    "isDeveloper": true
                }
                """;
            Console.WriteLine(json);

            // Example 3: Using $ for interpolated raw strings
            var name = "Alice";
            var age = 25;
            string interpolated = $"""
                Hello {name},
                You are {age} years old.
                """;
            Console.WriteLine(interpolated);
        }
    }
}