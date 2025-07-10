using System;

namespace chemmylemmy
{
    public static class FormulaParserTestHarness
    {
        public static void Run()
        {
            Console.WriteLine("Formula Parser Test Harness");
            while (true)
            {
                Console.Write("Enter formula (or 'exit'): ");
                string input = Console.ReadLine();
                if (input == null || input.Trim().ToLower() == "exit")
                    break;
                var result = FormulaParser.ParseAndCalculateMolarMass(input.Trim());
                if (result.Success)
                {
                    Console.WriteLine($"Molar Mass: {result.MolarMass} g/mol");
                    foreach (var line in result.Breakdown)
                        Console.WriteLine(line);
                }
                else
                {
                    Console.WriteLine($"Error: {result.Error}");
                }
                Console.WriteLine();
            }
        }
    }
} 