using System;
using System.Collections.Generic;
using System.Linq;

namespace chemmylemmy
{
    public static class SmartFormulaParser
    {
        public class ParseResult
        {
            public bool Success { get; set; }
            public string Error { get; set; }
            public double MolarMass { get; set; }
            public List<string> Breakdown { get; set; } = new List<string>();
            public string ParsedFormula { get; set; } // Shows how the formula was interpreted
        }

        // Main parsing method that implements section-based parsing
        public static ParseResult ParseFormula(string input)
        {
            var result = new ParseResult();
            
            // Step 1: Check for exact match (case-sensitive)
            if (TryExactMatch(input, out var exactTokens))
            {
                result.Success = true;
                result.ParsedFormula = "Exact match";
                return CalculateMolarMass(exactTokens, result);
            }

            // Step 2: Section-based parsing (handle each part independently)
            if (TrySectionBasedParsing(input, out var sectionTokens))
            {
                result.Success = true;
                result.ParsedFormula = "Section-based parsing";
                return CalculateMolarMass(sectionTokens, result);
            }

            // Step 3: Try all possible combinations (fallback)
            if (TryAllCombinations(input, out var combinationTokens))
            {
                result.Success = true;
                result.ParsedFormula = "Fallback combinations";
                return CalculateMolarMass(combinationTokens, result);
            }

            result.Success = false;
            result.Error = "Could not parse formula using any method";
            return result;
        }

        // Step 1: Exact match (case-sensitive)
        private static bool TryExactMatch(string input, out List<Token> tokens)
        {
            tokens = new List<Token>();
            int i = 0;
            
            while (i < input.Length)
            {
                char c = input[i];
                
                if (c == '(')
                {
                    tokens.Add(new Token(TokenType.OpenParen, "("));
                    i++;
                }
                else if (c == ')')
                {
                    tokens.Add(new Token(TokenType.CloseParen, ")"));
                    i++;
                }
                else if (char.IsDigit(c))
                {
                    string number = c.ToString();
                    i++;
                    while (i < input.Length && char.IsDigit(input[i]))
                    {
                        number += input[i];
                        i++;
                    }
                    tokens.Add(new Token(TokenType.Number, number));
                }
                else if (char.IsLetter(c))
                {
                    // Try two-letter element first (exact case only for exact match)
                    if (i + 1 < input.Length && char.IsLetter(input[i + 1]))
                    {
                        string twoLetter = input.Substring(i, 2);
                        if (ChemicalElementData.Elements.ContainsKey(twoLetter))
                        {
                            tokens.Add(new Token(TokenType.Element, twoLetter));
                            i += 2;
                            continue;
                        }
                    }
                    
                    // Try single letter (exact case only for exact match)
                    string singleLetter = c.ToString();
                    if (ChemicalElementData.Elements.ContainsKey(singleLetter))
                    {
                        tokens.Add(new Token(TokenType.Element, singleLetter));
                        i++;
                        continue;
                    }
                    
                    // Invalid character
                    return false;
                }
                else if (char.IsWhiteSpace(c))
                {
                    i++;
                }
                else
                {
                    return false;
                }
            }
            
            return ValidateTokens(tokens);
        }

        // Step 2: Section-based parsing (handle each part independently)
        private static bool TrySectionBasedParsing(string input, out List<Token> tokens)
        {
            tokens = new List<Token>();
            int i = 0;
            
            while (i < input.Length)
            {
                char c = input[i];
                
                if (c == '(')
                {
                    tokens.Add(new Token(TokenType.OpenParen, "("));
                    i++;
                }
                else if (c == ')')
                {
                    tokens.Add(new Token(TokenType.CloseParen, ")"));
                    i++;
                }
                else if (char.IsDigit(c))
                {
                    string number = c.ToString();
                    i++;
                    while (i < input.Length && char.IsDigit(input[i]))
                    {
                        number += input[i];
                        i++;
                    }
                    tokens.Add(new Token(TokenType.Number, number));
                }
                else if (char.IsLetter(c))
                {
                    // Parse this section using the best available strategy
                    var sectionTokens = ParseSection(input, ref i);
                    if (sectionTokens == null)
                        return false;
                    
                    tokens.AddRange(sectionTokens);
                }
                else if (char.IsWhiteSpace(c))
                {
                    i++;
                }
                else
                {
                    return false;
                }
            }
            
            return ValidateTokens(tokens);
        }
        
        // Parse a section of the formula using the best available strategy
        private static List<Token> ParseSection(string input, ref int i)
        {
            // Find the end of this section (next number, parenthesis, or end)
            int start = i;
            int end = i;
            
            while (end < input.Length && char.IsLetter(input[end]))
            {
                end++;
            }
            
            string section = input.Substring(start, end - start);
            
            // Try different parsing strategies for this section
            var strategies = new List<List<Token>>();
            
            // Strategy 1: Exact match
            if (TryParseSectionExact(section, out var exactTokens))
                strategies.Add(exactTokens);
            
            // Strategy 2: Two-letter first
            if (TryParseSectionTwoLetterFirst(section, out var twoLetterTokens))
                strategies.Add(twoLetterTokens);
            
            // Strategy 3: Single letter first
            if (TryParseSectionSingleLetterFirst(section, out var singleLetterTokens))
                strategies.Add(singleLetterTokens);
            
            if (strategies.Count == 0)
                return null;
            
            // Choose the best strategy (prefer exact match, then single letter first for most cases)
            var bestStrategy = strategies[0];
            if (strategies.Count > 1)
            {
                // Prefer exact match over others
                if (strategies.Any(s => s.Count == 1 && s[0].Value == section))
                {
                    bestStrategy = strategies.First(s => s.Count == 1 && s[0].Value == section);
                }
                else
                {
                    // For mixed cases, prefer single letter first (like the original logic)
                    // Only use two-letter first if single letter approach fails
                    var singleLetterStrategy = strategies.FirstOrDefault(s => s.Count > 1 && s.Any(t => t.Value.Length == 1));
                    if (singleLetterStrategy != null)
                    {
                        bestStrategy = singleLetterStrategy;
                    }
                }
            }
            
            i = end;
            return bestStrategy;
        }
        
        // Try to parse a section using exact match
        private static bool TryParseSectionExact(string section, out List<Token> tokens)
        {
            tokens = new List<Token>();
            if (ChemicalElementData.Elements.ContainsKey(section))
            {
                tokens.Add(new Token(TokenType.Element, section));
                return true;
            }
            return false;
        }
        
        // Try to parse a section using two-letter first approach
        private static bool TryParseSectionTwoLetterFirst(string section, out List<Token> tokens)
        {
            tokens = new List<Token>();
            int i = 0;
            
            while (i < section.Length)
            {
                if (i + 1 < section.Length)
                {
                    string twoLetter = section.Substring(i, 2);
                    if (ChemicalElementData.Elements.ContainsKey(twoLetter))
                    {
                        tokens.Add(new Token(TokenType.Element, twoLetter));
                        i += 2;
                        continue;
                    }
                    else
                    {
                        string properCase = char.ToUpper(twoLetter[0]).ToString() + char.ToLower(twoLetter[1]).ToString();
                        if (ChemicalElementData.Elements.ContainsKey(properCase))
                        {
                            tokens.Add(new Token(TokenType.Element, properCase));
                            i += 2;
                            continue;
                        }
                    }
                }
                
                string singleLetter = char.ToUpper(section[i]).ToString();
                if (ChemicalElementData.Elements.ContainsKey(singleLetter))
                {
                    tokens.Add(new Token(TokenType.Element, singleLetter));
                    i++;
                    continue;
                }
                
                return false;
            }
            
            return true;
        }
        
        // Try to parse a section using single letter first approach
        private static bool TryParseSectionSingleLetterFirst(string section, out List<Token> tokens)
        {
            tokens = new List<Token>();
            int i = 0;
            
            while (i < section.Length)
            {
                string singleLetter = char.ToUpper(section[i]).ToString();
                if (ChemicalElementData.Elements.ContainsKey(singleLetter))
                {
                    tokens.Add(new Token(TokenType.Element, singleLetter));
                    i++;
                    continue;
                }
                
                if (i + 1 < section.Length)
                {
                    string twoLetter = section.Substring(i, 2);
                    if (ChemicalElementData.Elements.ContainsKey(twoLetter))
                    {
                        tokens.Add(new Token(TokenType.Element, twoLetter));
                        i += 2;
                        continue;
                    }
                    else
                    {
                        string properCase = char.ToUpper(twoLetter[0]).ToString() + char.ToLower(twoLetter[1]).ToString();
                        if (ChemicalElementData.Elements.ContainsKey(properCase))
                        {
                            tokens.Add(new Token(TokenType.Element, properCase));
                            i += 2;
                            continue;
                        }
                    }
                }
                
                return false;
            }
            
            return true;
        }



        // Step 3: Try all possible combinations (fallback)
        private static bool TryAllCombinations(string input, out List<Token> tokens)
        {
            tokens = new List<Token>();
            int i = 0;
            
            while (i < input.Length)
            {
                char c = input[i];
                
                if (c == '(')
                {
                    tokens.Add(new Token(TokenType.OpenParen, "("));
                    i++;
                }
                else if (c == ')')
                {
                    tokens.Add(new Token(TokenType.CloseParen, ")"));
                    i++;
                }
                else if (char.IsDigit(c))
                {
                    string number = c.ToString();
                    i++;
                    while (i < input.Length && char.IsDigit(input[i]))
                    {
                        number += input[i];
                        i++;
                    }
                    tokens.Add(new Token(TokenType.Number, number));
                }
                else if (char.IsLetter(c))
                {
                    // Try two-letter first (greedy approach)
                    if (i + 1 < input.Length && char.IsLetter(input[i + 1]))
                    {
                        string twoLetter = char.ToUpper(c).ToString() + char.ToLower(input[i + 1]).ToString();
                        if (ChemicalElementData.Elements.ContainsKey(twoLetter))
                        {
                            tokens.Add(new Token(TokenType.Element, twoLetter));
                            i += 2;
                            continue;
                        }
                    }
                    
                    // Try single letter
                    string singleLetter = char.ToUpper(c).ToString();
                    if (ChemicalElementData.Elements.ContainsKey(singleLetter))
                    {
                        tokens.Add(new Token(TokenType.Element, singleLetter));
                        i++;
                        continue;
                    }
                    
                    return false; // No valid element found
                }
                else if (char.IsWhiteSpace(c))
                {
                    i++;
                }
                else
                {
                    return false;
                }
            }
            
            return ValidateTokens(tokens);
        }

        // Helper method to validate tokens
        private static bool ValidateTokens(List<Token> tokens)
        {
            int parenDepth = 0;
            foreach (var token in tokens)
            {
                if (token.Type == TokenType.OpenParen)
                {
                    parenDepth++;
                }
                else if (token.Type == TokenType.CloseParen)
                {
                    parenDepth--;
                    if (parenDepth < 0) return false;
                }
                else if (token.Type == TokenType.Element)
                {
                    if (!ChemicalElementData.Elements.ContainsKey(token.Value))
                        return false;
                }
            }
            return parenDepth == 0;
        }

        // Calculate molar mass from tokens
        private static ParseResult CalculateMolarMass(List<Token> tokens, ParseResult result)
        {
            var elementCounts = new Dictionary<string, int>();
            var stack = new Stack<Dictionary<string, int>>();
            stack.Push(new Dictionary<string, int>());
            
            int i = 0;
            while (i < tokens.Count)
            {
                var token = tokens[i];
                
                if (token.Type == TokenType.Element)
                {
                    string symbol = token.Value;
                    int count = 1;
                    
                    if (i + 1 < tokens.Count && tokens[i + 1].Type == TokenType.Number)
                    {
                        count = int.Parse(tokens[i + 1].Value);
                        i++;
                    }
                    
                    if (!stack.Peek().ContainsKey(symbol))
                        stack.Peek()[symbol] = 0;
                    stack.Peek()[symbol] += count;
                }
                else if (token.Type == TokenType.OpenParen)
                {
                    stack.Push(new Dictionary<string, int>());
                }
                else if (token.Type == TokenType.CloseParen)
                {
                    var group = stack.Pop();
                    int multiplier = 1;
                    
                    if (i + 1 < tokens.Count && tokens[i + 1].Type == TokenType.Number)
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
                
                i++;
            }
            
            double totalMass = 0;
            foreach (var kvp in stack.Peek())
            {
                var element = ChemicalElementData.Elements[kvp.Key];
                double mass = element.AtomicMass * kvp.Value;
                totalMass += mass;
                result.Breakdown.Add($"{element.Symbol}: {element.AtomicMass} × {kvp.Value} = {mass}");
            }
            
            result.MolarMass = totalMass;
            return result;
        }

        // Token types for internal use
        private enum TokenType { Element, Number, OpenParen, CloseParen }
        private record Token(TokenType Type, string Value);
    }
} 