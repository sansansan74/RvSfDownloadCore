
-- =============================================
-- Author:		Alex
-- Create date: 17.02.2021
-- Description:	Возвращает 1, если ремонт был проверен вагонником
-- =============================================
CREATE FUNCTION [dbo].[IsVagonnikChecked]
(
	@mask varchar(32)
)
RETURNS int
AS
BEGIN
	/*
		Правила:
		если маска '' или NULL - не проверен
		если маска состоит из 1 и # - проверен
	*/

	if @mask is null
		return 0

	set @mask = trim(@mask)
	if @mask = ''
		return 0

	set @mask = replace(replace(@mask, '1', ''), '#', '')

	-- Return the result of the function
	return iif(@mask = '', 1, 0)
END


GO
