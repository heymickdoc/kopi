using Docker.DotNet;
using Docker.DotNet.Models;
using Kopi.Core.Models.Common;
using Kopi.Core.Utilities;

namespace Kopi.Core.Services.Docker;

public class DockerService
{
    private static DockerClient? _client = null;
    private const int MIN_PORT = 1450;
    private const int MAX_PORT = 1600;

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
        _client = OperatingSystem.IsWindows()
            ? new DockerClientConfiguration(new Uri("npipe://./pipe/docker_engine")).CreateClient()
            : new DockerClientConfiguration(new Uri("unix:///var/run/docker.sock")).CreateClient();

        return _client;
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
    /// <param name="sqlServerVersion"></param>
    /// <param name="sqlPort"></param>
    /// <param name="sqlPassword"></param>
    /// <returns></returns>
    /// <exception cref="NotImplementedException"></exception>
    public static async Task CreateContainer(string containerName, string sqlServerVersion, int sqlPort,
        string sqlPassword)
    {
        _client ??= CreateNewDockerClient();

        var dockerImage = GetDockerImage(sqlServerVersion);

        // --- FIX START: Pull Image Logic ---
        Msg.Write(MessageType.Info, $"Checking/Pulling Docker image: {dockerImage}...");

        // Split image:tag (e.g. mcr.microsoft.com/mssql/server:2022-latest)
        var imageParts = dockerImage.Split(':');
        var repo = imageParts[0];
        var tag = imageParts.Length > 1 ? imageParts[1] : "latest";

        try 
        {
            // This acts as "docker pull". We use a simple progress handler to avoid hanging the CLI UI.
            await _client.Images.CreateImageAsync(
                new ImagesCreateParameters { FromImage = repo, Tag = tag },
                null,
                new Progress<JSONMessage>(json => 
                {
                    if (!string.IsNullOrEmpty(json.Status))
                    {
                        Msg.Write(MessageType.Debug, $"\r{json.Status} {json.ProgressMessage}   ");
                    }
                })
            );
        }
        catch (Exception ex)
        {
            Msg.Write(MessageType.Error, $"Failed to pull Docker image {dockerImage}. Error: {ex.Message}");
            Environment.Exit(1);
        }
        // --- FIX END ---

        await _client.Containers.CreateContainerAsync(
            new CreateContainerParameters()
            {
                Env = new List<string>()
                {
                    "ACCEPT_EULA=Y",
                    $"SA_PASSWORD={sqlPassword}",
                },
                Image = dockerImage,
                Name = containerName,
                HostConfig = new HostConfig()
                {
                    PortBindings = new Dictionary<string, IList<PortBinding>>()
                    {
                        {
                            "1433/tcp", new List<PortBinding>()
                            {
                                new PortBinding() { HostPort = $"{sqlPort}" }
                            }
                        }
                    }
                }
            });

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
    /// <param name="sqlServerVersion"></param>
    /// <returns></returns>
    private static string GetDockerImage(string sqlServerVersion)
    {
        if (sqlServerVersion.Contains("2017"))
        {
            return "mcr.microsoft.com/mssql/server:2017-latest";
        }

        if (sqlServerVersion.Contains("2019"))
        {
            return "mcr.microsoft.com/mssql/server:2019-latest";
        }

        if (sqlServerVersion.Contains("2022"))
        {
            return "mcr.microsoft.com/mssql/server:2022-latest";
        }

        //Just assume latest
        return "mcr.microsoft.com/mssql/server:2025-latest";
    }

    public static async Task<int> GetAvailableSqlServerPort()
    {
        var portsInUse = await GetAllPublicPortsInUse();

        //Based on the MIN_PORT and MAX_PORT, find the first available port that is NOT in portsInUse
        for (int port = MIN_PORT; port <= MAX_PORT; port++)
        {
            if (!portsInUse.Contains(port))
            {
                return port;
            }
        }

        Msg.Write(MessageType.Error, "No available ports found for SQL Server Docker container.");
        Environment.Exit(1);

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
                    var portLabels = container.Labels.Where(x => x.Key.Contains("ports", StringComparison.CurrentCultureIgnoreCase) &&
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