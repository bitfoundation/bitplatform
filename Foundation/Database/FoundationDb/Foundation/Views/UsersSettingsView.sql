CREATE VIEW [Foundation].[UsersSettingsView]
	AS SELECT *,cast(ConcurrencyToken as bigint) as Version FROM [Foundation].[UsersSettings]
