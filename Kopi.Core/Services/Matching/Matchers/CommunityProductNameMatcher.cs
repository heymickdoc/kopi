using Kopi.Core.Models.SQLServer;
using Kopi.Core.Utilities;
using System.Linq;
using Kopi.Core.Models.Common;

namespace Kopi.Core.Services.Matching.Matchers;

/// <summary>
///  Matcher for Product Names.
///  Matches "ProductName", "ItemName", or generic "Name" in a Product context.
///  Strictly differentiates from Descriptions, Codes, or Categories.
/// </summary>
public class CommunityProductNameMatcher : IColumnMatcher
{
    public int Priority => 26; // High priority (runs before generic Name/Title matchers)
    public string GeneratorTypeKey => "product_name";

    // --- 1. Safe "Stop Words" ---
    private static readonly HashSet<string> InvalidSchemaNames = new()
    {
        "humanresources", "person", "employee", "user", // People contexts
        "log", "system", "error", "auth", "config" // System contexts
    };

    // --- 2. Strong Table Context ---
    private static readonly HashSet<string> ProductTableContexts = new()
    {
        "product", "item", "inventory", "catalog", "merchandise",
        "stock", "warehouse", "sku", "good", "material", "asset"
    };

    // --- 3. Strong Column Matches ---
    private static readonly HashSet<string> StrongColumnNames = new()
    {
        "productname",
        "itemname",
        "modelname",
        "goodsname",
        "merchandisename"
    };

    // --- 4. Exclusion Words ---
    private static readonly HashSet<string> ExclusionWords = new()
    {
        // Identifiers
        "id", "key", "code", "number", "num", "sku", "serial",
        // Metadata/Attributes (Not the Name)
        "desc", "description", "detail", "info", "note", "comment",
        "price", "cost", "value", "rate", "amount",
        "qty", "quantity", "count", "stock",
        "type", "category", "class", "group", "status",
        "image", "url", "file", "path"
    };

    public bool IsMatch(ColumnModel column, TableModel tableContext)
    {
        if (!DataTypeHelper.IsStringType(column.DataType)) return false;

        // 1. Tokenize inputs
        var schemaWords = StringUtils.SplitIntoWords(tableContext.SchemaName)
            .Select(StringUtils.ToSingular);

        var tableWords = StringUtils.SplitIntoWords(tableContext.TableName)
            .Select(StringUtils.ToSingular);

        var colWords = StringUtils.SplitIntoWords(column.ColumnName)
            .Select(s => s.ToLower())
            .ToList();

        // 2. Immediate Disqualification
        // "HumanResources" -> "humanresources"
        // "Human_Resources" -> "humanresources"
        var schemaRaw = tableContext.SchemaName?.ToLower().Replace("_", "").Replace("-", "") ?? "";
        if (InvalidSchemaNames.Overlaps(schemaWords) || InvalidSchemaNames.Contains(schemaRaw))
        {
            return false;
        }

        // 3. Negative Checks
        // Critical for avoiding "ProductDescription" or "ProductCode"
        if (ExclusionWords.Overlaps(colWords)) return false;

        // 4. Calculate Context
        var hasProductContext = ProductTableContexts.Overlaps(tableWords) ||
                                ProductTableContexts.Overlaps(schemaWords);

        // 5. Strong Normalized Match
        var normalizedCol = column.ColumnName.ToLower().Replace("_", "").Replace("-", "");
        if (StrongColumnNames.Contains(normalizedCol))
        {
            return true;
        }

        // 6. Token Analysis

        // Case A: "Product" or "Item" + "Name"/"Title"
        // Matches "Product_Title", "Item_Name"
        var hasName = colWords.Contains("name") || colWords.Contains("title");
        if (hasName && (colWords.Contains("product") || colWords.Contains("item")))
        {
            return true;
        }

        // Case B: Generic "Name" or "Title"
        // Matches "Products.Name", "Inventory.Title"
        // STRICTLY requires Product Context
        if (hasName && hasProductContext)
        {
            return true;
        }

        // Case C: Generic "Product"
        // Sometimes a column is just named "Product" (e.g. "Sales.Product")
        // Since we excluded "ID", "Code", "Desc" above, "Product" implies the name.
        if (colWords.Contains("product") && colWords.Count == 1)
        {
            return true;
        }

        return false;
    }
}