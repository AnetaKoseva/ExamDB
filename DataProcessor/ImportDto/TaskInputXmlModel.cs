using System;
using System.ComponentModel.DataAnnotations;
using System.Xml.Serialization;
using TeisterMask.Data.Models.Enums;

namespace TeisterMask.DataProcessor.ImportDto
{
    [XmlType("Task")]
    public class TaskInputXmlModel
    {
        [XmlElement("Name")]
        [Required]
        [StringLength(40,MinimumLength =2)]
        public string Name { get; set; }
        [XmlElement("OpenDate")]
        [Required]
        public string OpenDate { get; set; }
        [XmlElement("DueDate")]
        [Required]
        public string DueDate { get; set; }
        [XmlElement("ExecutionType")]
        [EnumDataType(typeof(ExecutionType))]
        [Required]
        public string ExecutionType { get; set; }
        [XmlElement("LabelType")]
        [EnumDataType(typeof(LabelType))]
        [Required]
        public string LabelType { get; set; }

    }
}