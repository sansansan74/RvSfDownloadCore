
CREATE view [web].[DocV] as
with att as (
	select da.DocId, da.AttachId, da.AttachTypeId, a.AttachFileName, a.AttachFileBody
		from dbo.Doc2Attach da 
			left join dbo.Attach a on da.AttachId=a.AttachId
)
SELECT 
	d.[DocId]
	, d.[DocVagon]
	, d.[DocRepairDate]
	, d.[DocSfNumber]
	, d.[DocSfDate]
	, d.[DocRepairContract]
	, d.[DocRepairContractor]
	, d.[DocDepoCode]
	, u.[DocRvRepairId]
	, de.EdoRvId DocRvSfId
	, cast (de.EdoSign1Date as Date) DocSfSign1Date
	, de.EdoSign1Date as DocSfSign1DateTime
	, cast(de.EdoSign2Date as Date) DocSfSign2Date
	, de.EdoSign2Date DocSfSign2DateTime
	, de.EdoNeedReload DocNeedReload
	, cast(attSfXml.AttachFileBody as xml) as DocXml	/* d.[DocXml] */
	, DocBody = cast (null as varbinary(100)) /* d.[DocBody] */
	, attSfPdf.AttachFileBody DocSfPdfBody			/* d.[DocSfPdfBody] */
	, DocName = cast (null as varchar(100)) /* d.[DocName] */
	, cast (d.[DocInserted] as Date) DocInsertedDate
	, d.[DocInserted] as DocInsertedDateTime
	, cast(d.[DocUpdated] as Date) DocUpdatedDate
	, d.[DocUpdated] as DocUpdatedDateTime
	, dc.dpName as DocDepoName
	, iif (d.DocUdalen=1, 'Да', 'Нет') as DocUdalen
	, u.SourceId
	, d.SellInn
	, d.SellKpp
	, d.SellCompanyName
	, d.SellAddress
	
  FROM dbo.DocUpdate u 
	join dbo.Doc d on u.DocId=d.DocId
	left join [nsi].[Depo] dc on d.DocDepoCode = dc.dpCode
	left join dbo.DocIgnore di on u.DocRvRepairId=di.DocRvRepairId
	
	left join att attSfXml on d.DocId=attSfXml.DocId and attSfXml.AttachTypeId=4	/* SfXml */
	left join att attSfPdf on d.DocId=attSfPdf.DocId and attSfPdf.AttachTypeId=3	/* Zip-archive */

	join dbo.Doc2Edo de on u.DocId=de.DocId and de.EdoStateId=0	and de.Doc2EdoDeleteDate is null -- это важно - теперь документ может встречаться более одного раза, обычный и устаревший EdoStateId=20
	join dbo.EdoType2Source et on de.EdoTypeId=et.EdoTypeId and u.SourceId=et.SourceId and et.AttachTypeId=2 /* Zip-archive */
  where di.DocRvRepairId is null
	and (de.EdoSign1Date is not null or de.EdoSign2Date is not null)

