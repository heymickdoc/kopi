using Bogus;
using Kopi.Core.Models.Common;
using Kopi.Core.Models.SQLServer;

namespace Kopi.Core.Services.Common.DataGeneration.Generators;

public class CommunityDefaultJsonGenerator : IDataGenerator
{
    public string TypeName => "default_json";
    
    private readonly Faker _faker = new();

    private readonly List<string> _jsonData =
    [
        "{\"id\":\"d3f9b2a1-8c4f-4a6e-9b0d-1e2a3b4c5d6e\",\"name\":\"Alpha Forge\",\"value\":42.7,\"count\":7,\"active\":true,\"tags\":[\"alpha\",\"test\"],\"created\":\"2025-10-01T12:34:56Z\",\"meta\":{\"priority\":\"high\",\"score\":0.83}}",
        "{\"id\":\"a7c1e9d4-2b5f-4f7b-98d2-0c1b2a3d4e5f\",\"name\":\"Blue Canary\",\"value\":13.14,\"count\":2,\"active\":false,\"tags\":[\"canary\",\"beta\"],\"created\":\"2024-07-19T03:21:10Z\",\"meta\":{\"region\":\"us-east\",\"retries\":1}}",
        "{\"id\":\"f1b2c3d4-5678-90ab-cdef-1234567890ab\",\"name\":\"Crimson Node\",\"value\":0.0021,\"count\":150,\"active\":true,\"tags\":[\"node\",\"crimson\"],\"created\":\"2023-11-05T18:00:00Z\",\"meta\":{\"owner\":\"team-x\",\"deprecated\":false}}",
        "{\"id\":\"e9f8d7c6-b5a4-3210-9fed-cba987654321\",\"name\":\"Delta Stream\",\"value\":99.99,\"count\":0,\"active\":false,\"tags\":[\"stream\"],\"created\":\"2022-01-15T09:45:30Z\",\"meta\":{\"latency_ms\":120}}",
        "{\"id\":\"0a1b2c3d-4e5f-6789-0abc-def123456789\",\"name\":\"Echo Harbor\",\"value\":7.5,\"count\":23,\"active\":true,\"tags\":[\"harbor\",\"echo\"],\"created\":\"2025-01-02T04:05:06Z\",\"meta\":{\"owner\":\"alice\",\"rating\":4.2}}",
        "{\"id\":\"c8b7a6d5-e4f3-21b0-9c8d-7e6f5a4b3c2d\",\"name\":\"Foxtrot Lab\",\"value\":314.159,\"count\":3,\"active\":true,\"tags\":[\"lab\",\"foxtrot\"],\"created\":\"2021-12-31T23:59:59Z\",\"meta\":{\"tests_passed\":12}}",
        "{\"id\":\"9f8e7d6c-5b4a-3c2d-1e0f-a9b8c7d6e5f4\",\"name\":\"Gamma Field\",\"value\":-10.5,\"count\":9,\"active\":false,\"tags\":[\"field\",\"gamma\"],\"created\":\"2020-06-30T12:00:00Z\",\"meta\":{\"notes\":\"archived\"}}",
        "{\"id\":\"11aa22bb-33cc-44dd-55ee-66ff77889900\",\"name\":\"Horizon Peak\",\"value\":2500,\"count\":1,\"active\":true,\"tags\":[\"mountain\",\"horizon\"],\"created\":\"2025-09-10T08:00:00Z\",\"meta\":{\"elevation_m\":1420}}",
        "{\"id\":\"22334455-6677-8899-aabb-ccddeeff0011\",\"name\":\"Ivy Circuit\",\"value\":3.14159,\"count\":42,\"active\":true,\"tags\":[\"circuit\",\"ivy\"],\"created\":\"2024-03-14T15:09:26Z\",\"meta\":{\"category\":\"math\"}}",
        "{\"id\":\"33445566-7788-99aa-bbcc-ddeeff001122\",\"name\":\"Juniper Gate\",\"value\":0,\"count\":0,\"active\":false,\"tags\":[],\"created\":\"2019-05-20T00:00:00Z\",\"meta\":{\"empty\":true}}",
        "{\"id\":\"44556677-8899-aabb-ccdd-eeff00112233\",\"name\":\"Kite Harbor\",\"value\":88.8,\"count\":8,\"active\":true,\"tags\":[\"kite\"],\"created\":\"2025-04-01T10:10:10Z\",\"meta\":{\"season\":\"spring\"}}",
        "{\"id\":\"55667788-99aa-bbcc-ddee-ff0011223344\",\"name\":\"Lumen Grid\",\"value\":6.02,\"count\":60,\"active\":true,\"tags\":[\"grid\",\"lumen\"],\"created\":\"2023-08-22T14:30:00Z\",\"meta\":{\"units\":\"mol\"}}",
        "{\"id\":\"66778899-aabb-ccdd-eeff-001122334455\",\"name\":\"Mesa Orchard\",\"value\":12.345,\"count\":5,\"active\":false,\"tags\":[\"orchard\",\"mesa\"],\"created\":\"2022-10-10T10:10:10Z\",\"meta\":{\"fruit\":\"apple\"}}",
        "{\"id\":\"778899aa-bbcc-ddee-ff00-112233445566\",\"name\":\"Nimbus Dock\",\"value\":1.618,\"count\":11,\"active\":true,\"tags\":[\"cloud\",\"nimbus\"],\"created\":\"2025-02-28T07:07:07Z\",\"meta\":{\"tier\":\"premium\"}}",
        "{\"id\":\"8899aabb-ccdd-eeff-0011-223344556677\",\"name\":\"Orion Vault\",\"value\":777,\"count\":77,\"active\":false,\"tags\":[\"vault\",\"orion\"],\"created\":\"2020-02-02T02:02:02Z\",\"meta\":{\"secure\":true}}",
        "{\"id\":\"99aabbcc-ddee-ff00-1122-334455667788\",\"name\":\"Pine Loop\",\"value\":0.75,\"count\":19,\"active\":true,\"tags\":[\"pine\"],\"created\":\"2024-12-12T12:12:12Z\",\"meta\":{\"loop_count\":3}}",
        "{\"id\":\"aabbccdd-eeff-0011-2233-445566778899\",\"name\":\"Quasar Node\",\"value\":420.0,\"count\":420,\"active\":true,\"tags\":[\"quasar\",\"node\"],\"created\":\"2023-03-03T03:03:03Z\",\"meta\":{\"flux\":9.5}}",
        "{\"id\":\"bbccddee-ff00-1122-3344-556677889900\",\"name\":\"Ridge Path\",\"value\":5.5,\"count\":6,\"active\":false,\"tags\":[\"ridge\",\"path\"],\"created\":\"2021-07-07T07:07:07Z\",\"meta\":{\"trail\":\"easy\"}}",
        "{\"id\":\"ccddee00-1122-3344-5566-77889900aabb\",\"name\":\"Sable Works\",\"value\":0.99,\"count\":100,\"active\":true,\"tags\":[\"works\",\"sable\"],\"created\":\"2025-06-15T16:20:00Z\",\"meta\":{\"build\":202}}",
        "{\"id\":\"ddeeff11-2233-4455-6677-889900aabbcc\",\"name\":\"Tango Mill\",\"value\":66.6,\"count\":13,\"active\":true,\"tags\":[\"mill\",\"tango\"],\"created\":\"2022-11-11T11:11:11Z\",\"meta\":{\"shifts\":3}}",
        "{\"id\":\"eeff0011-2233-4455-6677-889900aabbdd\",\"name\":\"Umbra Base\",\"value\":-0.5,\"count\":4,\"active\":false,\"tags\":[\"base\",\"umbra\"],\"created\":\"2018-09-09T09:09:09Z\",\"meta\":{\"hidden\":true}}",
        "{\"id\":\"ff001122-3344-5566-7788-9900aabbccdd\",\"name\":\"Violet Range\",\"value\":123.456,\"count\":21,\"active\":true,\"tags\":[\"range\",\"violet\"],\"created\":\"2025-08-08T08:08:08Z\",\"meta\":{\"color\":\"#8a2be2\"}}",
        "{\"id\":\"00112233-4455-6677-8899-aabbccddeeff\",\"name\":\"Willow Trace\",\"value\":2.71828,\"count\":314,\"active\":false,\"tags\":[\"willow\",\"trace\"],\"created\":\"2024-05-05T05:05:05Z\",\"meta\":{\"path\":\"river\"}}",
        "{\"id\":\"11223344-5566-7788-99aa-bbccddeeff00\",\"name\":\"Xeno Point\",\"value\":0.0001,\"count\":9999,\"active\":true,\"tags\":[\"xeno\"],\"created\":\"2023-01-01T01:01:01Z\",\"meta\":{\"exotic\":true}}",
        "{\"id\":\"22334455-6677-8899-aabb-ccddeeff1122\",\"name\":\"Yarrow Field\",\"value\":14,\"count\":14,\"active\":false,\"tags\":[\"yarrow\",\"field\"],\"created\":\"2022-04-04T04:04:04Z\",\"meta\":{\"herb\":\"yarrow\"}}",
        "{\"id\":\"33445566-7788-99aa-bbcc-ddeeff223344\",\"name\":\"Zephyr Bay\",\"value\":55.55,\"count\":5,\"active\":true,\"tags\":[\"bay\",\"zephyr\"],\"created\":\"2025-03-03T03:03:03Z\",\"meta\":{\"wind_kph\":22}}",
        "{\"id\":\"44556677-8899-aabb-ccdd-eeff33445566\",\"name\":\"Arcane Vault\",\"value\":9999.99,\"count\":0,\"active\":false,\"tags\":[\"arcane\"],\"created\":\"2017-10-10T10:10:10Z\",\"meta\":{\"mystic\":42}}",
        "{\"id\":\"55667788-99aa-bbcc-ddee-ff0011223344\",\"name\":\"Beryl Cove\",\"value\":21.21,\"count\":2,\"active\":true,\"tags\":[\"cove\",\"beryl\"],\"created\":\"2024-09-09T09:00:00Z\",\"meta\":{\"depth_m\":8}}",
        "{\"id\":\"66778899-aabb-ccdd-eeff-001122334455\",\"name\":\"Cedar Yard\",\"value\":17,\"count\":17,\"active\":true,\"tags\":[\"cedar\",\"yard\"],\"created\":\"2023-06-06T06:06:06Z\",\"meta\":{\"trees\":120}}",
        "{\"id\":\"778899aa-bbcc-ddee-ff0011223344\",\"name\":\"Dune Harbor\",\"value\":0.5,\"count\":50,\"active\":false,\"tags\":[\"dune\"],\"created\":\"2020-08-08T08:08:08Z\",\"meta\":{\"sand_pct\":93.2}}"
    ];

    public List<object?> GenerateBatch(ColumnModel column, int count, bool isUnique = false)
    {
        if (count > _jsonData.Count) count = _jsonData.Count;
        var values = new List<object?>(count);
        
        var uniqueValues = _faker.Random.Shuffle(_jsonData)
            .Take(count)
            .Cast<object?>()
            .ToList();
        values.AddRange(uniqueValues);
        
        
        if (!column.IsNullable) return values;

        for (var i = 0; i < values.Count; i++)
        {
            //10% chance
            if (_faker.Random.Bool(0.1f)) values[i] = null;
        }

        return values;
    }
}