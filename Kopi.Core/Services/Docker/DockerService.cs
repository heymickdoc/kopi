using System.Runtime.InteropServices;
using Docker.DotNet;
using Docker.DotNet.Models;
using Kopi.Core.Models.Common;
using Kopi.Core.Utilities;

namespace Kopi.Core.Services.Docker;

public class DockerService
{
    private static DockerClient? _client = null;
    private static readonly TargetDbPortRange SqlPortRange = new TargetDbPortRange(1450, 1600);
    private static readonly TargetDbPortRange PostgresPortRange = new TargetDbPortRange(5450, 5600);

    /// <summary>
    ///  Handles the Docker container lifecycle: stops and deletes any existing container, creates a new one if needed, and ensures it's running.
    /// </summary>
    /// <param name="config">The <see cref="KopiConfig"/> file</param>
    /// <param name="sourceDbData">The source data</param>
    /// <returns>The SQL Port and Password</returns>
    public static async Task<(int sqlPort, string sqlPassword)> HandleDockerContainerLifecycle(KopiConfig config,
        SourceDbModel sourceDbData)
    {
        Msg.Write(MessageType.Info, "Preparing Docker container for target database...");

        var containerName = DockerHelper.GetContainerName(config.ConfigFileFullPath);

        var stoppedAndDeleted = await StopAndDeleteRunningContainer(containerName);

        var sqlPort = await GetAvailableDatabaseServerPort(config.DatabaseType);
        var sqlPassword = config.Settings.AdminPassword;

        if (stoppedAndDeleted)
        {
            await CreateContainer(containerName, config.DatabaseType, sourceDbData.DatabaseVersion, sqlPort,
                sqlPassword);
        }
        
        
        var isReady = false;
        var stopwatch = System.Diagnostics.Stopwatch.StartNew();
        var timeout = TimeSpan.FromSeconds(30); // Give it 30s max to boot
        while (stopwatch.Elapsed < timeout)
        {
            // 1. Check if Docker is even running (Basic check)
            Msg.Write(MessageType.Info, "Waiting for Docker container to start...");
            if (!await VerifyContainerIsRunning(containerName))
            {
                await Task.Delay(1000);
                continue;
            }
            
            // 2. Check if Database is accepting connections (Deep check)
            // If this passes, we are 100% sure the next steps will work.
            Msg.Write(MessageType.Info, "Waiting for database engine to be ready for connections...");
            if (await IsDatabaseReady(config.DatabaseType, sqlPort, sqlPassword))
            {
                isReady = true;
                break;
            }
            
            await Task.Delay(1000); // Wait 1s before retrying connection
        }
        
        if (!isReady)
        {
            Msg.Write(MessageType.Error, $"Docker container started, but the database engine did not become ready within {timeout.TotalSeconds} seconds.");
            Environment.Exit(1);
        }

        Msg.Write(MessageType.Success, $"Docker container {containerName} is running and ready for connections.");
        Console.WriteLine("");
        

        var isContainerRunning = false;
        var tries = 0;
        //Wait for the container to be running
        while (!isContainerRunning)
        {
            isContainerRunning = await VerifyContainerIsRunning(containerName);
            if (isContainerRunning) continue;

            await Task.Delay(2000); //Wait 2 seconds before checking again
            tries++;
            if (tries < 5) continue; //Wait a maximum of 30 seconds

            Msg.Write(MessageType.Error, "Failed to start the Docker container in a timely manner. Exiting.");
            Environment.Exit(1);
        }

        Msg.Write(MessageType.Success, $"Docker container {containerName} is running.");
        Console.WriteLine("");
        
        return (sqlPort, sqlPassword);
    }
    
    /// <summary>
    /// Probes the database to see if it is accepting connections.
    /// Uses a very short timeout to fail fast.
    /// </summary>
    private static async Task<bool> IsDatabaseReady(DatabaseType dbType, int port, string password)
    {
        try
        {
            if (dbType == DatabaseType.SqlServer)
            {
                // Connect to 'master' just to handshake
                var connStr = $"Server=localhost,{port};Database=master;User Id=sa;Password={password};TrustServerCertificate=True;Connect Timeout=1;";
                await using var conn = new Microsoft.Data.SqlClient.SqlConnection(connStr);
                await conn.OpenAsync();
                return true;
            }
            
            if (dbType == DatabaseType.PostgreSQL)
            {
                // Connect to 'postgres' maintenance DB
                var connStr = $"Host=localhost;Port={port};Database=postgres;Username=postgres;Password={password};Timeout=1;";
                await using var conn = new Npgsql.NpgsqlConnection(connStr);
                await conn.OpenAsync();
                return true;
            }
        }
        catch
        {
            // Connection refused, authentication failed (yet), or network not ready
            return false;
        }

        return false;
    }

    public static async Task<bool> DoesContainerExist(string containerName)
    {
        _client ??= CreateNewDockerClient();

        IList<ContainerListResponse> containers = await _client.Containers.ListContainersAsync(
            new ContainersListParameters()
            {
                All = true
            });

        var container = containers.FirstOrDefault(c => c.Names.Contains("/" + containerName));

        return container != null;
    }

    /// <summary>
    /// Stops and deletes a running Docker container based on its name.
    /// </summary>
    /// <param name="runningContainerName">The name of the container</param>
    /// <returns></returns>
    /// <exception cref="DockerContainerNotFoundException">If not found, we return false. It's not fatal.</exception>
    public static async Task<bool> StopAndDeleteRunningContainer(string runningContainerName)
    {
        _client ??= CreateNewDockerClient();

        try
        {
            Msg.Write(MessageType.Info, "Stopping running Docker container: " + runningContainerName);
            var isStopped = await _client.Containers.StopContainerAsync(runningContainerName,
                new ContainerStopParameters() { WaitBeforeKillSeconds = 5 });

            if (isStopped) Msg.Write(MessageType.Info, $"Stopped running Docker container: {runningContainerName}");

            await _client.Containers.RemoveContainerAsync(runningContainerName,
                new ContainerRemoveParameters() { Force = true });
            Msg.Write(MessageType.Info, $"Deleted Docker container: {runningContainerName}");
        }
        catch (DockerContainerNotFoundException)
        {
            Msg.Write(MessageType.Info,
                $"Docker container not found: {runningContainerName}. It may have already been removed.");
            //This is ok... we just want to know if it's running or not.
            return true;
        }
        catch (Exception ex)
        {
            Msg.Write(MessageType.Error, $"Error stopping/deleting Docker container: {ex.Message}");
            Environment.Exit(1);
        }

        return true;
    }

    private static DockerClient CreateNewDockerClient()
    {
        // 1. Windows: Standard Named Pipe
        if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
        {
            return new DockerClientConfiguration(new Uri("npipe://./pipe/docker_engine"))
                .CreateClient();
        }

        // 2. Linux: Dynamic Discovery
        var socketPath = GetLinuxSocketPath();

        // Logging for your customers (Essential for support!)
        if (string.IsNullOrEmpty(socketPath))
        {
            Msg.Write(MessageType.Error, "CRITICAL: No container engine detected. Verified paths:\n" +
                                        " - DOCKER_HOST env var\n" +
                                        " - User Podman/Docker sockets\n" +
                                        " - System Docker/Podman sockets.\n" +
                                        "Please ensure Docker or Podman is running.");
            //Hard fail
            Environment.Exit(1);
        }

        Console.WriteLine($"Connecting to Container Engine at: {socketPath}"); // Replace with Msg.Write
        return new DockerClientConfiguration(new Uri(socketPath)).CreateClient();
    }
    
    private static string GetLinuxSocketPath()
    {
        // PRIORITY 1: Explicit Environment Variable (The "Escape Hatch")
        // If a sophisticated user sets this, we obey it immediately.
        var envHost = Environment.GetEnvironmentVariable("DOCKER_HOST");
        if (!string.IsNullOrEmpty(envHost) && envHost.StartsWith("unix://"))
        {
            return envHost;
        }

        // PRIORITY 2: User-Level (Rootless) Sockets
        // We use XDG_RUNTIME_DIR to find the user's specific temp folder (e.g., /run/user/1000 or /run/user/5001)
        var runtimeDir = Environment.GetEnvironmentVariable("XDG_RUNTIME_DIR");
    
        if (!string.IsNullOrEmpty(runtimeDir))
        {
            // Check for Rootless Podman (Fedora/RHEL standard)
            var podmanUser = $"unix://{Path.Combine(runtimeDir, "podman/podman.sock")}";
            if (File.Exists(podmanUser.Replace("unix://", ""))) return podmanUser;

            // Check for Rootless Docker (Ubuntu standard)
            var dockerUser = $"unix://{Path.Combine(runtimeDir, "docker.sock")}";
            if (File.Exists(dockerUser.Replace("unix://", ""))) return dockerUser;
        }

        // PRIORITY 3: System-Level (Root) Sockets
        // Standard Docker
        var systemDocker = "unix:///var/run/docker.sock";
        if (File.Exists(systemDocker.Replace("unix://", ""))) return systemDocker;

        // Standard System Podman
        var systemPodman = "unix:///run/podman/podman.sock";
        if (File.Exists(systemPodman.Replace("unix://", ""))) return systemPodman;

        return string.Empty; // Not found
    }

    /// <summary>
    /// Gets a list of all public ports in use by Docker containers.
    /// </summary>
    /// <returns></returns>
    private static async Task<List<int>> GetAllPublicPortsInUse()
    {
        _client ??= CreateNewDockerClient();

        IList<ContainerListResponse> containers = await _client.Containers.ListContainersAsync(
            new ContainersListParameters()
            {
                All = true
            });

        var publicPorts = new List<int>();

        foreach (var container in containers)
        {
            var portsList = container.Ports;
            foreach (var port in portsList)
            {
                //If it's not in publicPorts, add it.
                if (!publicPorts.Contains(port.PublicPort)) publicPorts.Add(port.PublicPort);
            }
        }

        //Order by port number
        publicPorts = publicPorts.OrderBy(p => p).ToList();

        return publicPorts;
    }


    /// <summary>
    /// Creates and starts a Docker container based on the config and container name. Returns the port number the container is running on.
    /// </summary>
    /// <param name="containerName"></param>
    /// <param name="dbType"></param>
    /// <param name="databaseVersion"></param>
    /// <param name="sqlPort"></param>
    /// <param name="dbPassword"></param>
    /// <returns></returns>
    /// <exception cref="NotImplementedException"></exception>
    public static async Task CreateContainer(string containerName, DatabaseType dbType, string databaseVersion,
        int sqlPort,
        string dbPassword)
    {
        _client ??= CreateNewDockerClient();

        var dockerImage = GetDockerImage(dbType, databaseVersion);

        // --- FIX START: Pull Image Logic ---
        Msg.Write(MessageType.Info, $"Checking/Pulling Docker image: {dockerImage}...");

        // Split image:tag (e.g. mcr.microsoft.com/mssql/server:2022-latest)
        var imageParts = dockerImage.Split(':');
        var repo = imageParts[0];
        var tag = imageParts.Length > 1 ? imageParts[1] : "latest";

        try
        {
            // This acts as "docker pull". We use a simple progress handler to avoid hanging the CLI UI.
            // Should probably show a message then a series of 
            Msg.Write(MessageType.Debug, $"Pulling image {repo}:{tag}...");
            await _client.Images.CreateImageAsync(
                new ImagesCreateParameters { FromImage = repo, Tag = tag },
                null,
                new Progress<JSONMessage>(json =>
                {
                    if (!string.IsNullOrEmpty(json.Status))
                    {
                        Msg.Status(MessageType.Debug, $"\r{json.Status} {json.ProgressMessage}   ");
                    }
                })
                
            );
            Console.WriteLine("");
        }
        catch (Exception ex)
        {
            Msg.Write(MessageType.Error, $"Failed to pull Docker image {dockerImage}. Error: {ex.Message}");
            Environment.Exit(1);
        }
        // --- FIX END ---

        if (dbType == DatabaseType.SqlServer)
        {
            await _client.Containers.CreateContainerAsync(
                new CreateContainerParameters()
                {
                    Env = new List<string>()
                    {
                        "ACCEPT_EULA=Y",
                        $"SA_PASSWORD={dbPassword}",
                    },
                    Image = dockerImage,
                    Name = containerName,
                    HostConfig = new HostConfig()
                    {
                        PortBindings = new Dictionary<string, IList<PortBinding>>()
                        {
                            {
                                "1433/tcp",
                                new List<PortBinding> { new() { HostPort = $"{sqlPort}" } }
                            }
                        }
                    }
                });
        }
        else if (dbType == DatabaseType.PostgreSQL)
        {
            await _client.Containers.CreateContainerAsync(
                new CreateContainerParameters
                {
                    Image = dockerImage, // Or config.Database.Version if you parse it
                    Name = containerName,
                    Env = new List<string>
                    {
                        $"POSTGRES_PASSWORD={dbPassword}",
                        "POSTGRES_USER=postgres" // Default, but good to be explicit
                    },
                    HostConfig = new HostConfig
                    {
                        PortBindings = new Dictionary<string, IList<PortBinding>>
                        {
                            {
                                "5432/tcp",
                                new List<PortBinding> { new() { HostPort = $"{sqlPort}" } }
                            }
                        }
                    }
                });
        }


        Msg.Write(MessageType.Info, $"Created Docker container: {containerName} using image: {dockerImage}");
        await StartContainer(containerName);
    }

    private static async Task StartContainer(string containerName)
    {
        _client ??= CreateNewDockerClient();
        await _client.Containers.StartContainerAsync(containerName, new ContainerStartParameters());

        Msg.Write(MessageType.Info, $"Started Docker container: {containerName}");
    }

    /// <summary>
    /// Gets the appropriate Docker image based on the SQL Server version.
    /// </summary>
    /// <param name="dbType"></param>
    /// <param name="databaseVersion"></param>
    /// <returns></returns>
    private static string GetDockerImage(DatabaseType dbType, string databaseVersion)
    {
        if (dbType == DatabaseType.SqlServer)
        {
            if (databaseVersion.Contains("2017")) return "mcr.microsoft.com/mssql/server:2017-latest";
            if (databaseVersion.Contains("2019")) return "mcr.microsoft.com/mssql/server:2019-latest";
            if (databaseVersion.Contains("2022")) return "mcr.microsoft.com/mssql/server:2022-latest";
            if (databaseVersion.Contains("2025")) return "mcr.microsoft.com/mssql/server:2025-latest";
            //If no version specified, just use latest
            return "mcr.microsoft.com/mssql/server:2025-latest";
        }
        else if (dbType == DatabaseType.PostgreSQL)
        {
            //For PostgreSQL, we can just return the latest image
            return "docker.io/library/postgres:latest";
        }


        //Just assume latest
        return "mcr.microsoft.com/mssql/server:2025-latest";
    }

    private static async Task<int> GetAvailableDatabaseServerPort(DatabaseType dbType)
    {
        var portsInUse = await GetAllPublicPortsInUse();
        
        if (dbType == DatabaseType.SqlServer)
        {
            for (var port = SqlPortRange.MinPort; port <= SqlPortRange.MaxPort; port++)
            {
                if (!portsInUse.Contains(port))
                {
                    return port;
                }
            }
        }
        else if (dbType == DatabaseType.PostgreSQL)
        {
            for (var port = PostgresPortRange.MinPort; port <= PostgresPortRange.MaxPort; port++)
            {
                if (!portsInUse.Contains(port))
                {
                    return port;
                }
            }
        }
        else
        {
            Msg.Write(MessageType.Error, "Unsupported database type for port allocation.");
            Environment.Exit(1);
        }

        return -1; // This will never be reached, but it's needed to satisfy the compiler.
    }

    public static async Task<bool> VerifyContainerIsRunning(string containerName)
    {
        _client ??= CreateNewDockerClient();

        IList<ContainerListResponse> containers = await _client.Containers.ListContainersAsync(
            new ContainersListParameters()
            {
                All = true
            });

        var container = containers.FirstOrDefault(c => c.Names.Contains("/" + containerName));

        return container != null && container.State == "running";
    }

    public static async Task<List<string>> ListAllKopiContainers()
    {
        _client ??= CreateNewDockerClient();
        IList<ContainerListResponse> containers = await _client.Containers.ListContainersAsync(
            new ContainersListParameters()
            {
                All = true
            });

        var kopiContainers = new List<string>();
        foreach (var container in containers)
        {
            foreach (var name in container.Names)
            {
                if (name.StartsWith("/kopi_"))
                {
                    kopiContainers.Add(name.TrimStart('/'));
                }
            }
        }

        return kopiContainers;
    }

    public static async Task<List<ContainerStatusModel>> GetAllContainersStatus()
    {
        var allContainersStatus = new List<ContainerStatusModel>();

        _client ??= CreateNewDockerClient();

        IList<ContainerListResponse> containers = null;

        try
        {
            containers = await _client.Containers.ListContainersAsync(
                new ContainersListParameters()
                {
                    All = true
                });
        }
        catch (Exception ex)
        {
            Msg.Write(MessageType.Error, $"Error connecting to Docker daemon: {ex.Message}");
            Environment.Exit(1);
        }

        foreach (var container in containers)
        {
            foreach (var name in container.Names)
            {
                if (name.StartsWith("/kopi_"))
                {
                    //Ports are in container.Labels. They should have the text "ports" in the key.
                    var portLabels = container.Labels.Where(x =>
                        x.Key.Contains("ports", StringComparison.CurrentCultureIgnoreCase) &&
                        x.Key.Contains("tcp", StringComparison.CurrentCultureIgnoreCase)).ToList();
                    //We need to strip all non-numeric characters from the value and the key.
                    var ports = "";
                    foreach (var portLabel in portLabels)
                    {
                        var key = new string(portLabel.Key.Where(char.IsDigit).ToArray());
                        var value = new string(portLabel.Value.Where(char.IsDigit).ToArray());

                        if (!string.IsNullOrEmpty(key) && !string.IsNullOrEmpty(value))
                        {
                            ports = $"{value}:{key}";
                        }
                        else if (!string.IsNullOrEmpty(value))
                        {
                            ports = $"?:{value}";
                        }
                        else
                        {
                            ports = portLabel.Key;
                        }
                    }


                    var containerStatus = new ContainerStatusModel
                    {
                        Name = name.TrimStart('/'),
                        Image = container.Image,
                        Ports = ports,
                        State = container.State
                    };
                    allContainersStatus.Add(containerStatus);
                }
            }
        }

        return allContainersStatus;
    }

    /// <summary>
    ///  Tears down the specified Docker container.
    /// </summary>
    /// <param name="containerName">The container to tear down</param>
    public static async Task ExecuteTearDown(string containerName)
    {
        var doesContainerExist = await DoesContainerExist(containerName);

        if (!doesContainerExist)
        {
            Msg.Write(MessageType.Info,
                $"Could not find Docker container \"{containerName}\" to tear down. Exiting.");
        }
        else
        {
            var stoppedAndDeleted = await StopAndDeleteRunningContainer(containerName);

            if (stoppedAndDeleted)
            {
                Msg.Write(MessageType.Success, $"Docker container {containerName} down");
            }
            else
            {
                Msg.Write(MessageType.Error, $"Failed to tear down the Docker container {containerName}");
            }
        }
    }

    public static async Task<bool> ExecuteTearDownAll(List<string> containers)
    {
        var isErrors = false;

        foreach (var container in containers)
        {
            var stoppedAndDeleted = await StopAndDeleteRunningContainer(container);
            if (stoppedAndDeleted)
            {
                Msg.Write(MessageType.Success, $"Docker container {container} torn down successfully.");
                Console.WriteLine("");
            }
            else
            {
                Msg.Write(MessageType.Error, $"Failed to tear down the Docker container {container}");
                isErrors = true;
            }
        }

        return isErrors;
    }
}