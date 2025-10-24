using GxFlow.WorkflowEngine.DataModel.Core;
using GxFlow.WorkflowEngine.DataModel.Script;
using System.Xml.Serialization;

namespace GxFlow.WorkflowEngine.DataModel.Core
{
    public interface IGraphProperty
    {
        string BindPath { get; set; }
        Task EvalValue(GraphVariable globalVar, CancellationToken token);

        Type ValueType { get; }

        object GetValue();
    }

    public class GraphProperty<T> : IGraphProperty
    {
        public GraphProperty()
        {

        }

        public GraphProperty(T value)
        {
            Value = value;
        }

        [XmlElement("value")]
        public T Value { get; set; } = default;

        [XmlElement("bindpath")]
        public string BindPath { get; set; } = string.Empty;

        public Type ValueType { get => typeof(T); }

        public async Task EvalValue(GraphVariable vars, CancellationToken token)
        {
            if (string.IsNullOrEmpty(BindPath))
            {
                return;
            }
            else
            {
                Value = await CSharpHelper.Eval<T>(BindPath, vars, token);
            }
        }

        public object GetValue()
        {
#pragma warning disable CS8603 // Possible null reference return.
            return Value;
#pragma warning restore CS8603 // Possible null reference return.
        }
    }
}
