# Getting updated PowerShell scripts

You can download our source code with the updated PowerShell helper scripts from  https://github.com/jaredmoo/elastic-db-tools/archive/SchemaInfoWorkaround.zip. Extract this zip, open a PowerShell commad prompt, and navigate to `Samples/PowerShell` in the extracted files.

These updates scripts will in the next few days be integrated into our main codebase and released on Script Center.

# Getting the scripts

I start a PowerShell command window and navigate to the updated PowerShell scripts. 

First I will load ShardManagement PowerShell script module, which will in turn automatically download the latest **Elastic Database tools client library** from NuGet (note that this uses the new PackageManagement on Windows 10 and falls back to nuget.exe on older operating systems, so you may see different output).

```
> Import-Module .\ShardManagement

D:\elastic-db-tools\Samples\PowerShell\ShardManagement\Microsoft.Azure.SqlDatabase.ElasticScale.Client.dll was not
found.
Would you like to download it from NuGet?
[Y] Yes  [N] No  [?] Help (default is "N"): y

The package(s) come from a package source that is not marked as trusted.
Are you sure you want to install software from 'nuget.org'?
[Y] Yes  [N] No  [S] Suspend  [?] Help (default is "Y"): y
```

Then I verify which version of Elastic Database tools client library is loaded. **The bug described will only reproduce with version 1.1.0.1 of the Elastic Database tools client library**. When a fixed version of Elastic Database tools client library is released in the next few days, you will need to manually download that version and place it in the .\ShardManagement folder if you wish to reproduce the issue. (Later versions of Elastic Database tools client library will still work with these scripts, but the bug will not reproduce).

```
> $(Get-Item .\ShardManagement\Microsoft.Azure.SqlDatabase.ElasticScale.Cli
ent.dll).VersionInfo

ProductVersion   FileVersion      FileName
--------------   -----------      --------
1.1.0.1          1.1.0.1          D:\git\elastic-db-tools\Samples\PowerShell\ShardManagement\Microsoft.Azure.SqlData...
```

# Reproducing the issue

The below guide assumes that I have a Split-Merge Service running with the below configuration:
 - The web endpoint is at `https://my-split-merge.example.com`
 - The status database is in server `my-server.database.windows.net`, database `MySplitMergeStatusDb`


In the PowerShell window sthat I started above, I set up a sample Shard Map Manager database and shards:

```
> .\SetupSampleSplitMergeEnvironment.ps1 -UserName myuser -Password mypassword -ShardMapManagerServerName my-server.database.windows.net

Sample Split-Merge Environment has been created.
  ShardMapManager Server: my-server.database.windows.net Database: SplitMergeShardManagement
  ShardMapName: MyTestShardMap ShardKeyType: Int32

To view the current shard mappings, execute:
  .\GetMappings.ps1 -UserName myuser -Password mypassword -ShardMapManagerServerName my-server.database.windows.net -ShardMapManagerDatabaseName SplitMergeShardManagement -ShardMapName MyTestShardMap
```

Next I run that suggested command to check what current mappings exist in this Shard Map Manager.

```
> .\GetMappings.ps1 -UserName myuser -Password mypassword -ShardMapManagerServerName my-server.database.windows.net -ShardMapManagerDatabaseName SplitMergeShardManagement -ShardMapName MyTestShardMap

Status        : Online
ShardLocation : [DataSource=my-server.database.windows.net Database=ShardDb1]
Value         : [0:200)
```

This shows me that all data is in `ShardDb1`.

Now to reproduce the issue, I execute a sample Split Operation.

```
>.\ExecuteSampleSplitMerge.ps1 -UserName myuser -Password mypassword -ShardMapManagerServerName my-server.database.windows.net -SplitMergeServiceEndpoint https://my-split-merge.example.com -SplitOnly

Sending split request
Began split operation with id 8ed0d9f1-d6ab-49af-b29b-55eea4bf277a
Polling request status. Press Ctrl-C to end
Progress: 0% | Status: Queued | Details: [Informational] Operation has been queued.
Progress: 5% | Status: Starting | Details: [Informational] Starting Split-Merge state machine for request.
Progress: 100% | Status: Succeeded | Details: [Informational] Successfully processed request.

> .\GetMappings.ps1 -UserName myuser -Password mypassword -ShardMapManagerServerName my-server.database.windows.net -ShardMapManagerDatabaseName SplitMergeShardManagement -ShardMapName MyTestShardMap

Status        : Online
ShardLocation : [DataSource=my-server.database.windows.net Database=ShardDb1]
Value         : [0:100)

Status        : Online
ShardLocation : [DataSource=my-server.database.windows.net Database=ShardDb2]
Value         : [100:200)
```

At this point everything looks like it is happily working, but when I query my data with T-SQL (`select * from MyShardedTable`), I can see that no data was actually moved!

In order to see what really happened in the Split operation, I use the `GetSplitMergeHistory.ps1` script.

```
> .\GetSplitMergeHistory.ps1 -SplitMergeStatusDbConnectionString "Server=my-server.database.windows.net;Database=MySplitMergeStatusDb;User=myuser;Password=mypassword"

CreateTime           : 11/5/2015 1:07:23 AM
OperationType        : Split
State                : Completed
MovedLowKey          : 100
MovedHighKey         : 200
KeyType              : Int32
SrcServer            : my-server.database.windows.net
SrcDb                : ShardDb1
TargetServer         : my-server.database.windows.net
TargetDb             : ShardDb2
ShardedTablesMoved   :
ReferenceTablesMoved :
```

This detailed history report shows me in the **ShardedTablesMoved** field that **no tables were moved**. This is the bug. The SchemaInfo stored in the Shard Map Manager database has the incorrect format, so the Split-Merge service does not successfully decode the SchemaInfo and therefore does not move any tables. 

# Fixing the Shard Map

In order to correct the Shard Map, I need to manually update the `[100, 200)` range to be mapped to `ShardDb` where the data still actually is.

```
> .\GetMappings.ps1 -UserName myuser -Password mypassword -ShardMapManagerServerName my-server.database.windows.net -ShardMapManagerDatabaseName SplitMergeShardManagement -ShardMapName MyTestShardMap

Status        : Online
ShardLocation : [DataSource=my-server.database.windows.net Database=ShardDb1]
Value         : [0:100)

Status        : Online
ShardLocation : [DataSource=my-server.database.windows.net Database=ShardDb2]
Value         : [100:200)

> $smm = Get-ShardMapManager -UserName myuser -Password mypassword -SqlServerName my-server.database.windows.net -SqlDatabaseName SplitMergeShardManagement
> $sm = Get-RangeShardMap -KeyType int -ShardMapManager $smm -RangeShardMapName MyTestShardMap
> Set-RangeMapping -KeyType int -RangeShardMap $sm -RangeLow 100 -RangeHigh 200 -SqlServerName . -SqlDatabaseName ShardDb1

Status ShardLocation                    Value
------ -------------                    -----
Online [DataSource=my-server.database.windows.net Database=ShardDb1] [100:200)

> .\GetMappings.ps1 -UserName myuser -Password mypassword -ShardMapManagerServerName my-server.database.windows.net -ShardMapManagerDatabaseName SplitMergeShardManagement -ShardMapName MyTestShardMap


Status        : Online
ShardLocation : [DataSource=my-server.database.windows.net Database=ShardDb1]
Value         : [0:100)

Status        : Online
ShardLocation : [DataSource=my-server.database.windows.net Database=ShardDb1]
Value         : [100:200)
```

Now the Shard Map is repaired.

# Moving forward

This issue will be resolved in Split-Merge version 1.2 by giving Split-Merge the ability to read the incorrectly formatted SchemaInfo created by EDCL v1.1. (We are also fixing EDCL to emit SchemaInfo in the original format).

If you urgently need to fix your on-disk SchemaInfo so that it works with Split-Merge v1.1, you can directly update the data in your Shard map Manager database with the script at https://raw.githubusercontent.com/jaredmoo/elastic-db-tools/SchemaInfoWorkaround/Samples/Sql/FixSchemaInfo.sql .
