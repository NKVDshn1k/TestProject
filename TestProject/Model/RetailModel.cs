using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TestProject.Model
{
    public class RetailModel
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
        /// Краткое наименование торговой точки (аналитика)
        /// </summary>
        public string RetailShortTitle { get; set; }

        /// <summary>
        /// Guid подразделения
        /// </summary>
        public Guid DepartmentGuid { get; set; }

        /// <summary>
        /// Наименование подразделения
        /// </summary>
        public string DepartmentTitle { get; set; }

        /// <summary>
        /// Guid региона
        /// </summary>
        public Guid RegionGuid { get; set; }

        /// <summary>
        /// Наименование региона
        /// </summary>
        public string RegionTitle { get; set; }

        /// <summary>
        /// Guid сетевого региона (который держат ДР)
        /// </summary>
        public Guid? DepartamentRegionGuid { get; set; }

        /// <summary>
        /// Наименование сетевого региона (который держат ДР)
        /// </summary>
        public string DepartamentRegionTitle { get; set; }

        /// <summary>
        /// Guid города
        /// </summary>
        public Guid CityGuid { get; set; }

        /// <summary>
        /// Наименование города
        /// </summary>
        public string CityTitle { get; set; }

        /// <summary>
        /// Guid брэнда
        /// </summary>
        public Guid BrandGuid { get; set; }

        /// <summary>
        /// Наименование брэнда
        /// </summary>
        public string BrandTitle { get; set; }

        /// <summary>
        /// Guid юр. лица
        /// </summary>
        public Guid OrganizationGuid { get; set; }

        /// <summary>
        /// Наименование юр. лица
        /// </summary>
        public string OrganizationTitle { get; set; }

        /// <summary>
        /// Guid типа торговой точки
        /// </summary>
        public Guid RetailTypeGuid { get; set; }

        /// <summary>
        /// Наименование типа торговой точки
        /// </summary>
        public string RetailTypeTitle { get; set; }

        /// <summary>
        /// Гуид дивизиона
        /// </summary>
        public Guid DivisionGuid { get; set; }

        /// <summary>
        /// Имя дивизиона
        /// </summary>
        public string DivisionTitle { get; set; }

        /// <summary>
        /// Идентификатор в DAX
        /// </summary>
        public int RetailCode { get; set; }

        /// <summary>
        /// Гуид формата
        /// </summary>
        public Guid FormatGuid { get; set; }

        /// <summary>
        /// Дата открытия
        /// </summary>
        public DateTime DateOpend { get; set; }

        /// <summary>
        /// Guid статуса
        /// </summary>
        public Guid StatusGuid { get; set; }

        /// <summary>
        /// Наименование статуса
        /// </summary>
        public string StatusTitle { get; set; }

    }
}
