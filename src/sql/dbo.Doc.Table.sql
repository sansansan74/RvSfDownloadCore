
CREATE TABLE [dbo].[Doc](
	[DocId] [int] NOT NULL,
	[DocVagon] [int] NULL,
	[DocRepairDate] [date] NULL,
	[DocSfNumber] [varchar](64) NULL,
	[DocSfDate] [date] NULL,
	[DocActNumber] [varchar](128) NULL,
	[DocActDate] [date] NULL,
	[DocRvContractCode] [bigint] NULL,
	[DocRvMainContragentCode] [bigint] NULL,
	[DocRvDepoContragentCode] [bigint] NULL,
	[DocRepairContract] [varchar](128) NULL,
	[DocRepairContractor] [varchar](255) NULL,
	[DocDepoCode] [int] NULL,
	[DocSumWithNds] [decimal](19, 9) NULL,
	[DocWorkflow] [tinyint] NULL,
	[DocCorrAvr] [tinyint] NOT NULL,
	[DocUdalen] [tinyint] NULL,
	[DocInserted] [datetime] NOT NULL,
	[DocUpdated] [datetime] NOT NULL,
	[DocSumUborka] [decimal](19, 9) NULL,
	[VagonChanged] [datetime] NULL,
	[DocCheckMask] [varchar](32) NULL,
	[SellInn] [varchar](16) NULL,
	[SellKpp] [varchar](16) NULL,
	[SellCompanyName] [varchar](128) NULL,
	[SellAddress] [varchar](255) NULL,
	[DocType] [int] NULL,
	[DocECP] [varchar](8) NULL,
	[CorrectSfType] [int] NULL,
	[CorrectSfNumber] [varchar](64) NULL,
	[CorrectSfDate] [date] NULL,
	[CorrectActType] [int] NULL,
	[CorrectActNumber] [varchar](64) NULL,
	[CorrectActDate] [date] NULL,
 CONSTRAINT [PK_Doc] PRIMARY KEY CLUSTERED 
(
	[DocId] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON, OPTIMIZE_FOR_SEQUENTIAL_KEY = OFF) ON [PRIMARY]
) ON [PRIMARY]
GO
ALTER TABLE [dbo].[Doc] ADD  CONSTRAINT [DF_Doc_CorrAvr]  DEFAULT ((0)) FOR [DocCorrAvr]
GO
ALTER TABLE [dbo].[Doc] ADD  CONSTRAINT [DF_Doc_DocInserted]  DEFAULT (getdate()) FOR [DocInserted]
GO
ALTER TABLE [dbo].[Doc] ADD  CONSTRAINT [DF_Doc_DocUpdated]  DEFAULT (getdate()) FOR [DocUpdated]
GO
ALTER TABLE [dbo].[Doc]  WITH CHECK ADD  CONSTRAINT [FK_Doc_DocUpdate] FOREIGN KEY([DocId])
REFERENCES [dbo].[DocUpdate] ([DocId])
GO
ALTER TABLE [dbo].[Doc] CHECK CONSTRAINT [FK_Doc_DocUpdate]
GO
