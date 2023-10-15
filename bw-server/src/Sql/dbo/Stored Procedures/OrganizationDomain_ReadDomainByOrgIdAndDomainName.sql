CREATE PROCEDURE [dbo].[OrganizationDomain_ReadDomainByOrgIdAndDomainName]
    @OrganizationId UNIQUEIDENTIFIER,
    @DomainName NVARCHAR(255)
AS
BEGIN
    SET NOCOUNT ON

SELECT
    *
FROM
    [dbo].[OrganizationDomain]
WHERE
    [OrganizationId] = @OrganizationId
  AND
    [DomainName] = @DomainName
END