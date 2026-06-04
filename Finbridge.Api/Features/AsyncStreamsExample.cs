using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Finbridge.Api.Features
{
    public static class AsyncStreamsExample
    {
        // Async streams (C# 8) - IAsyncEnumerable for streaming data
        public static async IAsyncEnumerable<int> GenerateNumbersAsync(int count, int delayMs = 100)
        {
            for (int i = 0; i < count; i++)
            {
                await Task.Delay(delayMs);
                yield return i;
            }
        }

        public static async Task ShowExampleAsync()
        {
            Console.WriteLine("Generating numbers with async stream:");
            await foreach (var number in GenerateNumbersAsync(5, 50))
            {
                Console.WriteLine($"Generated: {number}");
            }
        }

        // Async LINQ (C# 8) - using async LINQ operators
        public static async Task ShowAsyncLinqAsync()
        {
            var numbers = await GenerateNumbersAsync(10, 20)
                .Where(n => n % 2 == 0)
                .Select(n => n * 2)
                .ToListAsync();

            Console.WriteLine("Even numbers doubled: " + string.Join(", ", numbers));
        }
    }
}