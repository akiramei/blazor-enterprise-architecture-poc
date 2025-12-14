using System.Reflection;

namespace Application.Routing;

public static class RouteAssemblyRegistry
{
    public static readonly Assembly[] FeatureUIAssemblies = new[]
        {
            // ProductCatalog BC
            "GetProducts.UI",
            "GetProductById.UI",
            "UpdateProduct.UI",
            "CreateProduct.UI",
            "DeleteProduct.UI",
            "SearchProducts.UI",
            "BulkDeleteProducts.UI",
            "BulkUpdateProductPrices.UI",
            "ExportProductsToCsv.UI",
            "ImportProductsFromCsv.UI",

            // PurchaseManagement BC
            "SubmitPurchaseRequest.UI",
            "GetPurchaseRequests.UI",
            "GetPurchaseRequestById.UI"
        }
        .Select(name =>
        {
            try { return Assembly.Load(name); }
            catch { return null; }
        })
        .Where(a => a != null)
        .ToArray()!;
}

