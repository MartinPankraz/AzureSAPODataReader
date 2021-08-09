using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Identity.Client;
using Microsoft.Identity.Web;
using Microsoft.IdentityModel.Protocols.OpenIdConnect;

namespace AzureSAPODataReader
{
    public class InteractiveSignInRequiredExceptionFilterAttribute : ExceptionFilterAttribute
    {

        public override void OnException(ExceptionContext context)
        {
            if (context.Exception is MicrosoftIdentityWebChallengeUserException)
            {
                
                context.Result = new ChallengeResult();
                context.ExceptionHandled = true;
            }

            base.OnException(context);
        }
    }
}