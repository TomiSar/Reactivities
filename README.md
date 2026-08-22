# Reactivities Project Repository

This has been rewritten from scratch to take advantage of and to make it (hopefully) a bit more futureproof. This app is built using .Net 8 and React 18

# Running the project

To get into the app you will need to sign up with a valid email account or just use GitHub login as email verification is part of the app functionality in the published version of the app.

You can also run this app locally. The easiest way to do this without needing a database server is to use the version of the app before publishing which does not require a valid email address or Sql Server. Most of the functionality will work except for the photo upload which would require you to sign up to Cloudinary (free) and use your own API keys here. You need to have the following installed on your computer for this to work:

1. .Net SDK v7 or v8
2. NodeJS (at least version 18+ or 20+)
3. git

Once you have these then you can do the following:

1. Clone the project in a User folder on your computer by running:

```bash
# you will of course need git installed to run this
git clone https://github.com/TomiSar/Reactivities.git
cd Reactivities
```

2. Restore the packages by running:

```bash
# From the solution folder (Reactivities)
dotnet restore

# Change directory to client-app to run the npm install.  Only necessary if you want to run
# the react app in development mode using the Vite dev server
cd client-app
npm install
```

3. Setup Postgres Database create a file called appsettings.Development.json in the Reactivities/API folder and copy/paste the following configuration.

```json
{
  "Logging": {
    "LogLevel": {
      "Default": "Information",
      "Microsoft.AspNetCore": "Information"
    }
  },
  "ConnectionStrings": {
    "DefaultConnection": "Server=localhost; Port=5432; User Id={USERNAME}; Password={PASSWORD}; Database={DATABASE}"
  },
  "TokenKey": "{TOKENKEY}"
}
```

4. If you wish for the photo upload to work create a file called appsettings.json in the Reactivities/API folder and copy/paste the following configuration.

Create an account (free of charge, no credit card required) at https://cloudinary.com and then replace the Cloudinary keys in the appsettings.json file with your own cloudinary keys.

```json
{
  "Logging": {
    "LogLevel": {
      "Default": "Information",
      "Microsoft.AspNetCore": "Warning"
    }
  },
  "CloudinarySettings": {
    "CloudName": "{CLOUD_NAME}",
    "ApiKey": "{API_KEY}",
    "ApiSecret": "{API_SECRET}"
  },
  "AllowedHosts": "*"
}
```

5. You can then run the app and browse to it locally by running:

```bash
# run this from the API folder in one terminal/command prompt
cd API
dotnet run
dotnet watch run

# open another terminal/command prompt tab and run the following
cd client-app
npm run start

```

5. Run ESLint that statically analyzes your code to quickly find problems. Fix errors and warnings that occur.

```bash
cd client-app
npm run lint
npm run lint-fix
```

6. You can then browse to the app on https://localhost:3000 and login with either of the test users:

   email: bob@test.com or tom@test.com or jane@test.com

   password: Pa$$w0rd

## Database Migrations

Whenever you make changes to the data models in the `Domain` project, you need to generate a new migration and apply it to the PostgreSQL database.

### Add a New Migration

Generates the boilerplate C# code required to update the database schema. Run this command from the solution root directory Reactivities:

```bash
dotnet ef migrations add MigrationName -p Persistence -s API
```

- `-p Persistence`: Specifies the target project where the migration files will be created.
- `-s API`: Specifies the startup project containing the configuration and connection string.

### Update the Database after migration

Applies all pending migrations to your local PostgreSQL database instance:

```bash
dotnet ef database update -p Persistence -s API
```

_Note: The application is also configured to run migrations automatically on startup via `context.Database.MigrateAsync()` in `Program.cs`._
