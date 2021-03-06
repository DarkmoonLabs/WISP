USE [Game]
GO
/****** Object:  UserDefinedTableType [dbo].[Effects]    Script Date: 12/22/2015 10:42:02 PM ******/
CREATE TYPE [dbo].[Effects] AS TABLE(
	[DBID] [uniqueidentifier] NOT NULL,
	[EffectType] [int] NOT NULL,
	[TargetCharacter] [int] NOT NULL,
	[Instigator] [int] NOT NULL,
	[WhenAttached] [bigint] NOT NULL,
	[LastTick] [bigint] NOT NULL,
	[DurationRemaining] [bigint] NOT NULL
)
GO
/****** Object:  UserDefinedTableType [dbo].[GuidArray]    Script Date: 12/22/2015 10:42:02 PM ******/
CREATE TYPE [dbo].[GuidArray] AS TABLE(
	[Item] [uniqueidentifier] NULL
)
GO
/****** Object:  UserDefinedTableType [dbo].[IntArray]    Script Date: 12/22/2015 10:42:02 PM ******/
CREATE TYPE [dbo].[IntArray] AS TABLE(
	[Item] [int] NULL
)
GO
/****** Object:  UserDefinedTableType [dbo].[ItemDeleteData]    Script Date: 12/22/2015 10:42:02 PM ******/
CREATE TYPE [dbo].[ItemDeleteData] AS TABLE(
	[ItemID] [uniqueidentifier] NOT NULL,
	[PermaPurge] [tinyint] NOT NULL,
	[Account] [uniqueidentifier] NULL,
	[DeleteReason] [nvarchar](max) NULL
)
GO
/****** Object:  UserDefinedTableType [dbo].[ItemPropertyFloat]    Script Date: 12/22/2015 10:42:02 PM ******/
CREATE TYPE [dbo].[ItemPropertyFloat] AS TABLE(
	[PropertyOwner] [uniqueidentifier] NULL,
	[PropertyId] [int] NULL,
	[PropertyValue] [float] NULL,
	[PropertyName] [nvarchar](128) NULL
)
GO
/****** Object:  UserDefinedTableType [dbo].[ItemPropertyInt]    Script Date: 12/22/2015 10:42:02 PM ******/
CREATE TYPE [dbo].[ItemPropertyInt] AS TABLE(
	[PropertyOwner] [uniqueidentifier] NULL,
	[PropertyId] [int] NULL,
	[PropertyValue] [int] NULL,
	[PropertyName] [nvarchar](128) NULL
)
GO
/****** Object:  UserDefinedTableType [dbo].[ItemPropertyLong]    Script Date: 12/22/2015 10:42:02 PM ******/
CREATE TYPE [dbo].[ItemPropertyLong] AS TABLE(
	[PropertyOwner] [uniqueidentifier] NULL,
	[PropertyId] [int] NULL,
	[PropertyValue] [bigint] NULL,
	[PropertyName] [nvarchar](128) NULL
)
GO
/****** Object:  UserDefinedTableType [dbo].[ItemPropertyString]    Script Date: 12/22/2015 10:42:02 PM ******/
CREATE TYPE [dbo].[ItemPropertyString] AS TABLE(
	[PropertyOwner] [uniqueidentifier] NULL,
	[PropertyId] [int] NULL,
	[PropertyValue] [nvarchar](max) NULL,
	[PropertyName] [nvarchar](128) NULL
)
GO
/****** Object:  UserDefinedTableType [dbo].[ItemsData]    Script Date: 12/22/2015 10:42:02 PM ******/
CREATE TYPE [dbo].[ItemsData] AS TABLE(
	[Template] [nvarchar](max) NOT NULL,
	[CreatedOn] [datetime] NOT NULL,
	[GOT] [int] NOT NULL,
	[UID] [uniqueidentifier] NOT NULL,
	[Owner] [nvarchar](max) NOT NULL,
	[Context] [uniqueidentifier] NOT NULL,
	[TypeHash] [bigint] NOT NULL,
	[BinData] [varbinary](max) NOT NULL,
	[IsStatic] [tinyint] NOT NULL,
	[StackCount] [int] NOT NULL,
	[ObjectOwner] [uniqueidentifier] NULL
)
GO
/****** Object:  UserDefinedTableType [dbo].[ItemStat]    Script Date: 12/22/2015 10:42:02 PM ******/
CREATE TYPE [dbo].[ItemStat] AS TABLE(
	[StatOwner] [uniqueidentifier] NULL,
	[StatId] [int] NULL,
	[StatValue] [float] NULL,
	[StatMaxValue] [float] NULL,
	[StatMinValue] [float] NULL
)
GO
/****** Object:  UserDefinedTableType [dbo].[ItemStats]    Script Date: 12/22/2015 10:42:02 PM ******/
CREATE TYPE [dbo].[ItemStats] AS TABLE(
	[OwnerID] [uniqueidentifier] NOT NULL,
	[StatID] [int] NOT NULL,
	[StatValue] [int] NOT NULL
)
GO
/****** Object:  UserDefinedTableType [dbo].[ItemStringProperty]    Script Date: 12/22/2015 10:42:02 PM ******/
CREATE TYPE [dbo].[ItemStringProperty] AS TABLE(
	[PropertyOwner] [uniqueidentifier] NULL,
	[PropertyId] [int] NULL,
	[PropertyValue] [nvarchar](max) NULL
)
GO
/****** Object:  UserDefinedTableType [dbo].[MessagesTable]    Script Date: 12/22/2015 10:42:02 PM ******/
CREATE TYPE [dbo].[MessagesTable] AS TABLE(
	[ID] [int] NOT NULL,
	[FromID] [int] NOT NULL,
	[ToID] [int] NOT NULL,
	[MessageCategory] [int] NOT NULL,
	[StringID] [int] NOT NULL,
	[Parms] [nvarchar](max) NULL,
	[SendDateTime] [datetime] NOT NULL
)
GO
/****** Object:  UserDefinedTableType [dbo].[PropertyFloat]    Script Date: 12/22/2015 10:42:02 PM ******/
CREATE TYPE [dbo].[PropertyFloat] AS TABLE(
	[PropertyOwner] [int] NULL,
	[PropertyId] [int] NULL,
	[PropertyValue] [float] NULL,
	[PropertyName] [nvarchar](128) NULL
)
GO
/****** Object:  UserDefinedTableType [dbo].[PropertyInt]    Script Date: 12/22/2015 10:42:02 PM ******/
CREATE TYPE [dbo].[PropertyInt] AS TABLE(
	[PropertyOwner] [int] NULL,
	[PropertyId] [int] NULL,
	[PropertyValue] [int] NULL,
	[PropertyName] [nvarchar](128) NULL
)
GO
/****** Object:  UserDefinedTableType [dbo].[PropertyLong]    Script Date: 12/22/2015 10:42:02 PM ******/
CREATE TYPE [dbo].[PropertyLong] AS TABLE(
	[PropertyOwner] [int] NULL,
	[PropertyId] [int] NULL,
	[PropertyValue] [bigint] NULL,
	[PropertyName] [nvarchar](128) NULL
)
GO
/****** Object:  UserDefinedTableType [dbo].[PropertyString]    Script Date: 12/22/2015 10:42:02 PM ******/
CREATE TYPE [dbo].[PropertyString] AS TABLE(
	[PropertyOwner] [int] NULL,
	[PropertyId] [int] NULL,
	[PropertyValue] [nvarchar](max) NULL,
	[PropertyName] [nvarchar](128) NULL
)
GO
/****** Object:  UserDefinedTableType [dbo].[QueueList]    Script Date: 12/22/2015 10:42:02 PM ******/
CREATE TYPE [dbo].[QueueList] AS TABLE(
	[DBID] [uniqueidentifier] NOT NULL,
	[CharacterID] [int] NOT NULL,
	[UnitType] [int] NOT NULL,
	[NumToCreate] [int] NOT NULL,
	[TurnsLeft] [int] NOT NULL
)
GO
/****** Object:  UserDefinedTableType [dbo].[Stat]    Script Date: 12/22/2015 10:42:02 PM ******/
CREATE TYPE [dbo].[Stat] AS TABLE(
	[StatOwner] [int] NULL,
	[StatId] [int] NULL,
	[StatValue] [float] NULL,
	[StatMaxValue] [float] NULL,
	[StatMinValue] [float] NULL
)
GO
/****** Object:  UserDefinedTableType [dbo].[Stats]    Script Date: 12/22/2015 10:42:02 PM ******/
CREATE TYPE [dbo].[Stats] AS TABLE(
	[OwnerID] [int] NOT NULL,
	[StatID] [int] NOT NULL,
	[StatValue] [int] NOT NULL
)
GO
/****** Object:  UserDefinedTableType [dbo].[StringProperty]    Script Date: 12/22/2015 10:42:02 PM ******/
CREATE TYPE [dbo].[StringProperty] AS TABLE(
	[PropertyOwner] [int] NULL,
	[PropertyId] [int] NULL,
	[PropertyValue] [nvarchar](max) NULL
)
GO
/****** Object:  StoredProcedure [dbo].[Character_Create]    Script Date: 12/22/2015 10:42:02 PM ******/
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO
-- =============================================
-- Author:		<Author,,Name>
-- Create date: <Create Date,,>
-- Description:	<Description,,>
-- =============================================
CREATE PROCEDURE [dbo].[Character_Create]
	-- Add the parameters for the stored procedure here
@resultCode int out,
@owner uniqueidentifier,
@enforceUniqueName bit,
@namePropertyId int,
@characterName nvarchar(max),
@maxCharacters int,
@intProperties PropertyInt READONLY,
@floatProperties PropertyFloat READONLY,
@longProperties PropertyLong READONLY,
@stringProperties PropertyString READONLY,
@isTemp bit,
@stats Stat READONLY,
@characterId int output
	
AS
BEGIN
	-- SET NOCOUNT ON added to prevent extra result sets from
	-- interfering with SELECT statements.
	SET NOCOUNT ON;

--BEGIN TRANSACTION

    -- Create the character
    
    -- check for unique character name
IF @enforceUniqueName = 1
BEGIN 
    SELECT 
		Character_tblMaster.OwnerAccount 
	from 
		Character_tblMaster JOIN Character_tblPropertiesString On Character_tblMaster.ID = Character_tblPropertiesString.OwnerID
    WHERE 
		Character_tblPropertiesString.PropertyID = @namePropertyId AND  
		Character_tblPropertiesString.PropertyValue = @characterName
		AND Character_tblMaster.Deleted = 0
		
    if @@ROWCOUNT > 0
	 BEGIN
		--ROLLBACK
		Set @resultCode = -2
		RETURN
	 END
	 
END

	SELECT ID FROM Character_tblMaster WHERE OwnerAccount = @owner AND Deleted = 0
	if @@ROWCOUNT + 1 > @maxCharacters
	 BEGIN
--		ROLLBACK
		Set @resultCode = -9
		RETURN
	 END
    
	INSERT INTO 
	Character_tblMaster (OwnerAccount, IsTemp) 
	VALUES(@owner, @isTemp)
	
	IF @@ERROR <> 0
	 BEGIN
		-- Rollback the transaction
--		ROLLBACK

		-- Raise an error and return
		SET @resultCode = -1
		RETURN
	 END

	-- set starting properties
	DECLARE @charID int = -1
	SET @charID = @@IDENTITY
	SET @characterId = @charID
		
	-- float props
	EXEC Character_UpdateOrInsertFloatProperties @floatProperties, @charID

	IF @@ERROR <> 0
	 BEGIN
		-- Rollback the transaction
--		ROLLBACK

		-- Raise an error and return
		SET @resultCode = 0
		RETURN
	 END
	 
	-- int props
	EXEC Character_UpdateOrInsertIntProperties @intProperties, @charID
	
		IF @@ERROR <> 0
	 BEGIN
		-- Rollback the transaction
--		ROLLBACK

		-- Raise an error and return
		SET @resultCode = 0
		RETURN
	 END
	
	-- string props
	EXEC Character_UpdateOrInsertStringProperties @stringProperties, @charID
	
		IF @@ERROR <> 0
	 BEGIN
		-- Rollback the transaction
--		ROLLBACK

		-- Raise an error and return
		SET @resultCode = 0
		RETURN
	 END
	 
	-- long props
	EXEC Character_UpdateOrInsertLongProperties @longProperties, @charID	
	
		IF @@ERROR <> 0
	 BEGIN
		-- Rollback the transaction
--		ROLLBACK

		-- Raise an error and return
		SET @resultCode = 0
		RETURN
	 END
	 
	 -- stats
	EXEC Character_UpdateOrInsertStats @stats, @charID	
	
		IF @@ERROR <> 0
	 BEGIN
		-- Rollback the transaction
--		ROLLBACK

		-- Raise an error and return
		SET @resultCode = 0
		RETURN
	 END
	
	-- Everything went well, so
--	COMMIT
	
	SET @resultCode = 1	
END
GO
/****** Object:  StoredProcedure [dbo].[Character_Delete]    Script Date: 12/22/2015 10:42:02 PM ******/
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO
-- =============================================
-- Author:		<Author,,Name>
-- Create date: <Create Date,,>
-- Description:	<Description,,>
-- =============================================
CREATE PROCEDURE [dbo].[Character_Delete]
	-- Add the parameters for the stored procedure here
@owner uniqueidentifier,
@characterID int,
@permaPurge bit,
@numAffected int out,
@deleteReason nvarchar(max)
AS
BEGIN
	-- SET NOCOUNT ON added to prevent extra result sets from
	-- interfering with SELECT statements.
	SET NOCOUNT ON;
--	BEGIN TRANSACTION
    -- Insert statements for procedure here
	Update Character_tblMaster 
		SET 
			Deleted = 1,
			Active = 0
		WHERE
			OwnerAccount = @owner AND 
			ID = @characterID
		SET 
			@numAffected = @@ROWCOUNT	
	IF (@@ERROR <> 0) 
	BEGIN	
--	ROLLBACK 
	RETURN 
	END
		
	IF @permaPurge = 1	
		BEGIN	
			DELETE FROM Character_tblMaster WHERE ID = @characterID
			IF (@@ERROR <> 0) BEGIN 
--			ROLLBACK 
			SET @numAffected = -1 RETURN END		
		 
			DELETE FROM Character_tblPropertiesFloat WHERE OwnerID = @characterID 		
			IF (@@ERROR <> 0) BEGIN 
--			ROLLBACK 
			SET @numAffected = -1 RETURN END		
			
			DELETE FROM Character_tblPropertiesInt WHERE OwnerID = @characterID 
			IF (@@ERROR <> 0) BEGIN 
--			ROLLBACK 
			SET @numAffected = -1 RETURN END		
			
			DELETE FROM Character_tblPropertiesLong WHERE OwnerID = @characterID 
			IF (@@ERROR <> 0) BEGIN 
--			ROLLBACK 
			SET @numAffected = -1 RETURN END		
			
			DELETE FROM Character_tblPropertiesString WHERE OwnerID = @characterID 			
			IF (@@ERROR <> 0) BEGIN 
--			ROLLBACK 
			SET @numAffected = -1 RETURN END		
			
			DELETE FROM Character_tblStats WHERE OwnerID = @characterID 			
			IF (@@ERROR <> 0) BEGIN 
--			ROLLBACK 
			SET @numAffected = -1 RETURN END		
		END
	
	INSERT INTO Character_tblServiceLog (Note, EntryType, Account, CharacterID) 
		VALUES (@deleteReason, N'Delete', @owner, @characterID)
	IF (@@ERROR <> 0 OR @@ROWCOUNT != 1) BEGIN 
--	ROLLBACK	
	SET @numAffected = -2 RETURN END		
	 	
--	COMMIT TRANSACTION		

END
GO
/****** Object:  StoredProcedure [dbo].[Character_DeleteAllTempChars]    Script Date: 12/22/2015 10:42:02 PM ******/
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO
-- =============================================
-- Author:		<Author,,Name>
-- Create date: <Create Date,,>
-- Description:	<Description,,>
-- =============================================
CREATE PROCEDURE [dbo].[Character_DeleteAllTempChars]
	-- Add the parameters for the stored procedure here
@numAffected int out
AS
BEGIN

DELETE FROM
Character_tblPropertiesFloat WHERE
OwnerID IN (Select ID FROM Character_tblMaster WHERE IsTemp =1)

DELETE FROM
Character_tblPropertiesInt WHERE
OwnerID IN (Select ID FROM Character_tblMaster WHERE IsTemp =1)

DELETE FROM
Character_tblPropertiesLong WHERE
OwnerID IN (Select ID FROM Character_tblMaster WHERE IsTemp =1)

DELETE FROM
Character_tblPropertiesString WHERE
OwnerID IN (Select ID FROM Character_tblMaster WHERE IsTemp =1)

DELETE FROM
Character_tblStats WHERE
OwnerID IN (Select ID FROM Character_tblMaster WHERE IsTemp =1)

DELETE FROM
Character_tblMaster
WHERE IsTemp =1

SET @numAffected = @@ROWCOUNT
		
END
GO
/****** Object:  StoredProcedure [dbo].[Character_DeleteFloatProperties]    Script Date: 12/22/2015 10:42:02 PM ******/
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO
CREATE PROCEDURE [dbo].[Character_DeleteFloatProperties]

@InputTable PropertyFloat READONLY,
@charID int
AS 
BEGIN
SET NOCOUNT ON; 

MERGE Character_tblPropertiesFloat AS t USING @InputTable AS s ON 
	@charID = t.OwnerID
	AND s.PropertyId = t.PropertyID
WHEN MATCHED THEN 
	DELETE;

END;
GO
/****** Object:  StoredProcedure [dbo].[Character_DeleteIntProperties]    Script Date: 12/22/2015 10:42:02 PM ******/
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO
CREATE PROCEDURE [dbo].[Character_DeleteIntProperties]

@InputTable PropertyInt READONLY,
@charID int
AS 
BEGIN
SET NOCOUNT ON; 

MERGE Character_tblPropertiesInt AS t USING @InputTable AS s ON 
	@charID = t.OwnerID
	AND s.PropertyId = t.PropertyID
WHEN MATCHED THEN 
	DELETE;

END;
GO
/****** Object:  StoredProcedure [dbo].[Character_DeleteLongProperties]    Script Date: 12/22/2015 10:42:02 PM ******/
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO
CREATE PROCEDURE [dbo].[Character_DeleteLongProperties]

@InputTable PropertyLong READONLY,
@charID int
AS 
BEGIN
SET NOCOUNT ON; 

MERGE Character_tblPropertiesLong AS t USING @InputTable AS s ON 
	@charID = t.OwnerID
	AND s.PropertyId = t.PropertyID
WHEN MATCHED THEN 
	DELETE;

END;
GO
/****** Object:  StoredProcedure [dbo].[Character_DeleteStats]    Script Date: 12/22/2015 10:42:02 PM ******/
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO
CREATE PROCEDURE [dbo].[Character_DeleteStats]

@InputTable Stat READONLY,
@charID int
AS 
BEGIN
SET NOCOUNT ON; 

MERGE Character_tblStats AS t USING @InputTable AS s ON 
	@charID = t.OwnerID
	AND s.StatId = t.StatID
WHEN MATCHED THEN 
	DELETE;

END;
GO
/****** Object:  StoredProcedure [dbo].[Character_DeleteStringProperties]    Script Date: 12/22/2015 10:42:02 PM ******/
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO
CREATE PROCEDURE [dbo].[Character_DeleteStringProperties]

@InputTable PropertyString READONLY,
@charID int
AS 
BEGIN
SET NOCOUNT ON; 

MERGE Character_tblPropertiesString AS t USING @InputTable AS s ON 
	@charID = t.OwnerID
	AND s.PropertyId = t.PropertyID
WHEN MATCHED THEN 
	DELETE;

END;
GO
/****** Object:  StoredProcedure [dbo].[Character_Get]    Script Date: 12/22/2015 10:42:02 PM ******/
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO
-- =============================================
-- Author:		<Author,,Name>
-- Create date: <Create Date,,>
-- Description:	<Description,,>
-- =============================================
CREATE PROCEDURE [dbo].[Character_Get]
	-- Add the parameters for the stored procedure here
@resultCode int out,
@user uniqueidentifier,
@character int,
@includeDeleted bit = 0
	
AS
BEGIN
	-- SET NOCOUNT ON added to prevent extra result sets from
	-- interfering with SELECT statements.
	SET NOCOUNT ON;

IF @includeDeleted = 1
	BEGIN
		SELECT 
			OwnerAccount,
			CreatedOn,
			Active,
			Deleted
		FROM
			Character_tblMaster
		WHERE
			OwnerAccount = @user 
	END
ELSE
	BEGIN
	
	IF @user = '{00000000-0000-0000-0000-000000000000}'
		BEGIN
			SELECT 
					OwnerAccount,
					CreatedOn,
					Active,
					Deleted
				FROM
					Character_tblMaster
				WHERE
					-- if we passed in Guid.Empty, don't check against the owner account. assume the system is asking for the character, not the player
					-- OwnerAccount = @user AND
					ID = @character
					AND Deleted = 0
					AND Active = 1
		END
	ELSE
		BEGIN
			SELECT 
				OwnerAccount,
				CreatedOn,
				Active,
				Deleted
			FROM
				Character_tblMaster
			WHERE
				OwnerAccount = @user 
				AND ID = @character
				AND Deleted = 0
				AND Active = 1
		
		END
	END

	EXEC Character_GetPropertiesForCharacter @user, @character, @resultCode
	EXEC Character_GetStatsForCharacter @user, @character, @resultCode

END
GO
/****** Object:  StoredProcedure [dbo].[Character_GetAny]    Script Date: 12/22/2015 10:42:02 PM ******/
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO
-- =============================================
-- Author:		<Author,,Name>
-- Create date: <Create Date,,>
-- Description:	<Description,,>
-- =============================================
CREATE PROCEDURE [dbo].[Character_GetAny]
	-- Add the parameters for the stored procedure here
@resultCode int out,
@user uniqueidentifier
	
AS
BEGIN
	
	DECLARE @id int;
	SELECT TOP 1 @id = ID from Character_tblMaster WHERE OwnerAccount = @user AND Deleted = 0  AND Active = 1 ORDER BY ID DESC

	-- SET NOCOUNT ON added to prevent extra result sets from
	-- interfering with SELECT statements.
	SET NOCOUNT ON;
		SELECT
			OwnerAccount,
			CreatedOn,
			Active,
			Deleted
		FROM
			Character_tblMaster
		WHERE
			OwnerAccount = @user 
			AND Deleted = 0 
			AND ID = @id

	EXEC Character_GetPropertiesForCharacter @user, @id, @resultCode

	EXEC Character_GetStatsForCharacter @user, @id, @resultCode

END
GO
/****** Object:  StoredProcedure [dbo].[Character_GetPropertiesForCharacter]    Script Date: 12/22/2015 10:42:02 PM ******/
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO
-- =============================================
-- Author:		<Author,,Name>
-- Create date: <Create Date,,>
-- Pass in the property id of the "name" property, for instance and get all names of all characters that an account has
-- =============================================
CREATE PROCEDURE [dbo].[Character_GetPropertiesForCharacter] 
	-- Add the parameters for the stored procedure here
@user uniqueidentifier,
@character int,
@resultCode int out

AS
BEGIN
	-- SET NOCOUNT ON added to prevent extra result sets from
	-- interfering with SELECT statements.
	SET NOCOUNT ON;
    -- Insert statements for procedure here
IF @user != '{00000000-0000-0000-0000-000000000000}'

BEGIN
    SELECT ID from Character_tblMaster WHERE OwnerAccount = @user AND Deleted = 0  AND Active = 1
    if @@ROWCOUNT < 1
	 BEGIN
		Set @resultCode = 0
		RETURN
	 END
END    
    ----------------- FLOAT
	SELECT 
		Character_tblPropertiesFloat.ID, 
		Character_tblPropertiesFloat.PropertyID, 
		Character_tblPropertiesFloat.PropertyValue, 
		Character_tblPropertiesFloat.PropertyName
	FROM 
		Character_tblPropertiesFloat
	
	WHERE 
	Character_tblPropertiesFloat.OwnerID = @character
	
	----------------- Int
	SELECT 
		Character_tblPropertiesInt.ID, 
		Character_tblPropertiesInt.PropertyID, 
		Character_tblPropertiesInt.PropertyValue, 
		Character_tblPropertiesInt.PropertyName 
	
	FROM 
		Character_tblPropertiesInt
	WHERE 
	Character_tblPropertiesInt.OwnerID = @character
	
	----------------- Long
	SELECT 
		Character_tblPropertiesLong.ID, 
		Character_tblPropertiesLong.PropertyID, 
		Character_tblPropertiesLong.PropertyValue,
		Character_tblPropertiesLong.PropertyName
	
	FROM 
		Character_tblPropertiesLong
	WHERE 
	Character_tblPropertiesLong.OwnerID = @character		
	
	----------------- String
	SELECT 
		Character_tblPropertiesString.ID, 
		Character_tblPropertiesString.PropertyID, 
		Character_tblPropertiesString.PropertyValue,
		Character_tblPropertiesString.PropertyName
	FROM 
		Character_tblPropertiesString
	WHERE 
	Character_tblPropertiesString.OwnerID = @character
	

END
GO
/****** Object:  StoredProcedure [dbo].[Character_GetPropertiesFromAllCharacters]    Script Date: 12/22/2015 10:42:02 PM ******/
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO
-- =============================================
-- Author:		<Author,,Name>
-- Create date: <Create Date,,>
-- Pass in the property id of the "name" property, for instance and get all names of all characters that an account has
-- =============================================
CREATE PROCEDURE [dbo].[Character_GetPropertiesFromAllCharacters] 
	-- Add the parameters for the stored procedure here
@user uniqueidentifier,
@intProps IntArray READONLY,	
@stringProps IntArray READONLY,
@longProps IntArray READONLY,
@floatProps IntArray READONLY

AS
BEGIN
	-- SET NOCOUNT ON added to prevent extra result sets from
	-- interfering with SELECT statements.
	SET NOCOUNT ON;
    -- Insert statements for procedure here
    
    --WITH ToonIds (ID) AS (SELECT ID FROM Character_tblMaster WHERE OwnerAccount = @user AND Deleted = 0 AND Active = 1)
    
    ----------------- FLOAT
	SELECT 
		Character_tblMaster.ID,
		Character_tblPropertiesFloat.ID, 
		Character_tblPropertiesFloat.PropertyID, 
		Character_tblPropertiesFloat.PropertyValue,
		Character_tblPropertiesFloat.PropertyName
	
	FROM 
		Character_tblPropertiesFloat, 
		Character_tblMaster
	
	WHERE 
	Character_tblPropertiesFloat.OwnerID = Character_tblMaster.ID AND
	Character_tblPropertiesFloat.PropertyId in (SELECT Item FROM @floatProps) AND
	Character_tblPropertiesFloat.OwnerID in (SELECT ID FROM Character_tblMaster WHERE OwnerAccount = @user AND Deleted = 0 AND Active = 1)
	
	----------------- Int
	SELECT 
		Character_tblMaster.ID,
		Character_tblPropertiesInt.ID, 
		Character_tblPropertiesInt.PropertyID, 
		Character_tblPropertiesInt.PropertyValue,
		Character_tblPropertiesInt.PropertyName
	
	FROM 
		Character_tblPropertiesInt,
		Character_tblMaster
	
	WHERE 
	Character_tblPropertiesInt.OwnerID = Character_tblMaster.ID AND
	Character_tblPropertiesInt.PropertyId in (SELECT Item FROM @intProps) AND
	Character_tblPropertiesInt.OwnerID in (SELECT ID FROM Character_tblMaster WHERE OwnerAccount = @user AND Deleted = 0 AND Active = 1)
	
	----------------- Long
	SELECT 
		Character_tblMaster.ID,
		Character_tblPropertiesLong.ID, 
		Character_tblPropertiesLong.PropertyID, 
		Character_tblPropertiesLong.PropertyValue,
		Character_tblPropertiesLong.PropertyName
	
	FROM 
		Character_tblPropertiesLong,
		Character_tblMaster
	
	WHERE 
	Character_tblPropertiesLong.OwnerID = Character_tblMaster.ID AND
	Character_tblPropertiesLong.PropertyId in (SELECT Item FROM @longProps) AND
	Character_tblPropertiesLong.OwnerID in (SELECT ID FROM Character_tblMaster WHERE OwnerAccount = @user AND Deleted = 0 AND Active = 1)
		
	
	----------------- String
	SELECT 
		Character_tblMaster.ID,
		Character_tblPropertiesString.ID, 
		Character_tblPropertiesString.PropertyID, 
		Character_tblPropertiesString.PropertyValue,
		Character_tblPropertiesString.PropertyName
	
	FROM 
		Character_tblPropertiesString,
		Character_tblMaster
	
	WHERE 
	Character_tblPropertiesString.OwnerID = Character_tblMaster.ID AND
	Character_tblPropertiesString.PropertyId in (SELECT Item FROM @stringProps) AND
	Character_tblPropertiesString.OwnerID in (SELECT ID FROM Character_tblMaster WHERE OwnerAccount = @user AND Deleted = 0 AND Active = 1)
	

END
GO
/****** Object:  StoredProcedure [dbo].[Character_GetStatsForCharacter]    Script Date: 12/22/2015 10:42:02 PM ******/
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO
-- =============================================
-- Author:		<Author,,Name>
-- Create date: <Create Date,,>
-- Pass in the property id of the "name" property, for instance and get all names of all characters that an account has
-- =============================================
CREATE PROCEDURE [dbo].[Character_GetStatsForCharacter] 
	-- Add the parameters for the stored procedure here
@user uniqueidentifier,
@character int,
@resultCode int out

AS
BEGIN
	-- SET NOCOUNT ON added to prevent extra result sets from
	-- interfering with SELECT statements.
	SET NOCOUNT ON;
    -- Insert statements for procedure here
IF @user != '{00000000-0000-0000-0000-000000000000}'
BEGIN
    SELECT ID from Character_tblMaster WHERE OwnerAccount = @user AND Deleted = 0  AND Active = 1
    if @@ROWCOUNT < 1
	 BEGIN
		Set @resultCode = 0
		RETURN
	 END
END    
    ----------------- FLOAT
	SELECT 
		Character_tblStats.ID, 
		Character_tblStats.StatID, 
		Character_tblStats.CurrentValue,
		Character_tblStats.MaxValue, 
		Character_tblStats.MinValue 
	FROM 
		Character_tblStats		
	
	WHERE 
	Character_tblStats.OwnerID = @character
	
	
	

END
GO
/****** Object:  StoredProcedure [dbo].[Character_Save]    Script Date: 12/22/2015 10:42:02 PM ******/
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO
-- =============================================
-- Author:		<Author,,Name>
-- Create date: <Create Date,,>
-- Description:	<Description,,>
-- =============================================
CREATE PROCEDURE [dbo].[Character_Save]
	-- Add the parameters for the stored procedure here
@resultCode int out,
@charID int,
@owner uniqueidentifier,
@enforceUniqueName bit,
@namePropertyId int,
@characterName nvarchar(max),
@intProperties PropertyInt READONLY,
@floatProperties PropertyFloat READONLY,
@longProperties PropertyLong READONLY,
@stringProperties PropertyString READONLY,
@stats Stat READONLY
	
AS
BEGIN
	-- SET NOCOUNT ON added to prevent extra result sets from
	-- interfering with SELECT statements.
	SET NOCOUNT ON;

--BEGIN TRANSACTION

    -- Create the character
    
    -- check for unique character name
IF @enforceUniqueName = 1
BEGIN 
    SELECT 
		Character_tblMaster.OwnerAccount 
	from 
		Character_tblMaster JOIN Character_tblPropertiesString On Character_tblMaster.ID = Character_tblPropertiesString.OwnerID
    WHERE 
		Character_tblPropertiesString.PropertyID = @namePropertyId AND  
		Character_tblPropertiesString.PropertyValue = @characterName
		AND Character_tblMaster.Deleted = 0
		
    if @@ROWCOUNT > 0
	 BEGIN
		--ROLLBACK
		Set @resultCode = -2
		RETURN
	 END
	 
END

	-- char exists?
	SELECT * FROM Character_tblMaster WHERE ID = @charID
	if @@ROWCOUNT < 1
	 BEGIN
		--ROLLBACK
		Set @resultCode = -9
		RETURN
	 END
		
	-- float props
	EXEC Character_UpdateOrInsertFloatProperties @floatProperties, @charID

	IF @@ERROR <> 0
	 BEGIN
		-- Rollback the transaction
--		ROLLBACK

		-- Raise an error and return
		SET @resultCode = 0
		RETURN
	 END
	 
	-- int props
	EXEC Character_UpdateOrInsertIntProperties @intProperties, @charID
	
		IF @@ERROR <> 0
	 BEGIN
		-- Rollback the transaction
--		ROLLBACK

		-- Raise an error and return
		SET @resultCode = 0
		RETURN
	 END
	
	-- string props
	EXEC Character_UpdateOrInsertStringProperties @stringProperties, @charID
	
		IF @@ERROR <> 0
	 BEGIN
		-- Rollback the transaction
--		ROLLBACK

		-- Raise an error and return
		SET @resultCode = 0
		RETURN
	 END
	 
	-- long props
	EXEC Character_UpdateOrInsertLongProperties @longProperties, @charID	
	
		IF @@ERROR <> 0
	 BEGIN
		-- Rollback the transaction
--		ROLLBACK

		-- Raise an error and return
		SET @resultCode = 0
		RETURN
	 END
	 
	 -- stats
	EXEC Character_UpdateOrInsertStats @stats, @charID	
	
		IF @@ERROR <> 0
	 BEGIN
		-- Rollback the transaction
--		ROLLBACK

		-- Raise an error and return
		SET @resultCode = 0
		RETURN
	 END
	
	-- Everything went well, so
--	COMMIT
	
	SET @resultCode = 1	
END
GO
/****** Object:  StoredProcedure [dbo].[Character_UpdateOrInsertFloatProperties]    Script Date: 12/22/2015 10:42:02 PM ******/
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO
CREATE PROCEDURE [dbo].[Character_UpdateOrInsertFloatProperties]

@InputTable PropertyFloat READONLY,
@charID int
AS 
BEGIN
SET NOCOUNT ON; 

MERGE Character_tblPropertiesFloat AS t USING @InputTable AS s ON 
	@charID = t.OwnerID
	AND s.PropertyId = t.PropertyID
WHEN MATCHED THEN 
	UPDATE SET  t.PropertyValue = s.PropertyValue
WHEN NOT MATCHED THEN 
	INSERT (OwnerID, PropertyID, PropertyValue,PropertyName) 
	VALUES (@charID, s.PropertyId, s.PropertyValue, s.PropertyName)
WHEN NOT MATCHED BY SOURCE THEN
	DELETE;

END;
GO
/****** Object:  StoredProcedure [dbo].[Character_UpdateOrInsertIntProperties]    Script Date: 12/22/2015 10:42:02 PM ******/
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO
CREATE PROCEDURE [dbo].[Character_UpdateOrInsertIntProperties]

@InputTable PropertyInt READONLY,
@charID int
AS 
BEGIN
SET NOCOUNT ON; 

MERGE Character_tblPropertiesInt AS t USING @InputTable AS s ON 
	@charID = t.OwnerID
	AND s.PropertyId = t.PropertyID
WHEN MATCHED THEN 
	UPDATE SET  t.PropertyValue = s.PropertyValue
WHEN NOT MATCHED THEN 
	INSERT (OwnerID, PropertyID, PropertyValue,PropertyName) 
	VALUES (@charID, s.PropertyId, s.PropertyValue, s.PropertyName)
WHEN NOT MATCHED BY SOURCE THEN
	DELETE;

END;
GO
/****** Object:  StoredProcedure [dbo].[Character_UpdateOrInsertLongProperties]    Script Date: 12/22/2015 10:42:02 PM ******/
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO
CREATE PROCEDURE [dbo].[Character_UpdateOrInsertLongProperties]

@InputTable PropertyLong READONLY,
@charID int
AS 
BEGIN
SET NOCOUNT ON; 

MERGE Character_tblPropertiesLong AS t USING @InputTable AS s ON 
	@charID = t.OwnerID
	AND s.PropertyId = t.PropertyID
WHEN MATCHED THEN 
	UPDATE SET  t.PropertyValue = s.PropertyValue
WHEN NOT MATCHED THEN 
	INSERT (OwnerID, PropertyID, PropertyValue,PropertyName) 
	VALUES (@charID, s.PropertyId, s.PropertyValue, s.PropertyName)
WHEN NOT MATCHED BY SOURCE THEN
	DELETE;

END;
GO
/****** Object:  StoredProcedure [dbo].[Character_UpdateOrInsertStats]    Script Date: 12/22/2015 10:42:02 PM ******/
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO
CREATE PROCEDURE [dbo].[Character_UpdateOrInsertStats]

@InputTable Stat READONLY,
@charID int
AS 
BEGIN
SET NOCOUNT ON; 

MERGE Character_tblStats AS t USING @InputTable AS s ON 
	@charID = t.OwnerID
	AND s.StatId = t.StatID
WHEN MATCHED THEN 
	UPDATE SET  
		t.CurrentValue = s.StatValue,
		t.MaxValue = s.StatMaxValue,
		t.MinValue = s.StatMinValue
WHEN NOT MATCHED THEN 
	INSERT (OwnerID, StatID, CurrentValue, MaxValue, MinValue) 
	VALUES (@charID, s.StatId, s.StatValue, s.StatMaxValue, s.StatMinValue)
WHEN NOT MATCHED BY SOURCE THEN
	DELETE;

END;
GO
/****** Object:  StoredProcedure [dbo].[Character_UpdateOrInsertStringProperties]    Script Date: 12/22/2015 10:42:02 PM ******/
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO
CREATE PROCEDURE [dbo].[Character_UpdateOrInsertStringProperties]

@InputTable PropertyString READONLY,
@charID int
AS 
BEGIN
SET NOCOUNT ON; 

MERGE Character_tblPropertiesString AS t 
USING @InputTable AS s 
ON 
	@charID = t.OwnerID
	AND s.PropertyId = t.PropertyID
	
WHEN MATCHED THEN 
	UPDATE SET  t.PropertyValue = s.PropertyValue
	
WHEN NOT MATCHED BY TARGET THEN 
	INSERT (OwnerID, PropertyID, PropertyValue,PropertyName) 
	VALUES (@charID, s.PropertyId, s.PropertyValue, s.PropertyName)
WHEN NOT MATCHED BY SOURCE THEN
	DELETE;

END;
GO
/****** Object:  StoredProcedure [dbo].[GetTurnStatus]    Script Date: 12/22/2015 10:42:02 PM ******/
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO
-- =============================================
-- Author:		<Author,,Name>
-- Create date: <Create Date,,>
-- Description:	<Description,,>
-- =============================================
create PROCEDURE [dbo].[GetTurnStatus]
	-- Add the parameters for the stored procedure here
	@owner uniqueidentifier,
	@characterID int
	
AS
BEGIN
	-- SET NOCOUNT ON added to prevent extra result sets from
	-- interfering with SELECT statements.
	SET NOCOUNT ON;

    -- Insert statements for procedure here
	SELECT TurnsLeft, MaxTurns, LastTurnGrant, TurnGrantInterval FROM Character_tblMaster
	WHERE ID = @characterID AND OwnerAccount = @owner 
END

GO
/****** Object:  StoredProcedure [dbo].[Items_BatchUpdateOrInsert]    Script Date: 12/22/2015 10:42:02 PM ******/
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO




-- =============================================
-- Author:		<Author,,Name>
-- Create date: <Create Date,,>
-- Description:	<Description,,>
-- =============================================
CREATE PROCEDURE [dbo].[Items_BatchUpdateOrInsert]
	-- Add the parameters for the stored procedure here
@resultCode int out,
@Items ItemsData READONLY,
@ItemPropertyInts ItemPropertyInt READONLY,
@ItemPropertyFloats ItemPropertyFloat READONLY,
@ItemPropertyLongs ItemPropertyLong READONLY,
@ItemPropertyStrings ItemPropertyString READONLY,
@ItemPropertyStats ItemStat READONLY,
@DeleteItems ItemDeleteData READONLY
	
AS
BEGIN
	-- SET NOCOUNT ON added to prevent extra result sets from
	-- interfering with SELECT statements.
	SET NOCOUNT ON;

BEGIN TRANSACTION

-- delete
DECLARE @itemid uniqueidentifier;
DECLARE @permapurge tinyint;
DECLARE @account uniqueidentifier;
DECLARE @deletereason nvarchar(max);
DECLARE @numaffect int;

DECLARE DelCursor CURSOR FAST_FORWARD FOR SELECT ItemID, PermaPurge, Account, DeleteReason FROM @DeleteItems;
OPEN DelCursor;
FETCH NEXT FROM DelCursor INTO @itemid, @permapurge, @account, @deletereason;

WHILE @@FETCH_STATUS= 0
BEGIN
	EXEC Items_Delete @itemid, @permapurge, @account, @numaffect output, @deletereason;
	FETCH NEXT FROM DelCursor INTO @itemid, @permapurge, @account, @deletereason;
END

CLOSE DelCursor
DEALLOCATE DelCursor

IF @@ERROR <> 0
BEGIN
-- Rollback the transaction
		ROLLBACK

-- Raise an error and return
SET @resultCode = -8
RETURN
END


MERGE Items_tblMaster AS t USING @Items AS s ON 
	t.ID = s.[UID]
	AND (t.LoadedBy = s.Owner OR t.LoadedBy IS NULL OR t.LoadedBy = '')
WHEN MATCHED THEN 
	UPDATE SET  
	t.Template = s.Template,
	t.GOT = s.GOT,
	t.CreatedOn = s.CreatedOn,
	t.LoadedBy = s.[Owner],
	t.BinData = s.BinData,
	t.TypeHash = s.TypeHash,
	t.Context = s.Context,
	t.[Owner] = s.ObjectOwner,
	t.StackCount = s.StackCount

WHEN NOT MATCHED THEN 
	INSERT (ID, Template, Deleted, CreatedOn, GOT, LoadedBy, Context, TypeHash, BinData, StackCount, [Owner]) 
	VALUES(s.UID, s.Template, 0, s.CreatedOn, s.GOT, s.Owner, s.Context, s.TypeHash, s.BinData, s.StackCount, s.ObjectOwner );

	IF @@ERROR <> 0
	 BEGIN
		-- Rollback the transaction
		ROLLBACK

		-- Raise an error and return
		SET @resultCode = -9
		RETURN
	 END

	-- float props
	MERGE Items_tblPropertiesFloat AS t USING @ItemPropertyFloats AS s ON 
	s.PropertyOwner = t.OwnerID
	AND s.PropertyId = t.PropertyID
WHEN MATCHED THEN 
	UPDATE SET  t.PropertyValue = s.PropertyValue
WHEN NOT MATCHED THEN 
	INSERT (OwnerID, PropertyID, PropertyValue,PropertyName) 
	VALUES (s.PropertyOwner, s.PropertyId, s.PropertyValue, s.PropertyName);


	IF @@ERROR <> 0
	 BEGIN
		-- Rollback the transaction
		ROLLBACK

		-- Raise an error and return
		SET @resultCode = 0
		RETURN
	 END
	 
	-- int props
MERGE Items_tblPropertiesInt AS t USING @ItemPropertyInts AS s ON 
	s.PropertyOwner = t.OwnerID
	AND s.PropertyId = t.PropertyID
WHEN MATCHED THEN 
	UPDATE SET  t.PropertyValue = s.PropertyValue
WHEN NOT MATCHED THEN 
	INSERT (OwnerID, PropertyID, PropertyValue,PropertyName) 
	VALUES (s.PropertyOwner, s.PropertyId, s.PropertyValue, s.PropertyName);

	
		IF @@ERROR <> 0
	 BEGIN
		-- Rollback the transaction
		ROLLBACK

		-- Raise an error and return
		SET @resultCode = 0
		RETURN
	 END
	
	-- string props
MERGE Items_tblPropertiesString AS t 
USING @ItemPropertyStrings AS s 
ON 
	s.PropertyOwner = t.OwnerID
	AND s.PropertyId = t.PropertyID
	
WHEN MATCHED THEN 
	UPDATE SET  t.PropertyValue = s.PropertyValue
	
WHEN NOT MATCHED BY TARGET THEN 
	INSERT (OwnerID, PropertyID, PropertyValue,PropertyName) 
	VALUES (s.PropertyOwner, s.PropertyId, s.PropertyValue, s.PropertyName);

	
		IF @@ERROR <> 0
	 BEGIN
		-- Rollback the transaction
		ROLLBACK

		-- Raise an error and return
		SET @resultCode = 0
		RETURN
	 END
	 
	-- long props
MERGE Items_tblPropertiesLong AS t USING @ItemPropertyLongs AS s ON 
	s.PropertyOwner = t.OwnerID
	AND s.PropertyId = t.PropertyID
WHEN MATCHED THEN 
	UPDATE SET  t.PropertyValue = s.PropertyValue
WHEN NOT MATCHED THEN 
	INSERT (OwnerID, PropertyID, PropertyValue,PropertyName) 
	VALUES (s.PropertyOwner, s.PropertyId, s.PropertyValue, s.PropertyName);
	
		IF @@ERROR <> 0
	 BEGIN
		-- Rollback the transaction
		ROLLBACK

		-- Raise an error and return
		SET @resultCode = 0
		RETURN
	 END
	 
	 -- stats
MERGE Items_tblStats AS t USING @ItemPropertyStats AS s ON 
	s.StatOwner = t.OwnerID
	AND s.StatId = t.StatID
WHEN MATCHED THEN 
	UPDATE SET  
		t.CurrentValue = s.StatValue,
		t.MaxValue = s.StatMaxValue,
		t.MinValue = s.StatMinValue
WHEN NOT MATCHED THEN 
	INSERT (OwnerID, StatID, CurrentValue, MaxValue, MinValue) 
	VALUES (s.StatOwner, s.StatId, s.StatValue, s.StatMaxValue, s.StatMinValue);

	
		IF @@ERROR <> 0
	 BEGIN
		-- Rollback the transaction
		ROLLBACK

		-- Raise an error and return
		SET @resultCode = 0
		RETURN
	 END
	
	-- Everything went well, so
	COMMIT
	
	SET @resultCode = 1	
END
GO
/****** Object:  StoredProcedure [dbo].[Items_Create]    Script Date: 12/22/2015 10:42:02 PM ******/
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO






-- =============================================
-- Author:		<Author,,Name>
-- Create date: <Create Date,,>
-- Description:	<Description,,>
-- =============================================
CREATE PROCEDURE [dbo].[Items_Create]
	-- Add the parameters for the stored procedure here
@resultCode int out,
@template nvarchar(64), 
@createdOn datetime,
@GOT int,
@UID uniqueidentifier,
@intProperties ItemPropertyInt READONLY,
@floatProperties ItemPropertyFloat READONLY,
@longProperties ItemPropertyLong READONLY,
@stringProperties ItemPropertyString READONLY,
@stats ItemStat READONLY,
@owner nvarchar(64) = NULL,
@context uniqueidentifier,
@typeHash bigint,
@binData varbinary(max),
@stackCount int,
@objectOwner uniqueidentifier

	
AS
BEGIN
	-- SET NOCOUNT ON added to prevent extra result sets from
	-- interfering with SELECT statements.
	SET NOCOUNT ON;

--BEGIN TRANSACTION
    
    DECLARE @GUID uniqueidentifier
	
	INSERT INTO 
	Items_tblMaster (ID, Template, Deleted, CreatedOn, GOT, LoadedBy, Context, TypeHash, BinData, StackCount, [Owner]) 
	VALUES(@UID, @template, 0, @createdOn, @GOT, @owner, @context, @typeHash, @binData, @stackCount, @objectOwner)
	
	IF @@ERROR <> 0
	 BEGIN
		-- Rollback the transaction
--		ROLLBACK

		-- Raise an error and return
		SET @resultCode = -1
		RETURN
	 END

	-- set starting properties
	DECLARE @itemID uniqueidentifier
	SET @itemID = @UID
		
	-- float props
	EXEC Items_UpdateOrInsertFloatProperties @floatProperties, @itemID

	IF @@ERROR <> 0
	 BEGIN
		-- Rollback the transaction
--		ROLLBACK

		-- Raise an error and return
		SET @resultCode = 0
		RETURN
	 END
	 
	-- int props
	EXEC Items_UpdateOrInsertIntProperties @intProperties, @itemID
	
		IF @@ERROR <> 0
	 BEGIN
		-- Rollback the transaction
--		ROLLBACK

		-- Raise an error and return
		SET @resultCode = 0
		RETURN
	 END
	
	-- string props
	EXEC Items_UpdateOrInsertStringProperties @stringProperties, @itemID
	
		IF @@ERROR <> 0
	 BEGIN
		-- Rollback the transaction
--		ROLLBACK

		-- Raise an error and return
		SET @resultCode = 0
		RETURN
	 END
	 
	-- long props
	EXEC Items_UpdateOrInsertLongProperties @longProperties, @itemID	
	
		IF @@ERROR <> 0
	 BEGIN
		-- Rollback the transaction
--		ROLLBACK

		-- Raise an error and return
		SET @resultCode = 0
		RETURN
	 END
	 
	 -- stats
	EXEC Items_UpdateOrInsertStats @stats, @itemID	
	
		IF @@ERROR <> 0
	 BEGIN
		-- Rollback the transaction
--		ROLLBACK

		-- Raise an error and return
		SET @resultCode = 0
		RETURN
	 END
	
	-- Everything went well, so
--	COMMIT
	
	SET @resultCode = 1	
END
GO
/****** Object:  StoredProcedure [dbo].[Items_Delete]    Script Date: 12/22/2015 10:42:02 PM ******/
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO

-- =============================================
-- Author:		<Author,,Name>
-- Create date: <Create Date,,>
-- Description:	<Description,,>
-- =============================================
CREATE PROCEDURE [dbo].[Items_Delete]
	-- Add the parameters for the stored procedure here
@ItemID uniqueidentifier,
@permaPurge bit,
@account uniqueidentifier,
@numAffected int out,
@deleteReason nvarchar(max)

AS
BEGIN
	-- SET NOCOUNT ON added to prevent extra result sets from
	-- interfering with SELECT statements.
	SET NOCOUNT ON;
--	BEGIN TRANSACTION
    -- Insert statements for procedure here
	Update Items_tblMaster 
		SET 
			Deleted = 1
		WHERE
			ID = @ItemID
		SET 
			@numAffected = @@ROWCOUNT	
	IF (@@ERROR <> 0) 
	BEGIN	
--	ROLLBACK 
	RETURN 
	END
		
	IF @permaPurge = 1	
		BEGIN	
			DELETE FROM Items_tblMaster WHERE ID = @ItemID
			IF (@@ERROR <> 0) BEGIN 
--			ROLLBACK 
			SET @numAffected = -1 RETURN END		
		 
			DELETE FROM Items_tblPropertiesFloat WHERE OwnerID = @ItemID 		
			IF (@@ERROR <> 0) BEGIN 
--			ROLLBACK 
			SET @numAffected = -1 RETURN END		
			
			DELETE FROM Items_tblPropertiesInt WHERE OwnerID = @ItemID 
			IF (@@ERROR <> 0) BEGIN 
--			ROLLBACK 
			SET @numAffected = -1 RETURN END		
			
			DELETE FROM Items_tblPropertiesLong WHERE OwnerID = @ItemID 
			IF (@@ERROR <> 0) BEGIN 
--			ROLLBACK 
			SET @numAffected = -1 RETURN END		
			
			DELETE FROM Items_tblPropertiesString WHERE OwnerID = @ItemID 			
			IF (@@ERROR <> 0) BEGIN 
--			ROLLBACK 
			SET @numAffected = -1 RETURN END		
			
			DELETE FROM Items_tblStats WHERE OwnerID = @ItemID 			
			IF (@@ERROR <> 0) BEGIN 
--			ROLLBACK 
			SET @numAffected = -1 RETURN END		
		END
	
	INSERT INTO Items_tblServiceLog (Note, EntryType, Account, ItemsID) 
		VALUES (@deleteReason, N'Item Delete', @account, @ItemID)
	IF (@@ERROR <> 0 OR @@ROWCOUNT != 1) BEGIN 
--	ROLLBACK	
	SET @numAffected = -2 RETURN END		
	 	
--	COMMIT TRANSACTION		

END
GO
/****** Object:  StoredProcedure [dbo].[Items_DeleteFloatProperties]    Script Date: 12/22/2015 10:42:02 PM ******/
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO


CREATE PROCEDURE [dbo].[Items_DeleteFloatProperties]

@InputTable ItemPropertyFloat READONLY,
@itemID uniqueidentifier
AS 
BEGIN
SET NOCOUNT ON; 

MERGE Items_tblPropertiesFloat AS t USING @InputTable AS s ON 
	@itemID = t.OwnerID
	AND s.PropertyId = t.PropertyID
WHEN MATCHED THEN 
	DELETE;

END;
GO
/****** Object:  StoredProcedure [dbo].[Items_DeleteIntProperties]    Script Date: 12/22/2015 10:42:02 PM ******/
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO


CREATE PROCEDURE [dbo].[Items_DeleteIntProperties]

@InputTable ItemPropertyFloat READONLY,
@itemID uniqueidentifier
AS 
BEGIN
SET NOCOUNT ON; 

MERGE Items_tblPropertiesInt AS t USING @InputTable AS s ON 
	@itemID = t.OwnerID
	AND s.PropertyId = t.PropertyID
WHEN MATCHED THEN 
	DELETE;

END;
GO
/****** Object:  StoredProcedure [dbo].[Items_DeleteLongProperties]    Script Date: 12/22/2015 10:42:02 PM ******/
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO


CREATE PROCEDURE [dbo].[Items_DeleteLongProperties]

@InputTable ItemPropertyFloat READONLY,
@itemID uniqueidentifier
AS 
BEGIN
SET NOCOUNT ON; 

MERGE Items_tblPropertiesLong AS t USING @InputTable AS s ON 
	@itemID = t.OwnerID
	AND s.PropertyId = t.PropertyID
WHEN MATCHED THEN 
	DELETE;

END;
GO
/****** Object:  StoredProcedure [dbo].[Items_DeleteStats]    Script Date: 12/22/2015 10:42:02 PM ******/
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO


CREATE PROCEDURE [dbo].[Items_DeleteStats]

@InputTable ItemStat READONLY,
@itemID uniqueidentifier
AS 
BEGIN
SET NOCOUNT ON; 

MERGE Items_tblStats AS t USING @InputTable AS s ON 
	@itemID = t.OwnerID
	AND s.StatId = t.StatID
WHEN MATCHED THEN 
	DELETE;

END;
GO
/****** Object:  StoredProcedure [dbo].[Items_DeleteStringProperties]    Script Date: 12/22/2015 10:42:02 PM ******/
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO


CREATE PROCEDURE [dbo].[Items_DeleteStringProperties]

@InputTable ItemPropertyFloat READONLY,
@itemID uniqueidentifier
AS 
BEGIN
SET NOCOUNT ON; 

MERGE Items_tblPropertiesString AS t USING @InputTable AS s ON 
	@itemID = t.OwnerID
	AND s.PropertyId = t.PropertyID
WHEN MATCHED THEN 
	DELETE;

END;
GO
/****** Object:  StoredProcedure [dbo].[Items_Get]    Script Date: 12/22/2015 10:42:02 PM ******/
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO






-- =============================================
-- Author:		<Author,,Name>
-- Create date: <Create Date,,>
-- Description:	<Description,,>
-- =============================================
CREATE PROCEDURE [dbo].[Items_Get]
	-- Add the parameters for the stored procedure here
@resultCode int out,
@itemID uniqueidentifier,
@includeDeleted bit = 0,
@lockedBy nvarchar(64)
AS
BEGIN
	-- SET NOCOUNT ON added to prevent extra result sets from
	-- interfering with SELECT statements.
	SET NOCOUNT ON;


IF @includeDeleted = 1 -- INCLUDE DELETED ITEMS
BEGIN
		-- READ ONLY
		IF (@lockedBy IS NULL OR @lockedBy = '') 
		BEGIN
				SELECT 
					ID, 
					Template,
					Deleted,
					GOT,
					CreatedOn,
					LoadedBy,
					Context,
					TypeHash,
					BinData,
					StackCount,
					[Owner]
				FROM
					Items_tblMaster
				WHERE
					ID = @itemID	
		END
		ELSE -- READ/WRITE
		BEGIN									
				BEGIN TRANSACTION
				SELECT 
					ID, 
					Template,
					Deleted,
					GOT,
					CreatedOn,
					LoadedBy,
					Context,
					TypeHash,
					BinData,
					StackCount,
					[Owner]
					FROM
						Items_tblMaster
					WHERE
						ID = @itemID
						AND (LoadedBy = '' OR LoadedBy IS NULL OR LoadedBy = @lockedBy)					
				IF @@ROWCOUNT > 0
				BEGIN
					UPDATE Items_tblMaster SET LoadedBy = @lockedBy 
					WHERE ID = @itemID AND (LoadedBy = '' OR LoadedBy IS NULL OR LoadedBy = @lockedBy)
					
					IF @@ROWCOUNT = 0 
					BEGIN
						ROLLBACK
						SET @resultCode = 0; -- Can't acquire lock on item
						RETURN
					END										
					ELSE 
					BEGIN
						COMMIT
					END
				END	
				ELSE
				BEGIN					
					ROLLBACK
					RETURN
				END					
		END
END
			
ELSE -- DO NOT INCLUDE DELETED ITEMS
BEGIN
		-- READ ONLY
		IF @lockedBy IS NULL OR @lockedBy = ''
			BEGIN
					SELECT 
					ID, 
					Template,
					Deleted,
					GOT,
					CreatedOn,
					LoadedBy,
					Context,
					TypeHash,
					BinData,
					StackCount,
					[Owner]
				FROM
					Items_tblMaster
				WHERE
					ID = @itemID
					AND Deleted = 0
			END
		ELSE -- READ/WRITE
		BEGIN
			
			BEGIN TRANSACTION
				SELECT 
					ID, 
					Template,
					Deleted,
					GOT,
					CreatedOn,
					LoadedBy,
					Context,
					TypeHash, 
					BinData,
					StackCount,
					[Owner]
					FROM
						Items_tblMaster
					WHERE
						ID = @itemID
						AND Deleted = 0 
						AND (LoadedBy = '' OR LoadedBy IS NULL OR LoadedBy = @lockedBy)
					
				IF @@ROWCOUNT > 0
				BEGIN
					UPDATE Items_tblMaster SET LoadedBy = @lockedBy 
					WHERE ID = @itemID AND (LoadedBy = '' OR LoadedBy IS NULL OR LoadedBy = @lockedBy)
					
					IF @@ROWCOUNT = 0 
					BEGIN
						ROLLBACK
						SET @resultCode = 0; -- Can't acquire lock on item
						RETURN
					END										
					ELSE 
					BEGIN
						COMMIT
					END
				END			
				ELSE
				BEGIN
					ROLLBACK
					RETURN
				END
		END
END 

	EXEC Items_GetPropertiesForItem @itemID, @resultCode
	EXEC Items_GetStatsForItem @itemID, @resultCode

END
GO
/****** Object:  StoredProcedure [dbo].[Items_GetBatch]    Script Date: 12/22/2015 10:42:02 PM ******/
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO


-- =============================================
-- Author:		<Author,,Name>
-- Create date: <Create Date,,>
-- Description:	<Description,,>
-- =============================================
CREATE PROCEDURE [dbo].[Items_GetBatch]
	-- Add the parameters for the stored procedure here
@resultCode int out,
@includeDeleted bit = 0,
@lockedBy nvarchar(64),
@itemIDs GuidArray READONLY
 
AS
BEGIN
	
	declare @ItemId uniqueidentifier
	declare @RowNum int

	declare ItemList cursor for
	select Item FROM @itemIDs
	OPEN ItemList
	FETCH NEXT FROM ItemList
	INTO @ItemId
	set @RowNum = 0 
	WHILE @@FETCH_STATUS = 0
	BEGIN
	  set @RowNum = @RowNum + 1
		EXEC Items_Get @resultCode, @itemID, @includeDeleted, @lockedBy
	  FETCH NEXT FROM ItemList 
	    INTO @ItemId
	END
	CLOSE CustList
	DEALLOCATE CustList


END
GO
/****** Object:  StoredProcedure [dbo].[Items_GetPropertiesForItem]    Script Date: 12/22/2015 10:42:02 PM ******/
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO

-- =============================================
-- Author:		<Author,,Name>
-- Create date: <Create Date,,>
-- Pass in the property id of the "name" property, for instance and get all names of all Itemss that an account has
-- =============================================
CREATE PROCEDURE [dbo].[Items_GetPropertiesForItem] 
	-- Add the parameters for the stored procedure here
@itemID uniqueidentifier,
@resultCode int out

AS
BEGIN
	-- SET NOCOUNT ON added to prevent extra result sets from
	-- interfering with SELECT statements.
	SET NOCOUNT ON;
    -- Insert statements for procedure here
   
    ----------------- FLOAT
	SELECT 
		Items_tblPropertiesFloat.ID, 
		Items_tblPropertiesFloat.PropertyID, 
		Items_tblPropertiesFloat.PropertyValue, 
		Items_tblPropertiesFloat.PropertyName
	FROM 
		Items_tblPropertiesFloat
	
	WHERE 
	Items_tblPropertiesFloat.OwnerID = @itemID
	
	----------------- Int
	SELECT 
		Items_tblPropertiesInt.ID, 
		Items_tblPropertiesInt.PropertyID, 
		Items_tblPropertiesInt.PropertyValue, 
		Items_tblPropertiesInt.PropertyName 
	
	FROM 
		Items_tblPropertiesInt
	WHERE 
	Items_tblPropertiesInt.OwnerID = @itemID
	
	----------------- Long
	SELECT 
		Items_tblPropertiesLong.ID, 
		Items_tblPropertiesLong.PropertyID, 
		Items_tblPropertiesLong.PropertyValue,
		Items_tblPropertiesLong.PropertyName
	
	FROM 
		Items_tblPropertiesLong
	WHERE 
	Items_tblPropertiesLong.OwnerID = @itemID		
	
	----------------- String
	SELECT 
		Items_tblPropertiesString.ID, 
		Items_tblPropertiesString.PropertyID, 
		Items_tblPropertiesString.PropertyValue,
		Items_tblPropertiesString.PropertyName
	FROM 
		Items_tblPropertiesString
	WHERE 
	Items_tblPropertiesString.OwnerID = @itemID
	

END
GO
/****** Object:  StoredProcedure [dbo].[Items_GetStatsForItem]    Script Date: 12/22/2015 10:42:02 PM ******/
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO

-- =============================================
-- Author:		<Author,,Name>
-- Create date: <Create Date,,>
-- Pass in the property id of the "name" property, for instance and get all names of all Itemss that an account has
-- =============================================
CREATE PROCEDURE [dbo].[Items_GetStatsForItem] 
	-- Add the parameters for the stored procedure here
@itemId uniqueidentifier,
@resultCode int out

AS
BEGIN
	-- SET NOCOUNT ON added to prevent extra result sets from
	-- interfering with SELECT statements.
	SET NOCOUNT ON;
    -- Insert statements for procedure here

    ----------------- FLOAT
	SELECT 
		Items_tblStats.ID, 
		Items_tblStats.StatID, 
		Items_tblStats.CurrentValue,
		Items_tblStats.MaxValue, 
		Items_tblStats.MinValue 
	FROM 
		Items_tblStats		
	
	WHERE 
	Items_tblStats.OwnerID = @itemId
	
	
	

END
GO
/****** Object:  StoredProcedure [dbo].[Items_Save]    Script Date: 12/22/2015 10:42:02 PM ******/
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO




-- =============================================
-- Author:		<Author,,Name>
-- Create date: <Create Date,,>
-- Description:	<Description,,>
-- =============================================
CREATE PROCEDURE [dbo].[Items_Save]
	-- Add the parameters for the stored procedure here
@resultCode int out,
@itemID uniqueidentifier,
@template nvarchar(128),
@intProperties ItemPropertyInt READONLY,
@floatProperties ItemPropertyFloat READONLY,
@longProperties ItemPropertyLong READONLY,
@stringProperties ItemPropertyString READONLY,
@stats ItemStat READONLY,
@owner nvarchar(64) = NULL,
@context uniqueidentifier,
@typeHash bigint,
@binData varbinary(max),
@stackCount int,
@createdOn datetime,
@got int,
@objectOwner uniqueidentifier

AS
BEGIN
	-- SET NOCOUNT ON added to prevent extra result sets from
	-- interfering with SELECT statements.
	SET NOCOUNT ON;

--BEGIN TRANSACTION

    -- Create the Items
    
    -- check for unique Items name
	-- char exists?
	IF @owner IS NULL OR @owner = ''
		SELECT * FROM Items_tblMaster WHERE ID = @itemID
	ELSE
		SELECT * FROM Items_tblMaster WHERE ID = @itemID AND (LoadedBy = @owner OR LoadedBy IS NULL OR LoadedBy = '')
		
	if @@ROWCOUNT < 1
	 BEGIN
		--ROLLBACK
		Set @resultCode = -9
		RETURN
	 END

	 -- Update master table
	 UPDATE Items_tblMaster 
		SET 

			Context = @context,
			TypeHash = @typeHash, 
			BinData = @binData,
			StackCount = @stackCount,
			LoadedBy = @owner,
			CreatedOn = @createdOn,
			Template = @template,
			GOT = @got,
			[Owner] = @objectOwner

		WHERE
			ID = @itemID
	
	if @@ROWCOUNT < 1
	 BEGIN
		--ROLLBACK
		Set @resultCode = 0
		RETURN
	 END
	 	
	-- float props
	EXEC Items_UpdateOrInsertFloatProperties @floatProperties, @itemID

	IF @@ERROR <> 0
	 BEGIN
		-- Rollback the transaction
--		ROLLBACK

		-- Raise an error and return
		SET @resultCode = 0
		RETURN
	 END
	 
	-- int props
	EXEC Items_UpdateOrInsertIntProperties @intProperties, @itemID
	
		IF @@ERROR <> 0
	 BEGIN
		-- Rollback the transaction
--		ROLLBACK

		-- Raise an error and return
		SET @resultCode = 0
		RETURN
	 END
	
	-- string props
	EXEC Items_UpdateOrInsertStringProperties @stringProperties, @itemID
	
		IF @@ERROR <> 0
	 BEGIN
		-- Rollback the transaction
--		ROLLBACK

		-- Raise an error and return
		SET @resultCode = 0
		RETURN
	 END
	 
	-- long props
	EXEC Items_UpdateOrInsertLongProperties @longProperties, @itemID	
	
		IF @@ERROR <> 0
	 BEGIN
		-- Rollback the transaction
--		ROLLBACK

		-- Raise an error and return
		SET @resultCode = 0
		RETURN
	 END
	 
	 -- stats
	EXEC Items_UpdateOrInsertStats @stats, @itemID	
	
		IF @@ERROR <> 0
	 BEGIN
		-- Rollback the transaction
--		ROLLBACK

		-- Raise an error and return
		SET @resultCode = 0
		RETURN
	 END
	
	-- Everything went well, so
--	COMMIT
	
	SET @resultCode = 1	
END
GO
/****** Object:  StoredProcedure [dbo].[Items_UpdateOrInsertFloatProperties]    Script Date: 12/22/2015 10:42:02 PM ******/
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO


CREATE PROCEDURE [dbo].[Items_UpdateOrInsertFloatProperties]

@InputTable ItemPropertyFloat READONLY,
@itemID uniqueidentifier
AS 
BEGIN
SET NOCOUNT ON; 

MERGE Items_tblPropertiesFloat AS t USING @InputTable AS s ON 
	@itemID = t.OwnerID
	AND s.PropertyId = t.PropertyID
WHEN MATCHED THEN 
	UPDATE SET  t.PropertyValue = s.PropertyValue
WHEN NOT MATCHED THEN 
	INSERT (OwnerID, PropertyID, PropertyValue,PropertyName) 
	VALUES (@itemID, s.PropertyId, s.PropertyValue, s.PropertyName)
WHEN NOT MATCHED BY SOURCE THEN
	DELETE;
END;
GO
/****** Object:  StoredProcedure [dbo].[Items_UpdateOrInsertIntProperties]    Script Date: 12/22/2015 10:42:02 PM ******/
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO


CREATE PROCEDURE [dbo].[Items_UpdateOrInsertIntProperties]

@InputTable ItemPropertyInt READONLY,
@itemID uniqueidentifier
AS 
BEGIN
SET NOCOUNT ON; 

MERGE Items_tblPropertiesInt AS t USING @InputTable AS s ON 
	@itemID = t.OwnerID
	AND s.PropertyId = t.PropertyID
WHEN MATCHED THEN 
	UPDATE SET  t.PropertyValue = s.PropertyValue
WHEN NOT MATCHED THEN 
	INSERT (OwnerID, PropertyID, PropertyValue,PropertyName) 
	VALUES (@itemID, s.PropertyId, s.PropertyValue, s.PropertyName)
WHEN NOT MATCHED BY SOURCE THEN
	DELETE;

END;
GO
/****** Object:  StoredProcedure [dbo].[Items_UpdateOrInsertLongProperties]    Script Date: 12/22/2015 10:42:02 PM ******/
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO


CREATE PROCEDURE [dbo].[Items_UpdateOrInsertLongProperties]

@InputTable ItemPropertyLong READONLY,
@itemID uniqueidentifier

AS 
BEGIN
SET NOCOUNT ON; 

MERGE Items_tblPropertiesLong AS t USING @InputTable AS s ON 
	@itemID = t.OwnerID
	AND s.PropertyId = t.PropertyID
WHEN MATCHED THEN 
	UPDATE SET  t.PropertyValue = s.PropertyValue
WHEN NOT MATCHED THEN 
	INSERT (OwnerID, PropertyID, PropertyValue,PropertyName) 
	VALUES (@itemID, s.PropertyId, s.PropertyValue, s.PropertyName)
WHEN NOT MATCHED BY SOURCE THEN
	DELETE;

END;
GO
/****** Object:  StoredProcedure [dbo].[Items_UpdateOrInsertStats]    Script Date: 12/22/2015 10:42:02 PM ******/
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO


CREATE PROCEDURE [dbo].[Items_UpdateOrInsertStats]

@InputTable ItemStat READONLY,
@itemID uniqueidentifier

AS 
BEGIN
SET NOCOUNT ON; 

MERGE Items_tblStats AS t USING @InputTable AS s ON 
	@itemID = t.OwnerID
	AND s.StatId = t.StatID
WHEN MATCHED THEN 
	UPDATE SET  
		t.CurrentValue = s.StatValue,
		t.MaxValue = s.StatMaxValue,
		t.MinValue = s.StatMinValue
WHEN NOT MATCHED THEN 
	INSERT (OwnerID, StatID, CurrentValue, MaxValue, MinValue) 
	VALUES (@itemID, s.StatId, s.StatValue, s.StatMaxValue, s.StatMinValue)
WHEN NOT MATCHED BY SOURCE THEN
	DELETE;

END;
GO
/****** Object:  StoredProcedure [dbo].[Items_UpdateOrInsertStringProperties]    Script Date: 12/22/2015 10:42:02 PM ******/
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO


CREATE PROCEDURE [dbo].[Items_UpdateOrInsertStringProperties]

@InputTable ItemPropertyString READONLY,
@itemID uniqueidentifier

AS 
BEGIN
SET NOCOUNT ON; 

MERGE Items_tblPropertiesString AS t 
USING @InputTable AS s 
ON 
	@itemID = t.OwnerID
	AND s.PropertyId = t.PropertyID
	
WHEN MATCHED THEN 
	UPDATE SET  t.PropertyValue = s.PropertyValue
	
WHEN NOT MATCHED BY TARGET THEN 
	INSERT (OwnerID, PropertyID, PropertyValue,PropertyName) 
	VALUES (@itemID, s.PropertyId, s.PropertyValue, s.PropertyName)
WHEN NOT MATCHED BY SOURCE THEN
	DELETE;

END;
GO
/****** Object:  StoredProcedure [dbo].[UpdateTurnStatus]    Script Date: 12/22/2015 10:42:02 PM ******/
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO
-- =============================================
-- Author:		<Author,,Name>
-- Create date: <Create Date,,>
-- Description:	<Description,,>
-- =============================================
CREATE PROCEDURE [dbo].[UpdateTurnStatus]
	-- Add the parameters for the stored procedure here
@owner uniqueidentifier,
@characterID int,
@turnsLeft int out,
@maxTurns int out,
@timeUntilMoreTurns time out,
@resultCode int out

AS
BEGIN
	-- SET NOCOUNT ON added to prevent extra result sets from
	-- interfering with SELECT statements.
	SET NOCOUNT ON;
BEGIN TRANSACTION

DECLARE @lastGrant datetime -- last grant recorded
DECLARE @interval time -- interval timespan
DECLARE @now datetime -- current time
DECLARE @secsPerInterval int -- number of seconds per grant interval
DECLARE @secsPastSinceLastGrant int -- seconds since the last actual grant
DECLARE @intervalsPast int -- number of grants that should have occured
DECLARE @newLastGrant datetime -- last grant that will be recorded in the DB, given the number of grants since the last recorded grant
DECLARE @newCurrentTurns int -- number of turns after all retro active grants

    SELECT 
	  @lastGrant = LastTurnGrant, 
	  @interval = TurnGrantInterval,
	  @maxTurns = MaxTurns,
	  @turnsLeft = TurnsLeft,
	  @now = GETUTCDATE()
	FROM Character_tblMaster
	WHERE ID = @characterID AND Deleted != 1 AND OwnerAccount = @owner
	
	if @@ROWCOUNT < 1
    -- That character doesn't exist for that user
	 BEGIN
		ROLLBACK
		Set @resultCode = -1
		RETURN
	 END
	
	SET @newLastGrant = @lastGrant
		
	if((@lastGrant + CAST(@interval AS DATETIME)) <= GETUTCDATE())
		BEGIN		
			--print 'Time for more turns to be granted.'			
			SET @secsPerInterval = datediff(second, 0, @interval)
			SET  @secsPastSinceLastGrant = datediff(second, @lastGrant, GETUTCDATE())
			--print 'Secs int interval' print @secsPerInterval print ''
			--print 'Secs since last grant' print @secsPastSinceLastGrant print ''	
			SET @intervalsPast = @secsPastSinceLastGrant / @secsPerInterval
			--print 'Intervals past' print @intervalsPast print ''					
			SET @newLastGrant = DATEADD(second, (@intervalsPast * @secsPerInterval), @lastGrant)
			--print 'Last grant' print @newLastGrant print ''
			
			if(@turnsLeft + @intervalsPast > @maxTurns)			
				SET @newCurrentTurns = @maxTurns;
			else
				SET @newCurrentTurns = @turnsLeft + @intervalsPast
			
			SET @turnsLeft = @newCurrentTurns
			-- SET THE NEW TURNS
			UPDATE Character_tblMaster 
			SET 
				LastTurnGrant = @newLastGrant,
				TurnsLeft = @newCurrentTurns
			WHERE ID = @characterID AND Deleted != 1 AND OwnerAccount = @owner			
			 
			IF @@ERROR <> 0 OR @@ROWCOUNT < 1
			 BEGIN
				-- Rollback the transaction
				ROLLBACK

				-- Raise an error and return
				SET @resultCode = -2
				RETURN
			 END
				END
	
	SET @timeUntilMoreTurns = (@newLastGrant + CAST(@interval AS DATETIME)) - @now
	--print @timeUntilNextGrant       
	
	SET @resultCode = 1
	-- Everything went well, so
	COMMIT     	
END

GO
/****** Object:  UserDefinedFunction [dbo].[Split]    Script Date: 12/22/2015 10:42:02 PM ******/
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO
CREATE FUNCTION [dbo].[Split](@String varchar(8000), @Delimiter char(1))     
returns @temptable TABLE (items varchar(8000))     
as     
begin     
	declare @idx int     
	declare @slice varchar(8000)     
    
	select @idx = 1     
		if len(@String)<1 or @String is null  return     
    
	while @idx!= 0     
	begin     
		set @idx = charindex(@Delimiter,@String)     
		if @idx!=0     
			set @slice = left(@String,@idx - 1)     
		else     
			set @slice = @String     
		
		if(len(@slice)>0)
			insert into @temptable(Items) values(@slice)     

		set @String = right(@String,len(@String) - @idx)     
		if len(@String) = 0 break     
	end 
return     
end
GO
/****** Object:  Table [dbo].[Character_tblMaster]    Script Date: 12/22/2015 10:42:02 PM ******/
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO
CREATE TABLE [dbo].[Character_tblMaster](
	[ID] [int] IDENTITY(1,1) NOT NULL,
	[OwnerAccount] [uniqueidentifier] NOT NULL,
	[CreatedOn] [datetime] NOT NULL,
	[Active] [bit] NOT NULL,
	[Deleted] [bit] NOT NULL,
	[IsTemp] [bit] NOT NULL,
	[LastTurnSubmittedOn] [datetime] NOT NULL,
	[MaxTurns] [int] NOT NULL,
	[LastTurnGrant] [datetime] NOT NULL,
	[TurnGrantInterval] [time](7) NOT NULL,
	[TurnsLeft] [int] NOT NULL,
 CONSTRAINT [PK_Character_tblMaster] PRIMARY KEY CLUSTERED 
(
	[ID] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON) ON [PRIMARY]
) ON [PRIMARY]

GO
/****** Object:  Table [dbo].[Character_tblPropertiesFloat]    Script Date: 12/22/2015 10:42:02 PM ******/
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO
CREATE TABLE [dbo].[Character_tblPropertiesFloat](
	[ID] [int] IDENTITY(1,1) NOT NULL,
	[OwnerID] [int] NOT NULL,
	[PropertyID] [int] NOT NULL,
	[PropertyValue] [float] NOT NULL,
	[PropertyName] [nvarchar](128) NULL,
 CONSTRAINT [PK_Character_tblPropertiesFloat] PRIMARY KEY CLUSTERED 
(
	[OwnerID] ASC,
	[PropertyID] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON) ON [PRIMARY]
) ON [PRIMARY]

GO
/****** Object:  Table [dbo].[Character_tblPropertiesInt]    Script Date: 12/22/2015 10:42:02 PM ******/
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO
CREATE TABLE [dbo].[Character_tblPropertiesInt](
	[ID] [int] IDENTITY(1,1) NOT NULL,
	[OwnerID] [int] NOT NULL,
	[PropertyID] [int] NOT NULL,
	[PropertyValue] [int] NOT NULL,
	[PropertyName] [nvarchar](128) NULL,
 CONSTRAINT [PK_Character_tblPropertiesInt] PRIMARY KEY CLUSTERED 
(
	[OwnerID] ASC,
	[PropertyID] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON) ON [PRIMARY]
) ON [PRIMARY]

GO
/****** Object:  Table [dbo].[Character_tblPropertiesLong]    Script Date: 12/22/2015 10:42:02 PM ******/
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO
CREATE TABLE [dbo].[Character_tblPropertiesLong](
	[ID] [int] IDENTITY(1,1) NOT NULL,
	[OwnerID] [int] NOT NULL,
	[PropertyID] [int] NOT NULL,
	[PropertyValue] [bigint] NOT NULL,
	[PropertyName] [nvarchar](128) NULL,
 CONSTRAINT [PK_Character_tblPropertiesLong] PRIMARY KEY CLUSTERED 
(
	[OwnerID] ASC,
	[PropertyID] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON) ON [PRIMARY]
) ON [PRIMARY]

GO
/****** Object:  Table [dbo].[Character_tblPropertiesString]    Script Date: 12/22/2015 10:42:02 PM ******/
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO
CREATE TABLE [dbo].[Character_tblPropertiesString](
	[ID] [int] IDENTITY(1,1) NOT NULL,
	[OwnerID] [int] NOT NULL,
	[PropertyID] [int] NOT NULL,
	[PropertyValue] [nvarchar](max) NOT NULL,
	[PropertyName] [nvarchar](128) NULL,
 CONSTRAINT [PK_Character_tblPropertiesString] PRIMARY KEY CLUSTERED 
(
	[OwnerID] ASC,
	[PropertyID] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON) ON [PRIMARY]
) ON [PRIMARY] TEXTIMAGE_ON [PRIMARY]

GO
/****** Object:  Table [dbo].[Character_tblServiceLog]    Script Date: 12/22/2015 10:42:02 PM ******/
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO
CREATE TABLE [dbo].[Character_tblServiceLog](
	[ID] [int] IDENTITY(1,1) NOT NULL,
	[TimeStamp] [datetime] NOT NULL,
	[Note] [nvarchar](max) NOT NULL,
	[EntryType] [nvarchar](max) NOT NULL,
	[Account] [uniqueidentifier] NULL,
	[CharacterID] [int] NOT NULL,
	[EntryBy] [nvarchar](128) NOT NULL,
 CONSTRAINT [PK_Character_tblServiceLog] PRIMARY KEY CLUSTERED 
(
	[ID] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON) ON [PRIMARY]
) ON [PRIMARY] TEXTIMAGE_ON [PRIMARY]

GO
/****** Object:  Table [dbo].[Character_tblStats]    Script Date: 12/22/2015 10:42:02 PM ******/
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO
CREATE TABLE [dbo].[Character_tblStats](
	[ID] [int] IDENTITY(1,1) NOT NULL,
	[OwnerID] [int] NOT NULL,
	[StatID] [int] NOT NULL,
	[CurrentValue] [float] NOT NULL,
	[MaxValue] [float] NOT NULL,
	[MinValue] [float] NOT NULL,
 CONSTRAINT [PK_Character_tblStats] PRIMARY KEY CLUSTERED 
(
	[OwnerID] ASC,
	[StatID] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON) ON [PRIMARY]
) ON [PRIMARY]

GO
/****** Object:  Table [dbo].[Items_tblMaster]    Script Date: 12/22/2015 10:42:02 PM ******/
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO
SET ANSI_PADDING ON
GO
CREATE TABLE [dbo].[Items_tblMaster](
	[ID] [uniqueidentifier] NOT NULL,
	[Template] [nvarchar](64) NULL,
	[GOT] [int] NULL,
	[Deleted] [tinyint] NULL,
	[LoadedBy] [nvarchar](64) NULL,
	[CreatedOn] [datetime] NULL,
	[BinData] [varbinary](max) NULL,
	[TypeHash] [bigint] NULL,
	[Context] [uniqueidentifier] NULL,
	[StackCount] [int] NULL,
	[Owner] [uniqueidentifier] NULL,
 CONSTRAINT [PK_Items_tblMaster] PRIMARY KEY CLUSTERED 
(
	[ID] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON) ON [PRIMARY]
) ON [PRIMARY] TEXTIMAGE_ON [PRIMARY]

GO
SET ANSI_PADDING OFF
GO
/****** Object:  Table [dbo].[Items_tblPropertiesFloat]    Script Date: 12/22/2015 10:42:02 PM ******/
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO
CREATE TABLE [dbo].[Items_tblPropertiesFloat](
	[ID] [int] IDENTITY(1,1) NOT NULL,
	[OwnerID] [uniqueidentifier] NOT NULL,
	[PropertyID] [int] NOT NULL,
	[PropertyValue] [float] NOT NULL,
	[PropertyName] [nvarchar](128) NULL,
 CONSTRAINT [PK_Items_tblPropertiesFloat] PRIMARY KEY CLUSTERED 
(
	[OwnerID] ASC,
	[PropertyID] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON) ON [PRIMARY]
) ON [PRIMARY]

GO
/****** Object:  Table [dbo].[Items_tblPropertiesInt]    Script Date: 12/22/2015 10:42:02 PM ******/
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO
CREATE TABLE [dbo].[Items_tblPropertiesInt](
	[ID] [int] IDENTITY(1,1) NOT NULL,
	[OwnerID] [uniqueidentifier] NOT NULL,
	[PropertyID] [int] NOT NULL,
	[PropertyValue] [int] NOT NULL,
	[PropertyName] [nvarchar](128) NULL,
 CONSTRAINT [PK_Items_tblPropertiesInt] PRIMARY KEY CLUSTERED 
(
	[OwnerID] ASC,
	[PropertyID] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON) ON [PRIMARY]
) ON [PRIMARY]

GO
/****** Object:  Table [dbo].[Items_tblPropertiesLong]    Script Date: 12/22/2015 10:42:02 PM ******/
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO
CREATE TABLE [dbo].[Items_tblPropertiesLong](
	[ID] [int] IDENTITY(1,1) NOT NULL,
	[OwnerID] [uniqueidentifier] NOT NULL,
	[PropertyID] [int] NOT NULL,
	[PropertyValue] [bigint] NOT NULL,
	[PropertyName] [nvarchar](128) NULL,
 CONSTRAINT [PK_Items_tblPropertiesLong] PRIMARY KEY CLUSTERED 
(
	[OwnerID] ASC,
	[PropertyID] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON) ON [PRIMARY]
) ON [PRIMARY]

GO
/****** Object:  Table [dbo].[Items_tblPropertiesString]    Script Date: 12/22/2015 10:42:02 PM ******/
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO
CREATE TABLE [dbo].[Items_tblPropertiesString](
	[ID] [int] IDENTITY(1,1) NOT NULL,
	[OwnerID] [uniqueidentifier] NOT NULL,
	[PropertyID] [int] NOT NULL,
	[PropertyValue] [nvarchar](max) NOT NULL,
	[PropertyName] [nvarchar](128) NULL,
 CONSTRAINT [PK_Items_tblPropertiesString] PRIMARY KEY CLUSTERED 
(
	[OwnerID] ASC,
	[PropertyID] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON) ON [PRIMARY]
) ON [PRIMARY] TEXTIMAGE_ON [PRIMARY]

GO
/****** Object:  Table [dbo].[Items_tblServiceLog]    Script Date: 12/22/2015 10:42:02 PM ******/
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO
CREATE TABLE [dbo].[Items_tblServiceLog](
	[ID] [int] IDENTITY(1,1) NOT NULL,
	[TimeStamp] [datetime] NOT NULL,
	[Note] [nvarchar](max) NOT NULL,
	[EntryType] [nvarchar](max) NOT NULL,
	[Account] [uniqueidentifier] NULL,
	[ItemsID] [uniqueidentifier] NOT NULL,
	[EntryBy] [nvarchar](128) NOT NULL,
 CONSTRAINT [PK_Items_tblServiceLog] PRIMARY KEY CLUSTERED 
(
	[ID] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON) ON [PRIMARY]
) ON [PRIMARY] TEXTIMAGE_ON [PRIMARY]

GO
/****** Object:  Table [dbo].[Items_tblStats]    Script Date: 12/22/2015 10:42:02 PM ******/
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO
CREATE TABLE [dbo].[Items_tblStats](
	[ID] [int] IDENTITY(1,1) NOT NULL,
	[OwnerID] [uniqueidentifier] NOT NULL,
	[StatID] [int] NOT NULL,
	[CurrentValue] [float] NOT NULL,
	[MaxValue] [float] NOT NULL,
	[MinValue] [float] NOT NULL,
 CONSTRAINT [PK_Items_tblStats] PRIMARY KEY CLUSTERED 
(
	[OwnerID] ASC,
	[StatID] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON) ON [PRIMARY]
) ON [PRIMARY]

GO
ALTER TABLE [dbo].[Character_tblMaster] ADD  CONSTRAINT [DF_Character_tblMaster_CreatedOn]  DEFAULT (getutcdate()) FOR [CreatedOn]
GO
ALTER TABLE [dbo].[Character_tblMaster] ADD  CONSTRAINT [DF_Character_tblMaster_Active]  DEFAULT ((1)) FOR [Active]
GO
ALTER TABLE [dbo].[Character_tblMaster] ADD  CONSTRAINT [DF_Character_tblMaster_Deleted]  DEFAULT ((0)) FOR [Deleted]
GO
ALTER TABLE [dbo].[Character_tblMaster] ADD  CONSTRAINT [DF_Character_tblMaster_IsTemp]  DEFAULT ((0)) FOR [IsTemp]
GO
ALTER TABLE [dbo].[Character_tblMaster] ADD  CONSTRAINT [DF_Character_tblMaster_LastTurnSubmittedOn]  DEFAULT (getutcdate()) FOR [LastTurnSubmittedOn]
GO
ALTER TABLE [dbo].[Character_tblMaster] ADD  CONSTRAINT [DF_Character_tblMaster_MaxTurns]  DEFAULT ((-1)) FOR [MaxTurns]
GO
ALTER TABLE [dbo].[Character_tblMaster] ADD  CONSTRAINT [DF_Character_tblMaster_LastTurnGrant]  DEFAULT (getutcdate()) FOR [LastTurnGrant]
GO
ALTER TABLE [dbo].[Character_tblMaster] ADD  CONSTRAINT [DF_Character_tblMaster_TurnGrantInterval]  DEFAULT ('01:00:00') FOR [TurnGrantInterval]
GO
ALTER TABLE [dbo].[Character_tblMaster] ADD  CONSTRAINT [DF_Character_tblMaster_TurnsLeft]  DEFAULT ((-1)) FOR [TurnsLeft]
GO
ALTER TABLE [dbo].[Character_tblServiceLog] ADD  CONSTRAINT [DF_Character_tblServiceLog_TimeStamp]  DEFAULT (getutcdate()) FOR [TimeStamp]
GO
ALTER TABLE [dbo].[Character_tblServiceLog] ADD  CONSTRAINT [DF_Character_tblServiceLog_EntryType]  DEFAULT (N'General') FOR [EntryType]
GO
ALTER TABLE [dbo].[Character_tblServiceLog] ADD  CONSTRAINT [DF_Character_tblServiceLog_EntryBy]  DEFAULT (N'System') FOR [EntryBy]
GO
ALTER TABLE [dbo].[Character_tblStats] ADD  CONSTRAINT [DF_Character_tblStats_MinValue]  DEFAULT ((0)) FOR [MinValue]
GO
ALTER TABLE [dbo].[Items_tblMaster] ADD  CONSTRAINT [DF_Table_1_GOT]  DEFAULT ((0)) FOR [Template]
GO
ALTER TABLE [dbo].[Items_tblMaster] ADD  CONSTRAINT [DF_Items_tblMaster_Deleted]  DEFAULT ((0)) FOR [Deleted]
GO
ALTER TABLE [dbo].[Items_tblMaster] ADD  CONSTRAINT [DF_Items_tblMaster_StackCount]  DEFAULT ((1)) FOR [StackCount]
GO
ALTER TABLE [dbo].[Items_tblServiceLog] ADD  CONSTRAINT [DF_Items_tblServiceLog_TimeStamp]  DEFAULT (getutcdate()) FOR [TimeStamp]
GO
ALTER TABLE [dbo].[Items_tblServiceLog] ADD  CONSTRAINT [DF_Items_tblServiceLog_EntryType]  DEFAULT (N'General') FOR [EntryType]
GO
ALTER TABLE [dbo].[Items_tblServiceLog] ADD  CONSTRAINT [DF_Items_tblServiceLog_EntryBy]  DEFAULT (N'System') FOR [EntryBy]
GO
ALTER TABLE [dbo].[Items_tblStats] ADD  CONSTRAINT [DF_Items_tblStats_MinValue]  DEFAULT ((0)) FOR [MinValue]
GO
