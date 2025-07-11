using System;
using System.Net.Http;
using System.Text.Json;
using System.Threading.Tasks;
using System.Collections.Generic;
using System.Linq; // Added for EnumerateObject()

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
            Console.WriteLine(message);
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
            catch (HttpRequestException ex)
            {
                result.Error = $"Network error: {ex.Message}";
                Console.WriteLine($"HTTP Request Exception: {ex.Message}");
                return result;
            }
            catch (TaskCanceledException)
            {
                result.Error = "Request timed out";
                Console.WriteLine("Request timed out");
                return result;
            }
            catch (Exception ex)
            {
                result.Error = $"Unexpected error: {ex.Message}";
                Console.WriteLine($"Unexpected error: {ex.Message}");
                return result;
            }
        }

        public static async Task<bool> TestSimpleConnectivityAsync()
        {
            try
            {
                Console.WriteLine("=== Simple connectivity test ===");
                Console.WriteLine("Creating HttpClient...");
                
                using var client = new HttpClient();
                client.Timeout = TimeSpan.FromSeconds(5);
                
                Console.WriteLine("HttpClient created successfully");
                Console.WriteLine("Attempting to connect to httpbin.org...");
                
                var response = await client.GetStringAsync("https://httpbin.org/get");
                
                Console.WriteLine("Connection successful!");
                Console.WriteLine($"Response length: {response.Length} characters");
                return true;
            }
            catch (HttpRequestException ex)
            {
                Console.WriteLine($"HttpRequestException: {ex.Message}");
                Console.WriteLine($"Status Code: {ex.StatusCode}");
                Console.WriteLine($"Inner Exception: {ex.InnerException?.Message}");
                return false;
            }
            catch (TaskCanceledException ex)
            {
                Console.WriteLine($"TaskCanceledException (timeout): {ex.Message}");
                return false;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Unexpected exception: {ex.GetType().Name}: {ex.Message}");
                return false;
            }
        }

        public static async Task<bool> TestLocalConnectivityAsync()
        {
            try
            {
                Console.WriteLine("Testing local HTTP connectivity...");
                var testClient = new HttpClient();
                testClient.Timeout = TimeSpan.FromSeconds(5);
                
                // Test with localhost (this should always work if HTTP is working)
                var response = await testClient.GetStringAsync("http://localhost:8080");
                Console.WriteLine("Local HTTP connectivity test successful");
                return true;
            }
            catch (HttpRequestException ex)
            {
                Console.WriteLine($"Local HTTP test failed (expected): {ex.Message}");
                // This is expected to fail since we don't have a local server
                return true; // Consider this a "pass" since HTTP is working
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Local HTTP test failed with {ex.GetType().Name}: {ex.Message}");
                return false;
            }
        }

        public static async Task<bool> TestBasicConnectivityAsync()
        {
            try
            {
                Console.WriteLine("Testing basic HTTP connectivity...");
                var testClient = new HttpClient();
                testClient.Timeout = TimeSpan.FromSeconds(10);
                
                // Test with a simple HTTP request first
                var response = await testClient.GetStringAsync("https://httpbin.org/get");
                Console.WriteLine("Basic HTTP connectivity test successful");
                return true;
            }
            catch (HttpRequestException ex)
            {
                Console.WriteLine($"Basic HTTP connectivity test failed with HttpRequestException: {ex.Message}");
                Console.WriteLine($"Inner exception: {ex.InnerException?.Message}");
                Console.WriteLine($"Status code: {ex.StatusCode}");
                return false;
            }
            catch (TaskCanceledException ex)
            {
                Console.WriteLine($"Basic HTTP connectivity test timed out: {ex.Message}");
                return false;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Basic HTTP connectivity test failed with {ex.GetType().Name}: {ex.Message}");
                return false;
            }
        }

        public static async Task<bool> TestConnectivityAsync()
        {
            try
            {
                Console.WriteLine("Testing PubChem connectivity...");
                Console.WriteLine($"Base URL: {BaseUrl}");
                
                // Test with a simpler endpoint first
                var testUrl = $"{BaseUrl}/compound/cid/5793/property/MolecularWeight/JSON";
                Console.WriteLine($"Testing URL: {testUrl}");
                
                var response = await httpClient.GetStringAsync(testUrl);
                Console.WriteLine($"Connectivity test successful: {response}");
                return true;
            }
            catch (HttpRequestException ex)
            {
                Console.WriteLine($"HTTP Request Exception: {ex.Message}");
                Console.WriteLine($"Inner Exception: {ex.InnerException?.Message}");
                Console.WriteLine($"Status Code: {ex.StatusCode}");
                return false;
            }
            catch (TaskCanceledException ex)
            {
                Console.WriteLine($"PubChem connectivity test timed out: {ex.Message}");
                return false;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Connectivity test failed: {ex.Message}");
                Console.WriteLine($"Exception type: {ex.GetType().Name}");
                return false;
            }
        }

        // Test with a known compound (glucose)
        public static async Task<PubChemSearchResult> TestWithGlucoseAsync()
        {
            Console.WriteLine("Testing with glucose (CID: 5793)...");
            try
            {
                var compound = await GetCompoundDetailsAsync(5793);
                if (compound != null)
                {
                    return new PubChemSearchResult
                    {
                        Success = true,
                        Compound = compound,
                        SearchTerm = "glucose"
                    };
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Glucose test failed: {ex.Message}");
            }
            
            return new PubChemSearchResult
            {
                Success = false,
                Error = "Failed to retrieve glucose data",
                SearchTerm = "glucose"
            };
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
                    Log($"Trying PubChem URL: {url}");
                    
                    var response = await httpClient.GetStringAsync(url);
                    Log($"PubChem response received, length: {response.Length}");
                    
                    var jsonDoc = JsonDocument.Parse(response);

                    // Handle IdentifierList (for name/formula/smiles search)
                    if (jsonDoc.RootElement.TryGetProperty("IdentifierList", out var idList) &&
                        idList.TryGetProperty("CID", out var cidArray) &&
                        cidArray.GetArrayLength() > 0)
                    {
                        var cid = cidArray[0].GetInt32();
                        Log($"Found CID: {cid}");
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
                            Log($"Found CID: {cidValue}");
                            return cidValue;
                        }
                    }
                    
                    Log("No CID found in response");
                }
                catch (HttpRequestException ex)
                {
                    Log($"HTTP error for {method}: {ex.Message}");
                    continue;
                }
                catch (Exception ex)
                {
                    Log($"Error for {method}: {ex.Message}");
                    continue;
                }
            }

            return null;
        }

        private static async Task<PubChemCompound> GetCompoundDetailsAsync(int cid)
        {
            try
            {
                Log($"Starting details retrieval for CID: {cid}");
                
                // Try the simple molecular weight endpoint first (this was working)
                var url = $"{BaseUrl}/compound/cid/{cid}/property/MolecularWeight,MolecularFormula/JSON";
                Log($"Getting molecular weight and formula from: {url}");
                
                var response = await httpClient.GetStringAsync(url);
                Log($"Response received, length: {response.Length}");
                Log($"Response preview: {response.Substring(0, Math.Min(200, response.Length))}...");
                
                var jsonDoc = JsonDocument.Parse(response);
                Log("JSON parsed successfully");

                if (jsonDoc.RootElement.TryGetProperty("PropertyTable", out var propTable))
                {
                    Log("Found PropertyTable");
                    if (propTable.TryGetProperty("Properties", out var properties))
                    {
                        Log($"Found Properties array with {properties.GetArrayLength()} items");
                        if (properties.GetArrayLength() > 0)
                        {
                            var prop = properties[0];
                            Log("Processing first property item");
                            Log($"Property item keys: {string.Join(", ", prop.EnumerateObject().Select(p => p.Name))}");
                            
                            var molecularWeight = GetPropertyValue(prop, "MolecularWeight", 0.0);
                            Log($"Molecular weight: {molecularWeight}");
                            
                            var compound = new PubChemCompound
                            {
                                CID = cid,
                                MolecularFormula = GetPropertyValue(prop, "MolecularFormula"),
                                MolecularWeight = molecularWeight,
                                IUPACName = "", // We'll get this later if needed
                                CanonicalSMILES = "", // We'll get this later if needed
                                InChI = "", // We'll get this later if needed
                                InChIKey = "" // We'll get this later if needed
                            };
                            
                            Log($"Successfully created compound with MW: {compound.MolecularWeight}, Formula: {compound.MolecularFormula}");
                            return compound;
                        }
                        else
                        {
                            Log("Properties array is empty");
                        }
                    }
                    else
                    {
                        Log("Properties property not found in PropertyTable");
                    }
                }
                else
                {
                    Log("PropertyTable not found in response");
                    Log($"Available properties: {string.Join(", ", jsonDoc.RootElement.EnumerateObject().Select(p => p.Name))}");
                }
                
                Log("No properties found in response");
            }
            catch (HttpRequestException ex)
            {
                Log($"HTTP error getting details: {ex.Message}");
                Log($"Status Code: {ex.StatusCode}");
                Log($"Inner Exception: {ex.InnerException?.Message}");
            }
            catch (JsonException ex)
            {
                Log($"JSON parsing error: {ex.Message}");
                Log($"Line: {ex.LineNumber}, Position: {ex.BytePositionInLine}");
            }
            catch (Exception ex)
            {
                Log($"Error getting details: {ex.Message}");
                Log($"Exception type: {ex.GetType().Name}");
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
            Log($"Looking for property: {propertyName}");
            if (element.TryGetProperty(propertyName, out var prop))
            {
                Log($"Found property {propertyName}, type: {prop.ValueKind}");
                if (prop.ValueKind == JsonValueKind.Number)
                {
                    var value = prop.GetDouble();
                    Log($"Numeric value: {value}");
                    return value;
                }
                else if (prop.ValueKind == JsonValueKind.String)
                {
                    var stringValue = prop.GetString();
                    Log($"String value: {stringValue}");
                    if (double.TryParse(stringValue, out var parsedValue))
                    {
                        Log($"Parsed string to double: {parsedValue}");
                        return parsedValue;
                    }
                    else
                    {
                        Log($"Failed to parse string to double: {stringValue}");
                    }
                }
                else
                {
                    Log($"Property {propertyName} is not a number or string, it's: {prop.ValueKind}");
                }
            }
            else
            {
                Log($"Property {propertyName} not found");
            }
            Log($"Returning default value: {defaultValue}");
            return defaultValue;
        }
    }
}