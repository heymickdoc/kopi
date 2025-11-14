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
        internal static async Task ExecuteTearDown(string containerName)
        {
            var doesContainerExist = await DockerService.DoesContainerExist(containerName);

            if (!doesContainerExist)
            {
                Msg.Write(MessageType.Info,
                    $"Could not find Docker container \"{containerName}\" to tear down. Exiting.");
            }
            else
            {
                var stoppedAndDeleted = await DockerService.StopAndDeleteRunningContainer(containerName);

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
