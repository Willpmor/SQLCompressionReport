// Interfaces/ITableCompressionService.cs
using System.Collections.Generic;
using Microsoft.AspNetCore.Mvc; 
using SQLCompressionReport.Models;

namespace SQLCompressionReport.Interfaces
{
    public interface ITableCompressionService
    {
        IActionResult GetTablesCompression(DatabaseConnection connection);
    }
}