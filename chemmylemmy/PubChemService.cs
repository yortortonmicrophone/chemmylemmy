using System;
using System.Net.Http;
using System.Text.Json;
using System.Threading.Tasks;
using System.Collections.Generic;

namespace chemmylemmy
{
    public class PubChemCompound
    {
        public int CID { get; set; }
        public string? MolecularFormula { get; set; }
        public double MolecularWeight { get; set; }
        public string? IUPACName { get; set; }
        public string? CanonicalSMILES { get; set; }
        public string? InChI { get; set; }
        public string? InChIKey { get; set; }
        public string? Structure2DUrl { get; set; } // Added for 2D structure image
        public string? Synonym { get; set; } // Added for most common synonym
    }

    public class PubChemSearchResult
    {
        public bool Success { get; set; }
        public string Error { get; set; }
        public PubChemCompound Compound { get; set; }
        public string SearchTerm { get; set; }
    }

    public static class PubChemService
    {
        private static readonly HttpClient httpClient = new HttpClient();
        private const string BaseUrl = "https://pubchem.ncbi.nlm.nih.gov/rest/pug";
        public static Action<string> LogCallback { get; set; } = null;

        static PubChemService()
        {
            // Set a reasonable timeout
            httpClient.Timeout = TimeSpan.FromSeconds(15);
            // Add a user agent to avoid being blocked
            httpClient.DefaultRequestHeaders.Add("User-Agent", "ChemmyLemmy/1.0");
            
            // Add more detailed error handling
            httpClient.DefaultRequestHeaders.Add("Accept", "application/json");
        }

        private static void Log(string message)
        {
            LogCallback?.Invoke(message);
        }

        public static async Task<PubChemSearchResult> SearchCompoundAsync(string searchTerm)
        {
            var result = new PubChemSearchResult
            {
                SearchTerm = searchTerm,
                Success = false
            };

            try
            {
                // First, search for the compound to get its CID
                var cid = await GetCompoundCIDAsync(searchTerm);
                if (cid == null)
                {
                    result.Error = $"Compound '{searchTerm}' not found in PubChem";
                    return result;
                }

                // Then get detailed information
                var compound = await GetCompoundDetailsAsync(cid.Value);
                if (compound == null)
                {
                    result.Error = "Failed to retrieve compound details";
                    return result;
                }

                result.Success = true;
                result.Compound = compound;
                return result;
            }
            catch (HttpRequestException)
            {
                result.Error = "Network error occurred";
                return result;
            }
            catch (TaskCanceledException)
            {
                result.Error = "Request timed out";
                return result;
            }
            catch
            {
                result.Error = "Unexpected error occurred";
                return result;
            }
        }

        public static async Task<bool> TestSimpleConnectivityAsync()
        {
            try
            {
                using var client = new HttpClient();
                client.Timeout = TimeSpan.FromSeconds(5);
                
                var response = await client.GetStringAsync("https://httpbin.org/get");
                return true;
            }
            catch (HttpRequestException)
            {
                return false;
            }
            catch (TaskCanceledException)
            {
                return false;
            }
            catch
            {
                return false;
            }
        }

        public static async Task<bool> TestConnectivityAsync()
        {
            try
            {
                // Test with a simpler endpoint first
                var testUrl = $"{BaseUrl}/compound/cid/5793/property/MolecularWeight/JSON";
                
                var response = await httpClient.GetStringAsync(testUrl);
                return true;
            }
            catch (HttpRequestException)
            {
                return false;
            }
            catch (TaskCanceledException)
            {
                return false;
            }
            catch
            {
                return false;
            }
        }

        private static async Task<int?> GetCompoundCIDAsync(string searchTerm)
        {
            // Try different search methods
            var searchMethods = new[]
            {
                $"compound/name/{Uri.EscapeDataString(searchTerm)}/cids/JSON",
                $"compound/formula/{Uri.EscapeDataString(searchTerm)}/cids/JSON",
                $"compound/smiles/{Uri.EscapeDataString(searchTerm)}/cids/JSON"
            };

            foreach (var method in searchMethods)
            {
                try
                {
                    var url = $"{BaseUrl}/{method}";
                    
                    var response = await httpClient.GetStringAsync(url);
                    
                    var jsonDoc = JsonDocument.Parse(response);

                    // Handle IdentifierList (for name/formula/smiles search)
                    if (jsonDoc.RootElement.TryGetProperty("IdentifierList", out var idList) &&
                        idList.TryGetProperty("CID", out var cidArray) &&
                        cidArray.GetArrayLength() > 0)
                    {
                        var cid = cidArray[0].GetInt32();
                        return cid;
                    }

                    // Handle InformationList (for other endpoints)
                    if (jsonDoc.RootElement.TryGetProperty("InformationList", out var infoList) &&
                        infoList.TryGetProperty("Information", out var infoArray) &&
                        infoArray.ValueKind == JsonValueKind.Array &&
                        infoArray.GetArrayLength() > 0)
                    {
                        var info = infoArray[0];
                        if (info.TryGetProperty("CID", out var cid) && cid.ValueKind == JsonValueKind.Number)
                        {
                            var cidValue = cid.GetInt32();
                            return cidValue;
                        }
                    }
                }
                catch (HttpRequestException)
                {
                    continue;
                }
                catch
                {
                    continue;
                }
            }

            return null;
        }

        private static async Task<PubChemCompound> GetCompoundDetailsAsync(int cid)
        {
            try
            {
                // Try the simple molecular weight endpoint first (this was working)
                var url = $"{BaseUrl}/compound/cid/{cid}/property/MolecularWeight,MolecularFormula/JSON";
                
                var response = await httpClient.GetStringAsync(url);
                
                var jsonDoc = JsonDocument.Parse(response);

                if (jsonDoc.RootElement.TryGetProperty("PropertyTable", out var propTable))
                {
                    if (propTable.TryGetProperty("Properties", out var properties))
                    {
                        if (properties.GetArrayLength() > 0)
                        {
                            var prop = properties[0];
                            
                            var molecularWeight = GetPropertyValue(prop, "MolecularWeight", 0.0);
                            
                            var compound = new PubChemCompound
                            {
                                CID = cid,
                                MolecularFormula = GetPropertyValue(prop, "MolecularFormula"),
                                MolecularWeight = molecularWeight,
                                IUPACName = "", // We'll get this later if needed
                                CanonicalSMILES = "", // We'll get this later if needed
                                InChI = "", // We'll get this later if needed
                                InChIKey = "", // We'll get this later if needed
                                Structure2DUrl = Get2DStructureUrl(cid),
                                Synonym = "" // We'll get this in a separate call
                            };
                            
                            // Get the synonym in a separate call
                            compound.Synonym = await GetCompoundSynonymAsync(cid);
                            
                            return compound;
                        }
                    }
                }
            }
            catch (HttpRequestException)
            {
                // Handle HTTP errors silently
            }
            catch (JsonException)
            {
                // Handle JSON parsing errors silently
            }
            catch
            {
                // Handle other errors silently
            }

            return null;
        }

        private static string GetPropertyValue(JsonElement element, string propertyName, string defaultValue = "")
        {
            if (element.TryGetProperty(propertyName, out var prop))
            {
                return prop.ValueKind == JsonValueKind.Null ? defaultValue : prop.GetString() ?? defaultValue;
            }
            return defaultValue;
        }

        private static double GetPropertyValue(JsonElement element, string propertyName, double defaultValue)
        {
            if (element.TryGetProperty(propertyName, out var prop))
            {
                if (prop.ValueKind == JsonValueKind.Number)
                {
                    var value = prop.GetDouble();
                    return value;
                }
                else if (prop.ValueKind == JsonValueKind.String)
                {
                    var stringValue = prop.GetString();
                    if (double.TryParse(stringValue, out var parsedValue))
                    {
                        return parsedValue;
                    }
                }
            }
            return defaultValue;
        }

        private static async Task<string> GetCompoundSynonymAsync(int cid)
        {
            try
            {
                // Get the first synonym from the synonyms endpoint
                var url = $"{BaseUrl}/compound/cid/{cid}/synonyms/JSON";
                
                var response = await httpClient.GetStringAsync(url);
                var jsonDoc = JsonDocument.Parse(response);
                
                if (jsonDoc.RootElement.TryGetProperty("InformationList", out var infoList) &&
                    infoList.TryGetProperty("Information", out var infoArray) &&
                    infoArray.GetArrayLength() > 0)
                {
                    var info = infoArray[0];
                    if (info.TryGetProperty("Synonym", out var synonymArray) &&
                        synonymArray.GetArrayLength() > 0)
                    {
                        var firstSynonym = synonymArray[0].GetString();
                        return firstSynonym ?? "";
                    }
                }
            }
            catch
            {
                // Handle errors silently
            }
            
            return "";
        }

        public static string Get2DStructureUrl(int cid)
        {
            // PubChem provides 2D structure images at this URL pattern
            return $"https://pubchem.ncbi.nlm.nih.gov/rest/pug/compound/cid/{cid}/PNG";
        }
    }
}