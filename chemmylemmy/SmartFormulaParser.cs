using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

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

        // Debug method to show parsing steps
        public static string DebugParseResult(string input)
        {
            var result = new StringBuilder();
            result.AppendLine($"Input: {input}");
            result.AppendLine($"Input length: {input.Length}");
            result.AppendLine($"Characters: {string.Join(", ", input.Select(c => $"'{c}'"))}");
            
            // Check if basic elements exist
            result.AppendLine($"\nElement checks:");
            result.AppendLine($"'O' exists: {ChemicalElementData.Elements.ContainsKey("O")}");
            result.AppendLine($"'H' exists: {ChemicalElementData.Elements.ContainsKey("H")}");
            result.AppendLine($"'G' exists: {ChemicalElementData.Elements.ContainsKey("G")}");
            result.AppendLine($"'E' exists: {ChemicalElementData.Elements.ContainsKey("E")}");
            result.AppendLine($"'Ge' exists: {ChemicalElementData.Elements.ContainsKey("Ge")}");
            
            // Test each parsing method
            if (TryExactMatch(input, out var exactTokens))
            {
                result.AppendLine($"Exact match: {string.Join(", ", exactTokens.Select(t => t.Value))}");
            }
            else
            {
                result.AppendLine("Exact match: FAILED");
            }
            
            if (TrySectionBasedParsing(input, out var sectionTokens))
            {
                result.AppendLine($"Section parsing: {string.Join(", ", sectionTokens.Select(t => t.Value))}");
            }
            else
            {
                result.AppendLine("Section parsing: FAILED");
            }
            
            if (TryAllCombinations(input, out var combinationTokens))
            {
                result.AppendLine($"Combination parsing: {string.Join(", ", combinationTokens.Select(t => t.Value))}");
            }
            else
            {
                result.AppendLine("Combination parsing: FAILED");
            }
            
            // Test individual components
            result.AppendLine("\nTesting individual components:");
            
            // Test TryParseAllSingleLetters
            if (TryParseAllSingleLetters(input, out var singleLetterTokens))
            {
                result.AppendLine($"Single letter parsing: {string.Join(", ", singleLetterTokens.Select(t => t.Value))}");
            }
            else
            {
                result.AppendLine("Single letter parsing: FAILED");
            }
            
            // Test TryParseWithFallbackCapitalization
            if (TryParseWithFallbackCapitalization(input, out var fallbackTokens))
            {
                result.AppendLine($"Fallback parsing: {string.Join(", ", fallbackTokens.Select(t => t.Value))}");
            }
            else
            {
                result.AppendLine("Fallback parsing: FAILED");
            }
            
            // Test the actual ParseFormula method
            var parseResult = ParseFormula(input);
            result.AppendLine($"\nFinal result: Success={parseResult.Success}, Error={parseResult.Error}");
            if (parseResult.Success)
            {
                result.AppendLine($"Parsed formula: {parseResult.ParsedFormula}");
                result.AppendLine($"Molar mass: {parseResult.MolarMass}");
            }
            
            return result.ToString();
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
                // Failsafe check
                if (!LetterCountsMatch(input, exactTokens))
                {
                    result.Success = false;
                    result.Error = "Letter count mismatch between input and parsed result (failsafe).";
                    return result;
                }
                return CalculateMolarMass(exactTokens, result);
            }

            // Step 2: Section-based parsing (handle each part independently)
            if (TrySectionBasedParsing(input, out var sectionTokens))
            {
                result.Success = true;
                result.ParsedFormula = "Section-based parsing";
                // Failsafe check
                if (!LetterCountsMatch(input, sectionTokens))
                {
                    result.Success = false;
                    result.Error = "Letter count mismatch between input and parsed result (failsafe).";
                    return result;
                }
                return CalculateMolarMass(sectionTokens, result);
            }

            // Step 3: Try all possible combinations (fallback)
            if (TryAllCombinations(input, out var combinationTokens))
            {
                result.Success = true;
                result.ParsedFormula = "Fallback combinations";
                // Failsafe check
                if (!LetterCountsMatch(input, combinationTokens))
                {
                    result.Success = false;
                    result.Error = "Letter count mismatch between input and parsed result (failsafe).";
                    return result;
                }
                return CalculateMolarMass(combinationTokens, result);
            }

            result.Success = false;
            result.Error = "Could not parse formula using any method";
            return result;
        }

        // Failsafe: check if letter counts match between input and parsed tokens
        private static bool LetterCountsMatch(string input, List<Token> tokens)
        {
            var inputCounts = new Dictionary<char, int>();
            foreach (var c in input.ToLower())
            {
                if (char.IsLetter(c))
                {
                    if (!inputCounts.ContainsKey(c)) inputCounts[c] = 0;
                    inputCounts[c]++;
                }
            }

            var parsedCounts = new Dictionary<char, int>();
            foreach (var token in tokens)
            {
                if (token.Type == TokenType.Element)
                {
                    foreach (var c in token.Value.ToLower())
                    {
                        if (!parsedCounts.ContainsKey(c)) parsedCounts[c] = 0;
                        parsedCounts[c]++;
                    }
                }
            }

            // Compare counts
            if (inputCounts.Count != parsedCounts.Count) return false;
            foreach (var kvp in inputCounts)
            {
                if (!parsedCounts.ContainsKey(kvp.Key) || parsedCounts[kvp.Key] != kvp.Value)
                    return false;
            }
            return true;
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
            
            // Strategy 1: Always try single letter elements first
            if (TryParseAllSingleLetters(section, out var singleLetterTokens))
            {
                // Check if we can improve this by looking for capitalization patterns
                var improvedTokens = TryImproveWithCapitalization(section, singleLetterTokens);
                i = end;
                return improvedTokens;
            }
            
            // Strategy 1.5: If single letter parsing failed, try to fix it with capitalization patterns
            if (TryParseWithFallbackCapitalization(section, out var fallbackTokens))
            {
                i = end;
                return fallbackTokens;
            }
            
            // Strategy 2: Try exact match
            if (TryParseSectionExact(section, out var exactTokens))
            {
                i = end;
                return exactTokens;
            }
            
            // Strategy 3: Two-letter first
            if (TryParseSectionTwoLetterFirst(section, out var twoLetterTokens))
            {
                i = end;
                return twoLetterTokens;
            }
            
            // Strategy 4: Single letter first (original logic)
            if (TryParseSectionSingleLetterFirst(section, out var singleLetterFirstTokens))
            {
                i = end;
                return singleLetterFirstTokens;
            }
            
            return null;
        }
        
        // Try to parse using only single letter elements
        private static bool TryParseAllSingleLetters(string section, out List<Token> tokens)
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
                }
                else if (i + 1 < section.Length)
                {
                    // If single letter is invalid, check if the next two letters form a valid two-letter element
                    string twoLetter = char.ToUpper(section[i]).ToString() + char.ToLower(section[i + 1]).ToString();
                    if (ChemicalElementData.Elements.ContainsKey(twoLetter))
                    {
                        tokens.Add(new Token(TokenType.Element, twoLetter));
                        i += 2;
                        continue;
                    }
                    // If that didn't work, try fallback capitalization from this point
                    var fallbackTokens = new List<Token>(tokens);
                    if (TryParseWithFallbackCapitalization(section.Substring(i), out var remainingTokens))
                    {
                        fallbackTokens.AddRange(remainingTokens);
                        tokens = fallbackTokens;
                        return true;
                    }
                    return false;
                }
                else
                {
                    // If that didn't work, try fallback capitalization from this point
                    var fallbackTokens = new List<Token>(tokens);
                    if (TryParseWithFallbackCapitalization(section.Substring(i), out var remainingTokens))
                    {
                        fallbackTokens.AddRange(remainingTokens);
                        tokens = fallbackTokens;
                        return true;
                    }
                    return false;
                }
            }
            
            return true;
        }
        
        // Try to improve single letter parsing by looking for capitalization patterns
        private static List<Token> TryImproveWithCapitalization(string section, List<Token> originalTokens)
        {
            var improvedTokens = new List<Token>();
            int tokenIndex = 0;
            int charIndex = 0;
            
            while (tokenIndex < originalTokens.Count)
            {
                var currentToken = originalTokens[tokenIndex];
                
                // If this is a single letter and it's capitalized in the original input
                if (currentToken.Value.Length == 1 && charIndex < section.Length && char.IsUpper(section[charIndex]))
                {
                    // Check if the next character is lowercase and they form a valid two-letter element
                    if (charIndex + 1 < section.Length && char.IsLower(section[charIndex + 1]))
                    {
                        string twoLetter = char.ToUpper(section[charIndex]).ToString() + char.ToLower(section[charIndex + 1]).ToString();
                        if (ChemicalElementData.Elements.ContainsKey(twoLetter))
                        {
                            improvedTokens.Add(new Token(TokenType.Element, twoLetter));
                            charIndex += 2;
                            tokenIndex += 2; // Skip the next single letter token too
                            continue;
                        }
                    }
                }
                
                // Otherwise, keep the original token
                improvedTokens.Add(currentToken);
                charIndex += currentToken.Value.Length;
                tokenIndex++;
            }
            
            return improvedTokens;
        }
        
        // Try to parse with fallback capitalization when single letter parsing fails
        private static bool TryParseWithFallbackCapitalization(string section, out List<Token> tokens)
        {
            tokens = new List<Token>();
            int i = 0;
            
            while (i < section.Length)
            {
                // Try single letter first
                string singleLetter = char.ToUpper(section[i]).ToString();
                if (ChemicalElementData.Elements.ContainsKey(singleLetter))
                {
                    tokens.Add(new Token(TokenType.Element, singleLetter));
                    i++;
                    continue;
                }
                
                // If single letter failed, check if this character is capitalized
                // and the next character is lowercase, forming a valid two-letter element
                if (char.IsUpper(section[i]) && i + 1 < section.Length && char.IsLower(section[i + 1]))
                {
                    string twoLetter = char.ToUpper(section[i]).ToString() + char.ToLower(section[i + 1]).ToString();
                    if (ChemicalElementData.Elements.ContainsKey(twoLetter))
                    {
                        tokens.Add(new Token(TokenType.Element, twoLetter));
                        i += 2;
                        continue;
                    }
                }
                
                // If we get here, we can't parse this character
                return false;
            }
            
            return true;
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
                // Try single letter
                string singleLetter = char.ToUpper(section[i]).ToString();
                if (ChemicalElementData.Elements.ContainsKey(singleLetter))
                {
                    // Check if the next two letters form a valid element
                    if (i + 2 <= section.Length)
                    {
                        string twoLetter = char.ToUpper(section[i]).ToString() + char.ToLower(section[i + 1]).ToString();
                        if (ChemicalElementData.Elements.ContainsKey(twoLetter))
                        {
                            tokens.Add(new Token(TokenType.Element, twoLetter));
                            i += 2;
                            continue;
                        }
                    }
                    // Otherwise, just use the single letter
                    tokens.Add(new Token(TokenType.Element, singleLetter));
                    i++;
                    continue;
                }
                // If not a valid single letter, fail
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