
-- =============================================
-- Author:		Alex
-- Create date: 16.12.2020
-- Description:	Разбирает XML Sf из ЭДО и достает значение параметров
-- =============================================
CREATE procedure [dbo].[ParseSfXml]
	@DocXml as xml	-- тело СФ
	, @vSfDate varchar(32) out
	, @SfNumber varchar(64) out
	, @vSellInn varchar(32) out
	, @vSellKpp varchar(32) out
	, @vSellCompanyName varchar(256) out
	, @vSellAddress varchar(256) out

	, @vCorrType int out		-- 0 - норм, 1 - корр, 2 - испр
	, @vCorrDate varchar(32) out
	, @vCorrNumber varchar(64) out
as
begin
	SET XACT_ABORT, NOCOUNT ON;

	set @vSfDate = null
	set @SfNumber = null
	set @vSellInn = null
	set @vSellKpp = null
	set @vSellCompanyName = null
	set @vSellAddress = null
	set @vCorrType = null
	set @vCorrDate = null
	set @vCorrNumber = null

	declare @isCorrSf int = @DocXml.exist('/Файл/Документ/СвКСчФ')	-- признак того, что СФ - корректировочная. В ней надо брать из другого пути
	declare @isIspravSf int = @DocXml.exist('/Файл/Документ/СвСчФакт/ИспрСчФ')	-- признак того, что СФ - исправительная.

	-- Получаем из XML дату и номер СФ	
	if @isCorrSf = 0	-- Пытаемся найти для обычной СФ
		select @vSfDate=c.value('@ДатаСчФ', 'varchar(32)'),  @SfNumber=trim(c.value('@НомерСчФ', 'varchar(64)'))
			from @DocXml.nodes('/Файл/Документ/СвСчФакт') T(c);
	else	-- Пытаемся найти для корректировочной СФ <СвКСчФ КодОКВ="643" ДатаКСчФ="01.03.2020" НомерКСчФ="1080">
		select @vSfDate=c.value('@ДатаКСчФ', 'varchar(32)'),  @SfNumber=trim(c.value('@НомерКСчФ', 'varchar(64)'))
			from @DocXml.nodes('/Файл/Документ/СвКСчФ') T(c);

	-- обработка исправительных/корректировочных/обычных
	if @isCorrSf = 1			-- корректировочный
		select @vCorrType=2, @vCorrDate=@vSfDate, @vCorrNumber=trim(@SfNumber)
	else if @isIspravSf = 1		-- исправительный
		begin
			select @vCorrType=1
					, @vCorrDate=c.value('@ДатаИспрСчФ', 'varchar(32)')
					, @vCorrNumber=trim(c.value('@НомИспрСчФ', 'varchar(64)'))
				from @DocXml.nodes('/Файл/Документ/СвСчФакт/ИспрСчФ') T(c);
		end
	else	-- не корректировочный и не исправительный - значит обычный
		set @vCorrType=0
		

	if @vSfDate is null or (@SfNumber is null or @SfNumber = '' )
		return 0

	-- расчитываем адрес компании
	-- получаем для продавца ИНН, КПП, Компания, Адрес компании
	if @isCorrSf = 0
			select @vSellInn=c.value('@ИННЮЛ', 'varchar(16)')
					, @vSellKpp=c.value('@КПП', 'varchar(16)')
					, @vSellCompanyName=c.value('@НаимОрг', 'varchar(128)')
				from @DocXml.nodes('/Файл/Документ/СвСчФакт/СвПрод/ИдСв/СвЮЛУч') T(c);
		else
			select @vSellInn=c.value('@ИННЮЛ', 'varchar(16)')
					, @vSellKpp=c.value('@КПП', 'varchar(16)')
					, @vSellCompanyName=c.value('@НаимОрг', 'varchar(128)')
				from @DocXml.nodes('/Файл/Документ/СвКСчФ/СвПрод/ИдСв/СвЮЛУч') T(c);

		if @isCorrSf = 0
			select @vSellAddress=c.value('@АдрТекст', 'varchar(255)')
				from @DocXml.nodes('/Файл/Документ/СвСчФакт/СвПрод/Адрес/АдрИнф') T(c);
		else
			select @vSellAddress=c.value('@АдрТекст', 'varchar(255)')
				from @DocXml.nodes('/Файл/Документ/СвКСчФ/СвПрод/Адрес/АдрИнф') T(c);

	/* Есть 2 вида XML - для источника 2 попадается XML, где адрес содержится в теге АдрРФ 
	и он там разобран на отдельные параметры, из которого его надо собирать */
	if @vSellAddress is null
	begin
		declare 
			@vSellAddrIndex varchar(32) = null
			, @vSellAddrStreet varchar(64) = null
			, @vSellAddrTown varchar(64) = null
			, @vSellAddrDom varchar(32) = null
			, @vSellAddrKorpus varchar(32) = null
			, @vSellAddrKv varchar(32) = null
			, @vSellAddrDistrict varchar(32) = null


		if @isCorrSf = 0
			select	@vSellAddrIndex=c.value('@Индекс', 'varchar(32)')
					,@vSellAddrStreet=c.value('@Улица', 'varchar(64)')
					,@vSellAddrTown=c.value('@Город', 'varchar(64)')
					,@vSellAddrDom=c.value('@Дом', 'varchar(32)')
					,@vSellAddrKorpus=c.value('@Корпус', 'varchar(32)')
					,@vSellAddrKv=c.value('@Кварт', 'varchar(32)')
					,@vSellAddrDistrict=c.value('@Район', 'varchar(32)')
				from @DocXml.nodes('/Файл/Документ/СвСчФакт/СвПрод/Адрес/АдрРФ') T(c);
		else
			select	@vSellAddrIndex=c.value('@Индекс', 'varchar(32)')
					,@vSellAddrStreet=c.value('@Улица', 'varchar(64)')
					,@vSellAddrTown=c.value('@Город', 'varchar(64)')
					,@vSellAddrDom=c.value('@Дом', 'varchar(32)')
					,@vSellAddrKorpus=c.value('@Корпус', 'varchar(32)')
					,@vSellAddrKv=c.value('@Кварт', 'varchar(32)')
					,@vSellAddrDistrict=c.value('@Район', 'varchar(32)')
				from @DocXml.nodes('/Файл/Документ/СвКСчФ/СвПрод/Адрес/АдрРФ') T(c);

		declare @delim varchar(8) = ', '
		set @vSellAddress = 
			iif(len(@vSellAddrIndex)>0, @delim + @vSellAddrIndex, '')
			+ iif(len(@vSellAddrDistrict)>0, @delim + @vSellAddrDistrict, '')
			+ iif(len(@vSellAddrTown)>0, @delim + @vSellAddrTown, '')
			+ iif(len(@vSellAddrStreet)>0, @delim + @vSellAddrStreet, '')
			+ iif(len(@vSellAddrDom)>0, @delim + @vSellAddrDom, '')
			+ iif(len(@vSellAddrKorpus)>0, @delim + @vSellAddrKorpus, '')
			+ iif(len(@vSellAddrKv)>0, @delim + @vSellAddrKv, '')
		
		if len(@vSellAddress) > 0
			set @vSellAddress = stuff(@vSellAddress, 1, len(@delim), '')
	end
end

GO
