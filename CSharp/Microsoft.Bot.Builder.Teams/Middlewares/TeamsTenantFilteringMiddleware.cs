﻿// <copyright file="TeamsTenantFilteringMiddleware.cs" company="Microsoft">
// Licensed under the MIT License.
// </copyright>

namespace Microsoft.Bot.Builder.Teams.Middlewares
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Threading;
    using System.Threading.Tasks;
    using Microsoft.Bot.Schema.Teams;

    /// <summary>
    /// Filters request based on provided tenant list.
    /// </summary>
    /// <seealso cref="IMiddleware" />
    public class TeamsTenantFilteringMiddleware : IMiddleware
    {
        /// <summary>
        /// The tenant map. This is an optimization as dictionaries are hashmap based thus allowing quicker lookup.
        /// </summary>
        private readonly Dictionary<string, string> tenantMap = new Dictionary<string, string>();

        /// <summary>
        /// Initializes a new instance of the <see cref="TeamsTenantFilteringMiddleware"/> class.
        /// </summary>
        /// <param name="allowedTenantIds">The list of allowed tenants.</param>
        public TeamsTenantFilteringMiddleware(IEnumerable<string> allowedTenantIds)
        {
            this.tenantMap = allowedTenantIds.ToDictionary((tenantId) => tenantId, (tenantId) => tenantId);
        }

        /// <summary>
        /// When implemented in middleware, processess an incoming activity.
        /// </summary>
        /// <param name="turnContext">The context object for this turn.</param>
        /// <param name="next">The delegate to call to continue the bot middleware pipeline.</param>
        /// <param name="cancellationToken">A cancellation token that can be used by other objects
        /// or threads to receive notice of cancellation.</param>
        /// <returns>
        /// A task that represents the work queued to execute.
        /// </returns>
        /// <remarks>
        /// Middleware calls the <paramref name="next" /> delegate to pass control to
        /// the next middleware in the pipeline. If middleware doesn’t call the next delegate,
        /// the adapter does not call any of the subsequent middleware’s request handlers or the
        /// bot’s receive handler, and the pipeline short circuits.
        /// <para>The <paramref name="turnContext" /> provides information about the
        /// incoming activity, and other data needed to process the activity.</para>
        /// </remarks>
        /// <seealso cref="ITurnContext" />
        /// <seealso cref="Schema.IActivity" />
        public async Task OnTurnAsync(ITurnContext turnContext, NextDelegate next, CancellationToken cancellationToken = default(CancellationToken))
        {
            TeamsChannelData teamsChannelData = turnContext.Activity.GetChannelData<TeamsChannelData>();
            string tenantId = teamsChannelData?.Tenant?.Id;

            if (string.IsNullOrEmpty(tenantId) || !this.tenantMap.ContainsKey(tenantId))
            {
                throw new UnauthorizedAccessException("Tenant Id '" + tenantId + "' is not allowed access.");
            }

            await next(cancellationToken).ConfigureAwait(false);
        }
    }
}
