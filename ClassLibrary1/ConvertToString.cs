using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Navigator
{
    public class ConvertToString
    {
        public List<string> depDictoList (Dictionary<string, List<string>> stringListPairs)
        {
            List<string> result = new List<string>();
            foreach (var item in stringListPairs)
            {
                result.Add(item.Key + ":");
                foreach (var val in item.Value)
                {
                    result.Add("\t"+val);
                }
            }
            return result;
        }

    }
}
