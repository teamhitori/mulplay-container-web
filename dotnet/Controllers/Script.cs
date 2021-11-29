using Microsoft.AspNetCore.Mvc;
using System.Collections.Generic;

// For more information on enabling Web API for empty projects, visit https://go.microsoft.com/fwlink/?LinkID=397860

namespace TeamHitori.Mulplay.Container.Web.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class Script : ControllerBase
    {
        // GET: api/<Script>
        [HttpGet]
        public string Get()
        {
            return "console.log(\"Module loaded \")";
        }

        // GET api/<Script>/5
        [HttpGet("{id}")]
        public string Get(int id)
        {
            return "value";
        }

        // POST api/<Script>
        [HttpPost]
        public void Post([FromBody] string value)
        {
        }

        // PUT api/<Script>/5
        [HttpPut("{id}")]
        public void Put(int id, [FromBody] string value)
        {
        }

        // DELETE api/<Script>/5
        [HttpDelete("{id}")]
        public void Delete(int id)
        {
        }
    }
}
