using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.WebSockets;
using System.Reflection;
using System.Threading.Tasks;
using TriangleStreamingServer.Models;

namespace TriangleStreamingServer.WebSockets
{
	public static class WebSocketsExtensions
	{
		public static IApplicationBuilder MapWebSocketManager(this IApplicationBuilder app,
															 PathString path,
															 WebSocketHandler handler)
		{
			return app.Map(path, (_app) => _app.UseMiddleware<WebSocketMiddleware>(handler));
		}

		public static IServiceCollection AddWebSocketManager(this IServiceCollection services)
		{
			services.AddTransient<WebSocketConnectionManager>();

			foreach (var type in Assembly.GetEntryAssembly().ExportedTypes)
			{
				if (type.GetTypeInfo().BaseType == typeof(WebSocketHandler))
				{
					services.AddSingleton(type);
				}
			}

			return services;
		}

		public static IServiceCollection AddStreamManager(this IServiceCollection services)
		{
			services.AddSingleton<StreamQueueManager>();

			return services;
		}
	}
}
