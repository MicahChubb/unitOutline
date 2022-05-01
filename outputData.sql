CREATE PROCEDURE [dbo].[unitOut]
	@ID int
AS
	SELECT Course, Unit, "Unit Description", Accreditation, "Unit Goals", "Content Descriptors" from tblUnits WHERE Id = @ID
RETURN
