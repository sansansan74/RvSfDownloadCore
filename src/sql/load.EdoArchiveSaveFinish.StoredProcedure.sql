
-- =============================================
-- Author:		Alex
-- Create date: 22.01.2019
-- Description:	Завершает сохранение изменения документа ЭДО в БД
-- Для актуального (не отмененного) EDO-документа типа SF 
--		находит xml-файл с SF (он лежит в корне и имеет расширение xml), 
--		разбирает его
--		прописывает номер и дату СФ в dbo.Doc
-- Проставляет в dbo.Doc2Edo признак того, что документ скачан
-- =============================================
CREATE PROCEDURE [load].[EdoArchiveSaveFinish]
	@DocId int
	, @EdoRvId bigint
	, @saveRallback bit = 1
AS
BEGIN
	SET XACT_ABORT, NOCOUNT ON;
	
	declare @AttachTypeId int = null
		, @EdoStateId tinyint = null
		, @Doc2EdoId bigint = null
		, @SfXmlCount int = 0
		, @EdoSign1Date datetime = null
		, @EdoSign2Date datetime = null
	
	-- получить тип документа и его статус (актуальный или отмененных), а также даты подписей
	select @AttachTypeId=t.AttachTypeId, @EdoStateId=e.EdoStateId, @Doc2EdoId=e.Doc2EdoId, @EdoSign1Date=e.EdoSign1Date, @EdoSign2Date=e.EdoSign2Date
	from dbo.DocUpdate d
		join dbo.Doc dc on d.DocId=dc.DocId
		join dbo.Doc2Edo e on d.DocId=e.DocId
		join dbo.EdoType2Source t on e.EdoTypeId=t.EdoTypeId and d.SourceId=t.SourceId
	where d.DocId=@DocId and e.EdoRvId=@EdoRvId

	begin try
		if @EdoStateId = 0 and @AttachTypeId=2
			exec dbo.SaveEdoSfParams @DocId, @Doc2EdoId, @AttachTypeId, @EdoStateId, @EdoSign1Date, @EdoSign2Date, @saveRallback -- @saveRallback

		if @EdoStateId = 0 and @AttachTypeId=6
			exec dbo.SaveEdoActParams @DocId, @Doc2EdoId, @AttachTypeId, @EdoStateId
	end try
	begin catch
		declare @ErrorMessage NVARCHAR(4000),
				@ErrorSeverity INT,
				@ErrorState INT;

		select	@ErrorMessage = ERROR_MESSAGE(),
				@ErrorSeverity = ERROR_SEVERITY(),
				@ErrorState = ERROR_STATE();
		
		raiserror (@ErrorMessage, @ErrorSeverity, @ErrorState)
		return 0
	end catch
	
	-- помечаем ЭДО документ как скачанный успешно
	update dbo.Doc2Edo
		set 
			EdoNeedReload='N'			-- прописываем, что загрузка выполена
			, EdoErrorCount = 0			-- скидываем счетчик ошибок загрузки
			, EdoErrorMessage = null	-- скидываем сообщение об ошибке загрузки
			, Doc2EdoFilesCount=(select count(*) from dbo.Doc2Edo2Attach dea where dea.Doc2EdoId=@Doc2EdoId )
		where DocId=@docId 
			and EdoRvId=@EdoRvId
	
END

