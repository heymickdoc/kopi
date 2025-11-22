<p align="center">
</p>

<h1 align="center">Kopi</h1>

<p align="center">
<strong>Blazing-fast database replication and realistic test data generation in a single command.</strong>
</p>

<p align="center">
<img src="https://github.com/heymickdoc/kopi/actions/workflows/dotnet.yml/badge.svg" alt="Build Status">
<img src="https://img.shields.io/nuget/v/Kopi.svg" alt="NuGet Version">
<img src="https://img.shields.io/badge/license-MIT-blue.svg" alt="License">
</p>

## What is Kopi?

Kopi (pronounced "copy") is a command-line tool designed to solve a common, painful problem: getting a realistic, isolated database for local development.

As a developer, you don't need a 1TB production backup just to test a new feature. You need the **full production schema**, but you probably only need data in the **10 tables you're actually working with**, not all 200.

Kopi is *not* a database restore tool. It's a **surgical slicing** tool. It works in two stages:

1. **Schema Replication:** `kopi` reads your source database's *schema* (starting with **Microsoft SQL Server**... others to follow!) and perfectly recreates it (tables, views, functions, stored procedures, etc.) in a fresh, local Docker container in seconds.

2. **Surgical Data Generation:** You tell Kopi which tables you care about. It intelligently generates realistic, relational test data *only for that slice*, giving you a 50MB database, not a 1TB monster.

`Note: Kopi doesn't copy any actual data from your source database. It only reads the source DB schema and generates new, synthetic data for the tables you need.`

## Key Features

* **Blazing Fast:** Replicates complex schemas and generates data in seconds.
* **Surgical Slicing:** Doesn't restore entire databases. It intelligently generates a small, referentially-intact *slice* of your database. [Learn more](#the-kopi-difference-surgical-slicing).
* **Relational Awareness:** Understands foreign keys and automatically generates data for parent/grandparent tables.
* **Smart Data:** Generates realistic data for common column types (names, emails, addresses) instead of just "Lorem Ipsum."
* **Single Command:** Run `kopi up` and your database is ready.
* **Extensible:** Built on an open-core model, so you can extend the core library with your own data generators.

## The Kopi Difference: Surgical Slicing

### The Problem: Slow, "All-or-Nothing" Restores

In modern development, a full database restore is the standard. If your production database is 1TB, you wait 30+ minutes to get a 1TB copy. This is an "all-or-nothing" proposition, and it's slow, expensive, and terrible for CI/CD pipelines.

Even worse, you get the *whole database* when you may only need to work with 3-4 tables.

That's assuming you have somewhere to restore it to, and that your local environment can handle it. Often, developers resort to using shared development databases, which leads to conflicts and instability.

### The Kopi Solution: A Lightweight, Surgical Slice

Kopi is different. You don't restore *anything*. You specify the "seed" tables you care about in your `kopi.json` (e.g., `Sales.SalesOrderDetail`).

Kopi then:

1. Analyzes all foreign key dependencies for your seed tables.
2. Performs a topological sort to find the complete "slice" of required parent and grandparent tables.
3. Generates a *small, referentially-intact* database in Docker containing *only* the data you need.

Instead of waiting 30 mins for a 1TB monster, you wait 10 seconds for a 5MB database with exactly the data you need. This is the "surgical slicing" that makes Kopi so fast.

## Supported Databases

Kopi is being built to support multiple database engines. The table below shows the current status and roadmap.

| Feature | Microsoft SQL Server | PostgreSQL | SQLite | MySQL / MariaDB | 
 | ----- | ----- | ----- | ----- | ----- | 
| **Schema Replication** |  |  |  |  | 
| Tables & Keys | ✅ | 🗓️ | 🗓️ | 🗓️ | 
| Views | ✅ | 🗓️ | 🗓️ | 🗓️ | 
| Stored Procedures & Functions | ✅ | 🗓️ | 🗓️ | 🗓️ | 
| User-Defined Types | ✅ | 🗓️ | 🗓️ | 🗓️ | 
| **Data Generation** |  |  |  |  | 
| Heuristic Data (Community) | ✅ | 🗓️ | 🗓️ | 🗓️ | 
| Relational Awareness (FKs) | ✅ | 🗓️ | 🗓️ | 🗓️ | 

**Legend:**

* ✅ **Supported:** Implemented and available in the Community Edition.
* 🗓️ **Planned:** On the roadmap!
* (Enterprise features like Anonymization and AI-powered generation are tracked separately).

## Installation

Kopi is cross-platform and runs on Windows, macOS (Apple Silicon & Intel), and Linux.

### Method 1: .NET Global Tool (Recommended)

If you have the **.NET 8 SDK** installed, this is the easiest method. It works identically on all operating systems.

1. **Install:**
   ```sh
   dotnet tool install --global Kopi
   ```

2. **Update (in the future):**
   ```sh
   dotnet tool update --global Kopi
   ```

#### ⚠️ Troubleshooting: "Command not found" on macOS / Linux

If you install Kopi but running `kopi` gives you a `command not found` error, your .NET tools folder is likely not in your system PATH.

**Fix for macOS (Zsh):**
Run these commands to add the folder to your path:
```sh
echo 'export PATH=$PATH:$HOME/.dotnet/tools' >> ~/.zshrc
source ~/.zshrc
```

**Fix for Linux (Bash):**
```sh
echo 'export PATH=$PATH:$HOME/.dotnet/tools' >> ~/.bashrc
source ~/.bashrc
```

---

### Method 2: Standalone Binary (No SDK Required)

If you do not want to install the .NET SDK, you can download a standalone executable from the [Releases Page](https://github.com/heymickdoc/kopi/releases).

1. Download the zip file matching your OS (e.g., `Kopi-mac-arm64.zip`).
2. Extract the file.
3. Open your terminal in that folder.

#### 🍎 macOS Users: "Unidentified Developer" Warning

On macOS, you may see a "Developer cannot be verified" popup due to Apple's Gatekeeper. To fix this, you must remove the quarantine flag from the downloaded file:

```sh
# 1. Make it executable
chmod +x Kopi

# 2. Remove the "quarantine" flag
xattr -d com.apple.quarantine Kopi

# 3. Run it
./Kopi up
```

## Quick Start

1. **Install `kopi`** (see above).

2. **Create a config file:** In your project's root, create a `kopi.json` file. This example shows specifying multiple "seed" tables.

   ```json
   {
     "sourceConnectionString": "Server=tcp:your-server.database.windows.net;...",
     "saPassword": "YourOptionalPassword123!",
     "tables": [
       "Production.Product",
       "Person.Person",
       "Sales.SalesOrderDetail"
     ],
     "settings": {
       "maxRowCount": 100
     }
   }
   ```

   *Note: The `saPassword` field is optional. If omitted, Kopi will use the default `SuperSecretPassword123!`.*

3. **Run Kopi:** Open your terminal and run:

   ```sh
   kopi up
   ```

4. **Run with flags:** You can use flags to specify a config file path or override the password.

   ```sh
   kopi up -c "./path/to/my-config.json" -p "MySecurePassword!"
   ```

Kopi will connect to your source, read the schema, spin up a new Docker container, apply the schema, and generate test data. It will then print the new connection string for your local database.

Run `kopi -h` to see all available commands and options.

## ✨ Looking for More? Kopi Enterprise

Kopi Community Edition is free and open-source (MIT licensed), designed for individual developers seeking rapid database setup for local development.

For professional teams requiring advanced features like deterministic data generation (for stable CI/CD pipelines), PII anonymization, and AI-driven data generation, we are building Kopi Enterprise.

`Kopi Enterprise is not ready yet, but if you're interested in the roadmap, please visit our website.`

[**Learn more about Kopi Enterprise at kopidev.com**](https://kopidev.com)

## Contributing

For now, contributions are not being accepted while the initial versions are developed. This section will be updated when the project is ready for community contributions.

The underlying library, `Kopi.Core`, is also available on [NuGet](https://www.nuget.org/packages/Kopi.Core/) if you wish to build on top of it. The `Kopi` tool is the runnable CLI that consumes this library.

## License

The Kopi Community Edition and `Kopi.Core` are licensed under the **MIT License**. See the `LICENSE` file for details.