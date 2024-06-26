
CREATE TABLE [dbo].[DocUpdate](
	[DocId] [int] IDENTITY(1,1) NOT NULL,
	[SourceId] [tinyint] NOT NULL,
	[DocRvRepairId] [varchar](32) NOT NULL,
	[DocVagonChanged] [datetime] NOT NULL,
	[DocNeedReload] [char](1) NOT NULL,
	[DocUpdateInserted] [datetime] NOT NULL,
	[DocUpdateUpdated] [datetime] NOT NULL,
	[DocSaveErrorMessage] [varchar](4096) NULL,
	[DocSaveErrorTryCount] [int] NOT NULL,
	[DocSaveErrorDate] [datetime] NULL,
	[DocProcessed] [datetime] NULL,
	[DocAdminComment] [varchar](1024) NULL,
	[DocRollbackDate] [datetime] NULL,
	[DocRollbackCheckMask] [varchar](32) NULL,
	[FreshChanged] [datetime] NULL,
	[FreshCheckMask] [varchar](32) NULL,
	[CorrAvr] [char](1) NOT NULL,
 CONSTRAINT [PK_DocUpdate] PRIMARY KEY CLUSTERED 
(
	[DocId] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON, OPTIMIZE_FOR_SEQUENTIAL_KEY = OFF) ON [PRIMARY]
) ON [PRIMARY]
GO
ALTER TABLE [dbo].[DocUpdate] ADD  CONSTRAINT [DF_DocUpdate_DocUpdateInserted]  DEFAULT (getdate()) FOR [DocUpdateInserted]
GO
ALTER TABLE [dbo].[DocUpdate] ADD  CONSTRAINT [DF_DocUpdate_DocUpdateUpdated]  DEFAULT (getdate()) FOR [DocUpdateUpdated]
GO
ALTER TABLE [dbo].[DocUpdate] ADD  CONSTRAINT [DF_DocUpdate_DocSaveTryCount]  DEFAULT ((0)) FOR [DocSaveErrorTryCount]
GO
ALTER TABLE [dbo].[DocUpdate] ADD  CONSTRAINT [DF_DocUpdate_CorrArv]  DEFAULT ('0') FOR [CorrAvr]
GO
ALTER TABLE [dbo].[DocUpdate]  WITH CHECK ADD  CONSTRAINT [FK_DocUpdate_DocNeedReloadStatus] FOREIGN KEY([DocNeedReload])
REFERENCES [dbo].[DocNeedReloadStatus] ([DocNeedReload])
GO
ALTER TABLE [dbo].[DocUpdate] CHECK CONSTRAINT [FK_DocUpdate_DocNeedReloadStatus]
GO
ALTER TABLE [dbo].[DocUpdate]  WITH CHECK ADD  CONSTRAINT [FK_DocUpdate_Source] FOREIGN KEY([SourceId])
REFERENCES [dbo].[Source] ([SourceId])
GO
ALTER TABLE [dbo].[DocUpdate] CHECK CONSTRAINT [FK_DocUpdate_Source]
GO
