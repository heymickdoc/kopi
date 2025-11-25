using Bogus;
using Kopi.Core.Models.SQLServer;
using Kopi.Core.Utilities;

namespace Kopi.Core.Services.Common.DataGeneration.Generators;

public class CommunityDefaultXMLGenerator : IDataGenerator
{
    public string TypeName => "default_xml";

    private readonly Faker _faker = new();

    private readonly List<string> _xmlData = new()
    {
        @"<root>
              <user id=""101"">
                <firstName>Jane</firstName>
                <lastName>Doe</lastName>
                <email>jane.doe@example.com</email>
                <status>active</status>
              </user>
            </root>",
        @"<root>
              <product sku=""ABC-12345"">
                <name>Wireless Headphones</name>
                <price currency=""USD"">149.99</price>
                <stock>
                  <warehouse>WH-A</warehouse>
                  <quantity>85</quantity>
                </stock>
              </product>
            </root>",
        @"<root>
              <logEntry timestamp=""2025-11-01T09:30:15Z"">
                <level>INFO</level>
                <source>Application.Auth</source>
                <message>User 'admin' successfully logged in from 192.168.1.10</message>
              </logEntry>
            </root>",
        @"<root>
              <book isbn=""978-0321765723"">
                <title>The C++ Programming Language</title>
                <author>Bjarne Stroustrup</author>
                <published>2013</published>
                <publisher>Addison-Wesley</publisher>
              </book>
            </root>",
        @"<root>
              <order orderId=""Z-9876"">
                <customer_id>C-004</customer_id>
                <placed>2025-10-31</placed>
                <items>
                  <item id=""A-102"" quantity=""2"" />
                  <item id=""B-405"" quantity=""1"" />
                </items>
              </order>
            </root>",
        @"<root>
              <config>
                <server type=""web"">
                  <host>www.example.com</host>
                  <port>443</port>
                  <ssl enabled=""true"" />
                </server>
                <database>
                  <host>db.internal</host>
                  <port>5432</port>
                  <timeout>30</timeout>
                </database>
              </config>
            </root>",
        @"<root>
              <gpsData device=""Tracker-007"">
                <point>
                  <latitude>47.3769</latitude>
                  <longitude>8.5417</longitude>
                  <elevation>408</elevation>
                </point>
                <speed unit=""kmh"">15</speed>
              </gpsData>
            </root>",
        @"<root>
              <weatherReport city=""London"">
                <current>
                  <temperature unit=""C"">12</temperature>
                  <conditions>Cloudy</conditions>
                </current>
                <forecast date=""2025-11-02"">
                  <high>14</high>
                  <low>8</low>
                  <conditions>Showers</conditions>
                </forecast>
              </weatherReport>
            </root>",
        @"<root>
              <note priority=""high"">
                <to>Team</to>
                <from>Alex</from>
                <heading>Deployment</heading>
                <body>Production deployment is scheduled for 5 PM CET.</body>
              </note>
            </root>",
        @"<root>
              <recipe name=""Simple Bread"">
                <ingredients>
                  <ingredient name=""Flour"" amount=""500"" unit=""g"" />
                  <ingredient name=""Yeast"" amount=""7"" unit=""g"" />
                  <ingredient name=""Salt"" amount=""10"" unit=""g"" />
                  <ingredient name=""Water"" amount=""300"" unit=""ml"" />
                </ingredients>
                <steps>
                  <step number=""1"">Mix dry ingredients.</step>
                  <step number=""2"">Add water and knead.</step>
                  <step number=""3"">Let rise for 1 hour.</step>
                  <step number=""4"">Bake at 220°C for 30 minutes.</step>
                </steps>
              </recipe>
            </root>"
    };

    public List<object?> GenerateBatch(ColumnModel column, int count, bool isUnique = false)
    {
        if (count > _xmlData.Count) count = _xmlData.Count;

        var values = new List<object?>(count);

        var uniqueValues = _faker.Random.Shuffle(_xmlData)
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