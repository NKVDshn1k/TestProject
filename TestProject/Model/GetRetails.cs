using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TestProject.Model
{
    public class GetRetails
    {
        /// <summary>
        /// Guid'ы типов торговых точек
        /// </summary>
        public Guid[] RetailTypeGuids { get; set; }

        /// <summary>
        /// /// <summary>
        /// Guid'ы торговых точек
        /// </summary>
        public Guid[] RetailGuids { get; set; }

        /// <summary>
        /// Guid'ы подразделений
        /// </summary>
        public Guid[] DepartmentGuids { get; set; }

        /// <summary>
        /// Guid'ы дивизионов
        /// </summary>
        public Guid[] DivisionGuids { get; set; }

        /// <summary>
        /// Guid'ы юр лиц
        /// </summary>
        public Guid[] OrganizationGuids { get; set; }

        /// <summary>
        /// Дивизион != null
        /// </summary>
        public bool IsNotNullDivision { get; set; }

        /// <summary>
        /// Guid'ы протоколов Экспресс-расчетов
        /// </summary>
        public Guid[] ExpressCalculationProtocolGuids { get; set; }

        /// <summary>
        /// Guid's регионов, в которые входят сети
        /// </summary>
        public Guid[] DepartmentRegionGuids { get; set; }
    }
}
