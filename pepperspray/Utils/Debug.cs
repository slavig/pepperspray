using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.IO;
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
    private static Logging instance = new Logging();

    public static void ConfigureLogger(LogEventLevel level)
    {
      Log.Logger = new LoggerConfiguration()
        .Enrich.With(new CallerEnricher())
        .Enrich.With(new ThreadIdEnricher())
         .MinimumLevel.Verbose()
         .WriteTo.RollingFile(
           Path.Combine("peppersprayData", "logs", "pepperspray-{Date}.log"), 
           outputTemplate: "[{Timestamp:HH:mm:ss} {Level:u3}/{ThreadId}#{Class}] {Message:lj}{NewLine}{Exception}",
           restrictedToMinimumLevel: LogEventLevel.Verbose)
         .WriteTo.Console(
           outputTemplate: "[{Timestamp:HH:mm:ss} {Level:u3}] {Message:lj}{NewLine}{Exception}",
           restrictedToMinimumLevel: level)
         .CreateLogger();
    }

    public static void ConfigureExceptionHandler()
    {
      AppDomain.CurrentDomain.UnhandledException += new UnhandledExceptionEventHandler(instance.UnhandledExceptionCaught);
    }

    private void UnhandledExceptionCaught(object sender, UnhandledExceptionEventArgs args)
    {
      var ex = args.ExceptionObject;
      Log.Fatal("Unhandled exception at thread {ThreadId}/{ThreadName} (terminating {terminating}): {name}",
        Thread.CurrentThread.ManagedThreadId,
        Thread.CurrentThread.Name,
        args.IsTerminating,
        ex.ToString());
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
  }
}
