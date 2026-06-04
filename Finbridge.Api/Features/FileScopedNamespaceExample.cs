// File-scoped namespace (C# 10) - cleaner namespace declaration
namespace Finbridge.Api.Features;

using System;

public static class FileScopedNamespaceExample
{
    public static void ShowExample()
    {
        Console.WriteLine("File-scoped namespace example - no braces needed!");
    }
}

// Global using directives (C# 10) - typically in GlobalUsings.cs or implicit in .NET 6+
// This file demonstrates the syntax, but global usings are usually in a separate file.

// Global using example (typically in GlobalUsings.cs):
// global using System;
// global using System.Collections.Generic;
// global using System.Linq;
// global using Microsoft.AspNetCore.Mvc;

// Target-typed new expressions (C# 9)
public class TargetTypedNewExample
{
    public static void ShowExample()
    {
        // Before C# 9:
        // List<string> list = new List<string>();
        
        // With target-typed new (C# 9):
        var list = new List<string> { "a", "b", "c" };
        var dict = new Dictionary<string, int> { ["key"] = 42 };
        
        Console.WriteLine($"List count: {list.Count}");
        Console.WriteLine($"Dict value: {dict["key"]}");
    }
}

// Covariant return types (C# 9)
public abstract class Animal
{
    public abstract Animal Clone();
}

public class Dog : Animal
{
    public override Dog Clone() => new Dog();
    public void Bark() => Console.WriteLine("Woof!");
}

public static class CovariantReturnExample
{
    public static void ShowExample()
    {
        Animal animal = new Dog();
        var cloned = animal.Clone(); // Returns Dog, not just Animal
        if (cloned is Dog dog)
        {
            dog.Bark();
        }
    }
}

// Lambda expression improvements (C# 10)
public static class LambdaImprovementsExample
{
    public static void ShowExample()
    {
        // Natural type for lambda (C# 10)
        var add = (int a, int b) => a + b; // Type inferred as Func<int, int, int>
        
        // Static lambdas (C# 9) - no capture
        Func<int, int, int> staticAdd = static (a, b) => a + b;
        
        Console.WriteLine($"Add: {add(2, 3)}");
        Console.WriteLine($"Static add: {staticAdd(5, 7)}");
    }
}