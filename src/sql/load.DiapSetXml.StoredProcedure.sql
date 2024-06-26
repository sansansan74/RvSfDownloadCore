
-- =============================================
-- Author:		Alex
-- Create date: 23.01.2019
-- Description:	В существующий диапазон загрузки загружает данные для парсинга в БД. Ставит задачи на выкачку документов.
-- =============================================
CREATE PROCEDURE [load].[DiapSetXml] 
	@DiapId int,			-- диапазон
	@DiapXml xml				-- выгружаемые данные в XML
AS
BEGIN
	SET XACT_ABORT, NOCOUNT ON
	DECLARE @errorMessage varchar(4000), @errorSeverity int
	DECLARE @SourceId int

	-- Получаем из диапазона код источника: который нужен при парсинге
	-- документа. У ТОР ЦВ код СФ =51, у АСУ ВРК=13
	SELECT @SourceId=d.SourceId from dbo.Diap d where d.DiapId=@DiapId


	BEGIN TRY
		-- Заменить в XML головной тег для источников <>(1,2)
		exec dbo.ReplaceVagonTegName @SourceId, @DiapXml output

		UPDATE dbo.Diap
			SET DiapStatusId=2
				, DiapXML = NULL --@DiapXml,
				, DiapXmlReceived = GETDATE()
				, DiapOriginalXml = compress( cast(@DiapXml as varchar(max)) )	-- сжатый оригинальный текст
			WHERE DiapId=@DiapId

		-- Разобрать XML и создать задачи на загрузку документов
		EXEC DiapParse @DiapId, @DiapXml, @SourceId
		
		-- Указываем, что разбор диапазона завершен и проставляем XML
		UPDATE dbo.Diap
			SET DiapStatusId=3,
				DiapXmlParced=GETDATE()
			WHERE DiapId=@DiapId

		RETURN @DiapId
   END TRY
   BEGIN CATCH
		SELECT @errorMessage = ERROR_MESSAGE(), @errorSeverity = ERROR_SEVERITY()
		RAISERROR(@errorMessage, @errorSeverity, 1)
		
		RETURN 0
   END CATCH
END

GO
