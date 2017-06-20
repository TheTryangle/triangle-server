using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using System.Net.WebSockets;
using TriangleStreamingServer.WebSockets;
using TriangleStreamingServer.Models;

namespace TriangleStreamingServer
{
	public class Startup
	{
		public Startup(IHostingEnvironment env)
		{
			var builder = new ConfigurationBuilder()
				.SetBasePath(env.ContentRootPath)
				.AddJsonFile("appsettings.json", optional: false, reloadOnChange: true)
				.AddJsonFile($"appsettings.{env.EnvironmentName}.json", optional: true)
				.AddEnvironmentVariables();
			Configuration = builder.Build();
		}

		public IConfigurationRoot Configuration { get; }

		// This method gets called by the runtime. Use this method to add services to the container.
		public void ConfigureServices(IServiceCollection services)
		{
			// Add framework services.
			services.AddMvc();

			services.AddWebSocketManager();

			services.AddStreamManager();
		}

		// This method gets called by the runtime. Use this method to configure the HTTP request pipeline.
		public void Configure(IApplicationBuilder app, IHostingEnvironment env, ILoggerFactory loggerFactory)
		{
			loggerFactory.AddConsole(Configuration.GetSection("Logging"));
			loggerFactory.AddDebug();

			app.UseMvc();

			var webSocketOptions = new WebSocketOptions()
			{
				KeepAliveInterval = TimeSpan.FromSeconds(10),
				ReceiveBufferSize = 4 * 1024
			};

			app.UseWebSockets(webSocketOptions);

			var streamQueueManager = app.ApplicationServices.GetService<StreamQueueManager>();
			var receiveStream = app.ApplicationServices.GetService<ReceiveStream>();
			streamQueueManager.ReceivingWebSocket = receiveStream;

			app.MapWebSocketManager("/send", app.ApplicationServices.GetService<VideoStream>());
			app.MapWebSocketManager("/receive", receiveStream);
		}
	}
}
