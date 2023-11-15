using Microsoft.Azure.Functions.Worker.Extensions.OpenApi.Extensions;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using System.Threading.Tasks;

namespace Breezy.Muticaster
{
    public class Program
    {
        public static Task Main()
        {
            var host = new HostBuilder()
                .ConfigureFunctionsWorkerDefaults(builder =>
                {
                    builder.UseNewtonsoftJson();

                    builder.Services.AddOptions<CredentialOptions>()
                        .Configure<IConfiguration>((options, configuration) =>
                        {
                            configuration.GetSection(nameof(CredentialOptions)).Bind(options);

                            options.ScheduledEventService.Username = configuration.GetValue<string>("scheduled-event-service-user");
                            options.ScheduledEventService.Password = configuration.GetValue<string>("scheduled-event-service-password");
                            options.OnDemandMeetingService.Username = configuration.GetValue<string>("on-demand-meeting-service-user");
                            options.OnDemandMeetingService.Password = configuration.GetValue<string>("on-demand-meeting-service-password");
                            options.WaitingRoomService.Username = configuration.GetValue<string>("waiting-room-service-user");
                            options.WaitingRoomService.Password = configuration.GetValue<string>("waiting-room-service-password");
                            options.GraphServiceJson = configuration.GetValue<string>("graph-service-creds");

                        });


                    builder.Services.AddOptions<MsTeamsOptions>()
                       .Configure<IConfiguration>((options, configuration) =>
                           {
                               configuration.GetSection(nameof(MsTeamsOptions)).Bind(options);

                               options.DefaultRecorderPassword = configuration.GetValue<string>("default-recorder-password");
                               options.EveryoneTeamId = configuration.GetValue<string>("everyone-team-id");
                               options.GeneralChannelId = configuration.GetValue<string>("info-team-general-channel-id");

                           });

                    builder.Services.AddOptions<CosmosOptions>()
                        .Configure<IConfiguration>((options, configuration) =>
                            {
                                configuration.GetSection(nameof(CosmosOptions)).Bind(options);

                                options.ApplicationRegion = "West US 2";
                                options.DatabaseName = "breezy-DB";
                                options.ConnectionString = configuration.GetConnectionString("cosmosconnstr");

                            });
                })
                .ConfigureServices(builder =>
                {
                    builder.AddHttpClient();
                    builder.AddMemoryCache();
                    builder.AddSingleton<IGraphServiceClientFactory, GraphServiceClientFactory>();
                    builder.AddSingleton<ICosmosClientFactory, CosmosClientFactory>();
                    builder.AddScoped<IBreezyCosmosService, breezyCosmosService>();
                    builder.AddScoped<IGraphTeamsService, GraphTeamsService>();
                    builder.AddScoped<IGraphTeamsMembersService, GraphTeamsMembersService>();
                    builder.AddScoped<IGraphTeamsChannelsService, GraphTeamsChannelsService>();
                    builder.AddScoped<IGraphTeamsChannelsMembersService, GraphTeamsChannelsMembersService>();
                    builder.AddScoped<IGraphOnlineMeetingsService, GraphOnlineMeetingsService>();
                    builder.AddScoped<IGraphChatsService, GraphChatsService>();
                    builder.AddScoped<IGraphUsersService, GraphUsersService>();
                    builder.AddScoped<IGraphDomainsService, GraphDomainsService>();

                    builder.AddScoped<ICalendarService, CalendarService>();
                    builder.AddScoped<IRecorderService, RecorderService>();
                    builder.AddScoped<IUserService, UserService>();
                    builder.AddScoped<IAuthorisationService, AuthorisationService>();
                })
                .ConfigureOpenApi()
                .Build();

            return host.RunAsync();
        }
    }
}