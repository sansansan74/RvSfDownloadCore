
CREATE view [web].[vEdoDocument] as
	select d.DocId, t.AttachTypeId, at.AttachTypeName, e.Doc2EdoId, EdoStateName		
		from dbo.DocUpdate d
			join dbo.Doc2Edo e on d.DocId=e.DocId
			join dbo.EdoState es on e.EdoStateId=es.EdoStateId
			join dbo.EdoType2Source t on e.EdoTypeId=t.EdoTypeId and d.SourceId=t.SourceId
			join dbo.AttachType at on t.AttachTypeId=at.AttachTypeId
		where 
			e.Doc2EdoFilesCount>0

