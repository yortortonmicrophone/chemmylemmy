using System.Collections.Generic;

namespace chemmylemmy
{
    public static class FormulaValidator
    {
        public static bool Validate(List<FormulaTokenizer.Token> tokens, out string error)
        {
            error = null;
            int parenDepth = 0;
            foreach (var token in tokens)
            {
                if (token.Type == FormulaTokenizer.TokenType.OpenParen)
                {
                    parenDepth++;
                }
                else if (token.Type == FormulaTokenizer.TokenType.CloseParen)
                {
                    parenDepth--;
                    if (parenDepth < 0)
                    {
                        error = "Unmatched closing parenthesis.";
                        return false;
                    }
                }
                else if (token.Type == FormulaTokenizer.TokenType.Element)
                {
                    if (!ChemicalElementData.Elements.ContainsKey(token.Value))
                    {
                        error = $"Unknown element symbol: {token.Value}";
                        return false;
                    }
                }
            }
            if (parenDepth != 0)
            {
                error = "Unmatched opening parenthesis.";
                return false;
            }
            return true;
        }
    }
} 