﻿// <copyright file="WebApiConfig.cs" company="Microsoft">
// Licensed under the MIT License.
// </copyright>

namespace Microsoft.Bot.Builder.Teams.AuditBot.AspNet
{
    using System.Linq;
    using System.Reflection;
    using System.Web.Hosting;
    using System.Web.Http;
    using Autofac;
    using Autofac.Integration.WebApi;
    using Microsoft.Bot.Builder.Abstractions;
    using Microsoft.Bot.Builder.Abstractions.Teams;
    using Microsoft.Bot.Builder.Integration.AspNet.WebApi;
    using Microsoft.Bot.Builder.Teams.Middlewares;
    using Microsoft.Bot.Builder.Teams.StateStorage;
    using Microsoft.Bot.Configuration;
    using Microsoft.Bot.Connector.Authentication;

    /// <summary>
    /// Configure the application.
    /// </summary>
    public static class WebApiConfig
    {
        /// <summary>
        /// Registers the specified configuration.
        /// </summary>
        /// <param name="config">The configuration.</param>
        public static void Register(HttpConfiguration config)
        {
            var builder = new ContainerBuilder();

            // Register your Web API controllers.
            builder.RegisterApiControllers(Assembly.GetExecutingAssembly());

            config.MapBotFramework(botConfig =>
            {
                // Load Connected Services from .bot file
                string path = HostingEnvironment.MapPath(@"~/BotConfiguration.bot");
                var botConfigurationFile = BotConfiguration.Load(path);
                var endpointService = (EndpointService)botConfigurationFile.Services.First(s => s.Type == "endpoint");

                botConfig
                    .UseMicrosoftApplicationIdentity(endpointService?.AppId, endpointService?.AppPassword);

                // The Memory Storage used here is for local bot debugging only. When the bot
                // is restarted, everything stored in memory will be gone.
                IStorage dataStore = new MemoryStorage();

                // Create Conversation State object.
                // The Conversation State object is where we persist anything at the conversation-scope.
                TeamSpecificConversationState conversationState = new TeamSpecificConversationState(dataStore);
                botConfig.BotFrameworkOptions.State.Add(conversationState);

                // Drop all activites not received from Microsoft Teams channel.
                botConfig.BotFrameworkOptions.Middleware.Add(new DropNonTeamsActivitiesMiddleware());

                // --> Add Teams Middleware.
                botConfig.BotFrameworkOptions.Middleware.Add(
                    new TeamsMiddleware(
                        new SimpleCredentialProvider(endpointService?.AppId, endpointService?.AppPassword)));

                // Automatically drop all non Team messages.
                botConfig.BotFrameworkOptions.Middleware.Add(new DropChatActivitiesMiddleware());

                // Create the custom state accessor.
                // State accessors enable other components to read and write individual properties of state.
                var accessors = new AuditLogAccessor(conversationState)
                {
                    AuditLog = conversationState.CreateProperty<TeamOperationHistory>(AuditLogAccessor.AuditLogName),
                };

                builder.Register<AuditLogAccessor>((component) => accessors);
            });

            builder.RegisterType<TeamsActivityProcessor>().As<IActivityProcessor>();
            builder.RegisterType<TeamsConversationUpdateActivityHandler>().As<ITeamsConversationUpdateActivityHandler>();
            builder.RegisterType<MessageActivityHandler>().As<IMessageActivityHandler>();

            builder.RegisterType<AuditBot>().As<IBot>().InstancePerRequest();

            // Set the dependency resolver to be Autofac.
            IContainer container = builder.Build();

            config.DependencyResolver = new AutofacWebApiDependencyResolver(container);
        }
    }
}
