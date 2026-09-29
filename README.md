<div align="center">

# Kopi
### The Developer Database Slicer

[![Build Status](https://github.com/heymickdoc/kopi/actions/workflows/dotnet.yml/badge.svg)](https://github.com/heymickdoc/kopi/actions)
[![NuGet Version](https://img.shields.io/nuget/v/Kopi.svg)](https://www.nuget.org/packages/Kopi/)
[![License](https://img.shields.io/badge/license-MIT-blue.svg)](LICENSE)

**Stop waiting for 500GB backups. Start coding with realistic, relational data in seconds.**

[**Installation**](#installation) • [**Quick Start**](#quick-start) • [**How It Works**](#how-it-works) • [**Documentation**](https://kopidev.com/docs)

</div>

---

---

### 🚀 Upgrade to Pro, Team, or Enterprise
Looking for **Decaf Mode** (in-flight PII masking), deterministic test seeding, team license management, or on-device local AI data generation?  
Check out **[Kopi Pro, Team & Enterprise](https://kopidev.com)**.

---

## What is Kopi?

Kopi is a cross-platform CLI tool that solves the database bloat problem in local development.

Instead of restoring a massive production backup just to build or test a feature, Kopi creates a **surgical slice** of your database. You specify the target tables you need (e.g., `Users`, `Orders`), and Kopi automatically:

1. **Spins up** an ephemeral, local Docker container (SQL Server, PostgreSQL).
2. **Replicates** the target schema structure (tables, constraints, views, stored procedures).
3. **Traverses** the foreign key dependency graph via topological sort.
4. **Generates** realistic, referentially intact synthetic data for that exact slice.

The result is a lightweight, 50MB database that behaves like production and spins up in seconds.

## Features

* **⚡ Blazing Fast:** Go from zero to an active, seeded database in under 10 seconds.
* **🧠 Relational Intelligence:** Automatically resolves foreign keys and seeds required upstream parent records first.
* **🧬 Schema Fidelity:** Accurately replicates constraints, indexes, views, functions, and stored procedures.
* **🎲 Contextual Generation:** Uses heuristic matchers (Email, Name, Address, Phone) to generate realistic mock data instead of random strings.
* **🐳 Docker Native:** Keeps your workstation clean without installing local database server engines.
* **💻 Cross-Platform:** Native support for Windows, macOS (Apple Silicon & Intel), and Linux.

## Feature Matrix

| Feature | Community (Free) | Pro | Team | Enterprise |
| :--- | :---: | :---: | :---: | :---: |
| **SQL Server & PostgreSQL** | ✅ | ✅ | ✅ | ✅ |
| **Docker Container Orchestration** | ✅ | ✅ | ✅ | ✅ |
| **Schema Reverse-Engineering** | ✅ | ✅ | ✅ | ✅ |
| **Basic Synthetic Data Generation** | ✅ | ✅ | ✅ | ✅ |
| **Decaf Mode (In-Flight PII Masking)** | ❌ | ✅ | ✅ | ✅ |
| **Deterministic Test Seeding** | ❌ | ✅ | ✅ | ✅ |
| **Composite FK & Constraint Resolution** | ❌ | ✅ | ✅ | ✅ |
| **Team Dashboard & License Management** | ❌ | ❌ | ✅ | ✅ |
| **Local AI / NPU Generation (BYOM)** | ❌ | ❌ | ❌ | ✅ |
| **SSO / SAML & Custom SLA** | ❌ | ❌ | ❌ | ✅ |

## Enterprise Preview: AI Orchestration

Watch how Kopi uses Microsoft Semantic Kernel and local GGUF models to generate safe, deterministic synthetic data from a live database schema:

https://github.com/user-attachments/assets/acea1601-ce10-4ce0-b600-43b7032c8071



## Installation

Kopi runs on Windows, macOS (Apple Silicon & Intel), and Linux.

### Method 1: .NET Global Tool (Recommended)

Requires the [.NET 8+ SDK](https://dotnet.microsoft.com/download).

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
     "adminPassword": "YourOptionalPassword123!",
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

   *Note: The `adminPassword` field is optional. If omitted, Kopi will use the default `SuperSecretPassword123!`.*

3. **Run Kopi:** Open your terminal and run:

   ```sh
   kopi up
   ```

4. **Run with flags:** You can use flags to specify a config file path or override the password.

   ```sh
   kopi up -c "./path/to/my-config.json" -p "MySecurePassword!"
   ```

## How It Works

Kopi builds an in-memory directed acyclic graph (DAG) of your schema and performs a topological sort on foreign key constraints.

When you request a slice for `SalesOrderDetail`, Kopi detects the upstream dependencies (`SalesOrderHeader`, `Customer`, `Product`), recursively traverses parent tables, and populates prerequisite records in valid relational order. This prevents foreign key violations during seeding while isolating only the necessary tables for local execution.

## Issues & Feedback

Because Kopi is in active early development, **we are not currently accepting external pull requests**.

However, bug reports and feature requests are very welcome! If you run into an issue, notice an unsupported schema pattern, or have an idea for a new generator matcher:

1. Check existing [GitHub Issues](https://github.com/heymickdoc/kopi/issues) to see if it has already been reported.
2. Open a new issue with a minimal reproduction or sample schema.

**NOTE: Due to low-effort AI slop, pull requests submitted without prior discussion will be closed without review.**

## License

The Kopi Community Edition and `Kopi.Core` are licensed under the **MIT License**. See the `LICENSE` file for details.
