namespace ProxyForwarder.Core.Common;

/// <summary>
/// Classifies proxy type based on price (monthly) according to CloudMini pricing
/// </summary>
public static class ProxyTypeClassifier
{
    /// <summary>
    /// Classify proxy type based on price
    /// PrivateV4: 50,000 đ/tháng
    /// BudgetV4: 40,000 đ/tháng
    /// PrivateV6: 5,000 đ/tháng
    /// Residential VN: 120,000 đ/tháng
    /// ResidentialStatic: 120,000 đ/tháng
    /// BudgetResidentialStatic: 80,000 đ/tháng
    /// Other/Unknown: null
    /// </summary>
    public static string? ClassifyByPrice(int price)
    {
        return price switch
        {
            50000 => "PrivateV4",
            40000 => "BudgetV4",
            5000 => "PrivateV6",
            120000 => "ResidentialVN", // or ResidentialStatic, hard to distinguish by price alone
            80000 => "BudgetResidentialStatic",
            _ => null // Unknown price tier
        };
    }

    /// <summary>
    /// Get a friendly display name for the proxy type
    /// </summary>
    public static string GetDisplayName(string? type)
    {
        return type switch
        {
            "PrivateV4" => "Private IPv4",
            "BudgetV4" => "Budget IPv4",
            "PrivateV6" => "Private IPv6",
            "ResidentialVN" => "Residential VN",
            "ResidentialStatic" => "Residential Static",
            "BudgetResidentialStatic" => "Budget Residential",
            _ => "Unknown"
        };
    }
}
