namespace GxFlow.WorkflowEngine.DataModel.Core
{
    [AttributeUsage(AttributeTargets.Property, AllowMultiple = false, Inherited = false)]
    public class GraphInputAttribute : Attribute
    {
        public GraphInputAttribute(string name) => Name = name;

        public string Name { get; set; } = string.Empty;

        public string Description { get; set; } = string.Empty;

        public void CopyValue(GraphInputAttribute obj)
        {
            Description = obj.Description;
        }
    }

    [AttributeUsage(AttributeTargets.Property, AllowMultiple = false, Inherited = false)]
    public class GraphOutputAttribute : Attribute
    {
        public GraphOutputAttribute(string name) => Name = name;

        public string Name { get; set; } = string.Empty;

        public string Description { get; set; } = string.Empty;

        public void CopyValue(GraphOutputAttribute obj)
        {
            Description = obj.Description;
        }
    }
}
