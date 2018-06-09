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

namespace AppController.Controllers
{
    [Produces("application/json")]
    public class UserController : Controller
    {
        IConfiguration Configuration;
        AuthLib authLib;
        public UserController(IConfiguration configuration)
        {
            Configuration = configuration;
            authLib = new AuthLib(new MongoClient(Configuration["dbConnectionString"]).GetDatabase(Configuration["dbName"]));
        }

        [HttpPost]
        public object Login()
        {
            var username = Request.Form["username"].ToString();
            var password = Request.Form["password"].ToString();
            var result = authLib.AuthUser(username, password);
            if (!result)
            {
                Response.StatusCode = 401;
                return new
                {
                    ret = 1,
                    message = "Incorrect username or password."
                };
            }

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
                    message = "A username can only contain letters and numbers and the length of it should be in [3,16)."
                };
            }
            if (!AuthLib.ValidateMailLiterally(email))
            {
                Response.StatusCode = 403;
                return new
                {
                    ret = 2,
                    message = "Invalid E-Mail address."
                };
            }
            if (authLib.Exists(username))
            {
                Response.StatusCode = 403;
                return new
                {
                    ret = 3,
                    message = "User exists."
                };
            }
            authLib.AddUser(username, password);
            return new
            {
                ret = 0,
                message = "User created."
            };
        }
    }
}