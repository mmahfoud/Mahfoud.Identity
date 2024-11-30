using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.OpenApi.Attributes;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations;

namespace Mahfoud.Identity
{
    [Route("api/[controller]")]
    [ApiController]
    public class ValuesController : ControllerBase
    {
        [
            HttpGet, DisplayName("Mahfoud Values From Controller [controller]"), EndpointSummary("A great summary"),
            Description("Also great description!"), ActionName("Walaa"),
            Tags("X", "Y")
        ]
        public IActionResult Get()
        {
            return Ok("Hello World!");
        }

        [HttpGet, Route("again"), DisplayName("What the hell")]
        public IActionResult GetAgain()
        {
            return Ok("Hello World again!");
        }
    }
}
