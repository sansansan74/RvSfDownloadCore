
-- =============================================
-- Author:		Alex
-- Create date: 06.04.2021
-- Description:	Возвращает перечень печатных форм
-- select * from [web].[GetPrintForms](767222)
-- =============================================
create view [web].[vGetPrintForms] as
	with src as (
	select 
		d.DocId
		, da.AttachTypeId
		, t.AttachTypeName
		, da.AttachId
		from dbo.DocUpdate d
			join dbo.Doc dc on d.DocId=dc.DocId
			join dbo.Doc2Attach da on d.DocId=da.DocId
			join dbo.AttachType t on da.AttachTypeId=t.AttachTypeId
			join dbo.AttachType2Source a2s on t.AttachTypeId=a2s.AttachTypeId and d.SourceId=a2s.SourceId
	union all
	select 
		da.DocId, 1, 'actXml', da.AttachId
		from dbo.Doc2Attach da 
		where  da.AttachTypeId=1
	)


	select
		s.docId
		, s.AttachTypeId
		, s.AttachTypeName
		, AttachFileName = isnull(a.AttachFileName, CONCAT(s.AttachTypeId, '.pdf') )
		, a.AttachFileBody
		from src s
			join dbo.Attach a on s.AttachId=a.AttachId

