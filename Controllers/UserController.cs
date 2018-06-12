using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using MongoDB.Driver;
using MongoDB.Bson;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.AspNetCore;
using System.Text;
using AppController.Utils;
using System.Net;
using Microsoft.Extensions.Logging;

namespace AppController.Controllers
{
    [Produces("application/json")]
    public class UserController : Controller
    {
        private readonly IConfiguration Configuration;
        private readonly ILogger Logger;
        private AuthLib authLib;
        public UserController(IConfiguration configuration, ILogger<UserController> logger)
        {
            Configuration = configuration;
            Logger = logger;
            authLib = new AuthLib(new MongoClient(Configuration["dbConnectionString"]).GetDatabase(Configuration["dbName"]));
        }

        [HttpPost]
        public object Login()
        {
            var username = Request.Form["username"].ToString();
            var password = Request.Form["password"].ToString();
            var sessionKey = Request.Cookies["SessionKey"];
            var fastResult = authLib.AuthUser(Convert.FromBase64String(sessionKey));
            if (!String.IsNullOrWhiteSpace(fastResult)) return new
            {
                ret = 0,
                message = "User login automatically."
            };
            var result = authLib.AuthUser(username, password);
            if (result == default)
            {
                Response.StatusCode = 401;
                Logger.LogInformation("User {user} failed to login.", username);
                return new
                {
                    ret = 1,
                    message = "Incorrect username or password."
                };
            }
            Response.Cookies.Append("SessionKey", Convert.ToBase64String(result));
            Logger.LogInformation("User {user} logged in.", username);
            return new
            {
                ret = 0,
                message = "User login."
            };
        }

        [HttpPost]
        public object Register()
        {
            var username = Request.Form["username"].ToString();
            var password = Request.Form["password"].ToString();
            var email = Request.Form["email"].ToString();
            if (!AuthLib.ValidateUserName(username))
            {
                Response.StatusCode = 403;
                return new
                {
                    ret = 1,
                    message = "A username can only contain letters and numbers and the length of it should be in [4,16]."
                };
            }
            if (!AuthLib.ValidatePassword(password))
            {
                Response.StatusCode = 403;
                return new
                {
                    ret = 2,
                    message = "The length of password should be in [8,64]."
                };
            }
            if (!AuthLib.ValidateMailLiterally(email))
            {
                Response.StatusCode = 403;
                return new
                {
                    ret = 3,
                    message = "Invalid E-Mail address."
                };
            }
            if (authLib.Exists(username))
            {
                Response.StatusCode = 403;
                return new
                {
                    ret = 4,
                    message = "User exists."
                };
            }
            if (authLib.AddUser(username, password))
            {
                Logger.LogInformation("Created user {user}.", username);
                return new
                {
                    ret = 0,
                    message = "User created."
                };
            }
            else
            {
                Logger.LogWarning("Failed to create user {user}.", username);
                return new
                {
                    ret = 5,
                    message = $"Failed to create user {username}."
                };
            }
        }

        [HttpPost]
        public object Logout() => authLib.DeauthUser(Convert.FromBase64String(Request.Cookies["SessionKey"]))
            ? new
            {
                ret = 0,
                message = "User logout."
            }
            : new
            {
                ret = 1,
                message = "User is not login."
            };
    }
}