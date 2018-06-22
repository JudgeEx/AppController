using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using AppController.Models;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using MongoDB.Driver;
using RabbitMQ.Client;

namespace AppController.Controllers
{
    [Route("api/[controller]/[action]")]
    [ApiController]
    public class ProblemController : ControllerBase
    {

        public ProblemController()
        {
        }
    }

}