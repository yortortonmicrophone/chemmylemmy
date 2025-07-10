using System;
using System.Collections.Generic;
using System.Text.RegularExpressions;

namespace chemmylemmy
{
    public static class FormulaTokenizer
    {
        public enum TokenType { Element, Number, OpenParen, CloseParen }
        public record Token(TokenType Type, string Value);

        // Tokenizes a formula string into element symbols, numbers, and parentheses
        public static List<Token> Tokenize(string formula)
        {
            var tokens = new List<Token>();
            int i = 0;
            // Check if the formula is all lowercase (ignoring digits and parentheses)
            bool allLower = true;
            foreach (char ch in formula)
            {
                if (char.IsLetter(ch) && !char.IsLower(ch))
                {
                    allLower = false;
                    break;
                }
            }
            while (i < formula.Length)
            {
                char c = formula[i];
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
                else if (char.IsLetter(c))
                {
                    if (allLower)
                    {
                        if (i + 1 < formula.Length && char.IsLetter(formula[i + 1]))
                        {
                            string first = char.ToUpper(c).ToString();
                            string second = char.ToUpper(formula[i + 1]).ToString();
                            string twoLetter = first + formula[i + 1];
                            bool firstValid = ChemicalElementData.Elements.ContainsKey(first);
                            bool secondValid = ChemicalElementData.Elements.ContainsKey(second);
                            bool twoLetterValid = ChemicalElementData.Elements.ContainsKey(twoLetter);
                            if ((!firstValid || !secondValid) && twoLetterValid)
                            {
                                tokens.Add(new Token(TokenType.Element, twoLetter));
                                i += 2;
                                continue;
                            }
                        }
                        // Otherwise, treat as single-letter element
                        string symbol = char.ToUpper(c).ToString();
                        tokens.Add(new Token(TokenType.Element, symbol));
                        i++;
                    }
                    else
                    {
                        // Try to match a two-letter symbol if it exists
                        string symbol = char.ToUpper(c).ToString();
                        if (i + 1 < formula.Length && char.IsLetter(formula[i + 1]) && char.IsLower(formula[i + 1]))
                        {
                            string twoLetter = symbol + formula[i + 1];
                            if (ChemicalElementData.Elements.ContainsKey(twoLetter))
                            {
                                tokens.Add(new Token(TokenType.Element, twoLetter));
                                i += 2;
                                continue;
                            }
                        }
                        // Otherwise, treat as a one-letter symbol
                        tokens.Add(new Token(TokenType.Element, symbol));
                        i++;
                    }
                }
                else if (char.IsDigit(c))
                {
                    string number = c.ToString();
                    i++;
                    while (i < formula.Length && char.IsDigit(formula[i]))
                    {
                        number += formula[i];
                        i++;
                    }
                    tokens.Add(new Token(TokenType.Number, number));
                }
                else if (char.IsWhiteSpace(c))
                {
                    i++; // skip whitespace
                }
                else
                {
                    throw new Exception($"Invalid character '{c}' in formula.");
                }
            }
            return tokens;
        }
    }
} 