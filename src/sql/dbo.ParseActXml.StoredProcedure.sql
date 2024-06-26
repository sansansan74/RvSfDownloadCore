
-- =============================================
-- Author:		Alex
-- Create date: 26.05.2023
-- Description:	Разбирает XML Act из ЭДО и достает значение параметров
-- =============================================
CREATE procedure [dbo].[ParseActXml]
	@DocXml as xml	-- тело СФ
	, @vCorrActType int out		-- 0 - норм, 1 - корр, 2 - испр
	, @vCorrActDate varchar(32) out
	, @vCorrActNumber varchar(64) out
as
begin
	SET XACT_ABORT, NOCOUNT ON;

	select	@vCorrActType = null
			, @vCorrActDate = null
			, @vCorrActNumber = null

	declare @isCorrSf int = @DocXml.exist('/Файл/Документ/СвКСчФ')	-- признак того, что СФ - корректировочная. В ней надо брать из другого пути
	declare @isIspravSf int = @DocXml.exist('/Файл/Документ/СвСчФакт/ИспрСчФ')	-- признак того, что СФ - исправительная.

	-- Получаем из XML дату и номер СФ	
	if @isCorrSf = 1	-- Пытаемся найти для корректировочной СФ <СвКСчФ КодОКВ="643" ДатаКСчФ="01.03.2020" НомерКСчФ="1080">
		select @vCorrActType=2
				, @vCorrActDate=c.value('@ДатаКСчФ', 'varchar(32)')
				, @vCorrActNumber=trim(c.value('@НомерКСчФ', 'varchar(64)'))
			from @DocXml.nodes('/Файл/Документ/СвКСчФ') T(c);
	else if @isIspravSf = 1		-- исправительный
		select @vCorrActType=1
				, @vCorrActDate=c.value('@ДатаИспрСчФ', 'varchar(32)')
				, @vCorrActNumber=trim(c.value('@НомИспрСчФ', 'varchar(64)'))
			from @DocXml.nodes('/Файл/Документ/СвСчФакт/ИспрСчФ') T(c);
	else	-- не корректировочный и не исправительный - значит обычный
		set @vCorrActType=0
end

GO
