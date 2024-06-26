
-- =============================================
-- Author:		Alex
-- Create date: 28.04.2019
-- Description:	Разбирает 1 тег ВАГОН. 
-- Если вагона в dbo.Doc еще не было, то создает новую запись и прикрепляет XML-документ.
-- Если вагон в dbo.Doc был, то обновляет запись и обновляет XML-документ.
-- =============================================
CREATE proc [load].[VagonSave] 
	@vagonXml varbinary(max)			-- XML для разбора
	, @SourceId int 					-- Источник 1=ТОР ЦВ или 2=АСУ ВРК
as
begin
	SET XACT_ABORT, NOCOUNT ON

		declare @errorMessage varchar(4000), @errorSeverity int

		declare @vagonteg xml = cast( @vagonXml as xml )
		declare @DocId int = null
		declare @DocVagon int						-- номер вагона
		declare @DocRepairStart date				-- дата начала ремонта
		declare @vDocCode varchar(32)				-- ид ремонта
		declare @DocSumWithNds decimal(19,9)		-- сумма ремонта с НДС
		declare @DocSumUborka		decimal(19,9)	-- сумма уборки вагона
		declare @VagonChanged		datetime		-- Дата изменения ВАГОН.Изменен
		declare @VagonChangedOld	datetime		-- Дата изменения ВАГОН.Изменен
		declare @DocUdalen	tinyint					-- признак удаления
		declare @DocCheckMask	varchar(32) = null	-- маска проверки - на каком этапе осуществляется проверка

		declare @DocRepairContract varchar(128) 	-- договор с депо
		declare @DocDepoCode	int 				-- код депо
		declare @DocIdFirst			varchar(32) = NULL	-- для отслеживани ситуации, когда в таблицу doc еще не вставлена запись
		declare @EcpDocumentsTeg xml				-- тег с документами ЭЦП
		declare @ContractTeg xml					-- тег с договором
		declare @DocType		int	= null			-- вид документа
		declare @ECP varchar(4)		 = null			-- ECP ремонта. Статус электронного подписания

		declare @DocRvContractCode			bigint = null	-- код договора во внутреннем справочнике RV.RU
				, @DocRvMainContragentCode	bigint = null	-- код главного исполнителя во внутреннем справочнике RV.RU
				, @DocRvDepoContragentCode	bigint = null	-- код исполнителя депо во внутреннем справочнике RV.RU

		declare @DocActNumber	varchar(128)			-- Номер акта
				, @DocActDate	date					-- Дата акта
				, @DocWorkflow	tinyint					-- Порядок проходжения подписания (работает для АСУ ВРК)
				, @DocCorrAvr	tinyint					-- Признак того, что акт корректировался

		declare @RepairStation int = NULL				-- Станция ремонта


		declare @DocNeedReload	char(1)			-- признак того, что документ надо перекачать

		declare @dtnull datetime = '19900101'
		declare @xstate int

		declare @trancount int = @@trancount
		declare @tranname varchar(64) = '[load].[VagonSave]'

		exec [dbo].[VagonTegParse] 
			@vagonteg 						-- XML для разбора
			, @SourceId						-- источник ТОР ЦВ или АСУ ВРК
			, @DocVagon output				-- номер вагона
			, @DocRepairStart output		-- дата начала ремонта
			, @vDocCode output
			, @DocRepairContract output 	-- договор с депо
			, @DocDepoCode	output 			-- код депо
			, @DocSumWithNds output			-- сумма ремонта с НДС
			, @DocSumUborka output			-- сумма уборки вагона
			, @VagonChanged output			-- Дата изменения ВАГОН.Изменен
			, @DocUdalen	output			-- признак удаления
			, @EcpDocumentsTeg output
			, @DocCheckMask output			-- маска обработки акта - этапы обработки вагонник, экономист
			, @ContractTeg output
			, @DocActNumber output			-- Номер акта
			, @DocActDate output			-- Дата акта
			, @DocWorkflow output			-- Порядок проходжения подписания (работает для АСУ ВРК)
			, @DocCorrAvr	output			-- Признак того, что акт корректировался
			, @DocType output				-- Вид документа
			, @ECP output					-- ECP ремонта. Статус электронного подписания
			, @RepairStation output			-- Станция ремонта
			

		if isnull(@vDocCode, '')  = ''
		begin
			raiserror ('Не могу считать из XML тега ВАГОН код документа на портале', 16, 1)
			return
		end

		select @DocIdFirst=u.DocId
				, @DocId = d.DocId				-- Будет NULL, есди еще не вставили акт - первый раз
				, @DocNeedReload=u.DocNeedReload
				, @VagonChangedOld = d.VagonChanged	-- получаем дату обновления из таблицы dbo.Doc - старая дата обновления
			from dbo.DocUpdate u
				left join dbo.Doc d on u.DocId=d.DocId
			where u.SourceId=@SourceId and u.DocRvRepairId=@vDocCode --  dd.DocRvSfId=@DocRvSfId

		begin try
			begin tran
				if @DocId is null	-- записи нет - вставить такую новую запись
					begin
						set @DocId = @DocIdFirst

						exec dbo.RvContractEdit @SourceId, @ContractTeg ,@DocRvContractCode OUTPUT, @DocRvMainContragentCode OUTPUT ,@DocRvDepoContragentCode OUTPUT

						insert into dbo.Doc (DocId,		DocVagon,	DocRepairDate, DocRepairContract,	DocDepoCode, DocSumWithNds, DocSumUborka,	VagonChanged, DocUdalen, DocCheckMask, DocRvContractCode,	DocRvMainContragentCode, DocRvDepoContragentCode, DocActNumber, DocActDate, DocWorkflow, DocCorrAvr, DocType, DocEcp) 
									values (@DocId, @DocVagon, @DocRepairStart, @DocRepairContract, @DocDepoCode, @DocSumWithNds, @DocSumUborka, @VagonChanged, @DocUdalen, @DocCheckMask, @DocRvContractCode,	@DocRvMainContragentCode, @DocRvDepoContragentCode, @DocActNumber, @DocActDate, @DocWorkflow, @DocCorrAvr, @DocType, @ECP)
											

						exec dbo.AttachAdd @docId, 1, 'act_source.xml', @vagonXml
						exec dbo.EdoDocLinksEdit @SourceId, @docId, @EcpDocumentsTeg, @DocRepairStart
						exec dbo.PrintFormDocLinksEdit @SourceId, @docId, @docUdalen, @DocCheckMask

						update du
							set 
								du.DocVagonChanged = @VagonChanged
							from dbo.DocUpdate du
							where du.SourceId=@SourceId and du.DocRvRepairId=@vDocCode

					end
				else if  @VagonChangedOld<@VagonChanged or @DocNeedReload = 'F'	-- forse reload
					begin
						exec dbo.RvContractEdit @SourceId, @ContractTeg ,@DocRvContractCode OUTPUT, @DocRvMainContragentCode OUTPUT ,@DocRvDepoContragentCode OUTPUT

						update dbo.Doc		-- Пишем, что надо обновить документы. И устанавливает даты обновления, которые будут после обновления.
							set  VagonChanged = @VagonChanged		-- Дата обновления из тега ВАГОН.Изменен
								, DocUdalen = @DocUdalen			-- признак удаления
								, DocVagon = @DocVagon
								, DocRepairDate = @DocRepairStart
								, DocRepairContract = @DocRepairContract
								, DocDepoCode = @DocDepoCode
								, DocSumWithNds = @DocSumWithNds
								, DocSumUborka = @DocSumUborka
								, DocCheckMask = @DocCheckMask
								, DocRvContractCode = @DocRvContractCode
								, DocRvMainContragentCode=@DocRvMainContragentCode
								, DocRvDepoContragentCode=@DocRvDepoContragentCode
								, DocActNumber=@DocActNumber
								, DocActDate=@DocActDate
								, DocWorkflow=@DocWorkflow
								, DocCorrAvr=@DocCorrAvr
								, DocType = @DocType
								, DocECP = @ECP
								, DocUpdated = GETDATE()
							where DocId=@DocId

						exec dbo.AttachEdit @docId, 1, 'act_source.xml', @vagonXml
						exec dbo.EdoDocLinksEdit @SourceId, @docId, @EcpDocumentsTeg, @DocRepairStart
						exec dbo.PrintFormDocLinksEdit @SourceId, @docId, @docUdalen, @DocCheckMask

						exec dbo.TryRallbackFinish @DocId, @SourceId, @VagonChanged, @DocCheckMask		-- гасим откат, если комплект согласовали с прежней СФ, которая была до отката

						update du
							set 
								du.DocVagonChanged = @VagonChanged
								--, du.DocRollbackCheckMask=NULL		-- Скидываем маску отката
								--, du.DocRollbackDate = GETDATE()	-- Скидываем дату отката - признак отката
							from dbo.DocUpdate du
							where du.SourceId=@SourceId and du.DocRvRepairId=@vDocCode

					end		


				exec dbo.AutoSignAddTask @SourceId , @DocVagon, @vDocCode, @DocUdalen, @DocCheckMask, @ECP, @DocType, @DocDepoCode, @RepairStation
				
				-- Проставляю признак, что документ обновлен
				update du
					set du.DocNeedReload='N'
						, du.DocSaveErrorMessage=NULL	-- Стираем ошибку, если была
						, du.DocSaveErrorDate=NULL
						, du.DocSaveErrorTryCount=0
						, du.DocProcessed = GETDATE()
						--, du.DocVagonChanged = 
					from dbo.DocUpdate du
					where du.SourceId=@SourceId 
						and du.DocRvRepairId=@vDocCode
						--and du.DocVagonChanged<=@VagonChanged	-- считаем обновлением, если мы обновились на >= дату, чем требовалось
		
			commit tran;
		end try
		begin catch
			-- в этом блоке просто откатываем транзакцию и прокидываем ошибку дальте
			select @ErrorMessage = ERROR_MESSAGE(), @xstate = XACT_STATE();
			rollback tran

			raiserror (@ErrorMessage, 16, 1)
		end catch	
end

