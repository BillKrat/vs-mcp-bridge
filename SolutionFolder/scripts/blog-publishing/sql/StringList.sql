USE [db_a2cb58_adventure_1]
GO

/****** Object:  UserDefinedTableType [dbo].[StringList]    Script Date: 4/18/2026 1:01:33 PM ******/
CREATE TYPE [dbo].[StringList] AS TABLE(
	[Value] [nvarchar](255) NOT NULL
)
GO


