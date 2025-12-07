using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Kopi.Core.Utilities;

public enum MessageType
{
	Heading,
	Info,
	Warning,
	Error,
	Success,
	Debug
}

/// <summary>
/// Handles console messages
/// </summary>
public static class Msg
{
	/// <summary>
	/// Writes a message to the console with color coding based on the message type.
	/// </summary>
	/// <param name="messageType">The type of message</param>
	/// <param name="message">The message</param>
	public static void Write(MessageType messageType, string message)
	{
		SetConsoleColor(messageType);
		Console.WriteLine(message);
		Console.ResetColor();
	}

	/// <summary>
	///  Writes a status dot to the console with color coding based on the message type.
	/// </summary>
	/// <param name="messageType"></param>
	/// <param name="message">Optional message</param>
	public static void Status(MessageType messageType, string message = "")
	{
		SetConsoleColor(messageType);
		if (string.IsNullOrEmpty(message)) 
		{
			Console.Write(message);
		}
		else
		{
			Console.Write(".");
		}
		Console.ResetColor();
	}
	
	private static void SetConsoleColor(MessageType messageType)
	{
		//Set console colors based on message type
		switch (messageType)
		{
			case MessageType.Heading:
				Console.ForegroundColor = ConsoleColor.White;
				Console.BackgroundColor = ConsoleColor.Gray;
				break;
			case MessageType.Info:
				Console.ForegroundColor = ConsoleColor.Cyan;
				break;
			case MessageType.Warning:
				Console.ForegroundColor = ConsoleColor.Yellow;
				break;
			case MessageType.Error:
				Console.ForegroundColor = ConsoleColor.Red;
				break;
			case MessageType.Success:
				Console.ForegroundColor = ConsoleColor.Green;
				break;
			case MessageType.Debug:
				Console.ForegroundColor = ConsoleColor.Magenta;
				break;
			default:
				Console.ResetColor();
				break;
		}
	}
}
