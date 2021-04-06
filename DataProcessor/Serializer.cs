namespace TeisterMask.DataProcessor
{
    using System;
    using System.Globalization;
    using System.IO;
    using System.Linq;
    using System.Xml.Serialization;
    using Data;
    using Newtonsoft.Json;
    using TeisterMask.DataProcessor.ExportDto;
    using Formatting = Newtonsoft.Json.Formatting;

    public class Serializer
    {
        public static string ExportProjectWithTheirTasks(TeisterMaskContext context)
        {
            var data = context.Projects.Where(x => x.Tasks.Count > 0)
                .ToList()
                .Select(x => new ProjectsOutputXmlModel
                {
                    ProjectName = x.Name,
                    TasksCount = x.Tasks.Count,
                    HasEndDate = x.DueDate == null ? "No" : "Yes",
                    Tasks = x.Tasks.Select(t => new TaskOutputXmlModel
                    {
                        Name = t.Name,
                        Label = t.LabelType.ToString()
                    })
                    .OrderBy(t=>t.Name)
                    .ToArray()
                })
                .ToArray()
                .OrderByDescending(x=>x.TasksCount)
                .ThenBy(x=>x.ProjectName)
                .ToArray();
            XmlSerializer xmlSerializer =
                new XmlSerializer(typeof(ProjectsOutputXmlModel[]),
                    new XmlRootAttribute("Projects"));
            var sw = new StringWriter();
            XmlSerializerNamespaces ns = new XmlSerializerNamespaces();
            ns.Add("", "");
            xmlSerializer.Serialize(sw, data, ns);
            return sw.ToString();
        }

        public static string ExportMostBusiestEmployees(TeisterMaskContext context, DateTime date)
        {
            
            var employees = context.Employees.ToArray().Where(x => x.EmployeesTasks.Any(ep => ep.Task.OpenDate >= date))
                .Select(x => new
                {
                    Username = x.Username,
                    Tasks = x.EmployeesTasks.Where(t => t.Task.OpenDate >= date)
                    .OrderByDescending(x=>x.Task.DueDate)
                    .ThenBy(x=>x.Task.Name)
                    .Select(ep => new
                    {
                        TaskName = ep.Task.Name,
                        OpenDate = ep.Task.OpenDate.ToString("d", CultureInfo.InvariantCulture),
                        DueDate = ep.Task.DueDate.ToString("d", CultureInfo.InvariantCulture),
                        LabelType = ep.Task.LabelType.ToString(),
                        ExecutionType = ep.Task.ExecutionType.ToString()
                    }).ToArray()
                })
                .OrderByDescending(x=>x.Tasks.Length)
                .ThenBy(x=>x.Username)
                .Take(10)
                .ToArray();

                 return JsonConvert.SerializeObject(employees, Formatting.Indented);
        }
    }
}