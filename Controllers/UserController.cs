using AppController.Models;
using AppController.Utils;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using System;

namespace AppController.Controllers
{
    [Route("api/[controller]/[action]")]
    [ApiController]
    public class UserController : ControllerBase
    {
        private readonly ILogger Logger;
        private readonly UserModel userModel;

        public UserController(IConfiguration configuration, ILogger<UserController> logger)
        {
            Logger = logger;
            userModel = new UserModel(Misc.GetDatabase(configuration));
        }

        [HttpPost]
        public object Login()
        {
            var username = Request.Form["username"].ToString();
            var password = Request.Form["password"].ToString();
            var fastResult = userModel.AuthUser(Convert.FromBase64String(Request.Cookies["SessionKey"].Stringify()));
            if (fastResult != default) return new
            {
                ret = 0,
                message = "User login automatically."
            };
            var result = userModel.AuthUser(username, password);
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
            if (!UserModel.ValidateUserName(username))
            {
                Response.StatusCode = 403;
                return new
                {
                    ret = 1,
                    message = "A username can only contain letters and numbers and the length of it should be in [4,16]."
                };
            }
            if (!UserModel.ValidatePassword(password))
            {
                Response.StatusCode = 403;
                return new
                {
                    ret = 2,
                    message = "The length of password should be in [8,64]."
                };
            }
            if (!UserModel.ValidateMailLiterally(email))
            {
                Response.StatusCode = 403;
                return new
                {
                    ret = 3,
                    message = "Invalid E-Mail address."
                };
            }
            if (userModel.Exists(username))
            {
                Response.StatusCode = 403;
                return new
                {
                    ret = 4,
                    message = "User exists."
                };
            }
            if (userModel.AddUser(username, password) != default)
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
        public object Logout() => userModel.DeauthUser(Convert.FromBase64String(Request.Cookies["SessionKey"].Stringify()))
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