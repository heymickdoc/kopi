using Kopi.Core.Models.Common;
using Kopi.Core.Models.SQLServer;
using Kopi.Core.Services.Matching.Matchers.Special;
using Kopi.Core.Utilities;

namespace Kopi.Core.Services.Matching.Matchers;

/// <summary>
///  Matches columns named "name" and assigns appropriate generators based on table context.
/// </summary>
public class CommunitySpecialNameMatcher : IColumnMatcher
{
    /*
     * Ok, the point of this is to deal with all the columns in all the tables that are named "name".
     * This is common in databases where you have tables like "product", "category", "tag", "person", etc.
     * I need to figure out a way to intelligently determine what kind of "name" it is and then assign
     * an appropriate generator.
     *  For example:
     *   - If the table name contains "product", then "name" could be a product name.
     *   - If the table name contains "category", then "name" could be a category name.
     *   - If the table name contains "tag", then "name" could be a tag name.
     *   - If the table name contains "person", then "name" could be a person's name.
     *
     * I'm not going to get this right first time, so I'll need to iterate on this.
     */
    public int Priority => 50;
    public string GeneratorTypeKey { get; private set; } = null!;

    public bool IsMatch(ColumnModel column, TableModel tableContext)
    {
        if (!DataTypeHelper.IsStringType(column.DataType)) return false;
        
        // 2. Strict Name Check
        // We strictly look for the word "name" alone. 
        // "FirstName" or "ProductName" are handled by other matchers.
        // We accept "Name" or "[Name]" via simple normalization.
        var rawName = column.ColumnName.ToLower().Replace("[", "").Replace("]", "").Trim();
        if (rawName != "name") return false;

        // --- Core Matchers ---
        if (CommunitySpecialProductName.IsMatch(tableContext))
        {
            GeneratorTypeKey = "product_name";
            return true;
        }
        
        if (CommunitySpecialPersonName.IsMatch(tableContext))
        {
            GeneratorTypeKey = "full_name";
            return true;
        }

        var maxLength = DataTypeHelper.GetMaxLength(column);
        
        // --- Geographic Matchers ---
        if (CommunitySpecialCountryName.IsMatch(tableContext, maxLength))
        {
            GeneratorTypeKey = "country_name";
            return true;
        }
        
        if (CommunitySpecialStateName.IsMatch(tableContext, maxLength))
        {
            GeneratorTypeKey = "address_state";
            return true;
        }

        if (CommunitySpecialRegionName.IsMatch(tableContext, maxLength))
        {
            GeneratorTypeKey = "address_region";
            return true;
        }

        if (CommunitySpecialCityName.IsMatch(tableContext, maxLength))
        {
            GeneratorTypeKey = "address_city";
            return true;
        }

        // --- Generic Lookup Matchers ---
        // if (CommunitySpecialCategoryName.IsMatch(tableContext))
        // {
        //     GeneratorTypeKey = "category_name"; // You'll need to create this generator
        //     return true;
        // }
        //
        // if (CommunitySpecialStatusName.IsMatch(tableContext))
        // {
        //     GeneratorTypeKey = "status_name"; // You'll need to create this generator
        //     return true;
        // }
        
        return false;
    }
}