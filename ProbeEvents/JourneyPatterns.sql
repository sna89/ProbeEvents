USE [ProbeEvents]
GO

/****** Object:  Table [dbo].[JourneyPatterns]    Script Date: 11/7/2017 9:25:29 AM ******/
SET ANSI_NULLS ON
GO

SET QUOTED_IDENTIFIER ON
GO

CREATE TABLE [dbo].[JourneyPatterns](
	[Journey Pattern ID] [varchar](10) NOT NULL,
	[Stop Index] [int] NOT NULL,
	[A] [int] NOT NULL,
	[Stop ID] [int] NOT NULL,
	[B] [int] NULL,
	[C] [int] NOT NULL,
	[D] [int] NULL
) ON [PRIMARY]

CREATE UNIQUE INDEX cl_un__JPID_StopID
ON [dbo].[JourneyPatterns] ([Journey Pattern ID],[Stop ID]);

GO


