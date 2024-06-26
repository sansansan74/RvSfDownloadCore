
CREATE proc [dbo].[SaveEdoActParams] (
	@DocId int
	, @Doc2EdoId bigint
	, @AttachTypeId int
	, @EdoStateId tinyint
)
as
begin
	SET XACT_ABORT, NOCOUNT ON;

	if @AttachTypeId=6			-- Для документов типа СФ
			and @EdoStateId=0	-- документ актуальный, а не отмененный. У отмененного @EdoStateId=20
	begin
		-- найти СФ-xml в архиве. Он лежит в корне архива и имеет расширение xml. Не должно быть более 1 такого файла		
		if (select count(*) from dbo.EdoSfXml(@Doc2EdoId)) <> 1
		begin
			raiserror('В архиве с Акт только 1 файл должен лежать в корне архива и иметь расширение XML. Это и есть Акт.', 16, 1)
			return 0
		end

		declare @vCorrActType int
				, @vCorrActDate varchar(32)
				, @vCorrActNumber varchar(64)
				, @DocXmlBinary varbinary(MAX)

		select @DocXmlBinary = AttachFileBody	-- получаем сжатое тело SfXml
			from dbo.EdoSfXml(@Doc2EdoId)			

		declare @DocXml xml = cast( decompress(@DocXmlBinary) as xml)	-- Тело СФ xml


		-- получаем из XML акта Тип акта (обычный, корректировочный, исправительные), 
		-- дату корректировки/исправления и номер корректировки/исправления)
		exec dbo.ParseActXml 
			   @DocXml
			  , @vCorrActType OUTPUT
			  , @vCorrActDate OUTPUT
			  , @vCorrActNumber OUTPUT

		-- прописываем значения из Act xml в документ dbo.Doc
		update dbo.Doc	
			set
				CorrectActType = @vCorrActType
				, CorrectActDate = dbo.Str2Date(@vCorrActDate)
				, CorrectActNumber = @vCorrActNumber
				, DocUpdated = getdate()
			where DocId = @DocId
	end
end

