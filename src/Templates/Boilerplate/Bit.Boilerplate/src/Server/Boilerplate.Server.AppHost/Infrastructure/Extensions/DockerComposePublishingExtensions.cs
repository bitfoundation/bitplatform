//+:cnd:noEmit
using Aspire.Hosting.Docker;
using Aspire.Hosting.Docker.Resources.ComposeNodes;
using Aspire.Hosting.Docker.Resources.ServiceNodes;

namespace Aspire.Hosting;

/// <summary>
/// Shapes the Docker Compose project that <c>aspire publish</c> writes and the CD deploys to the server
/// (See .github/workflows/cd-template.yml). Nothing here changes run mode.
/// </summary>
public static class DockerComposePublishingExtensions
{
    extension(IDistributedApplicationBuilder builder)
    {
        /// <summary>
        /// Publish mode only. Sends the images to the registry the CD names, fixes the apps on port 8080 with no host
        /// port, runs them in the environment they are published for, gives every long-running service a restart policy
        /// and a health check, adds the one-shot <c>migrations</c> service, and wires in the settings the deploy job
        /// supplies: the connection strings and parameters of the generated <c>.env</c>, the <c>serverweb.env</c> (and
        /// <c>serverapi.env</c>) files, and the environment's own <c>AppCertificate</c>.
        /// </summary>
        public void ConfigureDockerComposePublishing(
            IResourceBuilder<DockerComposeEnvironmentResource> compose,
            IResourceBuilder<ProjectResource> serverWebProject
            //#if (api == "Standalone")
            , IResourceBuilder<ProjectResource> serverApiProject
            //#endif
            )
        {
            if (builder.ExecutionContext.IsPublishMode is false)
                return;

            // The images go to <REGISTRY_ENDPOINT>/<REGISTRY_REPOSITORY>/<resource>:<IMAGE_TAG>, which the CD sets to
            // ghcr.io/<owner>/<repo>/serverweb:<environment>-<commit sha>.
#pragma warning disable ASPIRECOMPUTE003 // AddContainerRegistry is experimental.
            builder.AddContainerRegistry("container-registry",
                builder.AddParameterFromConfiguration("registry-endpoint", "REGISTRY_ENDPOINT"),
                builder.AddParameterFromConfiguration("registry-repository", "REGISTRY_REPOSITORY"));
#pragma warning restore ASPIRECOMPUTE003

            var imageTag = builder.Configuration["IMAGE_TAG"] is { Length: > 0 } tag ? tag : "latest";

            // The project that hosts the api: Server.Web itself, unless the api is standalone.
            var apiProject = serverWebProject;
            //#if (api == "Standalone")
            apiProject = serverApiProject;
            //#endif

            List<IResourceBuilder<ProjectResource>> projects = [serverWebProject];
            //#if (api == "Standalone")
            projects.Add(serverApiProject);
            //#endif

#pragma warning disable ASPIREPIPELINES003 // WithRemoteImageTag is experimental.
            foreach (var project in projects)
            {
                project
                    // A fixed port, because cloudflared reaches the apps at http://serverweb:8080 (and http://serverapi:8080).
                    .WithEndpoint("http", endpoint => endpoint.TargetPort = 8080)
                    // The environment `aspire publish --environment` names (Test or Production). Without it the apps run as
                    // Production everywhere, and AppEnvironment takes its value from it on the server.
                    .WithEnvironment("ASPNETCORE_ENVIRONMENT", builder.Environment.EnvironmentName)
                    .WithRemoteImageTag(imageTag)
                    .PublishAsDockerComposeService((_, service) =>
                    {
                        // No host port: the server keeps every inbound port closed, and cloudflared dials out instead.
                        service.Ports.Clear();
                        service.Restart = "unless-stopped";
                        // The app's own settings that no parameter carries (AI keys, Sentry, external sign-in...), which the
                        // deploy job writes next to docker-compose.yaml.
                        service.EnvFile.Add($"{service.Name}.env");
                        service.Healthcheck = new()
                        {
                            // The aspnet base image has bash, but neither curl nor wget.
                            Test = ["CMD", "bash", "-c", "exec 3<>/dev/tcp/127.0.0.1/8080 && printf 'GET /alive HTTP/1.1\\r\\nHost: localhost\\r\\nConnection: close\\r\\n\\r\\n' >&3 && head -n 1 <&3 | grep -q ' 200 '"],
                            Interval = "10s",
                            Timeout = "5s",
                            Retries = 6,
                            StartPeriod = "60s"
                        };
                    });
            }
#pragma warning restore ASPIREPIPELINES003

            // Mailpit is development only. A deployed server sends its mail through a real SMTP server.
            apiProject.WithReference(builder.AddConnectionString("smtp"));
            //#if (database == "Other")
            // The database is outside the Docker Compose project: read it from the `otherdb` connection string.
            apiProject.WithReference(builder.AddConnectionString("otherdb"));
            //#endif
            //#if (notification == true)
            // ServerApiSettings refuses the template's sample VAPID private key and subject outside Development.
            apiProject.WithEnvironment("AdsPushVapid__PrivateKey", builder.AddParameter("vapid-private-key", secret: true))
                .WithEnvironment("AdsPushVapid__Subject", builder.AddParameter("vapid-subject"));
            //#endif
            //#if (api == "Standalone")
            // Pre-rendering calls the api over the Docker Compose network rather than through its public address.
            serverWebProject.WithEnvironment("ServerSideHttpClientBaseAddress", serverApiProject.GetEndpoint("http"));
            //#endif

            apiProject.PublishAsDockerComposeService((_, service) =>
            {
                // The image only carries the development AppCertificate, which the api refuses outside Development. The
                // deploy job writes the environment's own pair next to docker-compose.yaml (See AppCertificate.md).
                service.AddVolume(new Volume { Name = "app-certificate-crt", Type = "bind", Source = "./AppCertificate.crt", Target = "/app/AppCertificate.crt", ReadOnly = true });
                service.AddVolume(new Volume { Name = "app-certificate-key", Type = "bind", Source = "./AppCertificate.key", Target = "/app/AppCertificate.key", ReadOnly = true });
            });

            compose.ConfigureComposeFile(composeFile =>
            {
                var api = composeFile.Services[apiProject.Resource.Name];

                //#if (database == "Sqlite" || filesStorage == "Local")
                // The api keeps its SQLite database and its files under /container_volume/App_Data whenever that folder
                // exists (See Program.Services.cs), so it is a named volume. Docker creates a new volume's root owned by
                // root, while the app runs as the image's non-root `app` user: a one-shot service hands it over first.
                var dataVolume = VolumeNameGenerator.Generate(apiProject, "data");
                composeFile.AddVolume(new Volume { Name = dataVolume, Driver = "local" });
                api.AddVolume(NewDataVolumeMount());
                composeFile.AddService(new Service
                {
                    Name = "container-volume-init",
                    Image = api.Image,
                    User = "root",
                    Entrypoint = ["chown", "app:app", "/container_volume"],
                    Volumes = [NewDataVolumeMount()],
                    NetworkMode = "none",
                    Restart = "no"
                });
                api.DependsOn["container-volume-init"] = new() { Condition = "service_completed_successfully" };

                Volume NewDataVolumeMount() => new() { Name = dataVolume, Type = "volume", Source = dataVolume, Target = "/container_volume", ReadOnly = false };
                //#endif

                // The database migrations: a one-shot `migrations` service that runs the api's own image - the same build,
                // with the same settings - with MigrateDatabaseAndExit=true. The deploy job runs it on its own before
                // `docker compose up`, so a failed migration stops the deployment while the running version is left
                // untouched, and the api starts only once it has exited with 0.
                var migrations = new Service
                {
                    Name = "migrations",
                    Image = api.Image,
                    Environment = new(api.Environment) { ["MigrateDatabaseAndExit"] = "true" },
                    EnvFile = [.. api.EnvFile],
                    Volumes = [.. api.Volumes.Select(volume => new Volume { Name = volume.Name, Type = volume.Type, Source = volume.Source, Target = volume.Target, ReadOnly = volume.ReadOnly })],
                    Networks = [.. api.Networks],
                    Restart = "no"
                };
                foreach (var (name, dependency) in api.DependsOn)
                {
                    migrations.DependsOn[name] = new() { Condition = dependency.Condition };
                }
                composeFile.AddService(migrations);
                api.DependsOn["migrations"] = new() { Condition = "service_completed_successfully" };

                //#if (api == "Standalone")
                composeFile.Services[serverWebProject.Resource.Name].DependsOn[serverApiProject.Resource.Name] = new() { Condition = "service_started" };
                //#endif

                // Aspire writes WaitFor as `service_started`: wait for health instead wherever the dependency reports it.
                foreach (var service in composeFile.Services.Values)
                {
                    foreach (var (name, dependency) in service.DependsOn)
                    {
                        if (dependency.Condition is "service_started" && composeFile.Services.TryGetValue(name, out var target) && target.Healthcheck is not null)
                        {
                            dependency.Condition = "service_healthy";
                        }
                    }
                }
            });
        }
    }

    extension<T>(IResourceBuilder<T> resource) where T : IComputeResource
    {
        /// <summary>
        /// Publish mode only: the service comes back with the server, and reports its health to <c>docker compose ps</c>
        /// and to the dependents that wait for it.
        /// </summary>
        public IResourceBuilder<T> PublishAsHealthyComposeService(string healthCheck)
        {
            return resource.PublishAsDockerComposeService((_, service) =>
            {
                service.Restart = "unless-stopped";
                service.Healthcheck = new() { Test = ["CMD-SHELL", healthCheck], Interval = "5s", Timeout = "5s", Retries = 12, StartPeriod = "30s" };
            });
        }
    }

    //#if (redis == true)
    extension(IResourceBuilder<RedisResource> redis)
    {
        /// <summary>
        /// Aspire publishes Redis as <c>/bin/sh -c "redis-server --requirepass $REDIS_PASSWORD ..."</c> and writes every
        /// <c>WithArgs</c> value after that string, where the shell takes them as its own positional parameters and
        /// redis-server never sees them - published, redis-persistent ran with <c>appendonly no</c>. Run mode folds them
        /// in, so this does the same for the published service, and adds its restart policy and health check.
        /// </summary>
        public IResourceBuilder<RedisResource> PublishAsComposeRedis()
        {
            return redis.PublishAsHealthyComposeService("redis-cli -a \"$$REDIS_PASSWORD\" --no-auth-warning ping | grep -q PONG")
                .PublishAsDockerComposeService((_, service) =>
                {
                    if (service.Command is ["-c", ..] && service.Command.Count > 2)
                    {
                        service.Command = ["-c", string.Join(' ', service.Command.Skip(1).Select(argument => argument.Length is 0 ? "''" : argument))];
                    }
                });
        }
    }
    //#endif
}
