using System;
using System.Collections.Concurrent;
using System.Linq;
using System.Net;
using System.Reflection;
using System.Threading.Tasks;
using DotVVM.Framework.Hosting;
using Microsoft.Owin;

namespace DotVVM.Framework.Runtime.Filters
{
    /// <summary>
    /// Specifies that the class or method requires the specified authorization.
    /// </summary>
    public class AuthorizeAttribute : ActionFilterAttribute
    {
        private static readonly ConcurrentDictionary<Type, bool> canBeAuthorizedCache = new ConcurrentDictionary<Type, bool>();

        /// <summary>
        /// Initializes a new instance of the <see cref="AuthorizeAttribute" /> class.
        /// </summary>
        public AuthorizeAttribute()
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="AuthorizeAttribute" /> class.
        /// </summary>
        /// <param name="roles">The comma-separated list of roles. The user must be at least in one of them.</param>
        public AuthorizeAttribute(string roles)
        {
            Roles = roles.Split(',').Select(s => s.Trim()).Where(s => s.Length > 0).ToArray();
        }

        /// <summary>
        /// Gets or sets the list of allowed roles.
        /// </summary>
        public string[] Roles { get; set; }

        /// <inheritdoc />
        protected override Task OnViewModelCreatedAsync(IDotvvmRequestContext context)
        {
            Authorize(context);
            return Task.FromResult(0);
        }

        /// <inheritdoc />
        protected override Task OnCommandExecutingAsync(IDotvvmRequestContext context, ActionInfo actionInfo)
        {
            Authorize(context);
            return Task.FromResult(0);
        }

        /// <summary>
        /// Called when a request is being authorized. The authorization fails if: a) no user is associated with the request;
        /// b) the user is not authenticated; c) the user is not in any of the authorized <see cref="Roles" />.
        /// </summary>
        /// <param name="context">The request context.</param>
        protected virtual void Authorize(IDotvvmRequestContext context)
        {
            if (!CanBeAuthorized(context.ViewModel))
            {
                return;
            }

            var owinContext = context.GetOwinContext();

            if (!IsUserAuthenticated(owinContext) || !IsUserAuthorized(owinContext))
            {
                HandleUnauthorizedRequest(owinContext);
            }
        }

        /// <summary>
        /// Returns whether the view model does require authorization.
        /// </summary>
        /// <param name="viewModel">The view model.</param>
        protected bool CanBeAuthorized(object viewModel)
            => viewModel == null || canBeAuthorizedCache.GetOrAdd(viewModel.GetType(), t => !t.GetTypeInfo().IsDefined(typeof(NotAuthorizedAttribute)));

        /// <summary>
        /// Handles requests that fail authorization.
        /// </summary>
        /// <param name="context">The OWIN context.</param>
        protected virtual void HandleUnauthorizedRequest(IOwinContext context)
        {
            context.Authentication.Challenge("ApplicationCookie");

            if (IsUserAuthenticated(context))
            {
                context.Response.StatusCode = (int)HttpStatusCode.Forbidden;
            }

            throw new DotvvmInterruptRequestExecutionException("User unauthorized");
        }

        /// <summary>
        /// Returns whether the current user is authenticated (and is not anonymous).
        /// </summary>
        /// <param name="context">The OWIN context.</param>
        protected virtual bool IsUserAuthenticated(IOwinContext context)
        {
            var identity = context.Authentication.User?.Identity;
            return identity != null && identity.IsAuthenticated;
        }

        /// <summary>
        /// Returns whether the current user is in on of the specified <see cref="Roles" />.
        /// </summary>
        /// <param name="context">The OWIN context.</param>
        protected virtual bool IsUserAuthorized(IOwinContext context)
        {
            var user = context.Authentication.User;

            if (user == null)
            {
                return false;
            }

            if (Roles != null && Roles.Length > 0)
            {
                return Roles.Any(r => user.IsInRole(r));
            }

            return true;
        }
    }
}