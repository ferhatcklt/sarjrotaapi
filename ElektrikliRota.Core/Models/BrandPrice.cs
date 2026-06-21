namespace ElektrikliRota.Core.Models;

public class BrandPrice
{
    public double AC { get; set; }
    public double DC { get; set; }
    public string? Note { get; set; }
    public string? DcNote { get; set; }
}

public static class PricingConstants
{
    public const double DefaultChargePrice = 14.00;

    public static readonly Dictionary<string, BrandPrice> Prices = new(StringComparer.OrdinalIgnoreCase)
    {
        { "ZES", new BrandPrice { AC = 9.99, DC = 16.49, DcNote = "DC-1: 12.99 ₺ / DC-2: 16.49 ₺" } },
        { "Eşarj", new BrandPrice { AC = 9.90, DC = 13.50 } },
        { "Trugo", new BrandPrice { AC = 11.49, DC = 14.98 } },
        { "Tesla", new BrandPrice { AC = 12.30, DC = 12.30, Note = "Tesla araçlarına 9.90 ₺" } },
        { "Voltrun", new BrandPrice { AC = 10.00, DC = 14.00 } },
        { "Sharz.net", new BrandPrice { AC = 9.49, DC = 10.99 } },
        { "Astor", new BrandPrice { AC = 10.00, DC = 14.00 } },
        { "Ovolt", new BrandPrice { AC = 9.99, DC = 13.99 } },
        { "Neva", new BrandPrice { AC = 9.90, DC = 13.90 } },
        { "Wat Mobilite", new BrandPrice { AC = 10.99, DC = 14.49 } },
        { "En Yakıt", new BrandPrice { AC = 14.90, DC = 14.90, Note = "Tamamı DC hızlı şarj ağı" } },
        { "Aksa Şarj", new BrandPrice { AC = 9.90, DC = 12.49, Note = "Tarife 2 için AC: 10.90, DC: 13.49" } },
        { "RST Chargepoint", new BrandPrice { AC = 5.90, DC = 13.99 } }
    };
}
