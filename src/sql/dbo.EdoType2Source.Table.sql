
CREATE TABLE [dbo].[EdoType2Source](
	[EdoTypeId] [tinyint] NOT NULL,
	[SourceId] [tinyint] NOT NULL,
	[AttachTypeId] [int] NOT NULL,
	[NeedDownload] [char](1) NOT NULL,
	[AlwaysFirstSignDate] [bit] NOT NULL,
 CONSTRAINT [PK_EdoType] PRIMARY KEY CLUSTERED 
(
	[EdoTypeId] ASC,
	[SourceId] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON, OPTIMIZE_FOR_SEQUENTIAL_KEY = OFF) ON [PRIMARY]
) ON [PRIMARY]
GO
ALTER TABLE [dbo].[EdoType2Source] ADD  CONSTRAINT [DF_EdoType_NeedDownload]  DEFAULT ('N') FOR [NeedDownload]
GO
ALTER TABLE [dbo].[EdoType2Source] ADD  CONSTRAINT [DF_EdoType_AlwaysFirstSignDate]  DEFAULT ((0)) FOR [AlwaysFirstSignDate]
GO
ALTER TABLE [dbo].[EdoType2Source]  WITH CHECK ADD  CONSTRAINT [FK_EdoType_AttachType] FOREIGN KEY([AttachTypeId])
REFERENCES [dbo].[AttachType] ([AttachTypeId])
GO
ALTER TABLE [dbo].[EdoType2Source] CHECK CONSTRAINT [FK_EdoType_AttachType]
GO
ALTER TABLE [dbo].[EdoType2Source]  WITH CHECK ADD  CONSTRAINT [FK_EdoType_Source] FOREIGN KEY([SourceId])
REFERENCES [dbo].[Source] ([SourceId])
GO
ALTER TABLE [dbo].[EdoType2Source] CHECK CONSTRAINT [FK_EdoType_Source]
GO
ALTER TABLE [dbo].[EdoType2Source]  WITH CHECK ADD  CONSTRAINT [FK_EdoType2Source_EdoType] FOREIGN KEY([EdoTypeId])
REFERENCES [dbo].[EdoType] ([EdoTypeId])
GO
ALTER TABLE [dbo].[EdoType2Source] CHECK CONSTRAINT [FK_EdoType2Source_EdoType]
GO
