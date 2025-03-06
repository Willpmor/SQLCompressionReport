// Services/TableCompressionService.cs
using System.Collections.Generic;
using System.Data.SqlClient;
using Microsoft.AspNetCore.Mvc;
using SQLCompressionReport.Interfaces;
using SQLCompressionReport.Models;
using System.Text;

namespace SQLCompressionReport.Services
{
    public class TableCompressionService : ITableCompressionService
    {
        public IActionResult GetTablesCompression(DatabaseConnection connection)
        {
            var tablesCompression = new List<Dictionary<string, string>>();
            var connectionString = $"Server={connection.Server};Database={connection.Database};User Id={connection.User};Password={connection.Password};";

            using (SqlConnection conn = new SqlConnection(connectionString))
            {
                conn.Open();
                string query = @"
                SELECT 
                    t.name AS table_name,
                    p.data_compression_desc AS compression_type
                FROM 
                    sys.tables t
                JOIN 
                    sys.partitions p ON t.object_id = p.object_id
                WHERE 
                    p.index_id IN (0, 1) -- 0: Heap, 1: Clustered Index
                GROUP BY 
                    t.name, p.data_compression_desc;";

                using (SqlCommand cmd = new SqlCommand(query, conn))
                {
                    using (SqlDataReader reader = cmd.ExecuteReader())
                    {
                        while (reader.Read())
                        {
                            var tableCompression = new Dictionary<string, string>
                            {
                                { "TableName", reader["table_name"].ToString() },
                                { "CompressionType", reader["compression_type"].ToString() }
                            };
                            tablesCompression.Add(tableCompression);
                        }
                    }
                }
            }

            var htmlContent = GenerateHtmlReport(tablesCompression);
            var bytes = Encoding.UTF8.GetBytes(htmlContent);
            var result = new FileContentResult(bytes, "text/html")
            {
                FileDownloadName = "tables_compression_report.html"
            };

            return result;
        }

        private string GenerateHtmlReport(List<Dictionary<string, string>> data)
        {
            var sb = new StringBuilder();
            sb.AppendLine("<!DOCTYPE html>");
            sb.AppendLine("<html>");
            sb.AppendLine("<head>");
            sb.AppendLine("<title>Tables Compression Report</title>");
            sb.AppendLine("<style>");
            sb.AppendLine("table { width: 100%; border-collapse: collapse; }");
            sb.AppendLine("table, th, td { border: 1px solid black; }");
            sb.AppendLine("th, td { padding: 8px; text-align: left; }");
            sb.AppendLine("th { background-color: #f2f2f2; }");
            sb.AppendLine("</style>");
            sb.AppendLine("</head>");
            sb.AppendLine("<body>");
            sb.AppendLine("<h2>Tables Compression Report</h2>");
            sb.AppendLine("<table>");
            sb.AppendLine("<tr><th>Table Name</th><th>Compression Type</th></tr>");

            foreach (var item in data)
            {
                sb.AppendLine("<tr>");
                sb.AppendLine($"<td>{item["TableName"]}</td>");
                sb.AppendLine($"<td>{item["CompressionType"]}</td>");
                sb.AppendLine("</tr>");
            }

            sb.AppendLine("</table>");
            sb.AppendLine("</body>");
            sb.AppendLine("</html>");

            return sb.ToString();
        }
    }
}