using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TestProject.Model
{
    public class Document
    {
        /// <summary>
        /// Id контрагента
        /// </summary>
        public string CounteragentId { get; set; }

        /// <summary>
        /// Наименование контрагента
        /// </summary>
        public string CounteragentTitle { get; set; }

        /// <summary>
        /// Id подразделения
        /// </summary>
        public string DepartmentId { get; set; }

        /// <summary>
        /// Наименование подразделения
        /// </summary>
        public string DepartmentTitle { get; set; }

        /// <summary>
        /// Id сообщения
        /// </summary>
        public string MessageId { get; set; }

        /// <summary>
        /// Наименование документа
        /// </summary>
        public string Title { get; set; }

        /// <summary>
        /// Метка времени
        /// </summary>
        public DateTime Timestamp { get; set; }

        /// <summary>
        /// Дата документа
        /// </summary>
        public DateTime? DocumentDate { get; set; }

        /// <summary>
        /// Текст статуса
        /// </summary>
        public string PrimaryStatusText { get; set; }

        /// <summary>
        /// Уровень критичности статуса
        /// </summary>
        public string PrimaryStatusSeverity { get; set; }

        /// <summary>
        /// Id документа
        /// </summary>
        public string EntityId { get; set; }

        /// <summary>
        /// Id ящика
        /// </summary>
        public string BoxId { get; set; }

        /// <summary>
        /// Имя файла
        /// </summary>
        public string FileName { get; set; }

        /// <summary>
        /// Сумма документа
        /// </summary>
        public decimal? TotalSum { get; set; }
    }
}
