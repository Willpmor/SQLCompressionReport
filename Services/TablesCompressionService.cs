// Services/TableCompressionService.cs
using System.Collections.Generic;
using System.Data.SqlClient;
using Microsoft.AspNetCore.Mvc;
using SQLCompressionReport.Interfaces;
using SQLCompressionReport.Models;

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

            return new JsonResult(tablesCompression);
        }
    }
}