CREATE VIEW [report].[vDocSf] AS 
	SELECT
		u.DocId
		,u.DocRvRepairId
		,d.DocActNumber
		,d.DocActDate
		,d.DocSfNumber
		,d.DocSfDate
		,d.DocVagon 
		,d.SellInn
		,d.SellKpp
		,d.SellCompanyName
		,d.DocDepoCode
		,d.DocSumWithNds
		,u.SourceId
		,e.EdoSign1Date
		,e.EdoSign2Date
		,d.VagonChanged
		,d.DocUdalen
		, HasSign = iif(e.EdoSign1Date IS NOT NULL or e.EdoSign2Date IS NOT NULL, 1, 0)
		, d.CorrectActType
		, d.CorrectActNumber
		, d.CorrectActDate
		, d.CorrectSfType
		, d.CorrectSfNumber
		, d.CorrectSfDate
	FROM dbo.DocUpdate u WITH(NOLOCK)
		JOIN dbo.Doc d WITH(NOLOCK) ON d.DocId=u.DocId
		JOIN dbo.Doc2Edo e WITH(NOLOCK) ON u.DocId=e.DocId 
		JOIN dbo.EdoType2Source t WITH(NOLOCK) ON u.SourceId=t.SourceId AND e.EdoTypeId=t.EdoTypeId
	WHERE  
		(
			(d.DocSfDate>='20190301' AND u.SourceId=1) --ТОР ЦВ с 01.03.2019
			OR 
			(d.DocSfDate>='20190701' AND u.SourceId=2)
			OR
			u.SourceId IN (3,4,5)
		)
		AND t.AttachTypeId=2	-- выгружаем ЭСФ
		and e.EdoStateId=0
		and (e.Doc2EdoDeleteDate is null)	-- строка не была удалена

