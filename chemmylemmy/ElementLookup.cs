using System;
using System.Collections.Generic;
using System.Linq;

namespace chemmylemmy
{
    public static class ElementLookup
    {
        public static ChemicalElement FindBySymbol(string symbol)
        {
            if (symbol == null) return null;
            ChemicalElementData.Elements.TryGetValue(symbol, out var element);
            if (element != null) return element;
            // Try case-insensitive
            return ChemicalElementData.Elements.Values.FirstOrDefault(e => e.Symbol.Equals(symbol, StringComparison.OrdinalIgnoreCase));
        }

        public static ChemicalElement FindByName(string name)
        {
            if (name == null) return null;
            return ChemicalElementData.Elements.Values.FirstOrDefault(e => e.Name.Equals(name, StringComparison.OrdinalIgnoreCase));
        }

        public static List<ChemicalElement> SearchByName(string partialName)
        {
            if (string.IsNullOrEmpty(partialName)) return new List<ChemicalElement>();
            return ChemicalElementData.Elements.Values.Where(e => e.Name.IndexOf(partialName, StringComparison.OrdinalIgnoreCase) >= 0).ToList();
        }
    }
} 