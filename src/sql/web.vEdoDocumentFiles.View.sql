
CREATE view [web].[vEdoDocumentFiles] as
	select dea.Doc2EdoId, dea.AttachId, a.AttachFileName, a.AttachFileBody,  DECOMPRESS( a.AttachFileBody) as AttachFileBody_Decompressed
	from dbo.Doc2Edo2Attach dea
		join dbo.Attach a on dea.AttachId=a.AttachId

