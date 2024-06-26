
-- =============================================
-- Author:		Alex
-- Create date: 26.04.2019
-- Description:	Возвращает набор ид документов для обновления
-- exec [load].[ActsGetNext4Update] 2, 3
-- =============================================
CREATE PROCEDURE [load].[ActsGetNext4Update] 
	@SourceId int,  -- Источник ТОР ЦВ или АСУ ВРК
	@MaxCount int	-- Максимальное количество актов 
AS
BEGIN
	SET NOCOUNT ON;

	SELECT TOP (@MaxCount) d.DocRvRepairId		-- берем только одно значение
		FROM dbo.DocUpdate d 
		WHERE d.SourceId=@SourceId 
			and d.DocNeedReload in ('Y', 'F')			-- где требуется загрузка или перезагрузка
			and d.DocSaveErrorTryCount < 3		-- максимальное кол-во, когда не вернули документ
		ORDER BY d.DocUpdateUpdated				-- берем по порядку обновления в БД
END


