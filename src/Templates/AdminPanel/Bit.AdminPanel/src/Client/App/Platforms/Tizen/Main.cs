using System;
using Microsoft.Maui;
using Microsoft.Maui.Hosting;

namespace AdminPanel.Client.App;

class Program : MauiApplication
{
	protected override MauiApp CreateMauiApp() => MauiProgram.CreateMauiAppBuilder().CreateMauiApp();

	static void Main(string[] args)
	{
		var app = new Program();
		app.Run(args);
	}
}
