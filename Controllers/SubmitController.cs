using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;
using AppController.Models;
using AppController.Utils;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using RabbitMQ.Client;

namespace AppController.Controllers
{
    [Route("api/[controller]/[action]")]
    [ApiController]
    public class SubmitController : ControllerBase
    {
        private readonly RabbitMQ MQ;
        private readonly ILogger Logger;
        private readonly UserModel userModel;
        private readonly SubmitModel submitModel;

        public SubmitController(IConfiguration configuration, ILogger<UserController> logger)
        {
            Logger = logger;
            MQ = new RabbitMQ(configuration["MQEndpoint"]);
            userModel = new UserModel(Misc.GetDatabase(configuration));
            submitModel = new SubmitModel(Misc.GetDatabase(configuration));
        }

        [HttpPost]
        public object Create()
        {
            var userId = userModel.AuthUser(Convert.FromBase64String(Request.Cookies["SessionKey"].Stringify()));
            if (userId == default) return new
            {
                ret = 1,
                message = "You must login to submit code."
            };
            var user = userModel.GetUserById(userId);
            var problemId = Request.Form["ProblemId"].ToString();
            var rawCode = Request.Form["Code"].ToString();
            var lang = Request.Form["Lang"].ToString();
            if (String.IsNullOrWhiteSpace(problemId) || !problemId.All(c => Char.IsLetterOrDigit(c)) || String.IsNullOrWhiteSpace(rawCode)) return new
            {
                ret = 2,
                message = "Invalid ProblemId or empty code"
            };
            if (rawCode.Length > (1 << 20)) return new //1M
            {
                ret = 3,
                message = "Payload too large"
            };
            var code = WebUtility.UrlDecode(Encoding.UTF8.GetString(Convert.FromBase64String(rawCode)));
            if (code.Length > (1 << 16)) return new //64K
            {
                ret = 3,
                message = "Code should be no bigger than 64K."
            };
            var newSubmit = new Submit
            {
                SubmitId = Guid.NewGuid(),
                UserId = userId,
                ProblemId = problemId,
                Language = lang,
                SourceCode = code,
                Status = "Pending"
            };
            Logger.LogInformation($"User {user.UserName} made a new submit {newSubmit.SubmitId} of problem {newSubmit.ProblemId}.");
            submitModel.InsertSubmit(newSubmit);
            MQ.PublishMessage("PendingRequests", newSubmit.SubmitId.ToByteArray());
            submitModel.UpdateSubmitStatus(newSubmit.SubmitId, "InQueue");
            return new { newSubmit.SubmitId };
        }

        [HttpPost]
        public Submit Detail() => Guid.TryParse(Request.Form["SubmitId"].Stringify(), out var guid) ? submitModel.GetSubmit(guid) : null;

        [HttpPost]
        public object Update()
        {
            var submitId = Guid.Parse(Request.Form["SubmitId"].Stringify());
            var appkey = Request.Form["SubmitId"].Stringify();
            var newStatus = Request.Form["SubmitId"].Stringify();
            //TODO: appkey
            if (!submitModel.Exist(submitId)) return new
            {
                ret = 1,
                message = "No such submit."
            };
            submitModel.UpdateSubmitStatus(submitId, newStatus);
            var message = $"Update status of Submit {submitId} to {newStatus}";
            Logger.LogInformation(message);
            return new
            {
                ret = 0,
                message
            };
        }
    }

    internal class RabbitMQ
    {
        private readonly IConnection connection;

        public RabbitMQ(string hostName) : this(new ConnectionFactory() { HostName = hostName }) { }

        public RabbitMQ(ConnectionFactory factory) => connection = factory.CreateConnection();

        ~RabbitMQ() => connection.Dispose();


        public void PublishMessage(string queue, byte[] message)
        {
            using (var channel = connection.CreateModel())
            {
                channel.QueueDeclare(queue: queue,
                                     durable: true,
                                     exclusive: false,
                                     autoDelete: false);

                var properties = channel.CreateBasicProperties();
                properties.Persistent = true;
                channel.BasicPublish(exchange: "",
                                     routingKey: queue,
                                     basicProperties: properties,
                                     body: message);
            }
        }
    }

}