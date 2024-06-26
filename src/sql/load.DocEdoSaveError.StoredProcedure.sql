
-- =============================================
-- Author:		Alex
-- Create date: 08.05.2020
-- Description:	Устанавливает сообщение об ошибке в загрузку документа ЭДО и увеличивает счетчик ошибок
-- =============================================
CREATE PROCEDURE [load].[DocEdoSaveError] 
	@EdoRvId bigint,			-- Ид СФ для загрузки с портала RV
	@SourceId int,	-- Источник ТОР ЦВ или АСУ ВРК
	@ErrorMessage varchar(1024)			-- Имя файла
AS
BEGIN
	SET XACT_ABORT, NOCOUNT ON;

	update e
		set e.EdoErrorCount = isnull(e.EdoErrorCount, 0) + 1
			, e.EdoErrorMessage = @ErrorMessage
			, e.EdoUpdated = getdate()
		from dbo.Doc2Edo e
			join dbo.DocUpdate u on e.DocId=u.DocId
			join dbo.EdoType t on e.EdoTypeId=t.EdoTypeId
		where e.EdoRvId=@EdoRvId and u.SourceId=@SourceId	
END
GO
