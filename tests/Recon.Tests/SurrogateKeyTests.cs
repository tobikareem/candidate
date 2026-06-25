using Recon.Domain;

namespace Recon.Tests;

public class SurrogateKeyTests
{
    [Fact]
    public void Same_natural_key_yields_same_id()
    {
        var first  = SurrogateKey.For("invoice", "MRD-204-017", "INV-001");
        var second = SurrogateKey.For("invoice", "MRD-204-017", "INV-001");

        Assert.Equal(first, second);
    }

    [Fact]
    public void Same_natural_key_ignores_case_and_surrounding_whitespace()
    {
        var clean = SurrogateKey.For("invoice", "MRD-204-017", "INV-001");
        var messy = SurrogateKey.For("invoice", " mrd-204-017 ", "  inv-001  ");

        Assert.Equal(clean, messy);
    }

    [Fact]
    public void Same_printed_number_in_different_studies_yields_different_ids()
    {
        var horizon   = SurrogateKey.For("invoice", "MRD-204-017", "INV-001");
        var northstar = SurrogateKey.For("invoice", "CLX-115-300", "INV-001");

        Assert.NotEqual(horizon, northstar);
    }

    [Fact]
    public void Different_entity_types_with_same_parts_yield_different_ids()
    {
        var asInvoice = SurrogateKey.For("invoice", "X-1");
        var asPayment = SurrogateKey.For("payment", "X-1");

        Assert.NotEqual(asInvoice, asPayment);
    }
}
