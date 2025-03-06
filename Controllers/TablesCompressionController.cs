// Controllers/TablesCompressionController.cs
using Microsoft.AspNetCore.Mvc;
using SQLCompressionReport.Interfaces;
using SQLCompressionReport.Models;

namespace SQLCompressionReport.Controllers
{
    [ApiController]
    [Route("[controller]")]
    public class TablesCompressionController : ControllerBase
    {
        private readonly ITableCompressionService _tableCompressionService;

        public TablesCompressionController(ITableCompressionService tableCompressionService)
        {
            _tableCompressionService = tableCompressionService;
        }

        [HttpPost]
        [ProducesResponseType(typeof(JsonResult), 200)]
        public IActionResult GetTablesCompression([FromBody] DatabaseConnection connection)
        {
            var tablesCompression = _tableCompressionService.GetTablesCompression(connection);
            return tablesCompression;
        }
    }
}