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
            var compressionResults = new List<Dictionary<string, string>>(); // Mover a declaração para o escopo correto
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

                foreach (var table in tablesCompression)
                {
                    if (table["CompressionType"] == "NONE")
                    {
                        string compressionQuery = $@"
                        EXEC sp_estimate_data_compression_savings 'dbo', '{table["TableName"]}', NULL, NULL, 'PAGE';";

                        using (SqlCommand compressionCmd = new SqlCommand(compressionQuery, conn))
                        {
                            using (SqlDataReader compressionReader = compressionCmd.ExecuteReader())
                            {
                                while (compressionReader.Read())
                                {
                                    var compressionResult = new Dictionary<string, string> // Renomear a variável para evitar conflito
                                    {
                                        { "TableName", table["TableName"] },
                                        { "CompressionType", table["CompressionType"] },
                                        { "CurrentSizeKB", compressionReader["size_with_current_compression_setting(KB)"].ToString() },
                                        { "RequestedSizeKB", compressionReader["size_with_requested_compression_setting(KB)"].ToString() }
                                    };
                                    compressionResults.Add(compressionResult);
                                }
                            }
                        }
                    }
                }
            }

            var groupedResults = GroupCompressionResults(compressionResults);
            var htmlContent = GenerateHtmlReport(groupedResults);
            var bytes = Encoding.UTF8.GetBytes(htmlContent);
            var result = new FileContentResult(bytes, "text/html")
            {
                FileDownloadName = "tables_compression_report.html"
            };

            return result;
        }

        private List<Dictionary<string, object>> GroupCompressionResults(List<Dictionary<string, string>> data)
        {
            var groupedResults = new List<Dictionary<string, object>>();

            foreach (var item in data)
            {
                var existingGroup = groupedResults.Find(g => g["TableName"].ToString() == item["TableName"]);
                if (existingGroup != null)
                {
                    existingGroup["CurrentSizeKB"] = (int)existingGroup["CurrentSizeKB"] + int.Parse(item["CurrentSizeKB"]);
                    existingGroup["RequestedSizeKB"] = (int)existingGroup["RequestedSizeKB"] + int.Parse(item["RequestedSizeKB"]);
                    ((List<Dictionary<string, string>>)existingGroup["Details"]).Add(item);
                }
                else
                {
                    var newGroup = new Dictionary<string, object>
                    {
                        { "TableName", item["TableName"] },
                        { "CompressionType", item["CompressionType"] },
                        { "CurrentSizeKB", int.Parse(item["CurrentSizeKB"]) },
                        { "RequestedSizeKB", int.Parse(item["RequestedSizeKB"]) },
                        { "Details", new List<Dictionary<string, string>> { item } }
                    };
                    groupedResults.Add(newGroup);
                }
            }

            return groupedResults;
        }

        private string GenerateHtmlReport(List<Dictionary<string, object>> data)
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
            sb.AppendLine(".details { display: none; }");
            sb.AppendLine(".expandable:hover { cursor: pointer; }");
            sb.AppendLine("</style>");
            sb.AppendLine("<script>");
            sb.AppendLine("function toggleDetails(id) {");
            sb.AppendLine("var element = document.getElementById(id);");
            sb.AppendLine("var icon = document.getElementById('icon-' + id);");
            sb.AppendLine("if (element.style.display === 'none') {");
            sb.AppendLine("element.style.display = 'table-row-group';");
            sb.AppendLine("icon.innerHTML = '-';");
            sb.AppendLine("} else {");
            sb.AppendLine("element.style.display = 'none';");
            sb.AppendLine("icon.innerHTML = '+';");
            sb.AppendLine("}");
            sb.AppendLine("}");
            sb.AppendLine("</script>");
            sb.AppendLine("</head>");
            sb.AppendLine("<body>");
            sb.AppendLine("<h2>Tables Compression Report</h2>");
            sb.AppendLine("<table>");
            sb.AppendLine("<tr><th></th><th>Table Name</th><th>Compression Type</th><th>Current Size (KB)</th><th>Requested Size (KB)</th></tr>");

            foreach (var item in data)
            {
                var detailsId = $"details-{item["TableName"]}";
                sb.AppendLine($"<tr class='expandable' onclick='toggleDetails(\"{detailsId}\")'>");
                sb.AppendLine($"<td id='icon-{detailsId}'>+</td>");
                sb.AppendLine($"<td>{item["TableName"]}</td>");
                sb.AppendLine($"<td>{item["CompressionType"]}</td>");
                sb.AppendLine($"<td>{item["CurrentSizeKB"]}</td>");
                sb.AppendLine($"<td>{item["RequestedSizeKB"]}</td>");
                sb.AppendLine("</tr>");
                sb.AppendLine($"<tbody id='{detailsId}' class='details'>");

                foreach (var detail in (List<Dictionary<string, string>>)item["Details"])
                {
                    sb.AppendLine("<tr>");
                    sb.AppendLine($"<td colspan='3'></td>"); // Ajustar a indentação
                    sb.AppendLine($"<td>{detail["CurrentSizeKB"]}</td>");
                    sb.AppendLine($"<td>{detail["RequestedSizeKB"]}</td>");
                    sb.AppendLine("</tr>");
                }

                sb.AppendLine("</tbody>");
            }

            sb.AppendLine("</table>");
            sb.AppendLine("</body>");
            sb.AppendLine("</html>");

            return sb.ToString();
        }
    }
}