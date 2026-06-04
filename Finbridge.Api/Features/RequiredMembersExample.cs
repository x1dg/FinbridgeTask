using System;

namespace Finbridge.Api.Features
{
    // Required members (C# 11) - forces callers to initialize specific properties
    // Note: In C# 11, required members must be initialized in object initializer
    // This example shows the concept (compile-time error if uncommented)
    public class UserDto
    {
        public required string Username { get; init; }
        public required string Email { get; init; }
        public string? PhoneNumber { get; init; }
    }

    public static class RequiredMembersExample
    {
        public static void ShowExample()
        {
            // This works - required members initialized in object initializer
            var user = new UserDto
            {
                Username = "john_doe",
                Email = "john@example.com",
                PhoneNumber = "+1-555-0123"
            };

            // This would cause a compile error (uncomment to see):
            // var invalidUser = new UserDto(); // Error: Required members 'Username' and 'Email' must be set

            Console.WriteLine($"User: {user.Username}, Email: {user.Email}, Phone: {user.PhoneNumber}");
        }
    }

    // Inline arrays (C# 12) - fixed-size inline array for performance
    public struct InlineArrayBuffer
    {
        // Fixed-size inline array of 5 integers
        [System.Runtime.CompilerServices.InlineArray(5)]
        private struct Buffer
        {
            private int _element0;
        }

        private Buffer _buffer;

        public int this[int index]
        {
            get => _buffer[index];
            set => _buffer[index] = value;
        }
    }

    public static class InlineArrayExample
    {
        public static void ShowExample()
        {
            var array = new InlineArrayBuffer();
            for (int i = 0; i < 5; i++)
            {
                array[i] = i * 10;
            }

            Console.WriteLine("Inline array values:");
            for (int i = 0; i < 5; i++)
            {
                Console.Write($"{array[i]} ");
            }
            Console.WriteLine();
        }
    }
}