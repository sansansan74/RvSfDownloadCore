using Dapper;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using RvSfDownloadCore.Domain;
using RvSfDownloadCore.Repository.DTO;
using RvSfDownloadCore.Source;
using System.Data.SqlClient;
using System.Data.SqlTypes;

namespace RvSfDownloadCore.Repository
{
    internal class DbRepairRepository :  BaseSqlRepository
    {
        private readonly int _storedProcedureTimeout;

        internal class Next4DownloadRecordDTO
        {
            internal int DiapId;
            internal DateTime DiapStart;
            internal DateTime DiapFinish;
            internal DateTime DiapInserted;
        }
                

        public DbRepairRepository(ILogger logger, IConfiguration config) : base(logger) 
        {
            var connectionString = config.GetConnectionString("SfStoreConnect");
            _storedProcedureTimeout = config.GetValue<int>("HttpSettings:StoredProcedureTimeout");
            SetConnectionString(connectionString);
        }

        /// <summary>
        /// Загрузить параментры диапазона для загрузки
        /// </summary>
        /// <returns></returns>
        public Diapason GetNextDiapason4Download(SystemSourcesEnum SourceId)
        {
            Next4DownloadRecordDTO diap = null;

            ExecSql(
                "получение следующего диапазона для загрузки load.DiapGetNext4Download",
                con =>
                {
                    diap = con.Query<Next4DownloadRecordDTO>(
                            sql: "load.DiapGetNext4Download",
                            commandType: System.Data.CommandType.StoredProcedure,
                            param: new
                            {
                                SourceId = (int)SourceId
                            })
                        .FirstOrDefault();
                }
            );

            if (diap == null)
            {
                _logger.LogTrace("Диапазон для загрузки не получен - ничего скачивать не надо");
                return null;
            }

            var tf = new Diapason
            {
                DiapId = diap.DiapId,
                SourceId = SourceId,
                DiapStart = diap.DiapStart,
                DiapFinish = diap.DiapFinish
            };

            _logger.LogTrace($"Получен следующий диапазон для загрузки DiapId=[{tf.DiapId}], DiapStart=[{tf.DiapStart}], DiapFinish=[{tf.DiapFinish}]");

            return tf;
        }

        /// <summary>
        /// Сохраняем в БД загруженный XML диапазон
        /// </summary>
        /// <param name="sfFile">СФ</param>
        public void DiapasonSave(Diapason sfFile)
        {
            // Важно - используем OLE DB вместо Dapper,
            // т.к. через Dapper не смогли передать xml-параметр
            ExecSql(
                $"Сохранение выгруженного диапазона load.DiapSetXml SourceId={sfFile.SourceId}",
                con =>
                {
                    var cmd = new SqlCommand("load.DiapSetXml", con)
                    {
                        CommandType = System.Data.CommandType.StoredProcedure,
                        CommandTimeout = _storedProcedureTimeout  // Значение возвращает хранимая процедура в параметре OUTPUT
                    };

                    con.Open();
                    SqlCommandBuilder.DeriveParameters(cmd);                        // Загружаем перечень параметров
                    cmd.Parameters["@DiapId"].Value = sfFile.DiapId;
                    cmd.Parameters["@DiapXML"].Value = new SqlXml(new MemoryStream(sfFile.DiapXml));

                    cmd.ExecuteNonQuery();                                          // Выполняем хранимую процедуру
                }
            );
        }


        /// <summary> Получение из load.ActsGetNext4Update({sourceId}) id-актов для загрузки/обновления
        /// </summary>
        /// <param name="sourceId"></param>
        /// <param name="maxCount"></param>
        /// <returns></returns>
        public IEnumerable<string> ActsGetNext4Update(SystemSourcesEnum sourceId, int maxCount)
        {
            IEnumerable<string> acts = Enumerable.Empty<string>();

            ExecSql(
                $"Получение из load.ActsGetNext4Update({sourceId}) id-актов для загрузки/обновления",
                con =>
                {
                    acts = con.Query<string>(
                        sql: "load.ActsGetNext4Update",
                        commandType: System.Data.CommandType.StoredProcedure,
                        param: new
                        {
                            SourceId = (int)sourceId,
                            MaxCount = maxCount
                        });
                });

            return acts;
        }



        /// <summary>
        /// Загрузить номер скачиваемой СФ для загрузки
        /// </summary>
        /// <returns></returns>
        public IEnumerable<EdoDescriptor> GetNextEdo4Download(int maxCount)
        {
            List<EdoDownloadTaskDTO> edoDownloadTaskDtos = null;

            ExecSql(
                "Получение из load.EdoGetNext4Download следующую СФ для загрузки с портала",
                con =>
                {
                    edoDownloadTaskDtos = con.Query<EdoDownloadTaskDTO>(
                        sql: "load.EdoGetNext4Download",
                        commandType: System.Data.CommandType.StoredProcedure,
                        param: new
                        {
                            MaxCount = maxCount
                        }).ToList();
                });


            _logger.LogTrace($"Finish получения следующих док-ов Edo для загрузки Count=[{edoDownloadTaskDtos.Count}]");

            var res = edoDownloadTaskDtos
                .Select(edo => new EdoDescriptor(edo.DocId, edo.EdoRvId.ToString(), edo.DocRvRepairId, edo.SourceId, edo.AttachTypeId, edo.Doc2EdoId, _logger))
                .ToList();

            return res;
        }


        /// <summary>
        /// Получить номер следующей печатной формы для скачивания
        /// </summary>
        /// <returns></returns>
        public IEnumerable<PrintFormDescriptor> GetNextPrintForm4Download(int maxCount)
        {
            List<PrintFormDownloadTaskDTO> downloadTaskDtos = null;

            ExecSql(
                "Получение из load.PrintFormGetNext4Download следующую печатную форму для загрузки с портала",
                con =>
                {
                    downloadTaskDtos = con.Query<PrintFormDownloadTaskDTO>(
                        sql: "load.PrintFormGetNext4Download",
                        commandType: System.Data.CommandType.StoredProcedure,
                        param: new
                        {
                            MaxCount = maxCount
                        }).ToList();
                });


            _logger.LogTrace($"Finish получения следующих печатных форм для загрузки Count=[{downloadTaskDtos.Count}]");

            var res = downloadTaskDtos
                .Select(printFormDto => new PrintFormDescriptor(printFormDto.DocId, printFormDto.AttachTypeId, printFormDto.DocRvRepairId, printFormDto.SourceId, printFormDto.DownloadUrl));

            return res;
        }

         public void EdoArchiveSaveFinish(EdoDescriptor edoFile)
        {
            ExecSql(
                $"сохр. в БД док-ов Edo load.EdoArchiveSaveFinish DocId={edoFile.DocId} & DocRvSfId={edoFile.DocRvSfId}",
                con =>
                {
                    con.Execute(
                        sql: "[load].[EdoArchiveSaveFinish]",
                        commandType: System.Data.CommandType.StoredProcedure,
                        param: new
                        {
                            edoFile.DocId,
                            EdoRvId = Convert.ToInt64(edoFile.DocRvSfId)
                        });
                });
        }


        /// <summary>
        /// Сохраняем в БД прикрепленный аттач-документ (не ЭДО, обычную печатную форму!)
        /// </summary>
        public void AttachEdit(int docId, int attachTypeId, string attachFileName, byte[] attachFileBody)
        {
            ExecSql(
                $"сохр. в БД док-ов SF load.AttachEdit DocId={docId}",
                con =>
                {
                    con.Execute(
                        sql: "load.AttachEdit",
                        commandType: System.Data.CommandType.StoredProcedure,
                        param: new
                        {
                            DocId = docId,
                            AttachTypeId = attachTypeId,
                            AttachFileName = attachFileName,  // Тело PDF-файла
                            AttachFileBody = attachFileBody
                        });
                });
        }

        /// <summary>
        /// Сохраняем в БД загруженную СФ
        /// </summary>
        /// <param name="printForm">СФ</param>
        public void SavePrintForm(PrintFormDescriptor printForm)
        {
            ExecSql(
                $"сохр. в БД печатной формы docId={printForm.DocId} & RepairId={printForm.DocRepairId} & SourceId={printForm.SourceId} & AttachTypeId={printForm.AttachTypeId}",
                con =>
                {
                    con.Execute(
                        sql: "load.PrintFormSave",
                        commandType: System.Data.CommandType.StoredProcedure,
                        param: new
                        {
                            printForm.DocId,
                            printForm.SourceId,
                            printForm.AttachTypeId,
                            DocBody = printForm.PrintFormFileBody,        // Тело файла
                            DocName = printForm.PrintFormFileName         // Имя файла
                        });
                });
        }

        public void VagonSaveError(Int32 sourceId, string docRvRepairId, string errorMessage, byte[] vagonXmlBytes)
        {
            string truncateErrorMessage = TruncateErrorMessage(errorMessage, 4096);

            ExecSql(
                $"сохранение ошибки SourceId={sourceId} & DocRvRepairId={docRvRepairId}",
                con =>
                {
                    con.Execute(
                        sql: "load.VagonSaveError",
                        commandType: System.Data.CommandType.StoredProcedure,
                        param: new
                        {
                            DocRvRepairId = docRvRepairId,       // Ид акта в БД
                            SourceId = sourceId,         // Источник ТОР ЦВ, АСУ ВРК, другие
                            ErrorMessage = truncateErrorMessage,         // Сообщение об ошибке
                            vagonXml = vagonXmlBytes            // тег ВАГОН
                        });
                });
        }


        public void EdoSaveError(EdoDescriptor sfFile, string errorMessage)
        {
            string errMessage = TruncateErrorMessage(errorMessage, 1023);

            ExecSql(
                $"сохранение ошибки SF SfId={sfFile.DocRvSfId} & RepairId={sfFile.DocRepairId} & SourceId={sfFile.SourceId}",
                con =>
                {
                    con.Execute(
                        sql: "load.DocEdoSaveError",
                        commandType: System.Data.CommandType.StoredProcedure,
                        param: new
                        {
                            EdoRvId = sfFile.DocRvSfId,       // Ид СФ в БД
                            sfFile.SourceId,         // Источник ТОР ЦВ или АСУ ВРК
                            ErrorMessage = errMessage         // Сообщение об ошибке
                        });
                });
        }

        private static string TruncateErrorMessage(string errorMessage, int maxLen)
        {
            string msg = errorMessage ?? "";
            return msg.Length > maxLen ? msg.Substring(0, maxLen) : msg;
        }

        public void PrintFormSaveError(PrintFormDescriptor printForm, string errorMessage)
        {
            string truncateErrorMessage = TruncateErrorMessage(errorMessage, 1023);

            ExecSql(
                $"сохранение ошибки печатной форты DocId={printForm.DocId} & RepairId={printForm.DocRepairId} & SourceId={printForm.SourceId}",
                con =>
                {
                    con.Execute(
                        sql: "load.DocPrintFormSaveError",
                        commandType: System.Data.CommandType.StoredProcedure,
                        param: new
                        {
                            printForm.DocId,
                            printForm.AttachTypeId,
                            ErrorMessage = truncateErrorMessage         // Сообщение об ошибке
                        });
                });
        }



        /// <summary>
        /// Сохранение данных в базы dbo.Doc и dbo.DocUpdate
        /// </summary>
        /// <param name="sourceId"></param>
        /// <param name="documentId"></param>
        /// <param name="vagonXmlBytes"></param>
        public void VagonSave(SystemSourcesEnum sourceId, string documentId, byte[] vagonXmlBytes)
        {
            ExecSql(
                $"Сохранение данных акта load.VagonSave SourceId=[{sourceId}] documentId=[{documentId}]",
                con =>
                {
                    con.Execute(
                        sql: "load.VagonSave",
                        commandType: System.Data.CommandType.StoredProcedure,
                        param: new
                        {
                            SourceId = (int)sourceId,
                            vagonXml = vagonXmlBytes,
                        });
                });
        }

        public void MarkMissingActInDownloadResult(string documentId, SystemSourcesEnum sourceId)
        {
            ExecSql(
                $"Пометить акты как скачанные load.MarkMissingActInDownloadResult SourceId=[{sourceId}] DocumentId=[{documentId}]",
                con =>
                {
                    con.Execute(
                        sql: "load.MarkMissingActInDownloadResult",
                        commandType: System.Data.CommandType.StoredProcedure,
                        param: new
                        {
                            SourceId = (int)sourceId,
                            DocRvRepairId = documentId
                        });
                });
        }


        public void EdoArchiveFileAdd(Int64 doc2EdoId, string fileName, byte[] fileContent)
        {
            ExecSql(
                $"Добавить ЭДО-архив файл load.EdoArchiveFileAdd doc2EdoId=[{doc2EdoId}], ИмяФайла=[{fileName}]",
                con =>
                {
                    con.Execute(
                        sql: "load.EdoArchiveFileAdd",
                        commandType: System.Data.CommandType.StoredProcedure,
                        param: new
                        {
                            Doc2EdoId = doc2EdoId,
                            attachFileName = fileName,
                            attachFileBody = fileContent
                        });
                });
        }

        public void EdoArchiveFileEdit(Int32 attachId, byte[] attachFileBody)
        {
            ExecSql(
                $"Изменить ЭДО-архив файл load.EdoArchiveFileEdit attachId=[{attachId}]",
                con =>
                {
                    con.Execute(
                        sql: "load.EdoArchiveFileEdit",
                        commandType: System.Data.CommandType.StoredProcedure,
                        param: new
                        {
                            attachId,
                            attachFileBody
                        });
                });
        }

        public void EdoArchiveFileRemove(Int64 doc2EdoId, Int32 attachId)
        {
            ExecSql(
                $"Удалить ЭДО-архив файл load.EdoArchareFileRemove doc2EdoId=[{doc2EdoId}], attachId=[{attachId}]",
                con =>
                {
                    con.Execute(
                        sql: "load.EdoArchiveFileRemove",
                        commandType: System.Data.CommandType.StoredProcedure,
                        param: new
                        {
                            Doc2EdoId = doc2EdoId,
                            attachId
                        });
                });
        }


        public class ArchiveFileDTO
        {
            public Int64 Doc2EdoId;
            public string AttachFileName;
            public int AttachId;
        }
        public List<ArchiveFileDTO> GetArchiveFiles(Int64 doc2EdoId)
        {
            List<ArchiveFileDTO> archiveFiles = null;

            ExecSql(
                $"Получение из load.ActsGetNext4Update({doc2EdoId}) id-актов для загрузки/обновления",
                con =>
                {
                    archiveFiles = con.Query<ArchiveFileDTO>(
                        sql: "select Doc2EdoId ,AttachFileName, AttachId from [load].vArchiveFile a where a.Doc2EdoId=@Doc2EdoId",
                        commandType: System.Data.CommandType.Text,
                        param: new
                        {
                            Doc2EdoId = doc2EdoId,
                        }).ToList();
                });

            return archiveFiles;
        }

    }

}
