
CREATE proc [load].[PrintFormSave]
	@docId int
	, @sourceId int
	, @attachTypeId int
	, @docName varchar(1024)
	, @docBody varbinary(max)
as
begin
	set XACT_ABORT, NOCOUNT ON

	declare @attachId int = null

	select @attachId = da.AttachId
		from dbo.Doc2Attach da			
		where da.DocId=@docId and da.AttachTypeId=@attachTypeId
	
	if @attachId is null
		begin
			insert into dbo.Attach ( AttachFileName, AttachFileBody ) values ( @docName, @docBody )
			set @attachId = scope_identity()
		end
	else
		begin
			update dbo.Attach
				set AttachFileName=@docName
					, AttachFileBody = @docBody
					, AttachUpdated = getdate()
				where AttachId=@attachId
		end

	update dbo.Doc2Attach
		set AttachId=@attachId
			, Doc2AttachNeedReload='N'		-- прописываем, что загрузка выполена
			, Doc2AttachErrorCount= 0		-- скидываем счетчик ошибок загрузки
			, Doc2AttachErrorMessage = null
			, Doc2AttachUpdated = getdate()
		where DocId=@docId and AttachTypeId=@AttachTypeId
end

