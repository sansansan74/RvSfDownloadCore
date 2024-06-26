
-- =============================================
-- Author:		Alex
-- Create date: 24.01.2019
-- Description:	Возвращает 1, если строка вида dd.mm.yyyy разделить может быть (.-/)
-- =============================================
CREATE FUNCTION [dbo].[isRussianDate] 
(
	@strDate varchar(16)
)
RETURNS int
AS
BEGIN
	IF @strDate is null
		RETURN 0	

	if @strDate NOT like '[0-9][0-9][./-][0-9][0-9][./-][0-9][0-9][0-9][0-9]'
		RETURN 0

	RETURN isdate( [dbo].[Str2DateString](@strDate) )
END
GO
