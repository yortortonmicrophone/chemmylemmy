using System;
using System.Collections.Generic;
using System.Text;

namespace chemmylemmy
{
    public static class FormulaParser
    {
        public class ParseResult
        {
            public bool Success { get; set; }
            public string Error { get; set; }
            public double MolarMass { get; set; }
            public List<string> Breakdown { get; set; } = new List<string>();
        }

        // Parses a formula and calculates molar mass (now with parentheses support)
        public static ParseResult ParseAndCalculateMolarMass(string formula)
        {
            var result = new ParseResult();
            List<FormulaTokenizer.Token> tokens;
            try
            {
                tokens = FormulaTokenizer.Tokenize(formula);
            }
            catch (Exception ex)
            {
                result.Success = false;
                result.Error = ex.Message;
                return result;
            }

            if (!FormulaValidator.Validate(tokens, out string validationError))
            {
                result.Success = false;
                result.Error = validationError;
                return result;
            }

            // Stack for nested groups
            var stack = new Stack<Dictionary<string, int>>();
            stack.Push(new Dictionary<string, int>());
            int i = 0;
            while (i < tokens.Count)
            {
                var token = tokens[i];
                if (token.Type == FormulaTokenizer.TokenType.Element)
                {
                    string symbol = token.Value;
                    int count = 1;
                    if (i + 1 < tokens.Count && tokens[i + 1].Type == FormulaTokenizer.TokenType.Number)
                    {
                        count = int.Parse(tokens[i + 1].Value);
                        i++;
                    }
                    if (!stack.Peek().ContainsKey(symbol))
                        stack.Peek()[symbol] = 0;
                    stack.Peek()[symbol] += count;
                }
                else if (token.Type == FormulaTokenizer.TokenType.OpenParen)
                {
                    stack.Push(new Dictionary<string, int>());
                }
                else if (token.Type == FormulaTokenizer.TokenType.CloseParen)
                {
                    var group = stack.Pop();
                    int multiplier = 1;
                    if (i + 1 < tokens.Count && tokens[i + 1].Type == FormulaTokenizer.TokenType.Number)
                    {
                        multiplier = int.Parse(tokens[i + 1].Value);
                        i++;
                    }
                    foreach (var kvp in group)
                    {
                        if (!stack.Peek().ContainsKey(kvp.Key))
                            stack.Peek()[kvp.Key] = 0;
                        stack.Peek()[kvp.Key] += kvp.Value * multiplier;
                    }
                }
                // Numbers are handled as part of element or group, so skip
                i++;
            }

            // Final element counts
            var elementCounts = stack.Pop();
            double totalMass = 0;
            foreach (var kvp in elementCounts)
            {
                var element = ElementLookup.FindBySymbol(kvp.Key);
                if (element == null)
                {
                    result.Success = false;
                    result.Error = $"Unknown element: {kvp.Key}";
                    return result;
                }
                double mass = element.AtomicMass * kvp.Value;
                totalMass += mass;
                result.Breakdown.Add($"{element.Symbol}: {element.AtomicMass} × {kvp.Value} = {mass}");
            }
            result.Success = true;
            result.MolarMass = totalMass;
            return result;
        }
    }
} 