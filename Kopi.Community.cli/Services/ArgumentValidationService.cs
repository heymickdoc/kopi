using Kopi.Core.Utilities;

namespace Kopi.Community.cli.Services;

public static class ArgumentValidationService
{
    public static Dictionary<string, string> ValidateArguments(string[] args)
    {
        /*
         * Possible validations to implement:
         * -- These are commands that can have multiple args --
         * up -c <config file path> -p <password>
         * down --config <config file path>
         * -c, --config: Check if the provided config file path exists.
         * -p, --password: Up to 64 chars is enough validation... it's for a temp Docker database password.
         * -- These are single commands with no additional args --
         * up: No validation needed, just a flag to indicate setup.
         * down: No validation needed, just a flag to indicate teardown.
         * -v: No validation needed, just a flag to indicate version display.
         * -s: No validation needed, just a flag to indicate status check.
         * -- Example command usages --
         * up -c "/path/to/config.json" -p MyAwesomePassword
         * down --config "/path/to/config.json"
         * up
         * down
         * -v
         * -s
         * An example implementation could look like this:
         * kopi up -p MyAwesomePassword -c "/path/to/config.json"
         */

        // Dictionary to hold the command and its options
        var commandArgs = new Dictionary<string, string>();

        // This should always be the command, e.g. up, down, -v, -s
        var command = args[0].ToLower();
        commandArgs["command"] = command;

        // The rest are options for that command
        var options = args.Skip(1).ToArray();

        if (command is "up")
        {
            // Process options for 'up' and 'down' commands
            for (var i = 0; i < options.Length; i++)
            {
                var option = options[i].ToLower();
                switch (option)
                {
                    case "-c":
                    case "--config":
                        if (i + 1 < options.Length)
                        {
                            var configPath = options[i + 1];
                            // Here you could add file existence validation if needed
                            commandArgs["config"] = configPath;
                            i++; // Skip next since it's the value
                        }
                        else
                        {
                            throw new ArgumentException("Config file path is required after -c or --config.");
                        }

                        break;
                    case "-p":
                    case "--password":
                        if (i + 1 < options.Length)
                        {
                            var password = options[i + 1];
                            if (password.Length > 64)
                            {
                                throw new ArgumentException("Password cannot exceed 64 characters.");
                            }

                            commandArgs["password"] = password;
                            i++; // Skip next since it's the value
                        }
                        else
                        {
                            throw new ArgumentException("Password is required after -p or --password.");
                        }

                        break;
                    default:
                        throw new ArgumentException($"Unknown option for 'up' command: {option}");
                }
            }
        }
        else if (command is "down")
        {
            // Usually it will be just the config option for 'down' but it can have "-all" so that they can
            // tear down all docker instances starting with the name "kopi_" OR it will be -c <path>
            // == valid commands ==
            // kopi down -c "/path/to/config.json"
            // kopi down --config "/path/to/config.json"
            // kopi down -all
            // kopi down
            if (options.Length > 0)
            {
                var option = options[0].ToLower();
            
                switch (option)
                {
                    case "-c":
                    case "--config":
                        if (options.Length < 2)
                            throw new ArgumentException("Invalid number of arguments for 'down' command.");
                        
                        //Can only be -c or --config then the path
                        commandArgs["config"] = options[1].Length > 0 ? options[1] : throw new ArgumentException("Config file path is required after -c or --config.");
                        break;  //Can't have any other options with this
                    case "-all":
                        commandArgs["all"] = "true";
                        break;  //Can't have any other options with this
                    default:
                        throw new ArgumentException($"Unknown option for 'down' command: {option}");
                }
            }
        }
        else if (command is "version" or "-v" or "--version")
        {
            //Change command to "version" for consistency
            commandArgs["command"] = "version";
        }
        else if (command is "status" or "-s" or "--status")
        {
            //Change command to "status" for consistency
            commandArgs["command"] = "status";
        }
        else
        {
            throw new ArgumentException($"Unknown command: {command}");
        }
        
        if (options.Length == 0) return commandArgs;
        
        //Check if the "config" key exists in the dictionary and validate the path
        commandArgs.TryGetValue("config", out var configFilePath);

        if (!string.IsNullOrEmpty(configFilePath) && !ConfigFileHelper.IsValidConfigFilePath(configFilePath))
            throw new ArgumentException("The provided configuration file path is invalid or the file does not exist.");
        
        return commandArgs;
    }
}