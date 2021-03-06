USE [Session]
GO
/****** Object:  StoredProcedure [dbo].[Chess_LobbyGetQuickMatch]    Script Date: 12/22/2015 10:27:19 PM ******/
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO
CREATE PROCEDURE [dbo].[Chess_LobbyGetQuickMatch]
	-- Add the parameters for the stored procedure here
	@playerRating float,
	@targetWinChance float = 0.5, -- evenly matched games are preferred by default
	@maxRecords int = 1
AS
BEGIN
	-- SET NOCOUNT ON added to prevent extra result sets from
	-- interfering with SELECT statements.
	SET NOCOUNT ON;

    -- Insert statements for procedure here
	 SELECT TOP (@maxRecords) * FROM Chess_viewQuickMatch(@playerRating) ORDER BY ABS(WinProbability-@targetWinChance),WinProbability DESC
END
GO
/****** Object:  StoredProcedure [dbo].[Lobby_AddGameToServerMap]    Script Date: 12/22/2015 10:27:19 PM ******/
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO

-- =============================================
-- Author:		<Author,,Name>
-- Create date: <Create Date,,>
-- Description:	<Description,,>
-- =============================================
CREATE PROCEDURE [dbo].[Lobby_AddGameToServerMap]
	-- Add the parameters for the stored procedure here
	@ClusterServerID nvarchar(256),
	@GameID uniqueidentifier,
	@CreatedOn datetime,
	@CreatedByCharacter int,
	@GameName nvarchar(512),
	@MaxPlayers int
AS
BEGIN
	-- SET NOCOUNT ON added to prevent extra result sets from
	-- interfering with SELECT statements.
	SET NOCOUNT ON;
	UPDATE Lobby_tblGameToServerMap
	SET 
		ClusterServerID = @ClusterServerID,
		CreatedOn = @CreatedOn,
		CreatedByCharacter = @CreatedByCharacter,
		GameName = @GameName,
		MaxPlayersAllowed = @MaxPlayers
	WHERE GameID = @GameID
	
	IF @@ROWCOUNT = 0
	BEGIN
		-- Insert statements for procedure here
		INSERT INTO Lobby_tblGameToServerMap
		(ClusterServerID, GameID, CreatedOn, CreatedByCharacter, GameName, MaxPlayersAllowed)
		VALUES(@ClusterServerID, @GameID, @CreatedOn, @CreatedByCharacter, @GameName, @MaxPlayers)
	END
END
GO
/****** Object:  StoredProcedure [dbo].[Lobby_ClearServerRegistrations]    Script Date: 12/22/2015 10:27:19 PM ******/
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO
-- =============================================
-- Author:		<Author,,Name>
-- Create date: <Create Date,,>
-- Description:	<Description,,>
-- =============================================
CREATE PROCEDURE [dbo].[Lobby_ClearServerRegistrations]
	-- Add the parameters for the stored procedure here
@ServerType varchar(50)

AS
BEGIN
	-- SET NOCOUNT ON added to prevent extra result sets from
	-- interfering with SELECT statements.
	SET NOCOUNT ON;

    -- Insert statements for procedure here
    IF @ServerType = 'all'
		DELETE FROM Lobby_tblServers
	ELSE
		DELETE FROM Lobby_tblServers WHERE [Type] = @ServerType
END
GO
/****** Object:  StoredProcedure [dbo].[Lobby_DeleteGameFromServerMap]    Script Date: 12/22/2015 10:27:19 PM ******/
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO
-- =============================================
-- Author:		<Author,,Name>
-- Create date: <Create Date,,>
-- Description:	<Description,,>
-- =============================================
CREATE PROCEDURE [dbo].[Lobby_DeleteGameFromServerMap]
	-- Add the parameters for the stored procedure here
	@GameID uniqueidentifier
AS
BEGIN
	-- SET NOCOUNT ON added to prevent extra result sets from
	-- interfering with SELECT statements.
	SET NOCOUNT ON;

    -- Insert statements for procedure here
	DELETE FROM Lobby_tblGameToServerMap WHERE GameID = @GameID
END
GO
/****** Object:  StoredProcedure [dbo].[Lobby_DeleteGamesForServerFromServerMap]    Script Date: 12/22/2015 10:27:19 PM ******/
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO
-- =============================================
-- Author:		<Author,,Name>
-- Create date: <Create Date,,>
-- Description:	<Description,,>
-- =============================================
CREATE PROCEDURE [dbo].[Lobby_DeleteGamesForServerFromServerMap]
	-- Add the parameters for the stored procedure here
	@OwningClusterServerID nvarchar(256)
AS
BEGIN
	-- SET NOCOUNT ON added to prevent extra result sets from
	-- interfering with SELECT statements.
	SET NOCOUNT ON;

    -- Insert statements for procedure here
	DELETE FROM Lobby_tblGameToServerMap WHERE ClusterServerID = @OwningClusterServerID
END
GO
/****** Object:  StoredProcedure [dbo].[Lobby_DeleteGamesServerMap]    Script Date: 12/22/2015 10:27:19 PM ******/
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO
-- =============================================
-- Author:		<Author,,Name>
-- Create date: <Create Date,,>
-- Description:	<Description,,>
-- =============================================
CREATE PROCEDURE [dbo].[Lobby_DeleteGamesServerMap]
	-- Add the parameters for the stored procedure here
AS
BEGIN
	-- SET NOCOUNT ON added to prevent extra result sets from
	-- interfering with SELECT statements.
	SET NOCOUNT ON;

    -- Insert statements for procedure here
	DELETE FROM Lobby_tblGameToServerMap
END
GO
/****** Object:  StoredProcedure [dbo].[Lobby_GetGameServer]    Script Date: 12/22/2015 10:27:19 PM ******/
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO
-- =============================================
-- Author:		<Author,,Name>
-- Create date: <Create Date,,>
-- Description:	<Description,,>
-- =============================================
CREATE PROCEDURE [dbo].[Lobby_GetGameServer]
	-- Add the parameters for the stored procedure here
	@GameID uniqueidentifier
AS
BEGIN
	-- SET NOCOUNT ON added to prevent extra result sets from
	-- interfering with SELECT statements.
	SET NOCOUNT ON;

    SELECT 
Lobby_tblServers.Address,
Lobby_tblServers.Port,
Lobby_tblServers.ClusterServerID
/*,
Lobby_tblGameToServerMap.* */
FROM Lobby_tblGameToServerMap, Lobby_tblServers 
WHERE Lobby_tblGameToServerMap.GameID = @GameID 
AND Lobby_tblGameToServerMap.ClusterServerID = Lobby_tblServers.ClusterServerID
	
END
GO
/****** Object:  StoredProcedure [dbo].[Lobby_GetServerRegistrations]    Script Date: 12/22/2015 10:27:19 PM ******/
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO
-- =============================================
-- Author:		<Author,,Name>
-- Create date: <Create Date,,>
-- Description:	<Description,,>
-- =============================================
CREATE PROCEDURE [dbo].[Lobby_GetServerRegistrations]
	-- Add the parameters for the stored procedure here
	@ClusterServerID nvarchar(256)
AS
BEGIN
	-- SET NOCOUNT ON added to prevent extra result sets from
	-- interfering with SELECT statements.
	SET NOCOUNT ON;

    -- Insert statements for procedure here
	SELECT * FROM Lobby_tblServers WHERE ClusterServerID = @ClusterServerID
	
END
GO
/****** Object:  StoredProcedure [dbo].[Lobby_GetServerRegistrationsForType]    Script Date: 12/22/2015 10:27:19 PM ******/
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO
-- =============================================
-- Author:		<Author,,Name>
-- Create date: <Create Date,,>
-- Description:	<Description,,>
-- =============================================
CREATE PROCEDURE [dbo].[Lobby_GetServerRegistrationsForType]
	-- Add the parameters for the stored procedure here
	@Type varchar(50)
AS
BEGIN
	-- SET NOCOUNT ON added to prevent extra result sets from
	-- interfering with SELECT statements.
	SET NOCOUNT ON;

    -- Insert statements for procedure here
	SELECT * FROM Lobby_tblServers 
	WHERE [Type] = @Type
END
GO
/****** Object:  StoredProcedure [dbo].[Lobby_RegisterServer]    Script Date: 12/22/2015 10:27:19 PM ******/
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO
-- =============================================
-- Author:		<Author,,Name>
-- Create date: <Create Date,,>
-- Description:	<Description,,>
-- =============================================
CREATE PROCEDURE [dbo].[Lobby_RegisterServer]
	-- Add the parameters for the stored procedure here
	@ClusterServerID nvarchar(256),
	@Address nvarchar(256),
	@Port int,
	@RegisteredOn datetime,
	@Type varchar(50),
	@CurConnections int,
	@MaxConnections int
AS
BEGIN
	-- SET NOCOUNT ON added to prevent extra result sets from
	-- interfering with SELECT statements.
	SET NOCOUNT ON;
	
	UPDATE Lobby_tblServers
	SET 
		[Address] = @Address,
		Port = @Port,
		RegisteredOn = @RegisteredOn,
		[Type] = @Type,
		CurConnections = @CurConnections,
		MaxConnections = @MaxConnections
	WHERE ClusterServerID = @ClusterServerID
	
	IF @@ROWCOUNT = 0
	BEGIN
		-- Insert statements for procedure here
		INSERT INTO Lobby_tblServers
		(ClusterServerID, [Address], Port, RegisteredOn, [Type], CurConnections, MaxConnections)
		VALUES(@ClusterServerID, @Address, @Port, @RegisteredOn, @Type, @CurConnections, @MaxConnections)
	END
END
GO
/****** Object:  StoredProcedure [dbo].[Lobby_UnregisterServer]    Script Date: 12/22/2015 10:27:19 PM ******/
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO
-- =============================================
-- Author:		<Author,,Name>
-- Create date: <Create Date,,>
-- Description:	<Description,,>
-- =============================================
CREATE PROCEDURE [dbo].[Lobby_UnregisterServer]
	-- Add the parameters for the stored procedure here
	@ClusterServerID nvarchar(256)
AS
BEGIN
	-- SET NOCOUNT ON added to prevent extra result sets from
	-- interfering with SELECT statements.
	SET NOCOUNT ON;

    -- Insert statements for procedure here
	DELETE FROM Lobby_tblServers
	WHERE ClusterServerID = @ClusterServerID
END
GO
/****** Object:  StoredProcedure [dbo].[Lobby_UpdateGameToServerMap]    Script Date: 12/22/2015 10:27:19 PM ******/
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO
-- =============================================
-- Author:		<Author,,Name>
-- Create date: <Create Date,,>
-- Description:	<Description,,>
-- =============================================
CREATE PROCEDURE [dbo].[Lobby_UpdateGameToServerMap]
	-- Add the parameters for the stored procedure here
	@GameID uniqueidentifier,
	@GameName nvarchar(512),
	@MaxPlayers int,
	@CurPlayers int,
	@InProgress tinyint,
	@IsPrivate tinyint
AS
BEGIN
	-- SET NOCOUNT ON added to prevent extra result sets from
	-- interfering with SELECT statements.
	SET NOCOUNT ON;
	UPDATE Lobby_tblGameToServerMap
	SET 
		GameName = @GameName,
		MaxPlayersAllowed = @MaxPlayers,
		CurrentPlayers = @CurPlayers,
		InProgress = @InProgress,
		IsPrivate = @IsPrivate
	WHERE GameID = @GameID
	
	
END
GO
/****** Object:  StoredProcedure [dbo].[Session_AuthorizeAccount]    Script Date: 12/22/2015 10:27:19 PM ******/
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO
-- =============================================
-- Author:		<Author,,Name>
-- Create date: <Create Date,,>
-- Description:	<Description,,>
-- =============================================
CREATE PROCEDURE [dbo].[Session_AuthorizeAccount] 
	-- Add the parameters for the stored procedure here
	@AccountName nvarchar(512),
	@AuthorizedOn datetime,
	@AuthorizingServerID nvarchar(256),
	@Ticket uniqueidentifier,
	@Character int,
	@TargetServerID nvarchar(256),
	@AccountID uniqueidentifier
AS
BEGIN
	-- SET NOCOUNT ON added to prevent extra result sets from
	-- interfering with SELECT statements.
	SET NOCOUNT ON;

	UPDATE Session_tblAuth
	SET 
		AuthorizedOn = @AuthorizedOn,
		AuthorizingServerID = @AuthorizingServerID,
		Ticket = @Ticket,
		[Character] = @Character,
		TargetServerID = @TargetServerID,
		AccountID = @AccountID
	WHERE AccountName = @AccountName

	IF @@ROWCOUNT = 0
	BEGIN
		INSERT INTO Session_tblAuth
		(AccountName, AuthorizedOn, AuthorizingServerID, Ticket, [Character], TargetServerID, AccountID)
		VALUES(@AccountName, @AuthorizedOn, @AuthorizingServerID, @Ticket, @Character, @TargetServerID, @AccountID)
	END
END
GO
/****** Object:  StoredProcedure [dbo].[Session_ClearAllSessions]    Script Date: 12/22/2015 10:27:19 PM ******/
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO
-- =============================================
-- Author:		<Author,,Name>
-- Create date: <Create Date,,>
-- Description:	<Description,,>
-- =============================================
CREATE PROCEDURE [dbo].[Session_ClearAllSessions] 
	-- Add the parameters for the stored procedure here
AS
BEGIN
	-- SET NOCOUNT ON added to prevent extra result sets from
	-- interfering with SELECT statements.
	SET NOCOUNT ON;
	
	DELETE FROM Session_tblAuth
	
END
GO
/****** Object:  StoredProcedure [dbo].[Session_GetAuthorizationTicket]    Script Date: 12/22/2015 10:27:19 PM ******/
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO
-- =============================================
-- Author:		<Author,,Name>
-- Create date: <Create Date,,>
-- Description:	<Description,,>
-- =============================================
CREATE PROCEDURE [dbo].[Session_GetAuthorizationTicket] 
	-- Add the parameters for the stored procedure here
	@AccountName nvarchar(512)
AS
BEGIN
	-- SET NOCOUNT ON added to prevent extra result sets from
	-- interfering with SELECT statements.
	SET NOCOUNT ON;
	
	SELECT AuthorizedOn, AuthorizingServerID,Ticket, [Character], TargetServerID, AccountID FROM Session_tblAuth
	WHERE AccountName = @AccountName
	
END
GO
/****** Object:  StoredProcedure [dbo].[Session_UnauthorizeAccount]    Script Date: 12/22/2015 10:27:19 PM ******/
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO
-- =============================================
-- Author:		<Author,,Name>
-- Create date: <Create Date,,>
-- Description:	<Description,,>
-- =============================================
CREATE PROCEDURE [dbo].[Session_UnauthorizeAccount] 
	-- Add the parameters for the stored procedure here
	@AccountName nvarchar(512)
AS
BEGIN
	-- SET NOCOUNT ON added to prevent extra result sets from
	-- interfering with SELECT statements.
	SET NOCOUNT ON;
	
	DELETE FROM Session_tblAuth
	WHERE AccountName = @AccountName
	
END
GO
/****** Object:  Table [dbo].[Chess_tblGameToServerMap]    Script Date: 12/22/2015 10:27:19 PM ******/
SET ANSI_NULLS OFF
GO
SET QUOTED_IDENTIFIER ON
GO
SET ANSI_PADDING ON
GO
CREATE TABLE [dbo].[Chess_tblGameToServerMap](
	[CorGameID] [uniqueidentifier] NOT NULL,
	[BlackPlayerELO] [int] NULL,
	[WhitePlayerELO] [int] NULL,
	[BlackPlayerName] [nvarchar](max) NULL,
	[WhitePlayerName] [nvarchar](max) NULL,
	[CorClusterServerID] [nvarchar](256) NULL,
	[RowID] [int] IDENTITY(1,1) NOT NULL,
	[MaxSpectators] [int] NOT NULL,
	[CurSpectators] [int] NOT NULL,
	[WhiteStats] [varchar](max) NULL,
	[BlackStats] [varchar](max) NULL,
 CONSTRAINT [PrimaryKey_4c6dbd07-7214-4b3b-98a8-b1afddd5a178] PRIMARY KEY CLUSTERED 
(
	[RowID] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON) ON [PRIMARY]
) ON [PRIMARY] TEXTIMAGE_ON [PRIMARY]

GO
SET ANSI_PADDING OFF
GO
/****** Object:  Table [dbo].[Lobby_tblGameToServerMap]    Script Date: 12/22/2015 10:27:19 PM ******/
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO
CREATE TABLE [dbo].[Lobby_tblGameToServerMap](
	[ID] [int] IDENTITY(1,1) NOT NULL,
	[ClusterServerID] [nvarchar](256) NOT NULL,
	[GameID] [uniqueidentifier] NOT NULL,
	[CreatedOn] [datetime] NOT NULL,
	[CreatedByCharacter] [int] NOT NULL,
	[InProgress] [bit] NOT NULL,
	[CurrentPlayers] [int] NOT NULL,
	[MaxPlayersAllowed] [int] NOT NULL,
	[GameName] [nvarchar](512) NOT NULL,
	[IsPrivate] [bit] NOT NULL,
 CONSTRAINT [PK_Lobby_tblGameToServerMap] PRIMARY KEY CLUSTERED 
(
	[ID] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON) ON [PRIMARY]
) ON [PRIMARY]

GO
/****** Object:  Table [dbo].[Lobby_tblServers]    Script Date: 12/22/2015 10:27:19 PM ******/
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO
SET ANSI_PADDING ON
GO
CREATE TABLE [dbo].[Lobby_tblServers](
	[ID] [int] IDENTITY(1,1) NOT NULL,
	[ClusterServerID] [nvarchar](256) NOT NULL,
	[Address] [varchar](256) NOT NULL,
	[Port] [int] NOT NULL,
	[RegisteredOn] [datetime] NOT NULL,
	[Type] [varchar](50) NOT NULL,
	[CurConnections] [int] NOT NULL,
	[MaxConnections] [int] NOT NULL,
 CONSTRAINT [PK_Lobby_tblServers] PRIMARY KEY CLUSTERED 
(
	[ClusterServerID] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON) ON [PRIMARY]
) ON [PRIMARY]

GO
SET ANSI_PADDING OFF
GO
/****** Object:  Table [dbo].[Session_tblAuth]    Script Date: 12/22/2015 10:27:19 PM ******/
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO
CREATE TABLE [dbo].[Session_tblAuth](
	[ID] [int] IDENTITY(1,1) NOT NULL,
	[AccountName] [nvarchar](512) NOT NULL,
	[AccountID] [uniqueidentifier] NOT NULL,
	[AuthorizedOn] [datetime] NOT NULL,
	[AuthorizingServerID] [nvarchar](256) NOT NULL,
	[Ticket] [uniqueidentifier] NOT NULL,
	[Character] [int] NOT NULL,
	[TargetServerID] [nvarchar](256) NOT NULL,
 CONSTRAINT [PK_Session_tblAuth] PRIMARY KEY CLUSTERED 
(
	[ID] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON) ON [PRIMARY]
) ON [PRIMARY]

GO
/****** Object:  UserDefinedFunction [dbo].[Chess_viewQuickMatch]    Script Date: 12/22/2015 10:27:19 PM ******/
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO
 CREATE FUNCTION [dbo].[Chess_viewQuickMatch] (@playerELO float)
RETURNS TABLE
AS RETURN
SELECT 
CONVERT(DECIMAL(10,4), 
	(1.0 / 
		(1.0 + 
			POWER(10.0,
					(
					
					CASE WHEN WhitePlayerELO IS NULL THEN BlackPlayerELO ELSE WHITEPlayerELO END - @playerELO)  / 400.0					 
		          ) 
		 )
	 )) as WinProbability, Chess_tblGameToServerMap.*, Lobby_tblGameToServerMap.*
 FROM Chess_tblGameToServerMap JOIN Lobby_tblGameToServerMap ON GameID = CorGameID
 WHERE (BlackPlayerELO IS NULL AND WhitePlayerELO IS NOT NULL) OR (BlackPlayerELO IS NOT NULL AND WhitePlayerELO IS NULL)
GO
ALTER TABLE [dbo].[Chess_tblGameToServerMap] ADD  DEFAULT ((0)) FOR [MaxSpectators]
GO
ALTER TABLE [dbo].[Chess_tblGameToServerMap] ADD  DEFAULT ((0)) FOR [CurSpectators]
GO
ALTER TABLE [dbo].[Chess_tblGameToServerMap] ADD  DEFAULT (NULL) FOR [WhiteStats]
GO
ALTER TABLE [dbo].[Chess_tblGameToServerMap] ADD  DEFAULT (NULL) FOR [BlackStats]
GO
ALTER TABLE [dbo].[Lobby_tblGameToServerMap] ADD  CONSTRAINT [DF_Lobby_tblGameToServerMap_InProgress]  DEFAULT ((0)) FOR [InProgress]
GO
ALTER TABLE [dbo].[Lobby_tblGameToServerMap] ADD  CONSTRAINT [DF_Lobby_tblGameToServerMap_CurrentPlayers]  DEFAULT ((0)) FOR [CurrentPlayers]
GO
ALTER TABLE [dbo].[Lobby_tblGameToServerMap] ADD  CONSTRAINT [DF_Lobby_tblGameToServerMap_MaxPlayersAllowed]  DEFAULT ((-1)) FOR [MaxPlayersAllowed]
GO
ALTER TABLE [dbo].[Lobby_tblGameToServerMap] ADD  CONSTRAINT [DF_Lobby_tblGameToServerMap_Name]  DEFAULT (N'-') FOR [GameName]
GO
ALTER TABLE [dbo].[Lobby_tblGameToServerMap] ADD  CONSTRAINT [DF_Lobby_tblGameToServerMap_IsPrivate]  DEFAULT ((0)) FOR [IsPrivate]
GO
ALTER TABLE [dbo].[Lobby_tblServers] ADD  CONSTRAINT [DF_Lobby_tblServers_CurConnections]  DEFAULT ((0)) FOR [CurConnections]
GO
ALTER TABLE [dbo].[Lobby_tblServers] ADD  CONSTRAINT [DF_Lobby_tblServers_MaxConnections]  DEFAULT ((1)) FOR [MaxConnections]
GO
ALTER TABLE [dbo].[Session_tblAuth] ADD  CONSTRAINT [DF_Session_tblAuth_AuthorizedOn]  DEFAULT (getutcdate()) FOR [AuthorizedOn]
GO
ALTER TABLE [dbo].[Session_tblAuth] ADD  CONSTRAINT [DF_Session_tblAuth_AuthorizingServerID]  DEFAULT (N'') FOR [AuthorizingServerID]
GO
ALTER TABLE [dbo].[Session_tblAuth] ADD  CONSTRAINT [DF_Session_tblAuth_Character]  DEFAULT ((-1)) FOR [Character]
GO
ALTER TABLE [dbo].[Session_tblAuth] ADD  CONSTRAINT [DF_Session_tblAuth_TargetServerID]  DEFAULT (N'') FOR [TargetServerID]
GO
