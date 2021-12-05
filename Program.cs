using DbContextPoolTest;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using System.Runtime.Loader;
using Microsoft.Extensions.Logging;

string _connectionString = "xxxxx";
var cs = new CancellationTokenSource();
var ct = cs.Token;

var hostBuilder = Host.CreateDefaultBuilder(args);


var host = hostBuilder.ConfigureServices((_, services) =>
{
    services.AddDbContextPool<SimpleDbContext>(options => options
          .UseMySql(_connectionString, new MySqlServerVersion(new Version(8, 0, 23)))
          .EnableDetailedErrors());

    services.AddScoped<App>();

}).ConfigureLogging((context, b) =>
{
    b.AddFilter("Microsoft.EntityFrameworkCore.Database.Command", LogLevel.None);
    b.AddFilter("Microsoft.EntityFrameworkCore.Database.Connection", LogLevel.None);
    b.AddFilter("Microsoft.EntityFrameworkCore.Query", LogLevel.None); ;
    b.SetMinimumLevel(LogLevel.Warning);

}).Build();

await host.StartAsync();

Console.CancelKeyPress += (s, e) =>
{
    cs.Cancel();
};

AssemblyLoadContext.Default.Unloading += (AssemblyLoadContext obj) =>
{
    cs.Cancel();
};


var lifetime = host.Services.GetRequiredService<IHostApplicationLifetime>();

var maxRunCount = 150;
var runIntervalSeconds = 5;

var timer = new PeriodicTimer(TimeSpan.FromSeconds(runIntervalSeconds));

var batchCount = 1;

try
{
    while (await timer.WaitForNextTickAsync(ct))
    {
        var cs2 = new CancellationTokenSource();
        var ct2 = cs2.Token;

        var runCount = Random.Shared.Next(maxRunCount);
        
        Console.WriteLine($"Batch {batchCount} start. {runCount} runs.");
        Task[] taskArray = new Task[runCount];

        for (int i = 0; i < taskArray.Length; i++)
        {
            var x = i + 1;
            taskArray[i] = RunApp(host.Services, x, ct2);             
        }

        Random rand = new Random();

        if (rand.Next(0, 2) != 0)
        {
            Console.WriteLine($"Canceling batch {batchCount}.");
            cs2.Cancel();
        }

        Task.WaitAll(taskArray);        
        Console.WriteLine($"Batch {batchCount} end.");
        batchCount++;
    }

}
catch (System.OperationCanceledException)
{

}

Console.WriteLine("Application End.");
lifetime.StopApplication();
await host.WaitForShutdownAsync();

static async Task RunApp(IServiceProvider services, int runId, CancellationToken cancellationToken)
{
    using (IServiceScope serviceScope = services.CreateScope())
    {
        var _app = serviceScope.ServiceProvider.GetRequiredService<App>();
        await _app.Run(runId, cancellationToken);
    }
}
