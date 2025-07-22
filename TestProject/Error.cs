using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TestProject
{
    public class Error
    {
        /// <summary>
        /// Код ошибки
        /// </summary>
        public int Code { get; set; }

        /// <summary>
        /// Сообщение
        /// </summary>
        public string? Message { get; set; }
    }
}
