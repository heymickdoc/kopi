using Kopi.Core.Models.SQLServer;

namespace Kopi.Core.Models.Common;

public class SourceDbModel
{
	/// <summary>
	/// The version of the database server, e.g. SQL Server 2019, PostgreSQL 13, etc.
	/// </summary>
	public string DatabaseVersion { get; set; } = "";

	public List<UserDefinedDataTypeModel> UserDefinedDataTypes { get; set; }
	public List<ViewModel> Views { get; set; }
	public List<TableModel> Tables { get; set; } = [];
	public List<ConstraintModel> Constraints { get; set; } = [];
	public List<PrimaryKeyModel> PrimaryKeys { get; set; } = [];
	public List<RelationshipModel> Relationships { get; set; } = [];
	public List<IndexModel> Indexes { get; set; } = [];
	public List<ProgrammabilityModel> StoredProcedures { get; set; } = [];
	public List<ProgrammabilityModel> Functions { get; set; } = [];
	public List<DatabaseExtensionModel> Extensions { get; set; } = [];
}

public class TableModel
{
	public string SchemaName { get; set; }
	public string TableName { get; set; } = "";
	public List<ColumnModel> Columns { get; set; } = [];
}

public class PrimaryKeyModel
{
	public string SchemaName { get; set; }
	public string TableName { get; set; }
	public string PrimaryKeyName { get; set; }
	public List<string> PrimaryKeyColumns { get; set; } = [];
}

public class RelationshipModel
{
	public string ParentSchema { get; set; }
	public string ForeignKeyName { get; set; }
	public string ParentTable { get; set; }
	public string ReferencedSchema { get; set; }
	public string ReferencedTable { get; set; }
	public List<ForeignKeyColumnModel> ForeignKeyColumns { get; set; } = [];
}

public class ForeignKeyColumnModel
{
	public string ParentColumnName { get; set; }
	public string ReferencedColumnName { get; set; }
	public int KeyOrdinal { get; set; }
}

public class IndexModel
{
	public string SchemaName { get; set; }
	public string TableName { get; set; }
	public string IndexName { get; set; }
	public List<IndexColumnModel> IndexColumns { get; set; } = [];
	public bool IsUnique { get; set; } = false;
	public bool IsPrimaryKey { get; set; }
}

public class IndexColumnModel
{
	public int KeyOrdinal { get; set; }
	public string ColumnName { get; set; }
	public bool IsIncludedColumn { get; set; }
}

// public class StoredProcedureModel
// {
// 	public string SchemaName { get; set; }
// 	public string ProcedureName { get; set; }
// 	public string Definition { get; set; }
// 	public DateTime CreateDate { get; set; }
// 	public DateTime ModifyDate { get; set; }
// 	public string ObjectType { get; set; } = "PROCEDURE";
// }
//
// public class FunctionModel
// {
// 	public string SchemaName { get; set; }
// 	public string FunctionName { get; set; }
// 	public string FunctionType { get; set; } // SCALAR, TABLE_VALUED
// 	public string Definition { get; set; }
// 	public DateTime CreateDate { get; set; }
// 	public DateTime ModifyDate { get; set; }
// }

// Or combine them into one class:
public class ProgrammabilityModel
{
	public string SchemaName { get; set; }
	public string ObjectName { get; set; }
	public string ObjectType { get; set; } // PROCEDURE, SCALAR_FUNCTION, TABLE_VALUED_FUNCTION
	public string Definition { get; set; }
}

public class UserDefinedDataTypeModel
{
	public string SchemaName { get; set; }
	public string TypeName { get; set; }
	public string BaseTypeName { get; set; }
	public string MaxLength { get; set; }
	public int Precision { get; set; }
	public int Scale { get; set; }
	public bool IsNullable { get; set; }
	public string CreateScript { get; set; }
}

public class ViewModel
{
	public string SchemaName { get; set; }
	public string ViewName { get; set; }
	public string Definition { get; set; }
	public string CreateScript { get; set; }
}

public class ConstraintModel
{
	public string SchemaName { get; set; }
	public string TableName { get; set; }
	public string ConstraintName { get; set; }
	public string ConstraintType { get; set; } // CHECK, DEFAULT, UNIQUE, PRIMARY KEY, FOREIGN KEY
	public string Definition { get; set; }
}