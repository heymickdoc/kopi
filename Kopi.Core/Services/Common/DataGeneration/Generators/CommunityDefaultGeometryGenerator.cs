using Kopi.Core.Models.SQLServer;

namespace Kopi.Core.Services.Common.DataGeneration.Generators;

public class CommunityDefaultGeometryGenerator : IDataGenerator
{
    public string TypeName => "default_geometry";
    
    private static readonly List<string> _wkt = new()
    {
        "POINT (30 10)",
        "LINESTRING (30 10, 10 30, 40 40)",
        "POLYGON ((0 0, 10 0, 10 10, 0 10, 0 0))",
        "POLYGON ((0 0, 100 0, 100 100, 0 100, 0 0)," +
        "(10 10, 10 20, 20 20, 20 10, 10 10))",
        "MULTIPOINT ((10 40), (40 30), (20 20), (30 10))",
        "MULTILINESTRING ((10 10, 20 20, 10 40), (40 40, 30 30, 40 20, 30 10))",
        "MULTIPOLYGON (((0 0, 10 0, 10 10, 0 10, 0 0)), ((20 20, 30 20, 30 30, 20 30, 20 20)))",
        "GEOMETRYCOLLECTION (POINT (40 10), LINESTRING (10 10, 20 20, 10 40))", "POINT (5 15)",
        "LINESTRING (0 0, 5 5, 10 0, 5 -5, 0 0)",
        "POINT (0 0)",
        "POINT (-10.5 50.2)",
        "POINT (110 -30)",
        "LINESTRING (0 0, 10 10, 0 20)",
        "LINESTRING (5 5, 6 6, 7 7, 8 8, 9 9, 10 10)",
        "LINESTRING (100 50, 120 50, 130 60, 120 70)",
        "POLYGON ((10 10, 100 10, 100 100, 10 100, 10 10))", // Simple square
        "POLYGON ((35 10, 45 45, 15 40, 10 20, 35 10))", // Simple pentagon
        "POLYGON ((0 0, 100 0, 100 100, 0 100, 0 0), (20 20, 20 80, 80 80, 80 20, 20 20))",
        "MULTIPOINT ((0 0), (5 5), (10 10))",
        "MULTIPOINT ((-1 -1), (1 1))",
        "MULTILINESTRING ((0 0, 10 10), (20 20, 30 30, 40 40))",
        "MULTILINESTRING ((5 5, 10 10), (15 15, 20 10))",
        "MULTIPOLYGON (((0 0, 10 0, 10 10, 0 10, 0 0)), ((20 20, 30 20, 30 30, 20 30, 20 20)))",
        "MULTIPOLYGON (((-10 -10, -10 10, 10 10, 10 -10, -10 -10)))", // Single polygon in a multi
        "GEOMETRYCOLLECTION (POINT (10 10), POINT (30 30), LINESTRING (15 15, 20 20))",
        "GEOMETRYCOLLECTION (POLYGON ((0 0, 5 0, 5 5, 0 5, 0 0)), POINT (10 10))"
    };

    public List<object?> GenerateBatch(ColumnModel column, int count, bool isUnique = false)
    {
        if (count > _wkt.Count) count = _wkt.Count;
        var values = new List<object?>(count);
        
        var uniqueValues = _wkt.OrderBy(x => Random.Shared.Next())
            .Take(count)
            .Cast<object?>();
        values.AddRange(uniqueValues);

        //Check for nullability. If so, make a maximum of 10% nulls
        if (!column.IsNullable) return values;

        // --- UPDATED: Use Random.Shared ---
        // We no longer create 'new Random()' here.
        for (var i = 0; i < values.Count; i++)
        {
            if (Random.Shared.NextDouble() < 0.1) //10% chance
            {
                values[i] = null;
            }
        }

        return values;
    }
}