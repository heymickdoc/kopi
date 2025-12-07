using Kopi.Core.Models.Common;
using Kopi.Core.Models.SQLServer;

namespace Kopi.Core.Interfaces;

public interface IDataInsertionService
{
    Task InsertData(
        KopiConfig config, 
        SourceDbModel sourceDb, 
        List<TargetDataModel> generatedData, 
        string targetConnectionString
    );
}