using System;
using System.IO;

namespace Fire.Core.Exceptions;
public static class ExceptionLogger
{
	public static readonly string LogFilePath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments), "firebrowserwinui.flog");
	public static readonly string InformationFilePath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments), "fire_logger.log");
	public static readonly string MsalLogFile = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments), "log_msalAuthenicaton.log");

	public static void LogInformation(string message)
	{

		try
		{
			using (StreamWriter writer = new StreamWriter(InformationFilePath, true))
			{
				writer.WriteLine($"Ilogger {DateTime.Now}:");
				writer.WriteLine(message);
				writer.WriteLine("------------------------------------------------------------------");
			}
		}
		catch (Exception logEx)
		{
			// Log the exception to the console if an error occurs while logging
			Console.WriteLine($"Error while logging information: {logEx.Message}");
		}


	}
	public static void LogException(Exception ex)
	{
		try
		{
			using (StreamWriter writer = new StreamWriter(LogFilePath, true))
			{
				writer.WriteLine($"Exception occurred at {DateTime.Now}:");
				LogExceptionDetails(ex, writer);
				writer.WriteLine("------------------------------------------------------------------");
			}
		}
		catch (Exception logEx)
		{
			// Log the exception to the console if an error occurs while logging
			Console.WriteLine($"Error while logging exception: {logEx.Message}");
		}
	}

	private static void LogExceptionDetails(Exception ex, StreamWriter writer)
	{
		if (ex != null)
		{
			writer.WriteLine($"Type: {ex.GetType().FullName}");
			writer.WriteLine($"Message: {ex.Message}");
			writer.WriteLine($"StackTrace: {ex.StackTrace}");
			writer.WriteLine();

			LogExceptionDetails(ex.InnerException, writer);
		}
	}
}