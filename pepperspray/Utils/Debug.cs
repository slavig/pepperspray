using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

using Serilog;
using Serilog.Configuration;
using Serilog.Core;
using Serilog.Events;
using ThreeDXChat.Networking.NodeNet;

namespace pepperspray.Utils
{
  internal class Logging
  {
    public static void ConfigureLogger(LogEventLevel level = LogEventLevel.Information)
    {
      Log.Logger = new LoggerConfiguration()
        .Enrich.With(new CallerEnricher())
        .Enrich.With(new ThreadIdEnricher())
         .MinimumLevel.Verbose()
         .WriteTo.File("peppersprayData\\pepperspray.log",
             outputTemplate: "[{Timestamp:HH:mm:ss} {Level:u3}/{ThreadId}|{ThreadName}|#{Class}] {Message:lj}{NewLine}{Exception}",
             rollingInterval: RollingInterval.Day,
             rollOnFileSizeLimit: true)
         .WriteTo.Console(
           outputTemplate: "[{Timestamp:HH:mm:ss} {Level:u3}] {Message:lj}{NewLine}{Exception}",
           restrictedToMinimumLevel: level
         )
         .CreateLogger();
    }
  }

  class ThreadIdEnricher : ILogEventEnricher
  {
    public void Enrich(LogEvent logEvent, ILogEventPropertyFactory propertyFactory)
    {
      logEvent.AddPropertyIfAbsent(propertyFactory.CreateProperty("ThreadId", Thread.CurrentThread.ManagedThreadId));
      logEvent.AddPropertyIfAbsent(propertyFactory.CreateProperty("ThreadName", Thread.CurrentThread.Name));
    }
  }

  class CallerEnricher : ILogEventEnricher
  {
    public void Enrich(LogEvent logEvent, ILogEventPropertyFactory propertyFactory)
    {
      var skip = 3;
      while (true)
      {
        var stack = new StackFrame(skip);
        if (!stack.HasMethod())
        {
          logEvent.AddPropertyIfAbsent(new LogEventProperty("Caller", new ScalarValue("<unknown method>")));
          return;
        }

        var method = stack.GetMethod();
        if (method.DeclaringType.Assembly != typeof(Log).Assembly)
        {
          var caller = $"{method.DeclaringType.FullName}.{method.Name}({string.Join(", ", method.GetParameters().Select(pi => pi.ParameterType.FullName))})";

          logEvent.AddPropertyIfAbsent(new LogEventProperty("Caller", new ScalarValue(caller)));
          logEvent.AddPropertyIfAbsent(new LogEventProperty("Class", new ScalarValue(method.DeclaringType.FullName)));
          logEvent.AddPropertyIfAbsent(new LogEventProperty("Method", new ScalarValue(method.Name)));
        }

        skip++;
      }
    }
  }

  static class LoggerCallerEnrichmentConfiguration
  {
    public static LoggerConfiguration WithCaller(this LoggerEnrichmentConfiguration enrichmentConfiguration)
    {
      return enrichmentConfiguration.With<CallerEnricher>();
    }
  }

  public static class Extensions
  {
    public static string DebugDescription(this NodeServerEvent msg) {
      string description = null;
      if (msg.data is List<object>)
      {
        description = (msg.data as List<object>).Aggregate("", (i, a) => i + " " + a);
      } else if (msg.data is List<string>)
      {
        description = (msg.data as List<string>).Aggregate("", (i, a) => i + " " + a);
      } else if (msg.data is Dictionary<string, string>)
      {
        description = (msg.data as Dictionary<string, string>).Aggregate("", (i, a) => i + " " + a.Key + "=" + a.Value);
      } else if (msg.data is Dictionary<string, object>)
      {
        description = (msg.data as Dictionary<string, object>).Aggregate("", (i, a) => i + " " + a.Key + "=" + a.Value);
      } else if (msg.data is Dictionary<String, object>)
      {
        description = (msg.data as Dictionary<String, object>).Aggregate("", (i, a) => i + " " + a.Key + "=" + a.Value);
      } else if (msg.data is Dictionary<String, String>)
      {
        description = (msg.data as Dictionary<String, String>).Aggregate("", (i, a) => i + " " + a.Key + "=" + a.Value);
      } else if (msg.data != null)
      {
        description = msg.data.ToString();
      }

      return description;
    }
  }
}
