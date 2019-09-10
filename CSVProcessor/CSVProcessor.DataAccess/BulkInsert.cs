using CSVProcessor.Domain;
using FastMember;
using System;
using System.Collections.Generic;
using System.Data.SqlClient;
using System.Linq;

namespace CSVProcessor.DataAccess
{
    public class BulkInsert<T> where T : BaseClass
    {
        /// <summary>
        /// Ejecuta el proceso de TRUNCATE y BULK de la tabla
        /// </summary>
        /// <param name="connectionString"></param>
        /// <param name="elements"></param>
        public void Execute(String connectionString, IEnumerable<T> elements)
        {
            GC.Collect();
            //TODO: Gestión de errores y log ¿Qué pasa si no hay nada en el fichero?¿Alerta?¿Todo bien?
            if (elements != null && elements.Count() > 0)
            {
                var element = elements.FirstOrDefault();
                if (element != null)
                {
                    try
                    {
                        using (var connection = new SqlConnection(connectionString))
                        {
                            connection.Open();

                            using (var transaction = connection.BeginTransaction())
                            {
                                try
                                {
                                    TruncateTable(connection, transaction, element.TableName);

                                    Transaction(connection, transaction, element.TableName, element.Parameters, elements);

                                    transaction.Commit();
                                }
                                catch (Exception)
                                {
                                    //TODO: Gestión de errores y log
                                    transaction.Rollback();
                                }
                            }
                        }
                    }
                    catch (Exception)
                    { //TODO: Gestión de errores y log 
                    }
                }
            }
            GC.Collect();
        }

        /// <summary>
        /// Limpieza previa de la tabla
        /// </summary>
        /// <param name="connection"></param>
        /// <param name="transaction"></param>
        /// <param name="tableName"></param>
        private void TruncateTable(SqlConnection connection, SqlTransaction transaction, string tableName)
        {
            using (SqlCommand cmd = new SqlCommand(String.Format("TRUNCATE TABLE {0}", tableName), connection, transaction))
            {
                cmd.ExecuteNonQuery();
            }
        }

        /// <summary>
        /// Ejecuta el BulkInsert en lotes
        /// </summary>
        /// <param name="connection"></param>
        /// <param name="transaction"></param>
        /// <param name="tableName"></param>
        /// <param name="parameters"></param>
        /// <param name="elements"></param>
        private void Transaction(SqlConnection connection, SqlTransaction transaction, string tableName, string[] parameters, IEnumerable<T> elements)
        {
            using (var sqlCopy = new SqlBulkCopy(connection, SqlBulkCopyOptions.TableLock, transaction))
            {
                sqlCopy.DestinationTableName = tableName;
                sqlCopy.BatchSize = 5000;
                using (var reader = ObjectReader.Create(elements, parameters))
                {
                    sqlCopy.WriteToServer(reader);
                }
            }
        }

    }
}
