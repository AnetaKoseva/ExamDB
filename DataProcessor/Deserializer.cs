namespace TeisterMask.DataProcessor
{
    using System;
    using System.Collections.Generic;

    using System.ComponentModel.DataAnnotations;
    using System.Globalization;
    using System.IO;
    using System.Linq;
    using System.Text;
    using System.Xml.Serialization;
    using Data;
    using Newtonsoft.Json;
    using TeisterMask.Data.Models;
    using TeisterMask.Data.Models.Enums;
    using TeisterMask.DataProcessor.ImportDto;
    using ValidationContext = System.ComponentModel.DataAnnotations.ValidationContext;

    public class Deserializer
    {
        private const string ErrorMessage = "Invalid data!";

        private const string SuccessfullyImportedProject
            = "Successfully imported project - {0} with {1} tasks.";

        private const string SuccessfullyImportedEmployee
            = "Successfully imported employee - {0} with {1} tasks.";

        public static string ImportProjects(TeisterMaskContext context, string xmlString)
        {
            var sb = new StringBuilder();
            var validProjects = new List<Project>();
            var xmlSerializer = new XmlSerializer(
                typeof(ProjectInputXmlModel[]),
                new XmlRootAttribute("Projects"));
            var projects =
                (ProjectInputXmlModel[])xmlSerializer.Deserialize(
                    new StringReader(xmlString));
            foreach (var xmlProject in projects)
            {
                if (!IsValid(xmlProject))
                {
                    sb.AppendLine(ErrorMessage);
                    continue;
                }
                DateTime projectDueDate;
                var isValidDueDate = DateTime.TryParseExact(xmlProject.DueDate, "dd/MM/yyyy",
                    CultureInfo.InvariantCulture, DateTimeStyles.None, out projectDueDate);
                var projectOpenDate = DateTime.ParseExact(xmlProject.OpenDate, "dd/MM/yyyy", CultureInfo.InvariantCulture);
                var project = new Project
                {
                    Name = xmlProject.Name,
                    OpenDate = projectOpenDate,
                    DueDate = isValidDueDate ? (DateTime?)projectDueDate : null
                };
                foreach (var xmlTask in xmlProject.Tasks)
                {
                    if (!IsValid(xmlTask))
                    {
                        sb.AppendLine(ErrorMessage);
                        continue;
                    }
                    var taskOpenDate = DateTime.ParseExact(xmlTask.OpenDate, "dd/MM/yyyy", CultureInfo.InvariantCulture);
                    var taskDueDate = DateTime.ParseExact(xmlTask.DueDate, "dd/MM/yyyy", CultureInfo.InvariantCulture);
                    if (taskOpenDate <= project.OpenDate || taskDueDate >= project.DueDate)
                    {
                        sb.AppendLine(ErrorMessage);
                        continue;
                    }
                    project.Tasks.Add(new Task
                    {
                        Name = xmlTask.Name,
                        OpenDate = taskOpenDate,
                        DueDate = taskDueDate,
                        ExecutionType = Enum.Parse<ExecutionType>(xmlTask.ExecutionType),
                        LabelType = Enum.Parse<LabelType>(xmlTask.LabelType)
                    });
                }
                validProjects.Add(project);
                sb.AppendLine(string.Format(SuccessfullyImportedProject, project.Name, project.Tasks.Count));
            }
            context.Projects.AddRange(validProjects);
            context.SaveChanges();
            return sb.ToString().Trim();
        }

        public static string ImportEmployees(TeisterMaskContext context, string jsonString)
        {
            var sb = new StringBuilder();
            var validEmployees = new List<Employee>();
            var employees = JsonConvert.DeserializeObject<IEnumerable<EmployeeInputJsonModel>>(jsonString);
            foreach (var jsonEmployee in employees)
            {
                if(!IsValid(jsonEmployee))
                {
                    sb.AppendLine(ErrorMessage);
                    continue;
                }
                var employee = new Employee 
                { 
                Username=jsonEmployee.Username,
                Email=jsonEmployee.Email,
                Phone=jsonEmployee.Phone
                };
                foreach (var jsonTask in jsonEmployee.Tasks.Distinct())
                {
                    var task = context.Tasks.FirstOrDefault(x => x.Id == jsonTask);
                    if(task==null)
                    {
                        sb.AppendLine(ErrorMessage);
                        continue;
                    }
                    employee.EmployeesTasks.Add(new EmployeeTask 
                    { 
                     TaskId=jsonTask
                    });
                }
                validEmployees.Add(employee);
                sb.AppendLine(string.Format(SuccessfullyImportedEmployee, employee.Username, employee.EmployeesTasks.Count));
            }
            context.Employees.AddRange(validEmployees);
            context.SaveChanges();
            return sb.ToString().Trim();
        }

        private static bool IsValid(object dto)
        {
            var validationContext = new ValidationContext(dto);
            var validationResult = new List<ValidationResult>();

            return Validator.TryValidateObject(dto, validationContext, validationResult, true);
        }
    }
}