# Overview
Since the `API` project is our entrypoint, all the commands below should be executed in the `API` project directory. AKA -- `cd` into API project for this commands to 
work

## Tools

Globally install (so it works on any project, not just this) the entity framework tool package.

```bash
dotnet tool install --global dotnet-ef
```

## Apply / Update Database
Whenever you need to recreate the database, or apply new updates run the following command

```bash
dotnet ef database update
```

## Migrations 
In the future, if you ever make a change to one of the EF models, or add new ones in the `Data` project folder you need to create a new migration!

```bash
dotnet ef migrations add NAME_OF_YOUR_MIGRATION_HERE
```

This only creates a migration file. It allows you to view the migration to ensure everything looks good to go. If things look good, execute the update command
