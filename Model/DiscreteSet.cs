using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace XmlEditor.Model
{
    public class DiscreteSet
    {       
        public string Id { get; set; }
        public string Name { get; set; }
        public List<string> ParameterIds { get; set; }
    }
}
