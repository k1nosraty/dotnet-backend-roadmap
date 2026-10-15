using SupportDesk.Infrastructure;
return await Lab.RunAsync("lab37", args, async (db, token) =>
{
    await Lab.ScriptAsync(db, "01-schema.sql", token);
    await Lab.ScriptAsync(db, "02-seed.sql", token);
    await Lab.PrintAsync(db, "03-queries.sql", token);
    await Lab.ScriptAsync(db, "04-checks.sql", token);
});
