using Kopi.Core.Services.Docker;
using Kopi.Core.Utilities;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Kopi.Community.cli
{
    internal class KopiDown
    {
        internal static async Task ExecuteTearDown(string configFileFullPath)
        {
            //This is a single Docker container teardown based on the specified config file.
            var containerNameFromPath = DockerHelper.GetContainerName(configFileFullPath);
            var doesContainerExist = await DockerService.DoesContainerExist(containerNameFromPath);

            if (!doesContainerExist)
            {
                Msg.Write(MessageType.Info,
                    $"Could not find Docker container \"{containerNameFromPath}\" to tear down. Exiting.");
            }
            else
            {
                var stoppedAndDeleted = await DockerService.StopAndDeleteRunningContainer(containerNameFromPath);

                if (stoppedAndDeleted)
                {
                    Msg.Write(MessageType.Success, $"Docker container {containerNameFromPath} down");
                }
                else
                {
                    Msg.Write(MessageType.Error, $"Failed to tear down the Docker container {containerNameFromPath}");
                }
            }
        }

        internal static async Task<bool> ExecuteTearDownAll(List<string> containers)
        {
            bool isErrors = false;

			foreach (var container in containers)
			{
				var stoppedAndDeleted = await DockerService.StopAndDeleteRunningContainer(container);
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
}
