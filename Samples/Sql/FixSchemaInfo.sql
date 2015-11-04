-- FixSchemaInfo.sql: Updates SchemaInfo from incorrect v1.1.0 format to correct v1.0.0 format
-- This works even if there is e.g. a reference table named '<_referenceTableset>', because
-- that is written to the database as '&lt;/_referenceTableSet&gt;'.

SELECT * FROM __ShardManagement.ShardedDatabaseSchemaInfosGlobal

SET XACT_ABORT ON
BEGIN TRANSACTION

UPDATE __ShardManagement.ShardedDatabaseSchemaInfosGlobal
SET SchemaInfo = 
	REPLACE(
		REPLACE(
			CAST(SchemaInfo AS NVARCHAR(MAX)), 
			'<_referenceTableSet i:type="ArrayOfReferenceTableInfo">', 
			'<ReferenceTableSet i:type="ArrayOfReferenceTableInfo">'),
		'</_referenceTableSet>',
		'</ReferenceTableSet>')

UPDATE __ShardManagement.ShardedDatabaseSchemaInfosGlobal
SET SchemaInfo = 
	REPLACE(
		REPLACE(
			CAST(SchemaInfo AS NVARCHAR(MAX)), 
			'<_shardedTableSet i:type="ArrayOfShardedTableInfo">', 
			'<ShardedTableSet i:type="ArrayOfShardedTableInfo">'),
		'</_shardedTableSet>',
		'</ShardedTableSet>')

COMMIT TRANSACTION

SELECT * FROM __ShardManagement.ShardedDatabaseSchemaInfosGlobal