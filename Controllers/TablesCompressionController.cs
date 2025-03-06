using Microsoft.AspNetCore.Mvc;
using SQLCompressionReport.Interfaces;
using SQLCompressionReport.Models;

namespace SQLCompressionReport.Controllers
{
    [ApiController]
    [Route("[controller]")]
    public class CompressionController : ControllerBase
    {
        private readonly ITableCompressionService _tableCompressionService;

        public CompressionController(ITableCompressionService tableCompressionService)
        {
            _tableCompressionService = tableCompressionService;
        }

        [HttpPost("start")]
        public IActionResult StartReportCompressionTask([FromBody] DatabaseConnection connection)
        {
            return _tableCompressionService.StartReportCompressionTask(connection);
        }

        [HttpGet("status/{taskId}")]
        public IActionResult GetTaskStatus(string taskId)
        {
            return _tableCompressionService.GetTaskStatus(taskId);
        }
    }
}