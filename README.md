<p align="center">
</p>

<h1 align="center">Kopi</h1>

<p align="center">
<strong>Blazing-fast database replication and realistic test data generation in a single command.</strong>
</p>

<p align="center">
<img src="https://github.com/heymickdoc/kopi/actions/workflows/dotnet.yml/badge.svg" alt="Build Status">
<img src="https://img.shields.io/nuget/v/Kopi.Core.svg" alt="NuGet Version">
<img src="https://img.shields.io/badge/license-MIT-blue.svg" alt="License">
</p>

## What is Kopi?

Kopi (pronounced "copy") is a command-line tool designed to solve a common, painful problem: getting a realistic, isolated database for local development. Quickly!

As a developer, you don't need a 1TB production backup just to test a new feature. You need the **full production schema**, but you probably only need data in the **10 tables you're actually working with**, not all 200.

Kopi is *not* a database restore tool. It's a **surgical slicing** tool. It works in two stages:

1. **Schema Replication:** `kopi` reads your source database's *schema* (starting with **Microsoft SQL Server**... others to follow!) and perfectly recreates it (tables, views, functions, stored procedures, etc.) in a fresh, local Docker container in seconds.

2. **Surgical Data Generation:** You tell Kopi which tables you care about. It intelligently generates realistic, relational test data *only for that slice*, giving you a 50MB database, not a 1TB monster.

`Note: Kopi doesn't copy any actual data from your source database. It only reads the source DB schema and generates new, synthetic data for the tables you need.`

## Key Features

* **Blazing Fast:** Replicates complex schemas and generates data in seconds.

* **Surgical Slicing:** Doesn't restore entire databases. It intelligently generates a small, referentially-intact *slice* of your database. [Learn more](https://www.google.com/search?q=%23the-kopi-difference-surgical-slicing).

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

Kopi is different. You don't restore *anything*. You specify the "seed" tables you care about in your `kopi.json` (e.g., `Sales.SalesOrderDetail`, `Sales.Amount`, `HumanResources.Person`).

Kopi then:

1. Analyzes all foreign key dependencies for your seed tables.

2. Performs a topological sort to find the complete "slice" of required parent and grandparent tables.

3. Generates a *small, referentially-intact* database in Docker containing *only* the data you need, for *only* the tables you need.

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

Kopi is distributed as a .NET Global Tool.

1. Make sure you have the <a href="https://dotnet.microsoft.com/en-us/download" target="\_blank">.NET SDK (8.0 or later)</a> installed.

2. Make sure you have <a href="https://www.docker.com/products/docker-desktop/" target="\_blank">Docker Desktop</a> installed and running.

3. Install the Kopi CLI from NuGet with the following command: