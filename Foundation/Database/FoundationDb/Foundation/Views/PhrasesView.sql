CREATE VIEW [Foundation].[PhrasesView]
	AS SELECT *, cast(ConcurrencyToken as bigint) as Version FROM [Foundation].[Phrases]
