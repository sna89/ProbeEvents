USE [ProbeEvents]
GO

/****** Object:  Table [dbo].[EventLog]    Script Date: 12/9/2017 2:33:40 PM ******/
SET ANSI_NULLS ON
GO

SET QUOTED_IDENTIFIER ON
GO

CREATE TABLE [dbo].[JourneyOverlapResults](
	[Stop ID] [int] NOT NULL,
	[First Vehicle Journey ID] [int] NOT NULL,
	[First Journey Pattern ID] [varchar](8) NOT NULL,
	[Second Vehicle Journey ID] [int] NULL,
	[Second Journey Pattern ID] [varchar](8) NOT NULL, 
	[NumberOfMissingEvents] [int] NOT NULL,
	[Probability] [float] NULL,
	[RunTimeDuration(ms)] [float] NULL,
	[Pruned] [int] NULL,
	
) ON [PRIMARY]

CREATE CLUSTERED INDEX [IX_JourneyOverlapResults_Columns] ON [dbo].[JourneyOverlapResults] ([Stop ID], [First Vehicle Journey ID], [First Journey Pattern ID], [Second Vehicle Journey ID],[Second Journey Pattern ID])

GO

