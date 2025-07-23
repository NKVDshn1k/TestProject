using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TestProject.Model
{
    public class ShortRetailModel
    {
        /// <summary>
        /// Guid торговой точки
        /// </summary>
        public Guid RetailGuid { get; set; }

        /// <summary>
        /// Наименование торговой точки
        /// </summary>
        public string RetailTitle { get; set; }

        /// <summary>
        /// Guid сети
        /// </summary>
        public Guid DepartmentGuid { get; set; }

        /// <summary>
        /// Название сети
        /// </summary>
        public string DepartmentTitle { get; set; }
    }
}
