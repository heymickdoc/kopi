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

### 🚀 Upgrade to Professional
Looking for **PostgreSQL** support, PII Anonymization, or Team Licensing?  
Check out **[Kopi Professional & Enterprise](https://kopidev.com)** for advanced features and dedicated support.

---

## What is Kopi?

Kopi is a cross-platform CLI tool that solves the "Database Bloat" problem in local development.

Instead of restoring a massive production backup to test a single feature, Kopi creates a **surgical slice** of your database. You tell it which tables you care about (e.g., `Users`, `Orders`), and Kopi automatically:

1.  **Spins up** a fresh, ephemeral Docker container (SQL Server, PostgreSQL).
2.  **Replicates** your exact production schema (tables, views, stored procs).
3.  **Traverses** the foreign key graph to find all dependencies.
4.  **Generates** realistic, referentially-intact synthetic data for that specific slice.

The result? A lightweight, 50MB database that looks and acts like production, ready in seconds.

## Features

* **⚡ Blazing Fast:** Go from zero to a working DB in under 10 seconds.
* **🧠 Relational Intelligence:** Automatically detects Foreign Keys and generates required parent data.
* **🧬 Schema Fidelity:** Copies constraints, indexes, views, functions, and stored procedures perfectly.
* **🎲 Smart Data:** Uses heuristics to detect column types (Email, Name, Address) and generates realistic data, not just random strings.
* **🐳 Docker Native:** Keeps your local machine clean; no messy SQL installs required.
* **💻 Cross-Platform:** Works seamlessly on Windows, macOS (Intel & Apple Silicon), and Linux.

## Supported Databases

| Feature | Community (Free) | Professional | Enterprise |
| :--- | :---: | :---: | :---: |
| **SQL Server** | ✅ | ✅ | ✅ |
| **PostgreSQL** | ✅ | ✅ | ✅ |
| **Smart Data Generation** | ✅ | ✅ | ✅ |
| **Deterministic Mode** | ❌ | ✅ | ✅ |
| **PII Anonymization** | ❌ | ❌ | ✅ |
| **AI Data Generation** | ❌ | ❌ | ✅ |
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

Kopi uses a Topological Sort algorithm to understand your database schema.

When you ask for data in the Orders table, Kopi knows that an Order cannot exist without a Customer. It recursively walks up the dependency tree, generating Customers first, then Orders, ensuring no Foreign Key constraint violations ever occur.

It effectively turns a 1TB "spaghetti" database into a neat, linear dependency graph and slices off only what you need.

## License

The Kopi Community Edition and `Kopi.Core` are licensed under the **MIT License**. See the `LICENSE` file for details.